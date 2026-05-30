using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeBroglie;
using DeBroglie.Constraints;
using DeBroglie.Models;
using DeBroglie.Rot;
using DeBroglie.Topo;
using DeBroglie.Wfc;
using Godot;

public partial class LayoutGeneratorDeBroglie : Node
{
    public AdjacentModel model;
    public GridTopology topology;

    public class RoomSettingsRotated
    {
        public RoomGenerationSettings Settings { get; private set; }
        public int Rotation { get; set; }

        public RoomSettingsRotated(RoomGenerationSettings settings, int rotation)
        {
            Settings = settings;
            Rotation = rotation;
        }

        public override string ToString()
        {
            return $"({Settings.ResourcePath}, {Rotation})";
        }
    }

    public static Direction[] GetDirections(ERoomDirectionFlags dir)
    {
        List<Direction> dirs = new List<Direction>();
        if (dir.HasFlag(ERoomDirectionFlags.PositiveX))
            dirs.Add(Direction.XPlus);
        if (dir.HasFlag(ERoomDirectionFlags.PositiveZ))
            dirs.Add(Direction.YPlus);
        if (dir.HasFlag(ERoomDirectionFlags.NegativeX))
            dirs.Add(Direction.XMinus);
        if (dir.HasFlag(ERoomDirectionFlags.NegativeZ))
            dirs.Add(Direction.YMinus);
        return dirs.ToArray();
    }

    public static ERoomDirectionFlags[] GetDirectionFlags(ERoomDirectionFlags dir)
    {
        List<ERoomDirectionFlags> dirs = new List<ERoomDirectionFlags>();
        if (dir.HasFlag(ERoomDirectionFlags.PositiveX))
            dirs.Add(ERoomDirectionFlags.PositiveX);
        if (dir.HasFlag(ERoomDirectionFlags.PositiveZ))
            dirs.Add(ERoomDirectionFlags.PositiveZ);
        if (dir.HasFlag(ERoomDirectionFlags.NegativeX))
            dirs.Add(ERoomDirectionFlags.NegativeX);
        if (dir.HasFlag(ERoomDirectionFlags.NegativeZ))
            dirs.Add(ERoomDirectionFlags.NegativeZ);
        return dirs.ToArray();
    }

    public static Direction InvertDirection(Direction dir)
    {
        switch (dir)
        {
            case Direction.XPlus:
                return Direction.XMinus;
            case Direction.YPlus:
                return Direction.YMinus;
            case Direction.XMinus:
                return Direction.XPlus;
            case Direction.YMinus:
                return Direction.YPlus;
            default:
                return Direction.XPlus;
        }
    }

    public static int[] GetRotations(ERoomDirectionFlags dir)
    {
        List<int> dirs = new List<int>();
        if (dir.HasFlag(ERoomDirectionFlags.PositiveX))
            dirs.Add(90);
        if (dir.HasFlag(ERoomDirectionFlags.PositiveZ))
            dirs.Add(0);
        if (dir.HasFlag(ERoomDirectionFlags.NegativeX))
            dirs.Add(270);
        if (dir.HasFlag(ERoomDirectionFlags.NegativeZ))
            dirs.Add(180);
        return dirs.ToArray();
    }

    public static int GetRotation(Direction dir)
    {
        switch (dir)
        {
            case Direction.XPlus:
                return 90;
            case Direction.YPlus:
                return 0;
            case Direction.XMinus:
                return 270;
            case Direction.YMinus:
                return 180;
            default:
                return 0;
        }
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

    public static int GetRotation(Rotation rot)
    {
        if (rot.ReflectX)
        {
            Log.PrintErr("mirror");
            return (rot.RotateCw + 180) / 90;
        }
        return rot.RotateCw / 90;
    }

    public static (bool, int) IsConnected(int r, ERoomDirectionFlags a, int r2, ERoomDirectionFlags b, int r3, bool allowConnected)
    {
        var a2 = a;
        var b2 = b;
        for (int i = 0; i < r + r3; i++)
        {
            a2 = RoomDirectionFlagsUtility.RotateRight(a2);
        }
        for (int i = 0; i < r2 + r3; i++)
        {
            b2 = RoomDirectionFlagsUtility.RotateRight(b2);
        }
        var zp = IsZPConnected(a2, b2);// || IsZPNotConnected(a2, b2);
        var zn = IsZPConnected(b2, a2);// || IsZPNotConnected(b2, a2);
        var xp = IsXPConnected(a2, b2);// || IsXPNotConnected(a2, b2);
        var xn = IsXPConnected(b2, a2);// || IsXPNotConnected(b2, a2);

        var zp2 = IsZPNotConnected(a2, b2);
        var zn2 = IsZPNotConnected(b2, a2);
        var xp2 = IsXPNotConnected(a2, b2);
        var xn2 = IsXPNotConnected(b2, a2);

        var arr = new[] { zp, zn, xp, xn };
        var arr2 = new[] { zp2, zn2, xp2, xn2 };
        var arr3 = new[] { zn2, zp2, xn2, xp2 };
        var c = arr.Count(x => x);
        var c2 = arr2.Count(x => x);
        var mask = new bool[4];
        for (int i = 0; i < 4; i++)
        {
            mask[i] = arr[i] || arr3[i];
        }
        var cm = mask.Count(x => x);
        if (allowConnected)
        {
            if (zp || zp2)
            {
                return (true, cm);
            }
        }
        else
        {
            if (!a2.HasFlag(ERoomDirectionFlags.PositiveZ) || b2 != 0)
            {
                return (true, cm);
            }
        }
        return (false, 0);
    }

    public static bool IsZPConnected(ERoomDirectionFlags a, ERoomDirectionFlags b)
    {
        return a.HasFlag(ERoomDirectionFlags.PositiveZ) && b.HasFlag(ERoomDirectionFlags.NegativeZ);
    }

    public static bool IsXPConnected(ERoomDirectionFlags a, ERoomDirectionFlags b)
    {
        return a.HasFlag(ERoomDirectionFlags.PositiveX) && b.HasFlag(ERoomDirectionFlags.NegativeX);
    }

    public static bool IsZPNotConnected(ERoomDirectionFlags a, ERoomDirectionFlags b)
    {
        return !a.HasFlag(ERoomDirectionFlags.PositiveZ) && !b.HasFlag(ERoomDirectionFlags.NegativeZ);
    }

    public static bool IsXPNotConnected(ERoomDirectionFlags a, ERoomDirectionFlags b)
    {
        return !a.HasFlag(ERoomDirectionFlags.PositiveX) && !b.HasFlag(ERoomDirectionFlags.NegativeX);
    }

    public List<Tile> AddCollection(HashSet<Tile> existingTiles, RoomGenerationSettingsCollection settingCollection)
    {
        List<Tile> tiles = new List<Tile>();
        foreach (var setting in settingCollection.Collection)
        {
            var arr = existingTiles.Where(x => ((RoomSettingsRotated)x.Value).Settings == setting);
            if (arr.Any())
            {
                tiles.AddRange(arr);
                continue;
            }
            var tile = new Tile(new RoomSettingsRotated(setting, 0));
            tiles.Add(tile);
            for (int i = 1; i < 4; i++)
            {
                var room = new RoomSettingsRotated(setting, i);
                var t = new Tile(room);
                tiles.Add(t);
            }
        }

        foreach (var tileSrc in tiles)
        {
            var srcRoom = (RoomSettingsRotated)tileSrc.Value;
            var src = srcRoom.Settings;
            model.SetFrequency(tileSrc, src.SpawnPoolEntries);
            foreach (var tileDst in tiles)
            {
                var dstRoom = (RoomSettingsRotated)tileDst.Value;
                var dst = dstRoom.Settings;

                for (int i = 0; i < 4; i++)
                {
                    var v = IsConnected(srcRoom.Rotation, src.Connections, dstRoom.Rotation, dst.Connections, i, true);
                    var a2 = src.BlockedNeighbours;
                    for (int k = 0; k < srcRoom.Rotation + i; k++)
                    {
                        a2 = RoomDirectionFlagsUtility.RotateRight(a2);
                    }
                    var b2 = dst.BlockedNeighbours;
                    for (int k = 0; k < dstRoom.Rotation + i; k++)
                    {
                        b2 = RoomDirectionFlagsUtility.RotateRight(b2);
                    }
                    var bo = a2.HasFlag(ERoomDirectionFlags.PositiveZ) && dst.Connections != 0;
                    bo = bo || b2.HasFlag(ERoomDirectionFlags.NegativeZ) && src.Connections != 0;
                    if (v.Item1 && !bo)
                    {
                        var dir = GetDirection(i);
                        model.AddAdjacency(new[] { tileSrc }, new[] { tileDst }, dir);
                        // Log.PrintInfo($"src: {src.ResourcePath}, dst: {dst.ResourcePath}, rot: {srcRoom.Rotation}, rot2: {dstRoom.Rotation}, i: {i}, v: {v.Item2}");
                    }
                    else if (v.Item1)
                    {
                        // Log.PrintInfo($"src: {src.ResourcePath}, dst: {dst.ResourcePath}, rot: {srcRoom.Rotation}, rot2: {dstRoom.Rotation}, i: {i}");
                    }
                }
            }
        }
        return tiles;
    }

    public ELayoutGenerationResult Generate(Layout layout, RoomGenerationSettingsCollection[] settingCollections, ulong seed)
    {
        model = new AdjacentModel();
        model.SetDirections(DirectionSet.Cartesian2d);

        HashSet<Tile> tiles = new HashSet<Tile>();
        foreach (var collection in settingCollections)
        {
            var result = AddCollection(tiles, collection);
            tiles.UnionWith(result);
        }
        // tiles = tiles.Distinct().ToList();

        var constraints = new List<ITileConstraint>();
        constraints.Add(new BorderConstraint()
        {
            Ban = false,
            InvertArea = false,
            Sides = BorderSides.XMin | BorderSides.XMax | BorderSides.YMin | BorderSides.YMax,
            Tiles = tiles.Where(x => ((RoomSettingsRotated)x.Value).Settings.Connections == 0).ToArray(),
        });

        foreach (var tile in tiles)
        {
            var room = (RoomSettingsRotated)tile.Value;
            if (room.Settings.Connections == 0)
                continue;
            if (room.Settings.MaxConsecutive >= 0)
            {
                constraints.Add(new MaxConsecutiveConstraint()
                {
                    MaxCount = room.Settings.MaxConsecutive,
                    Tiles = tiles.Where(x => ((RoomSettingsRotated)x.Value).Settings == room.Settings).ToHashSet(),
                });
            }
            if (room.Settings.Test)
            {
                constraints.Add(new LayoutConstraints()
                {
                    tile = tile,
                    empty = tiles.First(x => ((RoomSettingsRotated)x.Value).Settings.Connections == 0),
                });
            }
        }

        var dict = new Dictionary<Tile, ISet<Direction>>();
        foreach (var tile in tiles)
        {
            var room = (RoomSettingsRotated)tile.Value;
            if (room.Settings.Connections == 0)
                continue;
            if (!dict.ContainsKey(tile))
                dict.Add(tile, new HashSet<Direction>());
            var a2 = room.Settings.Connections;
            for (int i = 0; i < room.Rotation; i++)
            {
                a2 = RoomDirectionFlagsUtility.RotateRight(a2);
            }
            dict[tile].UnionWith(GetDirections(a2));
        }
        // /*
        constraints.Add(new ConnectedConstraint()
        {
            UsePickHeuristic = true,
            PathSpec = new EdgedPathSpec()
            {
                Exits = dict,
            },
        });
        // */

        foreach (var room in settingCollections.SelectMany(x => x.Collection))
        {
            var lst = new HashSet<Tile>();
            lst.UnionWith(tiles.Where(x => ((RoomSettingsRotated)x.Value).Settings == room));
            if (room.MaximumInstances == room.RequiredInstances && room.RequiredInstances > 0)
            {
                constraints.Add(new CountConstraint()
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
                    constraints.Add(new CountConstraint()
                    {
                        Comparison = CountComparison.AtLeast,
                        Tiles = lst,
                        Count = room.RequiredInstances,
                    });
                }
                if (room.MaximumInstances != -1 && room.MaximumInstances >= room.RequiredInstances)
                {
                    constraints.Add(new CountConstraint()
                    {
                        Comparison = CountComparison.AtMost,
                        Tiles = lst,
                        Count = room.MaximumInstances,
                    });
                }
            }
        }

        var topo = new GridTopology(layout.GridSize.X, layout.GridSize.Y, false);
        var rng = new RandomNumberGenerator();
        rng.Seed = seed;
        var propagator = new TilePropagator(model, topo, new TilePropagatorOptions()
        {
            Constraints = constraints.ToArray(),
            // RandomDouble = () => rng.RandfRange(-10_000f, 10_000f),
            RandomDouble = () => rng.Randf(),
            BacktrackType = BacktrackType.Backjump,
            MaxBacktrackDepth = 15,
            // ModelConstraintAlgorithm = ModelConstraintAlgorithm.OneStep,
            // IndexPickerType = IndexPickerType.Default,
            // MemoizeIndices = true,
        });
        // var status = propagator.Run();
        // /*
        var status = propagator.Step();
        int k = 0;
        while (status == Resolution.Undecided && k < 2_000)
        {
            status = propagator.Step();
            if (!IsInstanceValid(this))
            {
                break;
            }
            k++;
        }
        // */
        if (status != Resolution.Decided)
        {
            Log.Print($"Undecided: {status}");
            return ELayoutGenerationResult.Other;
        }
        var output = propagator.ToArray();
        for (int y = 0; y < layout.GridSize.Y; y++)
        {
            for (int x = 0; x < layout.GridSize.X; x++)
            {
                var cell = layout.GetCell(new Vector2I(x, y));
                var tile = output.Get(x, y);
                RoomGenerationSettings room = null;
                int dir = 0;
                /*if (tile.Value is RoomGenerationSettings)
                {
                    room = (RoomGenerationSettings)tile.Value;
                }
                else if (tile.Value is RotatedTile rotatedTile)
                {
                    room = (RoomGenerationSettings)rotatedTile.Tile.Value;
                    dir = GetRotation(rotatedTile.Rotation);
                }
                else*/ if (tile.Value is RoomSettingsRotated rotatedRoom)
                {
                    room = rotatedRoom.Settings;
                    // if (rotatedRoom.Rotation == 0 || rotatedRoom.Rotation == 2)
                        // dir = (rotatedRoom.Rotation + 2) % 4;
                    // else
                        dir = rotatedRoom.Rotation;
                }
                else
                {
                    if (tile.Value != null)
                        Log.PrintWarn("Tile is ", tile.Value.GetType().FullName);
                    else
                        Log.PrintWarn("Tile is null");
                    continue;
                }
                cell.SetUniqueCellSeed(0);
                cell.SetRoomGenerationResource(room, dir);
            }
        }
        for (int y = 0; y < layout.GridSize.Y; y++)
        {
            for (int x = 0; x < layout.GridSize.X; x++)
            {
                var cell = layout.GetCell(new Vector2I(x, y));
                if (cell.RoomGenerationResource != null)
                {
                    foreach (var post in cell.RoomGenerationResource.PostSpawnValidatorResources)
                    {
                        if (!post.IsSpawnValid(layout, cell))
                        {
                            return ELayoutGenerationResult.PostSpawnValidationFailed;
                        }
                    }
                }
            }
        }
        return ELayoutGenerationResult.Success;
    }
}
