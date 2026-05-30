using Godot;

public partial class FakePlayer : CharacterBody3D, IHealth
{
    [Export]
    public float Health { get; set; }
    [Export]
    public float MaxHealth { get; set; }

    [Export]
    public Label3D currentHealthLabel;

    [Export]
    public Node3D labelStart;
    [Export]
    public bool canHeal = true;
    [Export]
    public float healAmount = 10f;

    public Label3D lastLabel;

    public override void _EnterTree()
    {
        base._EnterTree();
        SetMeta(IHealth.MetaName, this);
    }

    public override void _Process(double delta)
    {
        Visible = Health > 0f;
        if (IsInstanceValid(currentHealthLabel))
        {
            currentHealthLabel.Text = $"{Health:0.0}/{MaxHealth:0.0}";
        }
    }

    public async void AddLabel(string text, Color color, double time = 1d)
    {
        /*
        var col = color;
        if (IsInstanceValid(lastLabel))
            col = lastLabel.Modulate;
        col.A = 1f;
        color.A = 1f;
        if (IsInstanceValid(lastLabel) && col.IsEqualApprox(color))
        {
            lastLabel.Text += $"{text}\n";
            return;
        }
        */
        var label = new Label3D();
        label.Modulate = color;
        label.Text = text;
        label.FontSize = 12;
        label.FixedSize = true;
        labelStart.AddChild(label);
        label.Position = Vector3.Zero;
        lastLabel = label;
        var tween = label.CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.SetParallel();
        tween.TweenProperty(label, new NodePath(Node3D.PropertyName.Position), new Vector3(0f, (float)time, 0f), time);
        tween.TweenProperty(label, new NodePath(GeometryInstance3D.PropertyName.Transparency), 1f, time);
        await ToSignal(tween, Tween.SignalName.Finished);
        if (IsInstanceValid(label))
            label.QueueFree();
        lastLabel = null;
    }

    public override void _Ready()
    {
        Spawn(null);
    }

    public void HealOverTime()
    {
        if (!canHeal && Health > 0f)
            return;
        if (Health >= MaxHealth)
            return;
        Heal(new HealInfo(10f));
    }

    public void Damage(DamageInfo info)
    {
        Health -= info.Amount;
        if (Health <= 0f)
            Kill(info);
        else
            AddLabel($"-{info.Amount:0.0} HP", Colors.Red);
    }

    public void Heal(HealInfo info)
    {
        if (Health <= 0f)
            Spawn(info);
        else
        {
            Health = Mathf.Min(Health + info.Amount, MaxHealth);
            AddLabel($"+{info.Amount:0.0} HP", Colors.Green);
        }
    }

    public void Kill(DamageInfo info)
    {
        Health = 0f;
        AddLabel($"Dead", Colors.DarkRed);
    }

    public void Spawn(HealInfo info)
    {
        Health = MaxHealth;
        AddLabel($"Spawned", Colors.DarkGreen);
    }
}
