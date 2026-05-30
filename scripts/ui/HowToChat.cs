using System;
using System.Linq;
using Godot;

public partial class HowToChat : Node
{
    public static StringName VoiceName = "player_voice_chat";
    public static StringName ListName = "player_list";

    public static StringName UseName = "player_use";
    public static StringName PrimaryName = "player_primary";

    [Export]
    public Control root;
    [Export]
    public Label voiceChatKeyLabel;
    [Export]
    public Label playerListKeyLabel;
    [Export]
    public double timeOnScreen = 5d;
    public Tween tween;

    public override void _EnterTree()
    {
        RoundManager.Instance.OnRoundState += OnState;
    }

    public override void _ExitTree()
    {
        RoundManager.Instance.OnRoundState -= OnState;
    }

    public override void _Ready()
    {
        OnState(RoundManager.RoundState.WaitingForPlayers);
    }

    private void Hide()
    {
        root.Visible = false;
    }

    private void Show()
    {
        float x = GetWindow().Size.X - root.Size.X;
        root.Position = new Vector2(x, 0);
        if (IsInstanceValid(tween) && tween.IsValid())
            tween.Kill();
        tween = CreateTween();
        tween.TweenCallback(Callable.From(StartHide)).SetDelay(timeOnScreen);
    }

    private void StartHide()
    {
        float x = GetWindow().Size.X - root.Size.X;
        if (IsInstanceValid(tween) && tween.IsValid())
            tween.Kill();
        tween = CreateTween();
        tween.TweenProperty(root, new NodePath(Control.PropertyName.Position), new Vector2(x, -root.Size.Y), 0.5d).From(new Vector2(x, 0));
        tween.TweenCallback(Callable.From(Hide));
    }

    private void OnState(RoundManager.RoundState state)
    {
        if (state == RoundManager.RoundState.WaitingForPlayers)
        {
            root.Visible = false;
            if (IsInstanceValid(RoleHintUI.Instance))
            {
                var chatBtn = InputSettings.GetMapping(VoiceName);
                var listBtn = InputSettings.GetMapping(ListName);
                RoleHintUI.Instance.ClearQueue();
                RoleHintUI.Instance.QueueHint(chatBtn.GetName(), chatBtn.GetButtonName(), null, timeOnScreen);
                RoleHintUI.Instance.QueueHint(listBtn.GetName(), listBtn.GetButtonName(), null, timeOnScreen);
            }
            return;
            voiceChatKeyLabel.Text = InputSettings.GetMapping(VoiceName).GetButtonName();
            playerListKeyLabel.Text = InputSettings.GetMapping(ListName).GetButtonName();
            if (IsInstanceValid(tween) && tween.IsValid())
                tween.Kill();
            tween = CreateTween();
            float x = GetWindow().Size.X - root.Size.X;
            root.Visible = true;
            tween.TweenProperty(root, new NodePath(Control.PropertyName.Position), new Vector2(x, 0), 0.25d).From(new Vector2(x + root.Size.X, 0));
            tween.TweenCallback(Callable.From(Show));
        }
    }
}