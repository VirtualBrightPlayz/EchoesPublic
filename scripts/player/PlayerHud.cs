using Godot;

public partial class PlayerHud
{
    public Tween introTween;

    public async void SpawnIntro(Control hud, PlayerRole Role, Control roleIntroRoot, Control roleThemedRoot, Label roleIntroLabel, RichTextLabel roleLabel, RichTextLabel roleObjective)
    {
        roleIntroRoot.Visible = true;

        roleLabel.Text = "[center]" + Role?.RichDisplayNameUpper ?? "";
        roleObjective.Text = "[center]" + hud.Tr(Role?.Objective ?? "");
        if (introTween != null && introTween.IsValid())
            introTween.Kill();

        roleLabel.Modulate = Colors.White;
        roleIntroLabel.Modulate = Colors.White;
        roleObjective.Modulate = Colors.Transparent;
        roleThemedRoot.Modulate = Colors.Transparent;
        roleLabel.Scale = Vector2.Zero;
        roleIntroLabel.Scale = Vector2.Zero;
        double delay = 0.2d;
        double time = 0.3d;
        var tween = hud.CreateTween();
        tween.SetTrans(Tween.TransitionType.Linear);
        tween.TweenProperty(roleLabel, new NodePath(Control.PropertyName.Scale), Vector2.One, time).From(Vector2.Zero).SetDelay(delay);
        tween.Parallel().TweenProperty(roleIntroLabel, new NodePath(Control.PropertyName.Scale), Vector2.One, time).From(Vector2.Zero).SetDelay(delay);
        delay = 1.1d;
        time = 0.2d;
        tween.Parallel().TweenProperty(roleObjective, new NodePath(CanvasItem.PropertyName.Modulate), Colors.White, time).From(Colors.Transparent).SetDelay(delay);
        time = 0.5d;
        tween.Parallel().TweenProperty(roleThemedRoot, new NodePath(CanvasItem.PropertyName.Modulate), Colors.White, time).From(Colors.Transparent).SetDelay(delay);
        delay = 2d;
        time = 5d;
        tween.TweenProperty(roleLabel, new NodePath(CanvasItem.PropertyName.Modulate), Colors.Transparent, time).From(Colors.White).SetDelay(delay);
        tween.Parallel().TweenProperty(roleIntroLabel, new NodePath(CanvasItem.PropertyName.Modulate), Colors.Transparent, time).From(Colors.White).SetDelay(delay);
        tween.Parallel().TweenProperty(roleObjective, new NodePath(CanvasItem.PropertyName.Modulate), Colors.Transparent, time).From(Colors.White).SetDelay(delay);

        introTween = tween;
        await tween.ToSignal(tween, Tween.SignalName.Finished);
        roleIntroRoot.Visible = false;
    }
}