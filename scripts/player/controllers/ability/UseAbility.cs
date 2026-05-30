using System.Linq;
using Godot;

[GlobalClass]
public partial class UseAbility : BaseAbility
{
    [Export] public RayCast3D rayCast;
    [Export] public ShapeCast3D shapeCast;

    public ButtonInputFlags ButtonUse = ButtonInputFlags.None;
    public ButtonInputFlags ButtonGrab = ButtonInputFlags.None;

    public IInteractable curInteract;

    public override void SetupFromDefinition()
    {
    }

    public override void _Ready()
    {
        base._Ready();
        if (IsInstanceValid(rayCast))
        {
            rayCast.Enabled = false;
            rayCast.Reparent(Player.RoleController.View, false);
            rayCast.AddException(Player.MainCollider);
        }
        if (IsInstanceValid(shapeCast))
        {
            shapeCast.Enabled = false;
            shapeCast.Reparent(Player.RoleController.View, false);
            shapeCast.AddException(Player.MainCollider);
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        SwitchInteract(null, null);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (IsMultiplayerAuthority())
        {
            HandleUse(delta);
        }
    }

    public bool FindCollider(out GodotObject collider)
    {
        rayCast.ForceRaycastUpdate();
        if (IsInstanceValid(shapeCast))
            shapeCast.ForceShapecastUpdate();
        collider = null;
        bool hasCollider = false;
        if (rayCast.IsColliding() && (rayCast.GetCollider() is IInteractable || (IsInstanceValid(rayCast.GetCollider()) && rayCast.GetCollider().HasMeta(IInteractable.META_NAME))))
        {
            collider = rayCast.GetCollider();
            hasCollider = true;
        }
        else if (IsInstanceValid(shapeCast) && shapeCast.IsColliding())
        {
            for (int i = 0; i < shapeCast.GetCollisionCount(); i++)
            {
                if (shapeCast.GetCollider(i) is IInteractable || shapeCast.GetCollider(i).HasMeta(IInteractable.META_NAME))
                {
                    collider = shapeCast.GetCollider(i);
                    hasCollider = true;
                    break;
                }
            }
        }
        return hasCollider;
    }

    public IInteractable GetInteractable(GodotObject obj)
    {
        if (obj is IInteractable interact)
        {
            return interact;
        }
        if (obj.GetMeta(IInteractable.META_NAME).AsGodotObject() is IInteractable interact2)
        {
            return interact2;
        }
        return null;
    }

    public void SwitchInteract(IInteractable interactable, ItemObject item)
    {
        if (IsInstanceValid(curInteract as GodotObject))
        {
            var lastInteract = curInteract;
            curInteract = null;

            lastInteract.UseEnd(Player, item);
        }
        if (interactable != null && interactable.CanUse(Player, item))
        {
            interactable.Use(Player, item);
            curInteract = interactable;
        }
        else
        {
            curInteract = null;
        }
    }

    public static bool AllowPrimaryDrag(IInteractable.InteractType type)
    {
        return type == IInteractable.InteractType.Drag || type == IInteractable.InteractType.DragCapture;
    }

    public override void ModifyMouseMotion(FPController controller, ref Vector2 value)
    {
        if (curInteract != null && curInteract.ActionType == IInteractable.InteractType.DragCapture)
        {
            value = Vector2.Zero;
        }
    }

    public void HandleUse(double delta)
    {
        if (!Player.IsLocalPlayer)
        {
            return;
        }
        if (!Player.RoleController.IsControllerActive)
        {
            NetworkPlayer.LocalInstance.Hud.handIconState = 0;
            return;
        }
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerUse, ref ButtonUse);
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerPrimary, ref ButtonGrab);
        ItemObject item = Player.TryGetAbility(out InventoryAbility inventory) ? inventory.InventoryEquipped.FirstOrDefault() : null;
        if (IsInstanceValid(rayCast) && Player is NetworkPlayer plr)
        {
            bool hasCollider = FindCollider(out GodotObject collider);
            if (ButtonUse.HasFlag(ButtonInputFlags.JustPressed) && hasCollider)
            {
                var interact = GetInteractable(collider);
                SwitchInteract(interact, item);
            }
            if (ButtonUse.HasFlag(ButtonInputFlags.JustReleased) && curInteract != null)
            {
                SwitchInteract(null, item);
            }
            if (!IsInstanceValid(item) && ButtonGrab.HasFlag(ButtonInputFlags.JustPressed) && hasCollider)
            {
                var interact = GetInteractable(collider);
                if (AllowPrimaryDrag(interact.ActionType))
                {
                    SwitchInteract(interact, item);
                }
            }
            if (ButtonGrab.HasFlag(ButtonInputFlags.JustReleased) && curInteract != null && AllowPrimaryDrag(curInteract.ActionType))
            {
                SwitchInteract(null, item);
            }

            // if (IsInstanceValid(plr.Hud.handIcon))
            {
                var interact = IsInstanceValid(collider) ? GetInteractable(collider) : null;
                if (curInteract != null)
                {
                    Texture2D icon = null;
                    switch (curInteract.ActionType)
                    {
                        default:
                        case IInteractable.InteractType.Touch:
                        case IInteractable.InteractType.Hit:
                            icon = plr.Hud.pointIcon;
                            plr.Hud.handIconState = 1;
                            break;
                        case IInteractable.InteractType.Drag:
                        case IInteractable.InteractType.DragCapture:
                        case IInteractable.InteractType.Grab:
                            icon = plr.Hud.useGrabbingIcon;
                            plr.Hud.handIconState = 3;
                            break;
                        /*case IInteractable.InteractType.Grab:
                            icon = plr.Hud.grabIcon;
                            break;*/
                    }
                    // plr.Hud.handIcon.Texture = icon;
                    // plr.Hud.handIcon.Visible = true;
                    // plr.Hud.handIconState = 0;
                    // plr.Hud.useLabel.Text = InputSettings.GetMapping(AllowPrimaryDrag(curInteract.ActionType) ? HowToChat.PrimaryName : HowToChat.UseName).GetButtonName();
                }
                else if (hasCollider && interact != null && interact.CanUse(Player, item))
                {
                    Texture2D icon = null;
                    switch (interact.ActionType)
                    {
                        default:
                        case IInteractable.InteractType.Touch:
                        case IInteractable.InteractType.Hit:
                            icon = plr.Hud.pointIcon;
                            plr.Hud.handIconState = 1;
                            break;
                        case IInteractable.InteractType.Drag:
                        case IInteractable.InteractType.DragCapture:
                        case IInteractable.InteractType.Grab:
                            icon = plr.Hud.useIcon;
                            plr.Hud.handIconState = 2;
                            break;
                        /*case IInteractable.InteractType.Grab:
                            icon = plr.Hud.grabIcon;
                            break;*/
                    }
                    // plr.Hud.handIcon.Texture = icon;
                    // plr.Hud.handIcon.Visible = true;
                    // plr.Hud.useLabel.Text = InputSettings.GetMapping(AllowPrimaryDrag(interact.ActionType) ? HowToChat.PrimaryName : HowToChat.UseName).GetButtonName();
                }
                else if ((!IsInstanceValid(inventory) || inventory.InventoryEquipped.Length == 0) && Player.TryGetAbility(out GrabbingAbility grabbing) && (IsInstanceValid(grabbing.grabbed) || (grabbing.rayCast.IsColliding() && grabbing.rayCast.GetCollider() is IGrabbable)))
                {
                    // plr.Hud.handIcon.Texture = IsInstanceValid(grabbing.grabbed) ? plr.Hud.grabbingIcon : plr.Hud.grabIcon;
                    // plr.Hud.handIcon.Visible = true;
                    plr.Hud.handIconState = IsInstanceValid(grabbing.grabbed) ? 5 : 4;
                    // plr.Hud.useLabel.Text = InputSettings.GetMapping(HowToChat.PrimaryName).GetButtonName();
                }
                else
                {
                    plr.Hud.handIconState = 0;
                    // plr.Hud.handIcon.Visible = false;
                }
            }
        }
    }
}
