using Godot;

[GlobalClass]
public partial class StatueAbility : BaseAbility
{
    [Export] public Area3D area;
    [Export] public float audioLerpSpeed = 1f;
    [Export] public AudioStreamPlayer3D footstepsAudio;
    public bool seen;

    private double footstepTimer;
    private double lastFootstepTimer;

    public override void SetupFromDefinition()
    {
    }

    public override void _Ready()
    {
        base._Ready();
        return;
        if (Player.RoleController is FPController fp && Player.Data.TryGetSound(Player.Role.DefaultFootsteps, out GameSound footsteps))
        {
            // fp.footstepInterval = 0.1f;
            footstepsAudio.Stream = footsteps.Stream;
            footstepsAudio.Play();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        ProcessFootsteps(delta);
        seen = IsSeen();
        if (RoundManager.Instance.IsBlinking || !seen)
        {
            Player.model.GlobalRotation = Player.RoleController.Root.GlobalRotation;
        }
        Player.model.GlobalPosition = Player.RoleController.Root.GlobalPosition;
        area.GlobalTransform = Player.RoleController.Root.GlobalTransform;
        if (IsMultiplayerAuthority() && Player.RoleController is FPController fp)
        {
            fp.ExtraSpeed = (!RoundManager.Instance.IsBlinking && seen) ? 0f : 1f;
        }
    }

    public bool IsSeenBy(Node3D body)
    {
        if (body is IPlayerController player && player.Player.Role.team == Player.Role.team)
        {
            return false;
        }
        return IPlayerController.IsSeenBy(Player.RoleController, body);
    }

    public bool IsSeen()
    {
        bool found = false;
        var bodies = area.GetOverlappingBodies();
        foreach (var body in bodies)
        {
            found = IsSeenBy(body);
            if (found)
                break;
        }
        return found;
    }

    public void ProcessFootsteps(double delta)
    {
        return;
        if (Player.RoleController is FPController fp)
        {
            if (IsMultiplayerAuthority())
            {
                if (fp.IsOnFloor())
                {
                    footstepTimer += delta * fp.Velocity.Length() / fp.EffectiveSpeed;
                    if (footstepTimer >= fp.footstepInterval)
                    {
                        footstepTimer = 0f;
                        Rpc(MethodName.RpcScrape);
                    }
                }
                lastFootstepTimer = footstepTimer;
            }
            footstepsAudio.VolumeLinear = Mathf.Lerp(footstepsAudio.VolumeLinear, 0f, (float)delta * audioLerpSpeed);
        }
    }

    // [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void RpcScrape()
    {
        return;
        if (Player.RoleController is FPController fp)
        {
            if (!footstepsAudio.Playing)
            {
                footstepsAudio.Play();
            }
            footstepsAudio.VolumeLinear = IsMultiplayerAuthority() ? 0.75f : 1f;
        }
    }
}