using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

[GlobalClass]
public partial class RoleHintUI : Node
{
    public static RoleHintUI Instance;

    [Export] public Control root;
    [Export] public Control keyRoot;
    [Export] public Label keyLabel;
    [Export] public TextureRect iconRect;
    [Export] public RichTextLabel hintLabel;
    [Export] public double timeOnScreen = 5d;
    [Export] public int maxQueueSize = 5;
    private Tween tween;
    private Queue<(string, string, Texture2D, double)> queue = new Queue<(string, string, Texture2D, double)>();

    public override void _Ready()
    {
        base._Ready();
        Hide();
        Instance = this;
    }

    private void Hide()
    {
        root.Visible = false;
        if (queue.TryDequeue(out var result))
        {
            ShowHintNow(result.Item1, result.Item2, result.Item3, result.Item4);
        }
    }

    private void Show()
    {
        // float x = root.GetViewportRect().Size.X - root.Size.X;
        // root.Position = new Vector2(x, 0);
        root.Modulate = Colors.White;
        if (IsInstanceValid(tween) && tween.IsValid())
            tween.Kill();
        tween = CreateTween();
        tween.TweenProperty(root, new NodePath(CanvasItem.PropertyName.Modulate), Colors.Transparent, timeOnScreen).From(Colors.White).SetDelay(1d);
        tween.TweenCallback(Callable.From(Hide));
        // tween.TweenCallback(Callable.From(StartHide)).SetDelay(timeOnScreen);
    }

    private void StartHide()
    {
        return;
        float x = root.GetViewportRect().Size.X - root.Size.X;
        if (IsInstanceValid(tween) && tween.IsValid())
            tween.Kill();
        tween = CreateTween();
        tween.TweenProperty(root, new NodePath(Control.PropertyName.Position), new Vector2(x, -root.Size.Y), 0.5d).From(new Vector2(x, 0));
        tween.TweenCallback(Callable.From(Hide));
    }

    public void ClearQueue()
    {
        queue.Clear();
        if (IsInstanceValid(tween) && tween.IsValid())
            tween.Kill();
        Hide();
    }

    public void QueueHint(string hintText, string inputName, Texture2D iconTexture, double time)
    {
        queue.Enqueue((hintText, inputName, iconTexture, time));
        if (!root.Visible || queue.Count > maxQueueSize)
        {
            Hide();
        }
    }

    public void ShowHintNow(string hintText, string inputName, Texture2D iconTexture, double time)
    {
        root.Visible = false;
        timeOnScreen = time;
        hintLabel.Text = MainMenuUI.TranslateText(hintText);
        keyRoot.Visible = !string.IsNullOrEmpty(inputName);
        keyLabel.Text = inputName;
        iconRect.Visible = IsInstanceValid(iconTexture);
        iconRect.Texture = iconTexture;
        if (IsInstanceValid(tween) && tween.IsValid())
            tween.Kill();
        // tween = CreateTween();
        // root.ResetSize();
        // float x = root.GetViewportRect().Size.X - root.Size.X;
        root.Visible = true;
        Show();
        // tween.TweenProperty(root, new NodePath(Control.PropertyName.Position), new Vector2(x, 0), 0.25d).From(new Vector2(x + root.Size.X, 0));
        // tween.TweenCallback(Callable.From(Show));
    }
}