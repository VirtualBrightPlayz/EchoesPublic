using System.Linq;
using Godot;

[GlobalClass]
public partial class SanityAbility : BaseAbility
{
    public double SanityTimer = 0;
    public bool IsBeingChased = false;
    public double SanityCooldownTimer = 0;
    [Export]
    public double MaxSanityCooldown = 20;
    [Export]
    public double SanityJumpscareAmount = 10;
    [Export]
    public double MaxSanity = 600;

    public override void SetupFromDefinition()
    {
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (IsMultiplayerAuthority())
        {
            UpdateSanity(delta);
        }
    }

    public void UpdateSanity(double delta)
    {
        bool canSee = Player.Role.team != TeamID.SCP && IPlayerList.List(this).PlayerList.Any(x => x.Role.team == TeamID.SCP && IPlayerController.IsSeenBy(x.RoleController, Player.RoleController.Root));
        bool canSeeHumans = Player.Role.team != TeamID.SCP && IPlayerList.List(this).PlayerList.Any(x => x.Role.team != TeamID.Dead && x.Role.team != TeamID.SCP && IPlayerController.IsSeenBy(x.RoleController, Player.RoleController.Root));
        /*
        if (canSee)
        {
            if (!IsBeingChased)
            {
                SanityTimer = Mathf.Clamp(SanityTimer + SanityJumpscareAmount, 0, MaxSanity);
            }
            IsBeingChased = true;
            SanityCooldownTimer = MaxSanityCooldown;
            SanityTimer = Mathf.Clamp(SanityTimer + delta * 2d, 0, MaxSanity);
        }
        else
        {
            if (IsBeingChased)
            {
                SanityCooldownTimer -= delta;
                if (SanityCooldownTimer <= 0)
                    IsBeingChased = false;
            }
            else if (canSeeHumans)
            {
                SanityTimer = Mathf.Clamp(SanityTimer - delta, 0, MaxSanity);
            }
        }
        */
        if (IsInstanceValid(MusicManager.Instance))
        {
            if (canSeeHumans)
            {
                // SanityTimer = Mathf.Clamp(SanityTimer - delta, 0, MaxSanity);
                SanityTimer = 0;
                if (MusicManager.Instance.state == MusicManager.MusicState.Alone)
                {
                    MusicManager.Instance.SwitchTrackToZone(false);
                }
            }
            else if (!canSee)
            {
                SanityTimer = Mathf.Clamp(SanityTimer + delta, 0, MaxSanity);
                if (SanityTimer >= MaxSanity)
                {
                    MusicManager.Instance.SwitchTrackToAlone(false);
                }
            }
        }
    }
}
