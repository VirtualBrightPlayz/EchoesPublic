using System;
using System.Collections.Generic;
using Godot;

// drunken walk map generator
public partial class DrunkMapGenerator : Node
{
    [Export]
    public Vector2I size = Vector2I.One * 10;
    [Export]
    public int numRuns = 2;
    [Export]
    public int numPaths = 4;
    [Export]
    public int numStumbles = 4;
    [Export]
    public int minPathSizes = 1;
    [Export]
    public int maxPathSizes = 4;
    [Export]
    public int pukeChance = 3;
    [Export]
    public TextureRect tex;

    private RandomNumberGenerator rng;
    public bool[] map;
    public List<List<Vector2I>> paths = new List<List<Vector2I>>();
    private List<Vector2I> path = new List<Vector2I>();
    private Rect2I bounds;
    public Rect2I BoundingBox => bounds;

    public readonly Vector2I[] directions = new Vector2I[]
    {
        Vector2I.Up,
        Vector2I.Right,
        Vector2I.Down,
        Vector2I.Left,
    };

    private Vector2I walkPosition;
    private int walkDirection;

    public override void _Ready()
    {
        // Generate(GD.Randi());
    }

    public bool[] Generate(ulong seed)
    {
        Log.Print(seed);
        rng = new RandomNumberGenerator();
        rng.Seed = seed;
        map = new bool[size.X * size.Y];
        Array.Fill(map, false);
        path.Clear();
        paths.Clear();
        bounds = new Rect2I(size / 2, Vector2I.Zero);

        paths.Add(new List<Vector2I>());
        TryPlace(size / 2);

        walkPosition = size / 2;
        walkDirection = (int)(rng.Randi() % 4);
        Run();

        for (int j = 1; j < numRuns; j++)
        {
            paths.Add(new List<Vector2I>());
            walkPosition = path[path.Count / 2];
            walkDirection = (int)(rng.Randi() % 4);
            for (int i = 0; i < 4; i++)
            {
                if (!TryGet(walkPosition + directions[i]))
                {
                    walkDirection = i;
                    break;
                }
            }
            Run();
        }

        if (IsInstanceValid(tex))
            Display();

        return map;
    }

    private void Display()
    {
        Image img = Image.Create(size.X, size.Y, false, Image.Format.R8);
        for (int y = 0; y < size.Y; y++)
            for (int x = 0; x < size.X; x++)
                img.SetPixel(x, y, map[x + y * size.X] ? Colors.Red : Colors.Black);
        tex.Texture = ImageTexture.CreateFromImage(img);
    }

    private void Run()
    {
        for (int i = 0; i < numPaths; i++)
        {
            for (int j = 0; j <= numStumbles; j++)
            {
                Walk(rng.RandiRange(minPathSizes, maxPathSizes + 1));
                if (pukeChance > 0 && rng.Randi() % pukeChance == 0)
                    Puke(rng.Randi() % 2 == 0);
                else if (j != numStumbles)
                    Stumble(rng.Randi() % 2 == 0);
            }
            if (rng.Randi() % 2 == 0)
                walkDirection = (walkDirection + 1) % 4;
            else
                walkDirection = (walkDirection + 3) % 4;
        }
    }

    private void Puke(bool right)
    {
        int dir = walkDirection;
        if (right)
            dir = (dir + 1) % 4;
        else
            dir = (dir + 3) % 4;
        Vector2I pos = walkPosition + directions[dir];
        TryPlace(pos);
    }

    private void Stumble(bool right)
    {
        int dir = walkDirection;
        if (right)
            dir = (dir + 1) % 4;
        else
            dir = (dir + 3) % 4;
        walkPosition += directions[dir];
        TryPlace(walkPosition);
    }

    private void Walk(int length)
    {
        for (int i = 0; i < length; i++)
        {
            walkPosition += directions[walkDirection];
            TryPlace(walkPosition);
        }
    }

    private bool TryPlace(Vector2I v)
    {
        if (v.X >= 0 && v.X < size.X && v.Y >= 0 && v.Y < size.Y)
        {
            map[v.X + v.Y * size.X] = true;
            bounds = bounds.Expand(v);
            path.Add(v);
            paths[^1].Add(v);
            return true;
        }
        else
        {
            Log.PrintErr("Hit a wall!");
            return false;
        }
    }

    public bool TryGet(Vector2I v)
    {
        if (v.X >= 0 && v.X < size.X && v.Y >= 0 && v.Y < size.Y)
        {
            return map[v.X + v.Y * size.X];
        }
        else
        {
            Log.PrintErr("Hit a wall!");
            return true;
        }
    }
}
