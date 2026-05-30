using Godot;

[GlobalClass]
public partial class GrabbingAbility : BaseAbility
{
    [Export] public PIDController pidController;
    [Export] public RayCast3D rayCast;
    [Export] public float PropReleaseForce { get; set; } = 8f;

    public PhysicsBody3D grabbed;
    public Vector3 grabbedPosLocal;
    private Vector2? wantedRotation = null;
    private double timer = 0d;
    private double maxTime = 0.25d;

    public Vector2 MouseMotion => Player.GetStickPadActionData(LocalPlayerInput.PlayerCamera) * Settings.User.MouseSensitivity;
    public ButtonInputFlags ButtonUse = ButtonInputFlags.None;
    public ButtonInputFlags ButtonPrimary = ButtonInputFlags.None;
    public ButtonInputFlags ButtonSecondary = ButtonInputFlags.None;

    public override void SetupFromDefinition()
    {
    }

    public override void _Ready()
    {
        base._Ready();
        pidController.Reparent(Player.RoleController.View, false);
        rayCast.Reparent(Player.RoleController.View, false);
        rayCast.AddException(Player.MainCollider);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (Player.IsLocalPlayer)
        {
            UpdateInputs();
            HandlePlayerHotkeys(delta);
            ApplyRotation(delta);
        }
    }

    private void UpdateInputs()
    {
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerUse, ref ButtonUse);
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerPrimary, ref ButtonPrimary);
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerSecondary, ref ButtonSecondary);
    }

    private void GrabbedSynced(double delta)
    {
        if (IsInstanceValid(grabbed) && grabbed is RigidbodySync grab)
        {
            if (wantedRotation != null)
            {
                float s = 1f;
                float angle = wantedRotation.Value.X * s;
                float angle2 = wantedRotation.Value.Y * s;
                Basis b = (grabbed.GlobalBasis * pidController.grabRotation).Inverse().Orthonormalized();
                b = Basis.Identity;
                pidController.grabRotation = pidController.grabRotation.Rotated(b.Y, angle).Rotated(b.X, angle2).Orthonormalized();
                wantedRotation = null;
            }
        }
    }

    private void ApplyRotation(double delta)
    {
        if (IsInstanceValid(grabbed))
        {
            if (ButtonUse.HasFlag(ButtonInputFlags.JustPressed))
            {
                wantedRotation = MouseMotion;
            }
            if (ButtonUse.HasFlag(ButtonInputFlags.Pressed))
            {
                wantedRotation = MouseMotion;
            }
            if (ButtonUse.HasFlag(ButtonInputFlags.JustReleased))
            {
                wantedRotation = null;
            }

            if (timer >= maxTime || true)
            {
                GrabbedSynced(delta);
                timer = 0d;
            }
            else
            {
                timer += delta;
            }
            if (ButtonUse.HasFlag(ButtonInputFlags.Pressed))
            {
                // TODO: fix the janky workaround being in the FPController
                return;
            }
        }
    }

    private void HandlePlayerHotkeys(double delta)
    {
        if (!IsInstanceValid(rayCast))
            return;
        if (!Player.TryGetAbility(out InventoryAbility inventory) || inventory.InventoryEquipped.Length == 0)
        {
            rayCast.ForceRaycastUpdate();
            if (ButtonUse.HasFlag(ButtonInputFlags.JustPressed) && rayCast.IsColliding() && rayCast.GetCollider() is IGrabbable g && !IsInstanceValid(grabbed) && g is not IInteractable && g is IPhysicsProp prop && g.AllowGrab)
            {
                Vector3 vec = Player.ActiveController.View.GlobalPosition.DirectionTo(((PhysicsBody3D)g).GlobalPosition) * PropReleaseForce;
                prop.OnImpulse(vec, Vector3.Zero);
            }

            if (ButtonPrimary.HasFlag(ButtonInputFlags.JustPressed) && rayCast.IsColliding() && rayCast.GetCollider() is PhysicsBody3D rb)
            {
                grabbed = rb;
                grabbedPosLocal = grabbed.ToLocal(rayCast.GetCollisionPoint());
                if (grabbed is IGrabbable sync && sync.AllowGrab)
                {
                    sync.Grab(true, grabbedPosLocal);
                    pidController.GlobalPosition = rayCast.GetCollisionPoint();
                    pidController.SetTarget((RigidBody3D)rb, grabbedPosLocal, rayCast.GlobalPosition);
                }
                else
                {
                    grabbed = null;
                }
            }

            if (ButtonSecondary.HasFlag(ButtonInputFlags.JustPressed) && IsInstanceValid(grabbed))
            {
                if (grabbed is IGrabbable sync)
                {
                    Vector3 vec = Player.ActiveController.View.GlobalPosition.DirectionTo(grabbed.GlobalPosition) * PropReleaseForce;
                    sync.Grab(false, default, vec);
                    pidController.SetTarget(null, default, default);
                }
                grabbed = null;
            }

            float maxGrabDist = 6f;

            if (grabbed is RigidbodySync rigidBody3D)
            {
                if (IsInstanceValid(grabbed) && (ButtonPrimary.HasFlag(ButtonInputFlags.JustReleased) || (grabbed.GlobalPosition + rigidBody3D.CenterOfMass).DistanceTo(Player.ActiveController.View.GlobalPosition) > maxGrabDist))
                {
                    rigidBody3D.Grab(false);
                    pidController.SetTarget(null, default, default);
                    grabbed = null;
                }
            }
            else if (grabbed is IGrabbable sync)
            {
                if (IsInstanceValid(grabbed) && (ButtonPrimary.HasFlag(ButtonInputFlags.JustReleased) || (grabbed.GlobalPosition).DistanceTo(Player.ActiveController.View.GlobalPosition) > maxGrabDist))
                {
                    sync.Grab(false);
                    pidController.SetTarget(null, default, default);
                    grabbed = null;
                }
            }
            else
            {
                grabbed = null;
            }
        }
        else
        {
            if (IsInstanceValid(grabbed) && grabbed is IGrabbable sync)
            {
                sync.Grab(false);
                pidController.SetTarget(null, default, default);
                grabbed = null;
            }
        }
    }
}
