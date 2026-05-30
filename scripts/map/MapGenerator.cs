using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class MapGenerator : Node3D
{
    public class MapGenState
    {
        public List<RoomGenerationSettings> specialRooms = new List<RoomGenerationSettings>();
        public List<RoomGenerationSettings> allRooms = new List<RoomGenerationSettings>();
        public List<RoomGenerationSettings> spawnedRooms = new List<RoomGenerationSettings>();

        public void ResetRooms(RoomGenerationSettingsCollection collection)
        {
            allRooms.Clear();
            spawnedRooms.Clear();
            foreach (var settings in collection.Collection)
            {
                if (settings.RequiredInstances <= 0)
                {
                    for (int i = 0; i < settings.SpawnPoolEntries; i++)
                    {
                        allRooms.Add(settings);
                    }
                }
                else
                {
                    for (int i = 0; i < settings.SpawnPoolEntries; i++)
                    {
                        specialRooms.Add(settings);
                    }
                }
            }
        }
    }

    [Export]
    public Layout TargetLayout;
    [Export]
    public MultiplayerSpawner mapSpawner;
    [Export]
    public DrunkMapGenerator drunkGen;
    [Export]
    public MazeMapGenerator mazeGen;
    [Export]
    public ImageMapGenerator imageGen;
    [Export]
    public ImageMapGenerator2 imageGen2;
    [Export]
    public bool useLegacy = false;
    [Export]
    public bool useWfc = false;
    [ExportGroup("Scenes")]
    [Export]
    public PackedScene[] doorScenes;
    [Export]
    public PresetRoom[] doorRooms;
    [Export]
    public PresetRoom[] RoomsOfInterest;
    [Export]
    public PresetRoom[] RoomsOfNonInterest;
    [Export]
    public PackedScene[] clusters;
    [ExportSubgroup("WFC")]
    [Export]
    public RoomGenerationSettingsCollection PointsOfInterestCollection;
    [Export]
    public RoomGenerationSettingsCollection PointsOfNonInterestCollection;
    [Export]
    public RoomGenerationSettingsCollection[] RoomGenerationSettingsCollections;
    [Export]
    public int[] zoneXSizes;
    [Export]
    public int[] zoneCrossings;

    public LayoutGeneratorDeBroglie generator2;
    public MapGeneratorPresets generator3;
    public GraphGenerator generator4;

    public bool foundOverlap = false;

    public Dictionary<Node, Godot.Collections.Array> preSpawnData = new Dictionary<Node, Godot.Collections.Array>();

    // public List<PackedScene> scenes = new List<PackedScene>();

    public override void _EnterTree()
    {
        AddMapScenes();
    }

    public override void _Ready()
    {
        // AddMapScenes();
    }

    public void AddMapScenes()
    {
        mapSpawner.ClearSpawnableScenes();
        // scenes.Clear();
        if (IsInstanceValid((Node)IInitScript.Instance))
        {
            foreach (var preset in IInitScript.Instance.Data.Rooms)
            {
                // Log.Print($"Adding spawnable scene {preset.Scene.ResourcePath}");
                mapSpawner.AddSpawnableScene(preset.sceneFile);
                // scenes.Add(preset.Scene);
            }
        }
        if (doorScenes != null)
        {
            foreach (var doorScene in doorScenes)
            {
                mapSpawner.AddSpawnableScene(doorScene.ResourcePath);
                // scenes.Add(doorScene);
            }
        }
        foreach (var collection in RoomGenerationSettingsCollections)
        {
            foreach (var roomSettings in collection.Collection)
            {
                foreach (var scene in roomSettings.RoomScenes)
                {
                    // Log.Print($"Adding spawnable scene {scene.ResourcePath}");
                    mapSpawner.AddSpawnableScene(scene.ResourcePath);
                    // scenes.Add(scene);
                }
            }
        }
        if (!useLegacy)
        {
            foreach (var room in RoomsOfInterest)
            {
                // Log.Print($"Adding spawnable scene {room.Scene.ResourcePath}");
                mapSpawner.AddSpawnableScene(room.sceneFile);
                // scenes.Add(room.Scene);
            }
            foreach (var room in RoomsOfNonInterest)
            {
                // Log.Print($"Adding spawnable scene {room.Scene.ResourcePath}");
                mapSpawner.AddSpawnableScene(room.sceneFile);
                // scenes.Add(room.Scene);
            }
            if (doorRooms != null)
            {
                foreach (var room in doorRooms)
                {
                    // Log.Print($"Adding spawnable scene {room.Scene.ResourcePath}");
                    mapSpawner.AddSpawnableScene(room.sceneFile);
                    // scenes.Add(room.Scene);
                }
            }
        }
        mapSpawner.SpawnFunction = Callable.From<Variant, Node>(CustomSpawnFunc);
    }

    public static Vector3 FromFloat32Array(float[] arr, int offset = 0)
    {
        return new Vector3(arr[offset+0], arr[offset+1], arr[offset+2]);
    }

    public static float[] FromVector3(Vector3 vector)
    {
        return new float[] { vector.X, vector.Y, vector.Z };
    }

    public Node CustomSpawnFunc(Variant variant)
    {
        Godot.Collections.Array arr = variant.AsGodotArray();
        // if (arr[0].VariantType == Variant.Type.Int)
        {
            int id = arr[0].AsInt32();
            Vector3 position = arr[1].AsVector3();
            Vector3 rotation = arr[2].AsVector3();
            PackedScene scene = IInitScript.Instance.Data.GetRoomScene(mapSpawner.GetSpawnableScene(id));
            if (scene == null)
            {
                // Log.PrintErr($"Unable to load scene with id {arr[0]}");
                return null;
            }
            // RoomProxyNode node = new RoomProxyNode();
            // node.Position = position;
            // node.Rotation = rotation;
            // node.scene = scene;
            // return node;
            // Stopwatch sw = new Stopwatch();
            // sw.Start();
            Node spawned = scene.Instantiate();
            // sw.Stop();
            // if (sw.ElapsedMilliseconds > 0)
                // Log.Print($"Instantiate ({scene.ResourcePath}) took {sw.ElapsedMilliseconds} ms");
            /*
            if (spawned.GetType() == typeof(Node3D))
            {
                RoomInfoNode node = new RoomInfoNode();
                spawned.ReplaceBy(node, true);
                spawned.QueueFree();
                spawned = node;
            }
            */
            // node.AddChild(spawned, true);
            if (spawned is Node3D spawned3d)
            {
                // spawned3d.Position = mapSpawner.GetNode<Node3D>(mapSpawner.SpawnPath).ToLocal(FromFloat32Array(Json.ParseString(arr[1]).AsFloat32Array()));
                spawned3d.Position = position;
                spawned3d.Rotation = rotation;
            }
            if (spawned is RoomInfoNode room)
            {
                var dict = arr[3].AsGodotDictionary();
                room.keyvalues = new Godot.Collections.Dictionary<string, string>();
                foreach (var kvp in dict)
                {
                    if (kvp.Key.VariantType == Variant.Type.String && kvp.Value.VariantType == Variant.Type.String)
                    {
                        room.keyvalues.Add(kvp.Key.AsString(), kvp.Value.AsString());
                    }
                }
            }
            return spawned;
        }
        // Log.PrintErr($"Unable to spawn scene with id {arr[0]}");
        return null;
    }

    public Node SpawnCustom(string scene, Vector3 position, Vector3 rotation, Godot.Collections.Dictionary keyvalues)
    {
        Godot.Collections.Array arr = new();
        arr.Resize(4);
        int idx = -1;
        for (int i = 0; i < mapSpawner.GetSpawnableSceneCount(); i++)
        {
            if (mapSpawner.GetSpawnableScene(i) == scene)
            {
                idx = i;
                break;
            }
        }
        if (idx == -1)
        {
            Log.PrintErr($"{scene}");
            return null;
        }
        var gpos = mapSpawner.GetNode<Node3D>(mapSpawner.SpawnPath).ToLocal(position);
        arr[0] = idx;
        arr[1] = gpos;
        arr[2] = rotation;
        arr[3] = keyvalues;
        return mapSpawner.Spawn(arr);
    }

    public Node PreSpawn(string scene, Vector3 position, Vector3 rotation, Godot.Collections.Dictionary keyvalues)
    {
        Godot.Collections.Array arr = new();
        arr.Resize(4);
        int idx = -1;
        for (int i = 0; i < mapSpawner.GetSpawnableSceneCount(); i++)
        {
            if (mapSpawner.GetSpawnableScene(i) == scene)
            {
                idx = i;
            }
        }
        if (idx == -1)
        {
            Log.PrintErr($"{scene}");
            return null;
        }
        var gpos = mapSpawner.GetNode<Node3D>(mapSpawner.SpawnPath).ToLocal(position);
        arr[0] = idx;
        arr[1] = gpos;
        arr[2] = rotation;
        arr[3] = keyvalues;
        PackedScene scn = IInitScript.Instance.Data.GetRoomScene(mapSpawner.GetSpawnableScene(idx));
        Node3D node = scn.Instantiate<Node3D>();
        Node3D colNode = StripNode(node);
        node.QueueFree();
        AddChild(colNode, true);
        colNode.GlobalPosition = position;
        colNode.GlobalRotation = rotation;
        preSpawnData.Add(colNode, arr);
        return colNode;
    }

    public Node3D StripNode(Node3D node)
    {
        Node3D parent = new Node3D();
        parent.Name = node.Name;
        foreach (ZoneArea area in node.FindChildren("*", nameof(Area3D), true, false).Where(x => x is ZoneArea))
        {
            Area3D zone = new Area3D();
            zone.Name = area.Name;
            zone.Transform = area.Transform;
            zone.Monitoring = false;
            zone.Monitorable = false;
            parent.AddChild(zone, true);
            foreach (var ch in area.GetChildren())
            {
                if (ch is CollisionShape3D shape)
                {
                    var collider = new CollisionShape3D();
                    collider.Name = shape.Name;
                    collider.Transform = shape.Transform;
                    collider.Disabled = shape.Disabled;
                    collider.Shape = (Shape3D)shape.Shape.Duplicate(true);
                    zone.AddChild(collider, true);
                }
            }
        }
        return parent;
    }

    public RoomGenerationSettings FindRoom(RandomNumberGenerator rng, MapGenState state, ERoomDirectionFlags flags, out int dir)
    {
        ERoomDirectionFlags flags2 = flags;
        List<(RoomGenerationSettings, int)> itemsSpecial = new List<(RoomGenerationSettings, int)>();
        List<(RoomGenerationSettings, int)> items = new List<(RoomGenerationSettings, int)>();
        for (int i = 0; i < 4; i++)
        {
            // dir = i;
            int j = i;
            foreach (var item in state.allRooms.Where(x => x.Connections == flags2))
            {
                items.Add((item, j));
            }
            foreach (var item in state.specialRooms.Where(x => x.Connections == flags2))
            {
                itemsSpecial.Add((item, j));
            }
            flags2 = RoomDirectionFlagsUtility.RotateLeft(flags2);
        }
        if (itemsSpecial.Count != 0)
        {
            int idx = rng.RandiRange(0, itemsSpecial.Count - 1);
            if (itemsSpecial[idx].Item1.MaximumInstances > 0)
            {
                state.spawnedRooms.Add(itemsSpecial[idx].Item1);
            }
            if (itemsSpecial[idx].Item1.MaximumInstances > 0 && state.spawnedRooms.Count(x => x == itemsSpecial[idx].Item1) >= itemsSpecial[idx].Item1.MaximumInstances)
            {
                state.specialRooms.RemoveAll(x => x == itemsSpecial[idx].Item1);
            }
            dir = itemsSpecial[idx].Item2;
            return itemsSpecial[idx].Item1;
        }
        if (items.Count != 0)
        {
            int idx = rng.RandiRange(0, items.Count - 1);
            if (items[idx].Item1.MaximumInstances > 0)
            {
                state.spawnedRooms.Add(items[idx].Item1);
            }
            if (items[idx].Item1.MaximumInstances > 0 && state.spawnedRooms.Count(x => x == items[idx].Item1) >= items[idx].Item1.MaximumInstances)
            {
                state.allRooms.RemoveAll(x => x == items[idx].Item1);
            }
            dir = items[idx].Item2;
            return items[idx].Item1;
        }
        dir = 0;
        Log.PrintErr($"Return null {flags}");
        return null;
    }

    private void Reset(MapGenState state)
    {
        foreach (var item in mapSpawner.GetNode(mapSpawner.SpawnPath).GetChildren())
        {
            item.QueueFreeNow();
        }

        if (IsInstanceValid(TargetLayout))
            TargetLayout.ResetLayout();

        state.allRooms.Clear();
        state.spawnedRooms.Clear();
        foreach (var collection in RoomGenerationSettingsCollections)
        {
            foreach (var settings in collection.Collection)
            {
                if (settings.RequiredInstances <= 0)
                {
                    for (int i = 0; i < settings.SpawnPoolEntries; i++)
                    {
                        state.allRooms.Add(settings);
                    }
                }
                else
                {
                    for (int i = 0; i < settings.SpawnPoolEntries; i++)
                    {
                        state.specialRooms.Add(settings);
                    }
                }
            }
        }
    }

    public void LoadImageGen(MapGenState mainState, ulong internalSeed)
    {
        var flags = new ERoomDirectionFlags[]
        {
            ERoomDirectionFlags.NegativeZ,
            ERoomDirectionFlags.PositiveX,
            ERoomDirectionFlags.PositiveZ,
            ERoomDirectionFlags.NegativeX,
        };
        var img = imageGen.Generate(internalSeed);
        var size = imageGen.size;
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Seed = internalSeed;
        
        MapGenState[] states = new MapGenState[RoomGenerationSettingsCollections.Length];
        for (int i = 0; i < states.Length; i++)
        {
            states[i] = new MapGenState();
            states[i].ResetRooms(RoomGenerationSettingsCollections[i]);
        }

        List<(Vector2I, int)> validRooms = new List<(Vector2I, int)>();
        List<(Vector2I, int)> validSpecialRooms = new List<(Vector2I, int)>();

        for (int y = 0; y < size.Y; y++)
        {
            int curZone = 0;
            for (int x = 0; x < size.X; x++)
            {
                if (ImageMapGenerator.IsEqualApprox(img.GetPixel(x, 0), Color.FromHtml("fff")))
                {
                    curZone++;
                }
                var cell = imageGen.TryGet(new Vector2I(x, y));
                bool has = cell != ImageMapGenerator.CellType.None && cell != ImageMapGenerator.CellType.Wall;
                if (has)
                {
                    if (cell == ImageMapGenerator.CellType.GenericRoom)
                        validRooms.Add((new Vector2I(x, y), curZone));
                    else
                        validSpecialRooms.Add((new Vector2I(x, y), curZone));
                }
            }
        }

        foreach (var item in validSpecialRooms.OrderBy(x => rng.Randf()).ToArray())
        {
            ERoomDirectionFlags flag = 0;
            for (int i = 0; i < 4; i++)
            {
                var cell2 = imageGen.TryGet(item.Item1 + imageGen.directions[i]);
                if (cell2 != ImageMapGenerator.CellType.None && cell2 != ImageMapGenerator.CellType.Wall)
                {
                    flag |= flags[i];
                }
            }
            LayoutCell lcell = TargetLayout.Grid[item.Item1.X, item.Item1.Y];

            lcell.SetUniqueCellSeed((ulong)item.Item2);
            lcell.SetRoomGenerationResource(FindRoom(rng, states[item.Item2], flag, out var dir), zoneCrossings[item.Item2] != -1 ? zoneCrossings[item.Item2] : dir);
        }

        foreach (var item in validRooms.OrderBy(x => rng.Randf()).ToArray())
        {
            ERoomDirectionFlags flag = 0;
            for (int i = 0; i < 4; i++)
            {
                var cell2 = imageGen.TryGet(item.Item1 + imageGen.directions[i]);
                if (cell2 != ImageMapGenerator.CellType.None && cell2 != ImageMapGenerator.CellType.Wall)
                {
                    flag |= flags[i];
                }
            }
            LayoutCell lcell = TargetLayout.Grid[item.Item1.X, item.Item1.Y];

            lcell.SetUniqueCellSeed((ulong)item.Item2);
            lcell.SetRoomGenerationResource(FindRoom(rng, states[item.Item2], flag, out var dir), zoneCrossings[item.Item2] != -1 ? zoneCrossings[item.Item2] : dir);
        }
    }

    public void LoadImageGen2(ulong internalSeed)
    {
        var img = imageGen2.Generate(internalSeed);
        var size = imageGen2.size;
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Seed = internalSeed;
        
        MapGenState[] states = new MapGenState[RoomGenerationSettingsCollections.Length];
        for (int i = 0; i < states.Length; i++)
        {
            states[i] = new MapGenState();
            states[i].ResetRooms(RoomGenerationSettingsCollections[i]);
        }

        List<(Vector2I, int)> validRooms = new List<(Vector2I, int)>();
        List<(Vector2I, int)> validSpecialRooms = new List<(Vector2I, int)>();

        for (int y = 0; y < size.Y; y++)
        {
            int curZone = 0;
            for (int x = 0; x < size.X; x++)
            {
                if (ImageMapGenerator2.IsEqualApprox(img.GetPixel(x * ImageMapGenerator2.div + 1, 0), Color.FromHtml("fff")))
                {
                    curZone++;
                }
                var cell = imageGen2.TryGet(new Vector2I(x, y));
                bool has = cell.type != ImageMapGenerator2.CellType.None && cell.type != ImageMapGenerator2.CellType.Wall;
                if (has)
                {
                    if (cell.type == ImageMapGenerator2.CellType.GenericRoom)
                        validRooms.Add((new Vector2I(x, y), curZone));
                    else
                        validSpecialRooms.Add((new Vector2I(x, y), curZone));
                }
            }
        }

        foreach (var item in validSpecialRooms.OrderBy(x => rng.Randf()).ToArray())
        {
            ImageMapGenerator2.Cell cell = imageGen2.TryGet(item.Item1);
            LayoutCell lcell = TargetLayout.Grid[item.Item1.X, item.Item1.Y];

            lcell.SetUniqueCellSeed((ulong)item.Item2);
            lcell.SetRoomGenerationResource(FindRoom(rng, states[item.Item2], cell.directionFlags, out var dir), zoneCrossings[item.Item2] != -1 ? zoneCrossings[item.Item2] : dir);
        }

        foreach (var item in validRooms.OrderBy(x => rng.Randf()).ToArray())
        {
            ImageMapGenerator2.Cell cell = imageGen2.TryGet(item.Item1);
            LayoutCell lcell = TargetLayout.Grid[item.Item1.X, item.Item1.Y];

            lcell.SetUniqueCellSeed((ulong)item.Item2);
            lcell.SetRoomGenerationResource(FindRoom(rng, states[item.Item2], cell.directionFlags, out var dir), zoneCrossings[item.Item2] != -1 ? zoneCrossings[item.Item2] : dir);
        }
    }

    public async Task LoadMapAsync(ulong internalSeed)
    {
        // MapGenState mainState = new MapGenState();
        // Reset(mainState);

        // if (IsInstanceValid(TargetLayout))
        //     TargetLayout.InitializeLayout();
        if (generator2 == null)
        {
            generator2 = new LayoutGeneratorDeBroglie();
            AddChild(generator2);
        }
        if (generator3 == null)
        {
            generator3 = new MapGeneratorPresets();
            AddChild(generator3);
        }
        if (generator4 == null)
        {
            generator4 = new GraphGenerator();
            AddChild(generator4);
        }

        List<Node> allSpawned = new List<Node>();
        while (true)
        {
            // yield return new WaitForSeconds(5d);
            foreach (var item in mapSpawner.GetNode(mapSpawner.SpawnPath).GetChildren())
            {
                item.QueueFreeNow();
            }
            foreach (var item in allSpawned)
            {
                item.QueueFreeNow();
            }
            foreach (var kvp in preSpawnData)
            {
                kvp.Key.QueueFreeNow();
            }
            preSpawnData.Clear();
            string[] mapFiles;
            string[] mapGraphs = RoundManager.Instance.Data.MapGraphs[Name];
            string graph = string.Empty;
            if (RoundManager.Instance.UseCustomMap)
            {
                mapFiles = Settings.Server.CustomMaps;
            }
            else
            {
                mapFiles = RoundManager.Instance.Data.MapFiles[Name];
                if (mapGraphs.Length != 0)
                    graph = mapGraphs[internalSeed % (ulong)mapGraphs.LongLength];
            }
            if (true)
            {
                // generator3.rooms = RoundManager.Instance.Data.Rooms;
                generator3.rooms = RoundManager.Instance.Data.Rooms.Where(x => (x.isGeneric || !x.isInterest) && x.type != PresetRoom.RoomClass.Other).ToArray();
                // generator3.interestRooms = RoundManager.Instance.Data.RoomsOfInterest;
                generator3.interestRooms = RoundManager.Instance.Data.Rooms.Where(x => x.isInterest && x.stage != -1 && x.type != PresetRoom.RoomClass.Other).ToArray();
            }
            else
            {
                generator4.rooms = generator3.rooms = RoomsOfNonInterest;
                generator4.interestRooms = generator3.interestRooms = RoomsOfInterest;
            }
            generator3.allMaps = mapFiles;// TODO
            generator3.doors = doorRooms;
            generator3.customSpawn = (scn, pos, rot, kv) => PreSpawn(scn.ResourcePath, pos, rot, kv);
            generator4.customSpawn = (scn, pos, rot) => PreSpawn(scn.ResourcePath, pos, rot, new());
            bool res = false;
            if (true)
            {
                // res = generator3.GenerateFile(internalSeed, mapFiles[0], graph);
                // Log.PrintErr("If you are reading this, Virtual broke something...");
                res = generator3.GenerateFile(internalSeed, mapFiles[internalSeed % (ulong)mapFiles.LongLength], graph);
                allSpawned.Clear();
                allSpawned.AddRange(generator3.allSpawned);
            }
            else if (string.IsNullOrEmpty(graph))
            {
                if (useWfc)
                {
                    Generator generator = new Generator();
                    LevelMapSchema? map = generator.GenerateBuilding(internalSeed, default, generator3.mapSize, TargetLayout.GridSize, RoomGenerationSettingsCollections.SelectMany(x => x.Collection).ToArray());
                    if (map.HasValue)
                    {
                        allSpawned.Clear();
                        foreach (var room in map.Value.rooms)
                        {
                            if (room.roomId.RoomScenes.Count == 0)
                                continue;
                            allSpawned.Add(PreSpawn(room.roomId.RoomScenes[0].ResourcePath, ToGlobal(room.position), room.rotation, new()));
                        }
                        res = true;
                    }
                    else
                    {
                        res = false;
                    }
                }
                else
                {
                    res = generator3.GenerateFile(internalSeed, mapFiles[internalSeed % (ulong)mapFiles.LongLength], graph);
                    allSpawned.Clear();
                    allSpawned.AddRange(generator3.allSpawned);
                }
            }
            else
            {
                res = generator4.GenerateFile(internalSeed, graph);
                generator3.Init(internalSeed);
                generator3.allSpawned.Clear();
                generator3.allSpawned.AddRange(generator4.allSpawned);
                generator3.GenerateDoors();
                allSpawned.Clear();
                allSpawned.AddRange(generator4.allSpawned);
                allSpawned.AddRange(generator3.allSpawned);
            }
            if (res || RoundManager.Instance.UseCustomMap)
            {
                Log.PrintInfo($"Done with seed {internalSeed}.");
                if (RoundManager.Instance.UseCustomMap)
                {
                    foreach (var kvp in preSpawnData)
                    {
                        if (IsInstanceValid(kvp.Key))
                        {
                            kvp.Key.QueueFreeNow();
                            allSpawned.Add(mapSpawner.Spawn(kvp.Value));
                        }
                    }
                    generator3.allSpawned.Clear();
                    generator3.allSpawned.AddRange(allSpawned.Where(x => IsInstanceValid(x)));
                    generator3.customSpawn = (scn, pos, rot, kv) => SpawnCustom(scn.ResourcePath, pos, rot, kv);
                    generator3.GenerateDoors();
                    break;
                }
            }
            if (!IsInstanceValid(this))
                return;
            internalSeed++;
            // yield return new WaitForSeconds(0.5d);
            await ToSignal(GetTree().CreateTimer(0.5d), SceneTreeTimer.SignalName.Timeout);
            if (!IsInstanceValid(this))
                return;
            if (!res)
            {
                continue;
            }
            await FinishMap(allSpawned);
            if (!foundOverlap)
            {
                generator3.allSpawned.Clear();
                generator3.allSpawned.AddRange(allSpawned.Where(x => IsInstanceValid(x)));
                generator3.customSpawn = (scn, pos, rot, kv) => SpawnCustom(scn.ResourcePath, pos, rot, kv);
                Stopwatch sw = new Stopwatch();
                sw.Start();
                generator3.GenerateDoors();
                sw.Stop();
                Log.Print($"GenerateDoors took {sw.ElapsedMilliseconds} ms");
                break;
            }
        }
    }

    public IEnumerator LoadMap(ulong internalSeed)
    {
        MapGenState mainState = new MapGenState();
        Reset(mainState);

        yield return new WaitForSeconds(0.1d);
        // await ToSignal(GetTree().CreateTimer(100d / 1000d), SceneTreeTimer.SignalName.Timeout);

        if (IsInstanceValid(TargetLayout))
            TargetLayout.InitializeLayout();
        if (generator2 == null)
        {
            generator2 = new LayoutGeneratorDeBroglie();
            AddChild(generator2);
        }
        if (generator3 == null)
        {
            generator3 = new MapGeneratorPresets();
            AddChild(generator3);
        }
        if (generator4 == null)
        {
            generator4 = new GraphGenerator();
            AddChild(generator4);
        }

        if (imageGen2 != null)
        {
            LoadImageGen2(internalSeed);
            Log.PrintInfo($"Done with seed {internalSeed}.");
            {
                Task t = FinishMap();
                while (!t.IsCompleted)
                    yield return null;
            }
            yield break;
        }

        if (imageGen != null)
        {
            LoadImageGen(mainState, internalSeed);
            Log.PrintInfo($"Done with seed {internalSeed}.");
            {
                Task t = FinishMap();
                while (!t.IsCompleted)
                    yield return null;
            }
            yield break;
        }

        if (mazeGen != null)
        {
            var flags = new ERoomDirectionFlags[]
            {
                ERoomDirectionFlags.NegativeZ,
                ERoomDirectionFlags.PositiveX,
                ERoomDirectionFlags.PositiveZ,
                ERoomDirectionFlags.NegativeX,
            };
            var map = mazeGen.Generate(internalSeed, RoomGenerationSettingsCollections.SelectMany(x => x.Collection).ToArray());
            var size = mazeGen.size;
            RandomNumberGenerator rng = new RandomNumberGenerator();
            rng.Seed = internalSeed;
            for (int y = 0; y < size.Y; y++)
            {
                for (int x = 0; x < size.X; x++)
                {
                    var cell = mazeGen.TryGet(new Vector2I(x, y));
                    bool has = cell != MazeMapGenerator.CellType.None && cell != MazeMapGenerator.CellType.Wall;
                    if (has)
                    {
                        ERoomDirectionFlags flag = 0;
                        for (int i = 0; i < 4; i++)
                        {
                            var cell2 = mazeGen.TryGet(new Vector2I(x, y) + mazeGen.allDirections[i]);
                            if (cell2 != MazeMapGenerator.CellType.None && cell2 != MazeMapGenerator.CellType.Wall)
                            {
                                flag |= flags[i];
                            }
                        }
                        LayoutCell lcell = TargetLayout.Grid[x, y];

                        lcell.SetUniqueCellSeed(0);
                        lcell.SetRoomGenerationResource(FindRoom(rng, mainState, flag, out var dir), dir);
                    }
                }
            }
            Log.PrintInfo($"Done with seed {internalSeed}.");
            {
                Task t = FinishMap();
                while (!t.IsCompleted)
                    yield return null;
            }
            yield break;
        }

        if (drunkGen != null)
        {
            while (true)
            {
                Reset(mainState);
                var flags = new ERoomDirectionFlags[]
                {
                    ERoomDirectionFlags.NegativeZ,
                    ERoomDirectionFlags.PositiveX,
                    ERoomDirectionFlags.PositiveZ,
                    ERoomDirectionFlags.NegativeX,
                };
                var map = drunkGen.Generate(internalSeed);
                var size = drunkGen.size;
                var bounds = drunkGen.BoundingBox;
                int extraX = 0;
                if (zoneCrossings != null)
                {
                    for (int i = 0; i < zoneCrossings.Length; i++)
                    {
                        if (zoneCrossings[i] == -1)
                            continue;
                        extraX += zoneXSizes[i];
                    }
                }
                var newmap = new bool[(size.X + extraX) * size.Y];
                for (int y = 0; y < size.Y; y++)
                {
                    int curZone = 0;
                    int curZoneCount = 0;
                    for (int x = 0, x2 = 0; x < size.X + extraX; x++, x2++)
                    {
                        if (x2 >= bounds.Position.X)
                            curZoneCount++;
                        if (zoneXSizes != null && zoneXSizes.Length != 0 && curZoneCount > zoneXSizes[curZone])
                        {
                            curZoneCount = 0;
                            curZone++;
                        }
                        if (zoneCrossings != null && zoneCrossings.Length != 0)
                        {
                            if (zoneCrossings[curZone] != -1)
                            {
                                if (/*y >= 1 && y < size.Y - 1 &&*/ map[x2 - 1 + y * size.X] && map[x2 + 0 + y * size.X])
                                    newmap[x + y * (size.X + extraX)] = true;
                                else
                                    newmap[x + y * (size.X + extraX)] = false;
                                x2--;
                            }
                            else
                            {
                                newmap[x + y * (size.X + extraX)] = map[x2 + y * size.X];
                            }
                        }
                        else
                        {
                            newmap[x + y * (size.X + extraX)] = map[x2 + y * size.X];
                        }
                    }
                }
                drunkGen.map = newmap;
                drunkGen.size = new Vector2I(size.X + extraX, size.Y);
                size = drunkGen.size;
                RandomNumberGenerator rng = new RandomNumberGenerator();
                rng.Seed = internalSeed;
                MapGenState[] states = new MapGenState[RoomGenerationSettingsCollections.Length];
                for (int i = 0; i < states.Length; i++)
                {
                    states[i] = new MapGenState();
                    states[i].ResetRooms(RoomGenerationSettingsCollections[i]);
                }
                List<Vector3I> positions = new List<Vector3I>();
                for (int y = 0; y < size.Y; y++)
                {
                    int curZone = 0;
                    int curZoneCount = 0;
                    for (int x = 0; x < size.X; x++)
                    {
                        if (x >= bounds.Position.X)
                            curZoneCount++;
                        if (zoneXSizes != null && zoneXSizes.Length != 0 && curZoneCount > zoneXSizes[curZone])
                        {
                            curZoneCount = 0;
                            curZone++;
                        }
                        bool has = drunkGen.TryGet(new Vector2I(x, y));
                        if (has)
                        {
                            positions.Add(new Vector3I(x, y, curZone));
                        }
                    }
                }
                while (positions.Count > 0)
                {
                    int i = (int)(rng.Randi() % positions.Count);
                    int x = positions[i].X;
                    int y = positions[i].Y;
                    int curZone = positions[i].Z;
                    positions.RemoveAt(i);
                    // if (has)
                    {
                        ERoomDirectionFlags flag = 0;
                        for (int j = 0; j < 4; j++)
                        {
                            if (drunkGen.TryGet(new Vector2I(x, y) + drunkGen.directions[j]))
                            {
                                flag |= flags[j];
                            }
                        }
                        LayoutCell cell = TargetLayout.Grid[x, y];

                        if (zoneCrossings != null && zoneCrossings[curZone] != -1)
                        {
                            flag = ERoomDirectionFlags.PositiveX | ERoomDirectionFlags.NegativeX;
                        }

                        var room = FindRoom(rng, states[curZone], flag, out var dir);
                        if (zoneCrossings != null && zoneCrossings[curZone] != -1)
                        {
                            dir = zoneCrossings[curZone];
                        }

                        cell.SetUniqueCellSeed((ulong)curZone);
                        cell.SetRoomGenerationResource(room, dir);
                    }
                }

                bool found = false;
                for (int i = 0; i < states.Length; i++)
                {
                    foreach (var settings in RoomGenerationSettingsCollections[i].Collection)
                    {
                        if (settings.RequiredInstances > 0)
                        {
                            if (states[i].spawnedRooms.Count(x => x == settings) < settings.RequiredInstances)
                            {
                                Log.Print($"Didn't spawn enough {settings.ResourcePath}");
                                found = true;
                                break;
                            }
                        }
                    }
                    if (found)
                        break;
                }
                if (!found)
                    break;
                internalSeed++;
                yield return null;
            }
            Log.PrintInfo($"Done with seed {internalSeed}.");
            {
                Task t = FinishMap();
                while (!t.IsCompleted)
                    yield return null;
            }
            yield break;
        }

        if (!useLegacy)
        {
            List<Node> allSpawned = new List<Node>();
            while (true)
            {
                // yield return new WaitForSeconds(5d);
                foreach (var item in mapSpawner.GetNode(mapSpawner.SpawnPath).GetChildren())
                {
                    item.QueueFreeNow();
                }
                foreach (var item in allSpawned)
                {
                    item.QueueFreeNow();
                }
                foreach (var kvp in preSpawnData)
                {
                    kvp.Key.QueueFreeNow();
                }
                preSpawnData.Clear();
                string[] mapFiles;
                string[] mapGraphs = RoundManager.Instance.Data.MapGraphs[Name];
                string graph = string.Empty;
                if (RoundManager.Instance.UseCustomMap)
                {
                    mapFiles = Settings.Server.CustomMaps;
                }
                else
                {
                    mapFiles = RoundManager.Instance.Data.MapFiles[Name];
                    if (mapGraphs.Length != 0)
                        graph = mapGraphs[internalSeed % (ulong)mapGraphs.LongLength];
                }
                if (true)
                {
                    // generator3.rooms = RoundManager.Instance.Data.Rooms;
                    generator3.rooms = RoundManager.Instance.Data.Rooms.Where(x => (x.isGeneric || !x.isInterest) && x.type != PresetRoom.RoomClass.Other).ToArray();
                    // generator3.interestRooms = RoundManager.Instance.Data.RoomsOfInterest;
                    generator3.interestRooms = RoundManager.Instance.Data.Rooms.Where(x => x.isInterest && x.stage != -1 && x.type != PresetRoom.RoomClass.Other).ToArray();
                }
                else
                {
                    generator4.rooms = generator3.rooms = RoomsOfNonInterest;
                    generator4.interestRooms = generator3.interestRooms = RoomsOfInterest;
                }
                generator3.allMaps = mapFiles;// TODO
                generator3.doors = doorRooms;
                generator3.customSpawn = (scn, pos, rot, kv) => PreSpawn(scn.ResourcePath, pos, rot, kv);
                generator4.customSpawn = (scn, pos, rot) => PreSpawn(scn.ResourcePath, pos, rot, new());
                bool res = false;
                if (true)
                {
                    // res = generator3.GenerateFile(internalSeed, mapFiles[0], graph);
                    // Log.PrintErr("If you are reading this, Virtual broke something...");
                    res = generator3.GenerateFile(internalSeed, mapFiles[internalSeed % (ulong)mapFiles.LongLength], graph);
                    allSpawned.Clear();
                    allSpawned.AddRange(generator3.allSpawned);
                }
                else if (string.IsNullOrEmpty(graph))
                {
                    if (useWfc)
                    {
                        Generator generator = new Generator();
                        LevelMapSchema? map = generator.GenerateBuilding(internalSeed, default, generator3.mapSize, TargetLayout.GridSize, RoomGenerationSettingsCollections.SelectMany(x => x.Collection).ToArray());
                        if (map.HasValue)
                        {
                            allSpawned.Clear();
                            foreach (var room in map.Value.rooms)
                            {
                                if (room.roomId.RoomScenes.Count == 0)
                                    continue;
                                allSpawned.Add(PreSpawn(room.roomId.RoomScenes[0].ResourcePath, ToGlobal(room.position), room.rotation, new()));
                            }
                            res = true;
                        }
                        else
                        {
                            res = false;
                        }
                    }
                    else
                    {
                        res = generator3.GenerateFile(internalSeed, mapFiles[internalSeed % (ulong)mapFiles.LongLength], graph);
                        allSpawned.Clear();
                        allSpawned.AddRange(generator3.allSpawned);
                    }
                }
                else
                {
                    res = generator4.GenerateFile(internalSeed, graph);
                    generator3.Init(internalSeed);
                    generator3.allSpawned.Clear();
                    generator3.allSpawned.AddRange(generator4.allSpawned);
                    generator3.GenerateDoors();
                    allSpawned.Clear();
                    allSpawned.AddRange(generator4.allSpawned);
                    allSpawned.AddRange(generator3.allSpawned);
                }
                if (res || RoundManager.Instance.UseCustomMap)
                {
                    Log.PrintInfo($"Done with seed {internalSeed}.");
                    if (RoundManager.Instance.UseCustomMap)
                    {
                        foreach (var kvp in preSpawnData)
                        {
                            kvp.Key.QueueFreeNow();
                            allSpawned.Add(mapSpawner.Spawn(kvp.Value));
                        }
                        generator3.allSpawned.Clear();
                        generator3.allSpawned.AddRange(allSpawned.Where(x => IsInstanceValid(x)));
                        generator3.customSpawn = (scn, pos, rot, kv) => SpawnCustom(scn.ResourcePath, pos, rot, kv);
                        generator3.GenerateDoors();
                        break;
                    }
                }
                if (!IsInstanceValid(this))
                    yield break;
                internalSeed++;
                yield return new WaitForSeconds(0.5d);
                if (!IsInstanceValid(this))
                    yield break;
                if (!res)
                {
                    continue;
                }
                Task t = FinishMap(allSpawned);
                while (!t.IsCompleted)
                    yield return null;
                if (!foundOverlap)
                {
                    generator3.allSpawned.Clear();
                    generator3.allSpawned.AddRange(allSpawned.Where(x => IsInstanceValid(x)));
                    generator3.customSpawn = (scn, pos, rot, kv) => SpawnCustom(scn.ResourcePath, pos, rot, kv);
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    generator3.GenerateDoors();
                    sw.Stop();
                    Log.PrintWarn($"GenerateDoors took {sw.ElapsedMilliseconds} ms");
                    break;
                }
            }
            yield break;
        }

        while (true)
        {
            yield return new SwitchToNewThread(System.Threading.ThreadPriority.AboveNormal);
            ELayoutGenerationResult result = generator2.Generate(TargetLayout, RoomGenerationSettingsCollections, internalSeed);
            if (!IsInstanceValid(this))
                yield break;
            yield return new SwitchToMainThread();
            if (!IsInstanceValid(this))
                yield break;

            if (result == ELayoutGenerationResult.Success)
            {
                List<LayoutCell> cells = new List<LayoutCell>();
                List<LayoutCell> ignoreCells = new List<LayoutCell>();
                for (int y = 0; y < TargetLayout.Grid.GetLength(1); y++)
                {
                    for (int x = 0; x < TargetLayout.Grid.GetLength(0); x++)
                    {
                        LayoutCell cell = TargetLayout.Grid[x, y];
                        if (cell.RoomGenerationResource == null)
                            continue;
                        if (PointsOfInterestCollection.Collection.Contains(cell.RoomGenerationResource))
                            cells.Add(cell);
                        else if (PointsOfNonInterestCollection.Collection.Contains(cell.RoomGenerationResource))
                            ignoreCells.Add(cell);
                    }
                }

                bool found = false;
                /*
                for (int i = 0; i < cells.Count; i++)
                {
                    for (int j = 0; j < cells.Count; j++)
                    {
                        if (!TargetLayout.DoesPathExist(cells[i], cells[j], new Godot.Collections.Array<LayoutCell>(ignoreCells)))
                        {
                            found = true;
                            break;
                        }
                    }
                    for (int j = 0; j < ignoreCells.Count; j++)
                    {
                        if (!TargetLayout.DoesPathExist(cells[i], ignoreCells[j], new Godot.Collections.Array<LayoutCell>()))
                        {
                            found = true;
                            break;
                        }
                    }
                }
                */
                if (!found)
                {
                    break;
                }
                Log.PrintWarn("Could not find path between POI.");
                break;
            }

            internalSeed++;
            TargetLayout.ResetLayout();
            yield return null;

            // yield return new WaitForSeconds(0.1d);
            // await ToSignal(GetTree().CreateTimer(100d / 1000d), SceneTreeTimer.SignalName.Timeout);
        }

        Log.PrintInfo($"Done with seed {internalSeed}.");
        {
            Task t = FinishMap();
            while (!t.IsCompleted)
                yield return null;
        }
    }

    private async Task FinishMap()
    {
        List<Node> nodes = new List<Node>();
        for (int y = 0; y < TargetLayout.Grid.GetLength(1); y++)
        {
            for (int x = 0; x < TargetLayout.Grid.GetLength(0); x++)
            {
                LayoutCell cell = TargetLayout.Grid[x, y];
                //cell.RotationDegrees = new Vector3(0f, cell.GridRotation * -90f, 0f);
                if (cell.RoomSceneResource == null)
                    continue;
                Node n = SpawnCustom(cell.RoomSceneResource.ResourcePath, cell.GetGlobalPosition(), cell.GetGlobalRotation(), new());
                nodes.Add(n);
            }
        }

        for (int i = 0; i < 10; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        foundOverlap = CheckForOverlaps(Name, nodes);

        if (doorScenes != null && doorScenes.Length != 0)
        {
            for (int y = 0; y < TargetLayout.Grid.GetLength(1); y++)
            {
                for (int x = 0; x < TargetLayout.Grid.GetLength(0); x++)
                {
                    LayoutCell cell = TargetLayout.Grid[x, y];
                    if (cell.RoomSceneResource == null)
                        continue;
                    var pos = cell.GetGlobalDoorPositions();
                    var rot = cell.GetGlobalDoorRotations();
                    for (int i = 0; i < pos.Length; i++)
                        SpawnCustom(doorScenes[cell.UniqueCellSeed].ResourcePath, pos[i], rot[i], new());
                }
            }
        }
    }

    private async Task FinishMap(List<Node> nodes)
    {
        // for (int i = 0; i < 10; i++)
            // await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        foundOverlap = false;

        await AsyncTools.RunPhysics(() =>
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            foundOverlap = CheckForOverlaps(Name, nodes);
            sw.Stop();
            Log.Print($"CheckForOverlaps took {sw.ElapsedMilliseconds} ms");
        });
        if (Settings.User.DebugMode)
            foundOverlap = false;

        if (!foundOverlap)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            nodes.EnsureCapacity(preSpawnData.Count);
            foreach (var kvp in preSpawnData)
            {
                kvp.Key.QueueFreeNow();
                nodes.Add(mapSpawner.Spawn(kvp.Value));
            }
            sw.Stop();
            Log.Print($"FinishMap took {sw.ElapsedMilliseconds} ms");
        }
    }

    public static bool CheckForOverlaps(string name, List<Node> nodes)
    {
        foreach (var node1 in nodes)
        {
            foreach (var node2 in nodes)
            {
                if (node1 == node2)
                    continue;
                foreach (Area3D body1 in node1.FindChildren("*", nameof(Area3D), true, false).OfType<Area3D>())
                {
                    foreach (Area3D body2 in node2.FindChildren("*", nameof(Area3D), true, false).OfType<Area3D>())
                    {
                        if (body1 != body2 && IsOverlapping(body1, body2))
                        {
                            Log.Print($"Overlap Found! ({name}, {body1.GetPath()} {body2.GetPath()})");
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    private static bool IsOverlapping(Area3D area1, Area3D area2)
    {
        // 364933524 (once broken seed)
        foreach (CollisionShape3D child1 in area1.GetChildren().Where(x => x is CollisionShape3D sh && sh.Shape is BoxShape3D))
        {
            {
                BoxShape3D box1 = new BoxShape3D();
                box1.Size = ((BoxShape3D)child1.Shape).Size * 0.75f;
                // box1.Size = (box1.Size - Vector3.One).Abs();
                PhysicsShapeQueryParameters3D args = new PhysicsShapeQueryParameters3D()
                {
                    CollideWithBodies = false,
                    CollideWithAreas = true,
                    Shape = box1,
                    Exclude = new Godot.Collections.Array<Rid>()
                    {
                        area1.GetRid(),
                    },
                    Margin = 0f,
                    CollisionMask = area2.CollisionLayer,
                    Transform = child1.GlobalTransform,
                };
                var results = child1.GetWorld3D().DirectSpaceState.IntersectShape(args, 64);
                {
                    foreach (var res in results)
                    {
                        if (res["collider"].AsGodotObject() == area2)
                        {
                            int shape = res["shape"].AsInt32();
                            uint owner = area2.ShapeFindOwner(shape);
                            CollisionShape3D shape2 = (CollisionShape3D)area2.ShapeOwnerGetOwner(owner);
                            BoxShape3D box2 = (BoxShape3D)shape2.Shape;
                            // /*
                            if (Settings.User.DebugMode)
                            {
                                DebugDrawManager.Instance.DrawDebugBox(child1.GlobalPosition, box1.Size / 2f, child1.GlobalRotation, Colors.Red, 10d);
                                DebugDrawManager.Instance.DrawDebugBox(shape2.GlobalPosition, box2.Size / 2f, shape2.GlobalRotation, Colors.Blue, 10d);
                                DebugDrawManager.Instance.DrawDebugString(child1.GlobalPosition, $"Overlap {area1.GetPath()} {area2.GetPath()}!", Colors.Red, 10d);
                                FreeCam.Instance.GlobalPosition = child1.GlobalPosition;
                            }
                            // */
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }
}
