using Godot;

// diffusion limited aggregation
// https://www.youtube.com/watch?v=gsJHzBTPG0Y
public partial class DLAMapGenerator : Node
{
    public struct Cell
    {
        public ERoomDirectionFlags directionFlags;

        public Cell()
        {
            directionFlags = 0;
        }
    }

    [Export]
    public Vector2I startSize = Vector2I.One * 16;
    [Export]
    public int iterations = 3;
    [Export]
    public int pixelCount = 10;
    [Export]
    public TextureRect rect;

    public readonly Vector2I[] directions = new Vector2I[]
    {
        Vector2I.Up,
        Vector2I.Right,
        Vector2I.Down,
        Vector2I.Left,
    };

    public RandomNumberGenerator rng;

    public override void _Ready()
    {
        rect.Texture = ImageTexture.CreateFromImage(Generate(GD.Randi()));
    }

    public Image Generate(ulong seed)
    {
        rng = new RandomNumberGenerator();
        rng.Seed = seed;
        Image img = Image.CreateEmpty(startSize.X, startSize.Y, false, Image.Format.Rgba8);
        img.Fill(Colors.Black);
        Vector2I pos = img.GetSize() / 2;
        img.SetPixelv(pos, Colors.White);
        for (int j = 0; j < iterations; j++)
        {
            for (int i = 0; i < pixelCount; i++)
                PlacePixel(img);
            if (j + 1 < iterations)
                img = Upscale(img);
        }
        return Finish(img);
    }

    public Image Upscale(Image img)
    {
        Image newImg = Image.CreateEmpty(img.GetWidth() * 2, img.GetHeight() * 2, false, Image.Format.Rgba8);
        newImg.Fill(Colors.Black);
        for (int x = 0; x < img.GetWidth(); x++)
        {
            for (int y = 0; y < img.GetHeight(); y++)
            {
                Color c = img.GetPixel(x, y);
                newImg.SetPixel(x * 2, y * 2, c);
                if (x + 1 < img.GetWidth() && !IsEqualApprox(img.GetPixel(x + 1, y), Colors.Black))
                {
                    newImg.SetPixel(x * 2 + 1, y * 2, c);
                }
                if (y + 1 < img.GetHeight() && !IsEqualApprox(img.GetPixel(x, y + 1), Colors.Black))
                {
                    newImg.SetPixel(x * 2, y * 2 + 1, c);
                }
            }
        }
        return newImg;
    }

    public Image Finish(Image img)
    {
        Image newImg = Image.CreateEmpty(img.GetWidth() * 3, img.GetHeight() * 3, false, Image.Format.Rgba8);
        newImg.Fill(Colors.Black);
        for (int x = 0; x < img.GetWidth(); x++)
        {
            for (int y = 0; y < img.GetHeight(); y++)
            {
                Color c = img.GetPixel(x, y);
                newImg.SetPixel(x * 3 + 1, y * 3 + 1, c);
                if (x - 1 >= 0 && !IsEqualApprox(img.GetPixel(x - 1, y), Colors.Black))
                {
                    newImg.SetPixel(x * 3 + 0, y * 3 + 1, c);
                }
                if (y - 1 >= 0 && !IsEqualApprox(img.GetPixel(x, y - 1), Colors.Black))
                {
                    newImg.SetPixel(x * 3 + 1, y * 3 + 0, c);
                }
                if (x + 1 < img.GetWidth() && !IsEqualApprox(img.GetPixel(x + 1, y), Colors.Black))
                {
                    newImg.SetPixel(x * 3 + 2, y * 3 + 1, c);
                }
                if (y + 1 < img.GetHeight() && !IsEqualApprox(img.GetPixel(x, y + 1), Colors.Black))
                {
                    newImg.SetPixel(x * 3 + 1, y * 3 + 2, c);
                }
            }
        }
        return newImg;
    }

    public void PlacePixel(Image img)
    {
        Vector2I pos;
        do
        {
            pos = new Vector2I(rng.RandiRange(0, img.GetWidth() - 1), rng.RandiRange(0, img.GetHeight() - 1));
        }
        while (!IsEqualApprox(img.GetPixelv(pos), Colors.Black));
        while (true)
        {
            bool found = false;
            for (int i = 0; i < directions.Length; i++)
            {
                if (!IsInImage(img, pos) || !IsInImage(img, pos + directions[i]))
                    continue;
                Color pix = img.GetPixelv(pos + directions[i]);
                if (!IsEqualApprox(pix, Colors.Black))
                {
                    img.SetPixelv(pos, Colors.White);
                    found = true;
                    break;
                }
            }
            if (found)
                break;
            Vector2I dir = directions[rng.Randi() % directions.Length];
            if (IsInImage(img, pos + dir))
                pos += dir;
        }
    }

    public static bool IsInImage(Image img, Vector2I pos)
    {
        return pos.X >= 0 && pos.X < img.GetWidth() && pos.Y >= 0 && pos.Y < img.GetHeight();
    }

    public static bool IsEqualApprox(Color a, Color b, float t = 5f / 255f)
    {
        return Mathf.IsEqualApprox(a.R, b.R, t) && Mathf.IsEqualApprox(a.G, b.G, t) && Mathf.IsEqualApprox(a.B, b.B, t);
    }
}