using System;
using System.Collections.Generic;
using Godot;

public partial class ImageMapGenerator : Node
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
    public Vector2I size = Vector2I.One;
    [Export]
    public Image[] images = Array.Empty<Image>();
    [Export]
    public TextureRect tex;

    private RandomNumberGenerator rng;
    public CellType[] map;

    public readonly Vector2I[] directions = new Vector2I[]
    {
        Vector2I.Up,
        Vector2I.Right,
        Vector2I.Down,
        Vector2I.Left,
    };

    public override void _Ready()
    {
        // Generate(GD.Randi());
    }

    public static bool IsEqualApprox(Color a, Color b, float t = 5f / 255f)
    {
        if (Mathf.IsEqualApprox(a.R, b.R, t) && Mathf.IsEqualApprox(a.G, b.G, t) && Mathf.IsEqualApprox(a.B, b.B, t))
        {
            return true;
        }
        return false;
    }

    public Image Generate(ulong seed)
    {
        Log.Print(seed);
        rng = new RandomNumberGenerator();
        rng.Seed = seed;
        Image image = images[rng.Randi() % images.Length];
        size = image.GetSize();
        map = new CellType[size.X * size.Y];
        Array.Fill(map, CellType.None);

        var generic = Color.FromHtml("f00");
        var special = Color.FromHtml("0f0");
        for (int y = 0; y < size.Y; y++)
        {
            for (int x = 0; x < size.X; x++)
            {
                Color color = image.GetPixel(x, y);
                int index = x + y * size.X;
                if (IsEqualApprox(color, generic))
                    map[index] = CellType.GenericRoom;
                else if (IsEqualApprox(color, special))
                    map[index] = CellType.SpecialRoom;
                else
                    map[index] = CellType.None;
                /*
                switch (color.ToHtml(false).ToLower())
                {
                    default:
                        map[index] = CellType.None;
                        break;
                    case "ff0000":
                        map[index] = CellType.GenericRoom;
                        break;
                    case "00ff00":
                        map[index] = CellType.SpecialRoom;
                        break;
                }
                */
            }
        }

        if (IsInstanceValid(tex))
            Display();

        return image;
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
                        color = Colors.Green;
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
