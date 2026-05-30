using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public partial class MapGeneratorPresets : Node3D
{
    public class MapLinkOffsetData
    {
        public Vector3I positionOffset = Vector3I.Zero;
        public Vector3I rotationPivot = Vector3I.Zero;
        public int rotationOffsetFinal = 0;
        public int rotationOffset = 0;
        public int rotationOffsetParent = 0;
    }

    [Export]
    public PresetRoom[] rooms;
    [Export]
    public PresetRoom[] interestRooms;
    [Export]
    public Node target;
    [Export]
    public PresetRoom[] doors;
    [Export]
    public GameData gameData;
    [Export]
    public float mapSize = 20.8f;
    [Export]
    public string[] allMaps;
    [Export]
    public string currentMap;

    public List<string> usedMaps = new List<string>();

    public readonly Vector3I[] directions = new Vector3I[]
    {
        Vector3I.Forward,
        Vector3I.Right,
        Vector3I.Back,
        Vector3I.Left,
    };

    public readonly ERoomDirectionFlags[] roomDirectionFlags = new ERoomDirectionFlags[]
    {
        ERoomDirectionFlags.NegativeZ,
        ERoomDirectionFlags.PositiveX,
        ERoomDirectionFlags.PositiveZ,
        ERoomDirectionFlags.NegativeX,
    };

    public readonly Dictionary<PresetRoom.RoomClass, ERoomDirectionFlags> lookup = new Dictionary<PresetRoom.RoomClass, ERoomDirectionFlags>()
    {
        { PresetRoom.RoomClass.Endoff, ERoomDirectionFlags.PositiveZ },
        { PresetRoom.RoomClass.Corner, ERoomDirectionFlags.PositiveZ | ERoomDirectionFlags.PositiveX },
        { PresetRoom.RoomClass.Hall, ERoomDirectionFlags.PositiveZ | ERoomDirectionFlags.NegativeZ },
        { PresetRoom.RoomClass.TRoom, ERoomDirectionFlags.PositiveZ | ERoomDirectionFlags.PositiveX | ERoomDirectionFlags.NegativeX },
        { PresetRoom.RoomClass.XRoom, ERoomDirectionFlags.AllDirections },
        { PresetRoom.RoomClass.Other, 0 },
    };

    public List<Node> allSpawned = new List<Node>();
    public List<PresetRoom> allSpawnedPresets = new List<PresetRoom>();
    public List<MapRoom> allRooms = new List<MapRoom>();
    public List<MapRoom> spawnedRooms = new List<MapRoom>();

    private List<PackedScene> validClusters = new List<PackedScene>();
    private List<PresetRoom> validRooms = new List<PresetRoom>();
    private List<PresetRoom> validInterestRooms = new List<PresetRoom>();
    private RandomNumberGenerator rng;
    private int clusterCount = 0;
    public Func<PackedScene, Vector3, Vector3, Godot.Collections.Dictionary, Node> customSpawn = null;

    public override void _Ready()
    {
        if (IsInstanceValid(gameData))
        {
            rooms = gameData.Rooms.Where(x => x.isGeneric).ToArray();
            interestRooms = gameData.Rooms.Where(x => !x.isGeneric).ToArray();
            // GenerateFile(GD.Randi(), gameData.MapFiles[Name][GD.Randi() % gameData.MapFiles[Name].Length]);
        }
    }

    public bool GenerateFile(ulong seed, string mapFile, string graphFile = "")
    {
        Log.PrintInfo(mapFile);
        currentMap = mapFile;
        usedMaps.Clear();
        usedMaps.Add(mapFile);
        return Generate(seed, JsonSerializer.Deserialize<MapLayout>(FileAccess.GetFileAsString(mapFile)), new MapLinkOffsetData(), true);
    }

    public void Init(ulong seed)
    {
        rng = new RandomNumberGenerator();
        rng.Seed = seed;
        clusterCount = 0;
        allSpawned.Clear();
        allSpawnedPresets.Clear();
        validClusters.Clear();
        validRooms.Clear();
        validRooms.AddRange(rooms);
        validInterestRooms.Clear();
        validInterestRooms.AddRange(interestRooms);
        allRooms.Clear();
        spawnedRooms.Clear();
    }

    public (MapLayout map, int layerIndex, int roomIndex) FindMapLink(string linkName)
    {
        foreach (var mapFile in allMaps)
        {
            if (mapFile == currentMap || usedMaps.Contains(mapFile))
                continue;
            MapLayout map = JsonSerializer.Deserialize<MapLayout>(FileAccess.GetFileAsString(mapFile));
            for (int y = 0; y < map.layers.Count; y++)
            {
                for (int i = 0; i < map.layers[y].layout.Count; i++)
                {
                    if (map.layers[y].layout[i].keyvalues.TryGetValue(RoomMapLink.MapLinkName, out object value))
                    {
                        if (linkName == value?.ToString())
                        {
                            usedMaps.Add(mapFile);
                            return (map, y, i);
                        }
                    }
                }
            }
        }
        return (null, 0, 0);
    }

    public void TransformMapLayout(MapLinkOffsetData offsetData, MapLayout map)
    {
        for (int y = 0; y < map.layers.Count; y++)
        {
            for (int i = 0; i < map.layers[y].layout.Count; i++)
            {
                // map.layers[y].layout[i].x += offsetData.positionOffset.X;
                // map.layers[y].layout[i].z += offsetData.positionOffset.Z;
                Vector2I pivot = new Vector2I(offsetData.rotationPivot.X, offsetData.rotationPivot.Z);
                Vector2I offset = new Vector2I(offsetData.positionOffset.X, offsetData.positionOffset.Z);
                Vector2I pos = new Vector2I(map.layers[y].layout[i].x, map.layers[y].layout[i].z);
                pos -= pivot;
                pos = TransformLayout(pos, (offsetData.rotationOffsetFinal) % 4);
                // pos += TransformLayoutPivot(pivot, (offsetData.rotationOffsetFinal) % 4);
                pos += offset;
                map.layers[y].layout[i].x = pos.X;
                map.layers[y].layout[i].z = pos.Y;
                map.layers[y].layout[i].rotation = (map.layers[y].layout[i].rotation + offsetData.rotationOffsetFinal) % 4;
            }
        }
    }

    public Vector2I TransformLayout(Vector2I worldPos, int rotationOffset)
    {
        switch (rotationOffset)
        {
            case 0:
                break;
            case 3:
                worldPos = new Vector2I(worldPos.Y, -worldPos.X);
                break;
            case 2:
                worldPos = new Vector2I(-worldPos.X, -worldPos.Y);
                break;
            case 1:
                worldPos = new Vector2I(-worldPos.Y, worldPos.X);
                break;
        }
        return worldPos;
    }

    public Vector2I TransformLayoutPivot(Vector2I worldPos, int rotationOffset)
    {
        switch (rotationOffset)
        {
            case 0:
                break;
            case 1:
                worldPos = new Vector2I(worldPos.Y, -worldPos.X);
                break;
            case 2:
                worldPos = new Vector2I(-worldPos.X, -worldPos.Y);
                break;
            case 3:
                worldPos = new Vector2I(-worldPos.Y, worldPos.X);
                break;
        }
        return worldPos;
    }

    public bool Generate(ulong seed, MapLayout mapFile, MapLinkOffsetData offsetData, bool reset)
    {
        bool flag = true;
        if (reset)
        {
            Log.PrintInfo($"seed={seed}");
            // setup
            rng = new RandomNumberGenerator();
            rng.Seed = seed;
            clusterCount = 0;
            allSpawned.Clear();
            allSpawnedPresets.Clear();
            validClusters.Clear();
            validRooms.Clear();
            validRooms.AddRange(rooms);
            validInterestRooms.Clear();
            validInterestRooms.AddRange(interestRooms);
            // collect all rooms
            allRooms.Clear();
            // /*
            while (spawnedRooms.Count > 0)
            {
                spawnedRooms[0].QueueFree();
                spawnedRooms.RemoveAt(0);
            }
            // */
            spawnedRooms.Clear();
        }
        else
        {
            TransformMapLayout(offsetData, mapFile);
        }
        for (int y = 0; y < mapFile.layers.Count; y++)
        {
            MapLayoutLayer layer = mapFile.layers[y];
            for (int i = 0; i < layer.layout.Count; i++)
            {
                PresetRoom room = FindRoomByName(layer.layout[i].name);
                if (!IsInstanceValid(room))
                {
                    flag = false;
                    continue;
                }
                // if (!reset && layer.layout[i].keyvalues.ContainsKey(RoomMapLink.MapLinkName))
                // {
                //     continue;
                // }
                int yAdjusted = y + offsetData.positionOffset.Y - offsetData.rotationPivot.Y;
                MapRoom node = new MapRoom();
                node.SetupFromLayout(layer.layout[i], room, new Vector3I(layer.layout[i].x, yAdjusted, layer.layout[i].z), layer.layout[i].rotation);
                node.Position = new Vector3(layer.layout[i].x, yAdjusted, layer.layout[i].z) * mapSize;
                node.RotationDegrees = Vector3.Up * -90f * layer.layout[i].rotation;
                AddChild(node);
                allRooms.Add(node);
            }
        }
        if (!reset)
        {
            return true;
        }
        // find and replace map links
        List<MapLayoutRoom> usedRooms = new List<MapLayoutRoom>();
        for (int i = 0; i < allRooms.Count; i++)
        {
            if (!usedRooms.Contains(allRooms[i].layoutRoom) && allRooms[i].layoutRoom.keyvalues.TryGetValue(RoomMapLink.MapLinkName, out object value))
            {
                Log.PrintInfo($"Searching for Map Link: {value?.ToString()}");
                var mapLinkData = FindMapLink(value?.ToString());
                if (mapLinkData.map == null)
                {
                    Log.PrintErr($"Map Link not found: {value?.ToString()}");
                    continue;
                }
                int min = Mathf.Min(mapLinkData.map.layers[mapLinkData.layerIndex].layout[mapLinkData.roomIndex].rotation, allRooms[i].layoutRotation);
                int max = Mathf.Max(mapLinkData.map.layers[mapLinkData.layerIndex].layout[mapLinkData.roomIndex].rotation, allRooms[i].layoutRotation);
                MapLinkOffsetData newOffsets = new MapLinkOffsetData()
                {
                    positionOffset = allRooms[i].layoutPosition,
                    rotationPivot = new Vector3I(mapLinkData.map.layers[mapLinkData.layerIndex].layout[mapLinkData.roomIndex].x, mapLinkData.layerIndex, mapLinkData.map.layers[mapLinkData.layerIndex].layout[mapLinkData.roomIndex].z),
                    rotationOffset = (mapLinkData.map.layers[mapLinkData.layerIndex].layout[mapLinkData.roomIndex].rotation) % 4,
                    rotationOffsetParent = (allRooms[i].layoutRotation) % 4,
                    rotationOffsetFinal = (max - min) % 4,
                };
                while (newOffsets.rotationOffsetParent != (newOffsets.rotationOffset + newOffsets.rotationOffsetFinal) % 4)
                {
                    newOffsets.rotationOffsetFinal = (newOffsets.rotationOffsetFinal + 1) % 4;
                }
                newOffsets.rotationOffsetFinal += 2;
                // Log.PrintInfo($"currentRoomRotation={allRooms[i].layoutRotation}, otherRoomRotation={mapLinkData.map.layers[mapLinkData.layerIndex].layout[mapLinkData.roomIndex].rotation}, rotationOffsetFinal={newOffsets.rotationOffsetFinal}");
                // Log.PrintInfo($"positionOffset={newOffsets.positionOffset}, rotationPivot={newOffsets.rotationPivot}");
                newOffsets.positionOffset -= directions[(allRooms[i].layoutRotation) % 4];
                usedRooms.Add(allRooms[i].layoutRoom);
                usedRooms.Add(mapLinkData.map.layers[mapLinkData.layerIndex].layout[mapLinkData.roomIndex]);
                Generate(seed, mapLinkData.map, newOffsets, false);
            }
        }
        // spawn all pre-placed rooms
        for (int i = 0; i < allRooms.Count; i++)
        {
            if (allRooms[i].allowedRooms.Length != 0)
            {
                // GD.PrintS(string.Join(',', allRooms[i].allowedRooms));
                PresetRoom preset = GetValidRoom(allRooms[i]);
                allSpawned.Add(SpawnPresetRoom(preset, allRooms[i].GlobalPosition, allRooms[i].GlobalRotation, allRooms[i].layoutRoom));
                allSpawnedPresets.Add(preset);
                validInterestRooms.Remove(preset);
                spawnedRooms.Add(allRooms[i]);
                allRooms[i].room = preset;
                // RemoveOverlappingRooms(allRooms[i].layoutPosition);
                allRooms.RemoveAt(i);
                i--;
            }
            else if (IsInstanceValid(allRooms[i].room) && !allRooms[i].room.isGeneric)
            {
                PresetRoom preset = allRooms[i].room;
                allSpawned.Add(SpawnPresetRoom(preset, allRooms[i].GlobalPosition, allRooms[i].GlobalRotation, allRooms[i].layoutRoom));
                allSpawnedPresets.Add(preset);
                validInterestRooms.Remove(preset);
                spawnedRooms.Add(allRooms[i]);
                allRooms[i].room = preset;
                // RemoveOverlappingRooms(allRooms[i].layoutPosition);
                allRooms.RemoveAt(i);
                i--;
            }
        }
        // finally loop and spawn remaining rooms
        int lastCount = allRooms.Count;
        int sameCount = 0;
        while (allRooms.Count > 0)
        {
            int i = (int)(rng.Randi() % allRooms.Count);
            PresetRoom preset = GetValidRoom(allRooms[i]);
            Rect2I rect = allRooms[i].GetRect(mapSize, preset);
            if (CheckForOverlaps(allRooms[i].layoutPosition, allRooms[i].type, rect, allRooms[i].layoutRotation) && allRooms.Count != lastCount && sameCount < 100)
            {
                // GD.PrintErr($"Womp womp 1 {preset.ResourcePath}");
                sameCount++;
                continue;
            }
            if (sameCount >= 100)
            {
                // GD.PrintErr($"Womp womp 2 {preset.ResourcePath}");
            }
            var room = allRooms[i];
            validInterestRooms.Remove(preset);
            allSpawned.Add(SpawnPresetRoom(preset, room.GlobalPosition, room.GlobalRotation, room.layoutRoom));
            allSpawnedPresets.Add(preset);
            spawnedRooms.Add(room);
            room.room = preset;
            allRooms.RemoveAt(i);
            // GD.PrintS(preset.ResourceName, rect);
            // RemoveOverlappingRooms(room.layoutPosition, rect);
            lastCount = allRooms.Count;
            sameCount = 0;
        }
        foreach (var r in spawnedRooms)
        {
            r._Ready();
        }
        // while (spawnedRooms.Count > 0)
        {
            // spawnedRooms[0].QueueFree();
            // spawnedRooms.RemoveAt(0);
        }
        // GenerateDoors();
        if (validInterestRooms.Count != 0)
        {
            Log.PrintS(string.Join(", ", validInterestRooms.Select(x => x.ResourceName)));
        }
        return validInterestRooms.Count == 0 && flag;
    }

    private bool CheckForOverlaps(Vector3I position, PresetRoom.RoomClass type, Rect2I rect, int rotation)
    {
        for (int i = 0; i < allRooms.Count; i++)
        {
            if (allRooms[i].layoutPosition.Y == position.Y && rect.HasPoint(allRooms[i].layoutPosition2d))
            {
                return true;
            }
        }
        return false;
        for (int i = 0; i < spawnedRooms.Count; i++)
        {
            if (spawnedRooms[i].layoutPosition.Y == position.Y && IsInstanceValid(spawnedRooms[i].room) && spawnedRooms[i].room.type != type && spawnedRooms[i].GetRect(mapSize, null).Intersects(rect) && !DoorsMatch(spawnedRooms[i], type, rotation))
            {
                return true;
            }
        }
        return false;
    }

    private bool CanMerge(MapRoom room, Rect2I rect)
    {
        if (Mathf.Abs(rect.Area) == 1)
        {
            return true;
        }
        for (int i = 0; i < spawnedRooms.Count; i++)
        {
            if (spawnedRooms[i].layoutPosition.Y == room.layoutPosition.Y && rect.HasPoint(spawnedRooms[i].layoutPosition2d))
            {
                return false;
            }
        }
        bool foundSelf = false;
        int count = 0;
        for (int i = 0; i < allRooms.Count; i++)
        {
            if (allRooms[i] == room)
            {
                foundSelf = true;
                continue;
                // return true;
            }
            if (allRooms[i].layoutPosition.Y == room.layoutPosition.Y && rect.HasPoint(allRooms[i].layoutPosition2d))
            {
                if (allRooms[i].type == room.type && DoorsMatch(allRooms[i], room.type, room.layoutRotation))
                {
                    count++;
                    return true;
                }
                // return false;
            }
        }
        // return foundSelf;
        return false;
    }

    private void RemoveOverlappingRooms(Vector3I position, Rect2I rect)
    {
        for (int i = 0; i < allRooms.Count; i++)
        {
            // if (allRooms[i].layoutPosition == position)
            //     continue;
            if (allRooms[i].layoutPosition.Y == position.Y && rect.HasPoint(allRooms[i].layoutPosition2d))
            {
                // DebugDrawManager.Instance.DrawDebugBox(ToGlobal((Vector3)position * mapSize), Vector3.One * mapSize / 2f, Vector3.Zero, Colors.Red, 10);
                // DebugDrawManager.Instance.DrawDebugBox(allRooms[i].GlobalPosition, Vector3.One * mapSize / 2f, Vector3.Zero, Colors.White, 10);
                spawnedRooms.Add(allRooms[i]);
                allRooms[i].room = null;
                allRooms.RemoveAt(i);
                i--;
                continue;
            }
        }
        /*
        for (int i = 0; i < spawnedRooms.Count; i++)
        {
            if (spawnedRooms[i].layoutPosition.Y == position.Y && rect.HasPoint(spawnedRooms[i].layoutPosition2d))
            {
                spawnedRooms.RemoveAt(i);
                i--;
                continue;
            }
        }
        */
    }

    private List<Vector3> GetDoorPoints(MapRoom room)
    {
        List<Vector3> doors = new List<Vector3>();
        ERoomDirectionFlags flags = lookup[room.type];
        for (int i = 0; i < room.layoutRotation; i++)
        {
            flags = RoomDirectionFlagsUtility.RotateRight(flags);
        }
        for (int i = 0; i < 4; i++)
        {
            if (flags.HasFlag(roomDirectionFlags[i]))
            {
                Vector3 dir = (Vector3)directions[i] * (mapSize / 2f);
                doors.Add(room.GlobalPosition + dir);
            }
        }
        return doors;
    }

    private List<Vector3> GetDoorPoints2(MapRoom room, PresetRoom preset)
    {
        List<Vector3> doors = new List<Vector3>();
        for (int x = 0; x < Mathf.Abs(preset.gridRect.Size.X); x++)
        {
            for (int y = 0; y < Mathf.Abs(preset.gridRect.Size.Y); y++)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector3 dir = (Vector3)directions[i] * (mapSize / 2f);
                    doors.Add(room.GlobalPosition + new Vector3(x, 0, y) + dir);
                }
            }
        }
        return doors;
    }

    private bool DoorPointsMatch(MapRoom a, PresetRoom room, List<Vector3> bList)
    {
        List<Vector3> aList = GetDoorPoints(a);
        List<Vector3> aList2 = GetDoorPoints2(a, room);
        // return true;
        int count = 0;
        foreach (var aPoint in aList)
        {
            foreach (var bPoint in bList)
            {
                if (aPoint.DistanceTo(bPoint) < 0.5f)
                {
                    count++;
                    break;
                }
            }
        }
        int count2 = 0;
        foreach (var aPoint in aList2)
        {
            foreach (var bPoint in bList)
            {
                if (aPoint.DistanceTo(bPoint) < 0.5f)
                {
                    count2++;
                    break;
                }
            }
        }
        return aList.Count == count;
    }

    private bool DoorsMatch(MapRoom room, PresetRoom.RoomClass b, int bRot)
    {
        ERoomDirectionFlags flags = lookup[room.type];
        for (int i = 0; i < room.layoutRotation; i++)
        {
            flags = RoomDirectionFlagsUtility.RotateRight(flags);
        }
        ERoomDirectionFlags flagsB = lookup[b];
        for (int i = 0; i < bRot; i++)
        {
            flagsB = RoomDirectionFlagsUtility.RotateRight(flagsB);
        }
        return (flags.HasFlag(ERoomDirectionFlags.PositiveZ) && flagsB.HasFlag(ERoomDirectionFlags.NegativeZ));
    }

    private float DistanceToTargets(MapGraph.MapGraphNode node, Vector3 position)
    {
        float? dist = null;
        for (int i = 0; i < allSpawned.Count; i++)
        {
            Node3D spawned = allSpawned[i] as Node3D;
            string preset = allSpawnedPresets[i].ResourceName;
            if (IsInstanceValid(spawned))
            {
                if (node.connections.IndexOf(preset) != -1)
                {
                    // GD.PrintS(node.name, preset);
                    float d = spawned.GlobalPosition.DistanceTo(position);
                    if (!dist.HasValue || dist.Value > d)
                    // if (dist.HasValue)
                        // dist += d;
                    // else
                        dist = d;
                }
            }
        }
        for (int i = 0; i < allRooms.Count; i++)
        {
            if (IsInstanceValid(allRooms[i].room))
            {
                string preset = allRooms[i].room.ResourceName;
                if (node.connections.IndexOf(preset) != -1)
                {
                    // GD.PrintS(node.name, preset);
                    float d = allRooms[i].GlobalPosition.DistanceTo(position);
                    if (!dist.HasValue || dist.Value > d)
                    // if (dist.HasValue)
                        // dist += d;
                    // else
                        dist = d;
                }
            }
        }
        return dist.GetValueOrDefault(rng.Randf() * 1000f);
    }

    public void GenerateDoors()
    {
        var mapDoors = GetTree().GetNodesInGroup("map_door").Where(x => IsInstanceValid(x) && x is Node3D n && n.Visible && allSpawned.Any(y => y.IsAncestorOf(x))).ToArray();
        List<Vector3> used = new List<Vector3>();
        foreach (Node3D door in mapDoors)
        {
            int doorType = door.GetMeta("door_type", 0).AsInt32();
            bool optional = door.GetMeta("optional", false).AsBool();
            bool overlapsExisting = false;
            List<Node3D> otherDoors = new List<Node3D>();
            foreach (Node3D otherDoor in mapDoors)
            {
                if (otherDoor == door)
                    continue;
                bool overlapFound = otherDoor.GlobalPosition.DistanceTo(door.GlobalPosition) < 0.5f;
                if (overlapFound && otherDoor.GetMeta("used", false).AsBool())
                {
                    overlapsExisting = true;
                    // break;
                }
                if (overlapFound && door is Marker3D m1 && otherDoor is Marker3D m2)
                {
                    if (m1.GizmoExtents < m2.GizmoExtents)
                    {
                        doorType = otherDoor.GetMeta("door_type", 0).AsInt32();
                    }
                }
                if (overlapFound)
                {
                    otherDoors.Add(otherDoor);
                }
            }
            // GD.PrintS(door.GlobalPosition, overlapsExisting, optional);
            if (!optional)
            {
                bool shouldSpawn = door is not MapDoor mapDoor1 || mapDoor1.ShouldSpawn;
                if (!overlapsExisting && shouldSpawn && otherDoors.Any(x => x is not MapDoor door1 || door1.ShouldSpawn))
                {
                    var newDoor = new MapRoom();
                    newDoor.room = doors[doorType];
                    newDoor.Position = ToLocal(door.GlobalPosition);
                    newDoor.Rotation = door.GlobalRotation;
                    AddChild(newDoor);
                    var d = SpawnRoomForce(newDoor);
                    // GD.PrintS(d, ((Node3D)d).GlobalPosition);
                    newDoor.QueueFree();
                }
                door.SetMeta("used", true);
                used.Add(door.GlobalPosition);
                foreach (var other in otherDoors)
                {
                    other.SetMeta("other_used", true);
                    if (other is MapDoor otherMapDoor)
                    {
                        otherMapDoor.Use();
                    }
                }
                if (door is MapDoor mapDoor)
                {
                    mapDoor.Use();
                }
            }
        }
    }

    public PresetRoom FindRoomByName(string name)
    {
        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i].ResourceName == name)
            {
                return rooms[i];
            }
        }
        for (int i = 0; i < interestRooms.Length; i++)
        {
            if (interestRooms[i].ResourceName == name)
            {
                return interestRooms[i];
            }
        }
        return null;
    }

    public void Align(Node3D parent1, Node3D child1, Node3D parent2, Node3D child2)
    {
        parent1.GlobalBasis = new Basis(Vector3.Up, (child2.GlobalRotation.Y - child1.GlobalRotation.Y) + Mathf.Pi);
        parent1.GlobalPosition += (child2.GlobalPosition - child1.GlobalPosition);
    }

    public Node SpawnRoom(MapRoom room)
    {
        var selectedRoom = GetValidRoom(room);
        var scn = selectedRoom.SceneCached;
        if (customSpawn == null)
        {
            var newRoom = scn.Instantiate<Node3D>();
            newRoom.Position = room.GlobalPosition;
            newRoom.Rotation = room.GlobalRotation;
            target.CallDeferred(Node3D.MethodName.AddChild, newRoom, true);
            return newRoom;
        }
        else
            return customSpawn(scn, room.GlobalPosition, room.GlobalRotation, new Godot.Collections.Dictionary());
    }

    public Node SpawnRoomForce(MapRoom room)
    {
        var selectedRoom = room.room;
        var scn = selectedRoom.SceneCached;
        if (customSpawn == null)
        {
            var newRoom = scn.Instantiate<Node3D>();
            newRoom.Position = room.GlobalPosition;
            newRoom.Rotation = room.GlobalRotation;
            target.CallDeferred(Node3D.MethodName.AddChild, newRoom, true);
            return newRoom;
        }
        else
            return customSpawn(scn, room.GlobalPosition, room.GlobalRotation, new Godot.Collections.Dictionary());
    }

    public Node SpawnPresetRoom(PresetRoom selectedRoom, Vector3 position, Vector3 rotation, MapLayoutRoom layoutRoom)
    {
        var scn = selectedRoom.SceneCached;
        if (customSpawn == null)
        {
            var newRoom = scn.Instantiate<Node3D>();
            newRoom.Position = position;
            newRoom.Rotation = rotation;
            if (layoutRoom != null)
            {
                ApplyRoomKeyValues(newRoom, layoutRoom.keyvalues);
            }
            target.CallDeferred(Node3D.MethodName.AddChild, newRoom, true);
            return newRoom;
        }
        else
        {
            if (layoutRoom != null)
            {
                return customSpawn(scn, position, rotation, ConvertDict(layoutRoom.keyvalues));
            }
            Node newRoom = customSpawn(scn, position, rotation, new Godot.Collections.Dictionary());
            return newRoom;
        }
    }

    public Godot.Collections.Dictionary ConvertDict(Dictionary<string, object> keyvalues)
    {
        var dict = new Godot.Collections.Dictionary();
        foreach (var kvp in keyvalues)
        {
            dict.Add(kvp.Key, kvp.Value?.ToString() ?? string.Empty);
        }
        return dict;
    }

    public void ApplyRoomKeyValues(Node root, Dictionary<string, object> keyvalues)
    {
        if (root is RoomInfoNode info)
        {
            info.keyvalues = new Godot.Collections.Dictionary<string, string>();
            foreach (var kvp in keyvalues)
            {
                info.keyvalues.Add(kvp.Key, kvp.Value?.ToString() ?? string.Empty);
            }
        }
    }

    public PresetRoom GetValidRoom(MapRoom room)
    {
        if (IsInstanceValid(room.room) && !room.room.isGeneric)
        {
            // validInterestRooms.Remove(room.room);
            return room.room;
        }
        var arr1 = validInterestRooms.Where(x => x.type == room.type && (room.stage == -1 || x.stage == room.stage) && (room.allowedRooms.Length == 0 || room.allowedRooms.Contains(x.ResourceName))).ToArray();
        if (arr1.Length == 0)
        {
            var arr2 = validRooms.Where(x => x.type == room.type && (room.stage == -1 || x.stage == room.stage)).ToArray();
            var selected2 = arr2[rng.Randi() % arr2.Length];
            // validRooms.Remove(selected2);
            return selected2;
        }
        // var selected1 = arr1[rng.Randi() % arr1.Length];
        var selected1 = arr1.OrderByDescending(x => x.size).First();
        // validInterestRooms.Remove(selected1);
        return selected1;
    }
}
