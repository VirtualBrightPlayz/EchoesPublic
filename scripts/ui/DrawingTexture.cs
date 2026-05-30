using System.Threading.Tasks;
using Godot;

public partial class DrawingTexture : TextureRect
{
    [Export]
    public Vector2I size = Vector2I.One * 1024;
    [Export]
    public bool useRect = false;

    [Export]
    public Control[] hideOnDraw = new Control[0];

    public Image image;
    public Vector2I lastPosition;
    public float currentColorHue = 0f;

    public override void _Ready()
    {
        base._Ready();
        Reset();
    }

    public void Reset()
    {
        if (useRect)
            size = (Vector2I)GetRect().Size;
        foreach (var item in hideOnDraw)
            item.Show();
        Hide();
    }

    public async void ShowTexture()
    {
        Hide();
        image = null;
        while (!IsInstanceValid(image))
        {
            await ToSignal(RenderingServer.Singleton, RenderingServerInstance.SignalName.FramePostDraw);
            image = GetViewport().GetTexture().GetImage();
            foreach (var item in hideOnDraw)
                item.Hide();
        }
        Show();
        Texture = ImageTexture.CreateFromImage(image);
        // image = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
    }

    public override void _GuiInput(InputEvent ev)
    {
        if (ev is InputEventMouseButton button)
        {
            var rect = GetRect();
            Vector2 vec = button.Position;
            vec /= rect.Size;
            vec *= size;
            if (button.ButtonIndex == MouseButton.Left || button.ButtonIndex == MouseButton.Right)
            {
                lastPosition = (Vector2I)vec;
            }
            else if (button.ButtonIndex == MouseButton.WheelUp)
            {
                currentColorHue += 5f / 360f;
                currentColorHue %= 1f;
            }
            else if (button.ButtonIndex == MouseButton.WheelDown)
            {
                currentColorHue += 1f - 5f / 360f;
                currentColorHue %= 1f;
            }
        }
        if (ev is InputEventMouseMotion motion)
        {
            var rect = GetRect();
            Vector2 vec = motion.Position;
            vec /= rect.Size;
            vec *= size;
            if (motion.ButtonMask.HasFlag(MouseButtonMask.Left))
            {
                DrawLine(lastPosition, (Vector2I)vec, Color.FromHsv(currentColorHue, 1f, 1f), 2);
                AcceptEvent();
            }
            else if (motion.ButtonMask.HasFlag(MouseButtonMask.Right))
            {
                DrawLine(lastPosition, (Vector2I)vec, Colors.Transparent, 16);
                AcceptEvent();
            }
            lastPosition = (Vector2I)vec;
        }
        if (IsInstanceValid(Texture) && Texture is ImageTexture tex)
        {
            tex.Update(image);
        }
        else
        {
            Texture = ImageTexture.CreateFromImage(image);
        }
    }

    public void DrawLine(Vector2I from, Vector2I to, Color color, int size = 4)
    {
        foreach (var pt in Geometry2D.BresenhamLine(from, to))
        {
            for (int x = -size; x <= size; x++)
            {
                for (int y = -size; y <= size; y++)
                {
                    Vector2I ptfin = pt + new Vector2I(x, y);
                    if (ptfin.X < 0 || ptfin.X >= image.GetWidth() || ptfin.Y < 0 || ptfin.Y >= image.GetHeight())
                    {
                        continue;
                    }
                    image.SetPixelv(ptfin, color);
                }
            }
        }
    }
}
