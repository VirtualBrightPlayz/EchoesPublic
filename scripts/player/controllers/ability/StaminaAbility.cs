using System;
using Godot;

[GlobalClass]
public partial class StaminaAbility : BaseAbility
{
    public float sprintStamina;
    public bool hasSprinted;
    public bool sprinting;
    public Timer sprintStaminaTimer;

    public override void SetupFromDefinition()
    {
    }

    public override void _Ready()
    {
        base._Ready();
        sprintStaminaTimer = new Timer()
        {
            Autostart = false,
            OneShot = true,
            ProcessCallback = Timer.TimerProcessCallback.Physics,
        };
        AddChild(sprintStaminaTimer);
    }

    public override void OnSpawn(PlayerRole role)
    {
        base.OnSpawn(role);
        sprintStamina = role.MaxSprintStamina;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcAddSprint(float amount)
    {
        if (Player.CallerIsServer)
        {
            sprintStamina += amount;
        }
    }

    public void ShowBar()
    {
        if (!hasSprinted)
        {
            if (Player is NetworkPlayer plr2)
                plr2.Hud.SprintBarSpawnIn();
            hasSprinted = true;
        }
    }

    public void HideBar()
    {
        if (hasSprinted)
        {
            if (Player is NetworkPlayer plr2)
                plr2.Hud.SprintBarSpawnOut();
            hasSprinted = false;
        }
    }

    public void ApplySprint(FPController controller, double delta, bool ignoreVelocity)
    {
        sprinting = controller.CanSprint();

        if (!ignoreVelocity && (controller.Velocity.IsZeroApprox() || controller.IsCrouching))
            sprinting = false;

        if (sprinting)
        {
            if (!ignoreVelocity && controller.Velocity.IsZeroApprox()) return;

            if (!hasSprinted)
            {
                if (Player is NetworkPlayer plr2)
                    plr2.Hud.SprintBarSpawnIn();
                hasSprinted = true;
            }

            sprintStamina -= (Player.Role.SprintStaminaDecreaseRate * controller.EffectiveStaminaDecreaseMultiplier) * (float)delta;
            sprintStamina = Math.Max(0, sprintStamina);
            sprintStaminaTimer.Start(Player.Role.SprintStaminaPause * controller.EffectiveStaminaPauseTimerMultiplier);
        }
        else if (sprintStaminaTimer.TimeLeft <= 0)
        {
            sprintStamina = Math.Min(sprintStamina + (Player.Role.SprintStaminaIncreaseRate * controller.EffectiveStaminaIncreaseMultiplier) * (float)delta, Player.Role.MaxSprintStamina * controller.EffectiveMaxStaminaMultiplier);

            if (sprintStamina == Player.Role.MaxSprintStamina * controller.EffectiveMaxStaminaMultiplier && hasSprinted)
            {
                if (Player is NetworkPlayer plr2)
                    plr2.Hud.SprintBarSpawnOut();
                hasSprinted = false;
            }
        }

        float normalizedStamina = sprintStamina / Player.Role.MaxSprintStamina * 100;
        if (Player is NetworkPlayer plr)
        {
            plr.Hud.SprintBar.Value = normalizedStamina;
            plr.HudVR.SprintBar.Value = normalizedStamina;
        }
    }
}
