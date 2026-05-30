using System.Collections.Generic;
using System.Linq;
using DeBroglie;
using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Topo;
using DeBroglie.Wfc;
using Godot;

public struct LevelMapSchema
{
    public List<LevelRoomSchema> rooms = [];

    public LevelMapSchema()
    {
    }
}

public struct LevelRoomSchema
{
    public RoomGenerationSettings roomId;
    public uint roomSeed;
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
}

public class Generator
{
    private readonly RandomNumberGenerator _rng = new();
    private AdjacentModel _model;

    public LevelMapSchema? GenerateBuilding(ulong seed, LevelMapSchema existing, float gridSize, Vector2I size, RoomGenerationSettings[] roomSettings)
    {
        _rng.Seed = seed;
        _model = new AdjacentModel();
        _model.SetDirections(DirectionSet.Cartesian2d);

        HashSet<Tile> tiles = [];
        // foreach (var collection in settings)
        {
            List<Tile> result = AddCollection(tiles, roomSettings);
            tiles.UnionWith(result);
        }

        List<ITileConstraint> constraints =
        [
            new BorderConstraint
            {
                Ban = false,
                InvertArea = false,
                Sides = BorderSides.XMin | BorderSides.XMax | BorderSides.YMin | BorderSides.YMax,
                Tiles = [.. tiles.Where(x => ((RoomSettingsRotated)x.Value).Connections == 0)],
            }
        ];

        // load from existing map
        if (existing.rooms != null)
        {
            foreach (var room in existing.rooms)
            {
                Vector3 pos = room.position;
                pos.X /= gridSize;
                pos.Z /= gridSize;
                float rot = room.rotation.Y / (-0.5f * Mathf.Pi);
                Tile settings = tiles.First(x =>
                {
                    RoomSettingsRotated t = (RoomSettingsRotated)x.Value;
                    return t.RoomSettings == room.roomId && t.IsBase && t.Rotation == (int)rot;
                });
                constraints.Add(new FixedTileConstraint()
                {
                    Point = new Point((int)pos.X, (int)pos.Z),
                    Tiles = [settings],
                });
            }
        }
        //

        List<int> usedGroups = [];
        foreach (var tile in tiles)
        {
            var room = (RoomSettingsRotated)tile.Value;
            if (room.Connections == 0)
                continue;
            if (room.RoomSettings.MaxConsecutive >= 0 && !usedGroups.Contains(room.RoomSettings.MaxConsecutiveGroup))
            {
                constraints.Add(new MaxConsecutiveConstraint
                {
                    MaxCount = room.RoomSettings.MaxConsecutive,
                    Tiles = tiles.Where(x => ((RoomSettingsRotated)x.Value).RoomSettings == room.RoomSettings || (((RoomSettingsRotated)x.Value).RoomSettings.MaxConsecutiveGroup == room.RoomSettings.MaxConsecutiveGroup && room.RoomSettings.MaxConsecutiveGroup != -1)).ToHashSet(),
                });
                if (room.RoomSettings.MaxConsecutiveGroup != -1)
                    usedGroups.Add(room.RoomSettings.MaxConsecutiveGroup);
            }
        }

        Dictionary<Tile, ISet<Direction>> dict = [];
        foreach (var tile in tiles)
        {
            var room = (RoomSettingsRotated)tile.Value;
            if (room.Connections == 0)
                continue;
            if (!dict.ContainsKey(tile))
                dict.Add(tile, new HashSet<Direction>());
            var a2 = room.Connections;
            for (int i = 0; i < room.Rotation; i++)
            {
                a2 = RoomDirectionFlagsUtility.RotateRight(a2);
            }
            dict[tile].UnionWith(GetDirections(a2));
        }
        constraints.Add(new ConnectedConstraint
        {
            UsePickHeuristic = true,
            PathSpec = new EdgedPathSpec
            {
                Exits = dict,
            },
        });

        foreach (RoomGenerationSettings room in roomSettings)
        {
            HashSet<Tile> lst = [];
            lst.UnionWith(tiles.Where(x => ((RoomSettingsRotated)x.Value).RoomSettings == room));
            if (room.MaximumInstances == room.RequiredInstances && room.RequiredInstances > 0)
            {
                constraints.Add(new CountConstraint
                {
                    Comparison = CountComparison.Exactly,
                    Tiles = lst,
                    Count = room.RequiredInstances,
                });
            }
            else
            {
                if (room.RequiredInstances > 0)
                {
                    constraints.Add(new CountConstraint
                    {
                        Comparison = CountComparison.AtLeast,
                        Tiles = lst,
                        Count = room.RequiredInstances,
                    });
                }
                if (room.MaximumInstances != -1 && room.MaximumInstances >= room.RequiredInstances)
                {
                    constraints.Add(new CountConstraint
                    {
                        Comparison = CountComparison.AtMost,
                        Tiles = lst,
                        Count = room.MaximumInstances,
                    });
                }
            }
        }

        GridTopology topo = new(size.X, size.Y, false);
        TilePropagator propagator = new(_model, topo, new TilePropagatorOptions
        {
            Constraints = [.. constraints],
            RandomDouble = () => _rng.Randf(),
            BacktrackType = BacktrackType.Backjump,
            MaxBacktrackDepth = 100,
            ModelConstraintAlgorithm = ModelConstraintAlgorithm.Ac4,
        });
        Resolution status = propagator.Step();
        int k = 0;
        while (status == Resolution.Undecided && k < 20_000)
        {
            status = propagator.Step();
            k++;
        }
        if (status != Resolution.Decided)
        {
            Log.Print($"Undecided: {status}");
            return null;
        }
        ITopoArray<Tile> output = propagator.ToArray();
        LevelMapSchema building = new();
        for (int y = 0; y < size.Y; y++)
        {
            for (int x = 0; x < size.X; x++)
            {
                Tile tile = output.Get(x, y);
                if (tile.Value is RoomSettingsRotated rotatedRoom)
                {
                    if (rotatedRoom.IsBase)
                    {
                        RoomGenerationSettings room = rotatedRoom.RoomSettings;
                        int dir = rotatedRoom.Rotation;
                        LevelRoomSchema roomSchema = new()
                        {
                            roomId = room,
                            roomSeed = _rng.Randi(),
                            position = new Vector3(x * gridSize, 0f, y * gridSize),
                            rotation = new Vector3(0f, dir * -0.5f * Mathf.Pi, 0f),
                            scale = Vector3.One,
                        };
                        building.rooms.Add(roomSchema);
                    }
                }
            }
        }
        return building;
    }

    public class RoomSettingsRotated(RoomGenerationSettings settings, int rotation, int positionIndex)

    {
        public RoomGenerationSettings RoomSettings { get; private set; } = settings;
        public int Rotation { get; set; } = rotation;
        public int PositionIndex { get; set; } = positionIndex;
        public bool IsBase => PositionIndex == 0;
        public Vector2I Position => PositionIndex < RoomSettings.CellPositions.Count ? RoomSettings.CellPositions[PositionIndex] : Vector2I.Zero;
        public ERoomDirectionFlags Connections => PositionIndex < RoomSettings.ConnectionDoors.Count ? RoomSettings.ConnectionDoors[PositionIndex] : RoomSettings.Connections;
        public ERoomDirectionFlags BlockedConnections => PositionIndex < RoomSettings.BlockedConnectionDoors.Count ? RoomSettings.BlockedConnectionDoors[PositionIndex] : 0;

        public override string ToString()
        {
            return $"({RoomSettings.ResourcePath}, {Rotation}, {PositionIndex})";
        }
    }

    public static Direction[] GetDirections(ERoomDirectionFlags dir)
    {
        List<Direction> dirs = [];
        if (dir.HasFlag(ERoomDirectionFlags.PositiveX))
            dirs.Add(Direction.XPlus);
        if (dir.HasFlag(ERoomDirectionFlags.PositiveZ))
            dirs.Add(Direction.YPlus);
        if (dir.HasFlag(ERoomDirectionFlags.NegativeX))
            dirs.Add(Direction.XMinus);
        if (dir.HasFlag(ERoomDirectionFlags.NegativeZ))
            dirs.Add(Direction.YMinus);
        return [.. dirs];
    }

    public static Direction GetDirection(int rot)
    {
        switch (rot)
        {
            case 1:
                return Direction.XPlus;
            case 0:
                return Direction.YPlus;
            case 3:
                return Direction.XMinus;
            case 2:
                return Direction.YMinus;
            default:
                return Direction.YPlus;
        }
    }

    public static (bool, int) IsConnected(int r, ERoomDirectionFlags a, int r2, ERoomDirectionFlags b, int r3, bool allowConnected)
    {
        ERoomDirectionFlags a2 = a;
        ERoomDirectionFlags b2 = b;
        {
            for (int i = 0; i < r + r3; i++)
            {
                a2 = RoomDirectionFlagsUtility.RotateRight(a2);
            }
        }
        {
            for (int i = 0; i < r2 + r3; i++)
            {
                b2 = RoomDirectionFlagsUtility.RotateRight(b2);
            }
        }
        var zp = IsZpConnected(a2, b2);
        var zp2 = IsZpNotConnected(a2, b2);
        if (allowConnected)
        {
            return (zp, 0);
        }

        return (zp2, 0);
    }

    public static bool IsZpConnected(ERoomDirectionFlags a, ERoomDirectionFlags b)
    {
        return a.HasFlag(ERoomDirectionFlags.PositiveZ) && b.HasFlag(ERoomDirectionFlags.NegativeZ);
    }

    public static bool IsXpConnected(ERoomDirectionFlags a, ERoomDirectionFlags b)
    {
        return a.HasFlag(ERoomDirectionFlags.PositiveX) && b.HasFlag(ERoomDirectionFlags.NegativeX);
    }

    public static bool IsZpNotConnected(ERoomDirectionFlags a, ERoomDirectionFlags b)
    {
        return !a.HasFlag(ERoomDirectionFlags.PositiveZ) && !b.HasFlag(ERoomDirectionFlags.NegativeZ);
    }

    public static bool IsXpNotConnected(ERoomDirectionFlags a, ERoomDirectionFlags b)
    {
        return !a.HasFlag(ERoomDirectionFlags.PositiveX) && !b.HasFlag(ERoomDirectionFlags.NegativeX);
    }

    public List<Tile> AddCollection(HashSet<Tile> existingTiles, RoomGenerationSettings[] settingCollection)
    {
        List<Tile> tiles = [];
        foreach (var setting in settingCollection)
        {
            var arr = existingTiles.Where(x => ((RoomSettingsRotated)x.Value).RoomSettings == setting);
            if (arr.Any())
            {
                tiles.AddRange(arr);
                continue;
            }
            if (setting.CellPositions.Count == 0)
            {
                for (int r = 0; r < 4; r++)
                {
                    var tile = new Tile(new RoomSettingsRotated(setting, r, 0));
                    tiles.Add(tile);
                }
            }
            for (int i = 0; i < setting.CellPositions.Count; i++)
            {
                for (int r = 0; r < 4; r++)
                {
                    var tile = new Tile(new RoomSettingsRotated(setting, r, i));
                    tiles.Add(tile);
                }
            }
        }

        foreach (var tileSrc in tiles)
        {
            var srcRoom = (RoomSettingsRotated)tileSrc.Value;
            var src = srcRoom.RoomSettings;
            _model?.SetFrequency(tileSrc, src.SpawnPoolEntries);
            foreach (var tileDst in tiles)
            {
                var dstRoom = (RoomSettingsRotated)tileDst.Value;
                var dst = dstRoom.RoomSettings;

                for (int i = 0; i < 4; i++)
                {
                    (bool, int) v = IsConnected(srcRoom.Rotation, srcRoom.Connections, dstRoom.Rotation, dstRoom.Connections, i, true);
                    (bool, int) vEmpty = IsConnected(srcRoom.Rotation, srcRoom.Connections, dstRoom.Rotation, dstRoom.Connections, i, false);
                    (bool, int) v2 = IsConnected(srcRoom.Rotation, srcRoom.BlockedConnections, dstRoom.Rotation, dstRoom.BlockedConnections, i, true);
                    ERoomDirectionFlags srcBlockedFlags = srcRoom.BlockedConnections;
                    for (int j = 0; j < i; j++)
                    {
                        srcBlockedFlags = RoomDirectionFlagsUtility.RotateRight(srcBlockedFlags);
                    }
                    ERoomDirectionFlags dstBlockedFlags = dstRoom.BlockedConnections;
                    for (int j = 0; j < i; j++)
                    {
                        dstBlockedFlags = RoomDirectionFlagsUtility.RotateRight(dstBlockedFlags);
                    }
                    bool isAllowed = v.Item1 || vEmpty.Item1;
                    bool isBlocked = srcBlockedFlags.HasFlag(ERoomDirectionFlags.PositiveZ);
                    isBlocked |= dstBlockedFlags.HasFlag(ERoomDirectionFlags.NegativeZ);
                    Direction dir = GetDirection(i);
                    if (isBlocked && src == dst && srcRoom.Rotation == dstRoom.Rotation)
                    {
                        Vector2I dirVec = (dstRoom.Position - srcRoom.Position).Abs();
                        if (v2.Item1 && dirVec.X <= 1 && dirVec.Y <= 1 && dirVec.X != dirVec.Y)
                        {
                            _model?.AddAdjacency(tileSrc, tileDst, dir);
                            // log.PrintDebug($"src: {srcRoom}, dst: {dstRoom}, i: {i}");
                        }
                        continue;
                    }
                    if (isAllowed && !isBlocked)
                    {
                        _model?.AddAdjacency(tileSrc, tileDst, dir);
                        // log.PrintDebug($"src: {srcRoom}, dst: {dstRoom}, i: {i}");
                    }
                }
            }
        }
        return tiles;
    }
}