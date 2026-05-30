using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class MazeMapGenerator : Node
{
    public enum CellType : byte
    {
        None = 0,
        GenericRoom = 1,
        SpecialRoom = 2,
        ReservedRoom = 3,
        Wall = 4,
    }

    [Export]
    public Vector2I size = Vector2I.One * 10;
    [Export]
    public TextureRect tex;

    private RandomNumberGenerator rng;
    private CellType[] map;
    
    [Export]
    public RoomGenerationSettingsCollection settingsCollection;

    public readonly Vector2I[] allDirections = new Vector2I[]
    {
        Vector2I.Up,
        Vector2I.Right,
        Vector2I.Down,
        Vector2I.Left,
    };
    public readonly ERoomDirectionFlags[] allFlags = new ERoomDirectionFlags[]
    {
        ERoomDirectionFlags.NegativeZ,
        ERoomDirectionFlags.PositiveX,
        ERoomDirectionFlags.PositiveZ,
        ERoomDirectionFlags.NegativeX,
    };

    public override void _Ready()
    {
        // Generate(0, settingsCollection.Collection.ToArray());
    }

    public int[] GetDirections(ERoomDirectionFlags flags)
    {
        List<int> dirs = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            if (flags.HasFlag(allFlags[i]))
            {
                dirs.Add(i);
            }
        }
        return dirs.ToArray();
    }

    public CellType[] Generate(ulong seed, RoomGenerationSettings[] settings)
    {
        Log.Print(seed);
        rng = new RandomNumberGenerator();
        rng.Seed = seed;
        map = new CellType[size.X * size.Y];
        Array.Fill(map, CellType.None);

        int div = 2;

        List<Vector2I> points = new List<Vector2I>();

        for (int i = 0; i < settings.Length; i++)
        {
            if (settings[i].RequiredInstances <= 0)
                continue;
            Vector2I pos = new Vector2I(rng.RandiRange(div, size.X - div) / div * div, rng.RandiRange(div, size.Y - div) / div * div);
            TryPlace(pos, CellType.SpecialRoom);
            int[] dirs = GetDirections(settings[i].Connections);
            foreach (var dir in dirs)
            {
                TryPlace(pos + allDirections[dir], CellType.GenericRoom);
                for (int j = 1; j > 0; j--)
                {
                    Vector2I pos2 = pos + allDirections[dir] * (j + 1);
                    CellType cell = TryGet(pos2);
                    if (cell == CellType.None)
                        TryPlace(pos2, CellType.GenericRoom);
                    else if (cell != CellType.GenericRoom)
                        break;
                }
            }
            points.Add(pos);
        }

        /*
        for (int y = div; y < size.Y - div; y+=div)
        {
            int maxFound = size.X - div * 2;
            int found = 0;
            for (int x = div; x < size.X - div; x++)
            {
                Vector2I pos0 = new Vector2I(x, y);
                Vector2I pos1 = new Vector2I(x, y - 1);
                CellType cell0 = TryGet(pos0);
                CellType cell1 = TryGet(pos1);
                if (cell0 == CellType.None && cell1 == CellType.None)
                {
                    found++;
                }
                else if (cell0 == CellType.None)
                {
                    found--;
                }
            }
            for (int x = div; x < size.X - div; x++)
            {
                Vector2I pos0 = new Vector2I(x, y);
                Vector2I pos1 = new Vector2I(x, y - 1);
                CellType cell0 = TryGet(pos0);
                CellType cell1 = TryGet(pos1);
                if (cell1 == CellType.None)
                {
                    if (found <= 0)
                        TryPlace(pos1, CellType.GenericRoom);
                }
                if (cell0 == CellType.None)
                {
                    if (found > 0)
                        TryPlace(pos0, CellType.GenericRoom);
                }
            }
        }
        */

        if (IsInstanceValid(tex))
            Display();

        return map;
    }

    private void Display()
    {
        Image img = Image.Create(size.X, size.Y, false, Image.Format.Rgb8);
        for (int y = 0; y < size.Y; y++)
        {
            for (int x = 0; x < size.X; x++)
            {
                Color color = Colors.Black;
                switch (map[x + y * size.X])
                {
                    case CellType.GenericRoom:
                        color = Colors.Red;
                        break;
                    case CellType.SpecialRoom:
                        color = Colors.Blue;
                        break;
                    case CellType.ReservedRoom:
                        color = Colors.Gray;
                        break;
                }
                img.SetPixel(x, y, color);
            }
        }
        tex.Texture = ImageTexture.CreateFromImage(img);
    }

    private bool TryPlace(Vector2I v, CellType type)
    {
        if (v.X >= 0 && v.X < size.X && v.Y >= 0 && v.Y < size.Y)
        {
            map[v.X + v.Y * size.X] = type;
            return true;
        }
        else
        {
            // Log.PrintErr("Hit a wall!");
            return false;
        }
    }

    public CellType TryGet(Vector2I v)
    {
        if (v.X >= 0 && v.X < size.X && v.Y >= 0 && v.Y < size.Y)
        {
            return map[v.X + v.Y * size.X];
        }
        else
        {
            // Log.PrintErr("Hit a wall!");
            return CellType.Wall;
        }
    }
}
