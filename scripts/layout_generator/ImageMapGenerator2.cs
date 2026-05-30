using System;
using Godot;

public partial class ImageMapGenerator2 : Node
{
    public enum CellType : byte
    {
        None = 0,
        GenericRoom = 1,
        SpecialRoom = 2,
        ReservedRoom = 3,
        Wall = 4,
    }

    public struct Cell
    {
        public CellType type;
        public ERoomDirectionFlags directionFlags;

        public Cell()
        {
            type = CellType.None;
            directionFlags = 0;
        }
    }

    public const int div = 3;

    [Export]
    public Vector2I size = Vector2I.One;
    [Export]
    public Image[] images = Array.Empty<Image>();
    [Export]
    public TextureRect tex;

    private RandomNumberGenerator rng;
    public Cell[] map;

    public readonly Vector2I[] directions = new Vector2I[]
    {
        Vector2I.Up,
        Vector2I.Right,
        Vector2I.Down,
        Vector2I.Left,
    };

    public readonly ERoomDirectionFlags[] roomDirectionFlags = new ERoomDirectionFlags[]
    {
        ERoomDirectionFlags.NegativeZ,
        ERoomDirectionFlags.PositiveX,
        ERoomDirectionFlags.PositiveZ,
        ERoomDirectionFlags.NegativeX,
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
        size = image.GetSize() / div;
        map = new Cell[size.X * size.Y];
        Array.Fill(map, new Cell());

        var door = Color.FromHtml("ff0");
        var generic = Color.FromHtml("f00");
        var special = Color.FromHtml("0f0");
        for (int y = 0; y < size.Y; y++)
        {
            for (int x = 0; x < size.X; x++)
            {
                Vector2I pixelPos = new Vector2I(x * div + 1, y * div + 1);
                Color color = image.GetPixelv(pixelPos);
                int index = x + y * size.X;
                if (IsEqualApprox(color, generic))
                    map[index].type = CellType.GenericRoom;
                else if (IsEqualApprox(color, special))
                    map[index].type = CellType.SpecialRoom;
                else
                    map[index].type = CellType.None;
                map[index].directionFlags = 0;
                for (int i = 0; i < roomDirectionFlags.Length; i++)
                {
                    Color dirColor = image.GetPixelv(pixelPos + directions[i]);
                    if (IsEqualApprox(dirColor, door))
                        map[index].directionFlags |= roomDirectionFlags[i];
                }
            }
        }

        if (IsInstanceValid(tex))
            Display();

        return image;
    }

    private void Display()
    {
        Image img = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgb8);
        for (int y = 0; y < size.Y; y++)
        {
            for (int x = 0; x < size.X; x++)
            {
                Color color = Colors.Black;
                switch (map[x + y * size.X].type)
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

    public Cell TryGet(Vector2I v)
    {
        if (v.X >= 0 && v.X < size.X && v.Y >= 0 && v.Y < size.Y)
        {
            return map[v.X + v.Y * size.X];
        }
        else
        {
            // Log.PrintErr("Hit a wall!");
            return new Cell()
            {
                type = CellType.Wall,
                directionFlags = 0,
            };
        }
    }
}
