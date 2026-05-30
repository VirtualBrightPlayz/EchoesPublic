using System.Linq;
using Godot;

public partial class Fuel457 : StaticBody3D, IInteractable, IDamageSource
{
    public Vector3 WorldInteractPosition => marker?.GlobalPosition ?? GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Hit;
    [Export]
    public Node3D marker;
    [Export]
    public float FuelAmount;
    [Export]
    public float MaxFuelAmount = 60f;
    [Export]
    public float FuelCooldown = 10f;
    [Export]
    public FireWorldEffects fire;
    [Export]
    public Area3D area;
    [Export]
    public float DamageAmount = 1f;
    public string AttackerDisplayName => "Fire";
    public NodePath AbsolutePath => GetPath();

    public override void _Ready()
    {
        if (IsMultiplayerAuthority())
        {
            FuelAmount = MaxFuelAmount;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Multiplayer.HasMultiplayerPeer() && IsMultiplayerAuthority())
        {
            FuelAmount = Mathf.Min(FuelAmount + (float)delta, MaxFuelAmount);
            if (IsInstanceValid(area) && FuelAmount < 0f)
            {
                foreach (var body in area.GetOverlappingBodies())
                {
                    if (body is IPlayerController ctrl && ctrl.Player.Role.team != TeamID.SCP)
                    {
                        ctrl.Player.Damage(new DamageInfo((float)delta * DamageAmount, this, DamageType.Fire));
                    }
                }
            }
        }
        if (IsInstanceValid(fire))
        {
            fire.FireAmount = Mathf.Clamp(FuelAmount / -FuelCooldown, 0f, 1f);
            fire.Visible = FuelAmount < 0f;
        }
    }

    public bool CanUse(IItemHolder holder, ItemObject item)
    {
        return holder.GetPlayer().RoleIndex == (int)RoleID.SCP457;
        // return holder.GetPlayer().controller is SCP457Controller;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcUse()
    {
        if (Multiplayer.IsServer())
        {
            int senderId = Multiplayer.GetRemoteSenderId();
            var victim = IPlayerList.List(this).PlayerList.FirstOrDefault(x => x.AuthorityId == senderId);
            if (victim != null)
            {
                // if (victim.controller is SCP457Controller ctrl)
                if (victim.RoleIndex == (int)RoleID.SCP457 && victim.TryGetAbility(out StaminaAbility ability))
                {
                    if (FuelAmount > 0f)
                    {
                        ability.Rpc(StaminaAbility.MethodName.RpcAddSprint, MaxFuelAmount);
                        // ctrl.Rpc(nameof(SCP457Controller.RpcAddFuel), MaxFuelAmount);
                        FuelAmount = -FuelCooldown;
                    }
                }
            }
        }
    }

    public void Use(IItemHolder holder, ItemObject item)
    {
        // if (holder.GetPlayer().controller is SCP457Controller ctrl)
        if (CanUse(holder, item))
        {
            RpcId(1, nameof(RpcUse));
        }
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
    }
}