using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ShaderPreloader : Node
{
    public Queue<string> fileQueue = new Queue<string>();
    public Queue<PackedScene> sceneQueue = new Queue<PackedScene>();
    public int fileCount;
    public Node menu;
    public string[] loadingTexts = ["\\", "-", "/", "|", "-"];
    public Node root;
    public string currentFile;

    public override void _Ready()
    {
        base._Ready();
        menu = GetParent();
        PreloadShaders();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (IsInstanceValid(root))
        {
            root.QueueFree();
            return;
        }
        if (!string.IsNullOrEmpty(currentFile))
        {
            var status = ResourceLoader.LoadThreadedGetStatus(currentFile);
            if (status != ResourceLoader.ThreadLoadStatus.InProgress)
            {
                Resource res = ResourceLoader.LoadThreadedGet(currentFile);
                if (res is PackedScene scn)
                {
                    sceneQueue.Enqueue(scn);
                }
                currentFile = null;
            }
        }
        else if (fileQueue.TryDequeue(out string file))
        {
            LoadScene(file, fileQueue.Count + fileCount);
        }
        else if (sceneQueue.TryDequeue(out PackedScene scene))
        {
            RenderScene(scene, sceneQueue.Count);
        }
        else
        {
            QueueFree();
        }
    }

    public void PreloadShaders()
    {
        menu.GetNode<Label>("%Label").Text = "Loading files [...]";
        List<string> files = new List<string>();
        FindScenes("res://scenes/rooms/", files);
        FindScenes("res://scenes/misc/", files);
        FindScenes("res://scenes/player/models/", files);
        fileQueue = new Queue<string>(files);
        sceneQueue = new Queue<PackedScene>();
        fileCount = files.Count;
        var cam = new Camera3D();
        menu.AddChild(cam);
        cam.MakeCurrent();
    }

    public void FindScenes(string dir, List<string> files)
    {
        var dirList = ResourceLoader.ListDirectory(dir);
        foreach (var item in dirList)
        {
            if (!item.EndsWith('/'))
            {
                continue;
            }
            FindScenes(dir.PathJoin(item), files);
        }
        files.AddRange(dirList.Where(x => x.EndsWith("scn")).Select(x => dir.PathJoin(x)));
        return;
    }

    public void LoadScene(string item, int count)
    {
        int index = count % loadingTexts.Length;
        menu.GetNode<Label>("%Label").Text = $"Loading files [{loadingTexts[index]}]\n{count} remaining.";
        GD.PrintS(count, item);
        ResourceLoader.LoadThreadedRequest(item);
        currentFile = item;
    }

    public void RenderScene(PackedScene scn, int count)
    {
        int index = count % loadingTexts.Length;
        menu.GetNode<Label>("%Label").Text = $"Rendering files [{loadingTexts[index]}]\n{count} remaining.";
        GD.PrintS(count, scn.ResourcePath);
        {
            Node node = scn.Instantiate();
            if (IsInstanceValid(node))
            {
                root = new Node();
                root.Name = node.Name;
                root.ProcessMode = ProcessModeEnum.Disabled;
                node.ProcessMode = ProcessModeEnum.Disabled;
                if (node is CanvasItem ci)
                {
                    ci.Visible = false;
                }
                foreach (var child in node.FindChildren("*", "", true, false).ToArray())
                {
                    if (IsInstanceValid(child))
                    {
                        child.ProcessMode = ProcessModeEnum.Disabled;
                        child.SetScript(default);
                    }
                }
                root.AddChild(node);
                node.SetScript(default);
                menu.AddChild(root);
                /*
                if (IsInstanceValid(root))
                {
                    GetTree().ProcessFrame += () =>
                    {
                        if (IsInstanceValid(root))
                        {
                            root.QueueFree();
                        }
                    };
                }
                */
            }
            else
            {
                GD.PrintErr($"Load Scene Failed: {count} {scn.ResourcePath}");
            }
        }
    }
}