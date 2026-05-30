using Godot;

[GlobalClass]
public partial class ElevatorShake : CameraShake
{
    [Export]
    public float lerpSpeed = 1f;

    public Tween tween;

    public void ShakeElevator(float amount, float length)
    {
        tween?.Kill();
        shake = amount;
        tween = CreateTween();
        tween.TweenProperty(this, new NodePath(PropertyName.shake), 0f, 0.5d).From(amount).SetDelay(length - 0.5d);
    }

    public override void _Process(double delta)
    {
        // shake = Mathf.Lerp(shake, 0f, (float)delta * lerpSpeed);
        base._Process(delta);
    }
}