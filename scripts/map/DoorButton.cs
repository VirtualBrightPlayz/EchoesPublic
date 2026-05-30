using Godot;
using System.Collections.Generic;

public partial class DoorButton : StaticBody3D, IInteractable, ISpecificEventSource<IInteractableEvent>
{
    public static List<DoorButton> buttons = new List<DoorButton>();

    public Vector3 WorldInteractPosition => marker?.GlobalPosition ?? GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Hit;

    [Export]
    public Node3D marker;
    [Export]
    public NodePath target;
    [Export]
    public AudioStreamPlayer3D audio;
    [Export]
    public AudioStream[] pressSounds = new AudioStream[0];
    [ExportGroup("Visuals")]
    [Export]
    public MeshInstance3D otherMesh;
    [Export]
    public Material[] otherMaterials = new Material[0];
    [Export]
    public int otherMaterialIndex = 0;
    [Export]
    public Node3D button;
    [Export]
    public Node3D keycard;
    [Export]
    public Material[] buttonMaterials = new Material[0];
    [Export]
    public Material[] buttonMaterials2 = new Material[0];
    [Export]
    public MeshInstance3D buttonMesh;
    [Export]
    public Material[] keycardMaterials = new Material[0];
    [Export]
    public Light3D keycardLight;
    [Export]
    public Color[] keycardLightColors = new Color[0];
    [Export]
    public MeshInstance3D keycardMesh;
    [Export]
    public float emission = 5f;
    [Export]
    public StringName shaderUniform = "emission_energy_multi";
    [Export]
    public StringName shaderUniformPulse = "emission_pulse_amount";
    [Export]
    public int materialIndex = 0;
    [Export]
    public int materialIndexButton = 0;
    [Export]
    public int materialIndexButton2 = 0;

    private double timer;

    private Node targetCached;
    private DoorStatus prevStatus = DoorStatus.Ok;
    private float prevPulse = -1f;

    public IInteractable targetInteract;
    public bool HasTarget = false;
    public IDoorStatus iDoor;
    public Door door;
    public Elevator elevator;
    public AnimElevator animElevator;
    public TimedDoor timedDoor;

    private bool isKeycardCached = false;
    private bool isButtonCached = false;
    private bool isOtherCached = false;

    public override void _EnterTree()
    {
        buttons.Add(this);
    }

    public override void _ExitTree()
    {
        buttons.Remove(this);
    }

    public override void _Ready()
    {
        targetCached = GetNodeOrNull(target);
        HasTarget = IsInstanceValid(targetCached);
        targetInteract = targetCached as IInteractable;
        iDoor = targetCached as IDoorStatus;
        door = targetCached as Door;
        elevator = targetCached as Elevator;
        animElevator = targetCached as AnimElevator;
        timedDoor = targetCached as TimedDoor;

        if (IsInstanceValid(button) && IsInstanceValid(keycard))
        {
            // /*
            if (IsInstanceValid(door))
            {
                button.Visible = door.validCardTypes.Count == 0;
                keycard.Visible = door.validCardTypes.Count != 0;
            }
            else if (IsInstanceValid(timedDoor))
            {
                button.Visible = timedDoor.validCardTypes.Count == 0;
                keycard.Visible = timedDoor.validCardTypes.Count != 0;
            }
            else
            // */
            {
                button.Visible = true;
                keycard.Visible = false;
            }
        }

        SetMaterials(true);

        if (HasTarget && iDoor != null)
        {
            iDoor.StatusChanged += Tick;
        }
    }

    public void Tick()
    {
        SetMaterials(false);
    }

    public void SetMaterials(bool force)
    {
        if (IInitScript.IsServerOnly)
            return;
        if (force)
        {
            isKeycardCached = IsInstanceValid(keycard) && keycard.Visible && IsInstanceValid(keycardMesh);
            isButtonCached = IsInstanceValid(button) && button.Visible && IsInstanceValid(buttonMesh);
            isOtherCached = IsInstanceValid(otherMesh);
        }
        if (isOtherCached)
        {
            if (HasTarget && iDoor != null)
            {
                DoorStatus status = iDoor.Status;
                if (force || prevStatus != status)
                {
                    switch (status)
                    {
                        default:
                        case DoorStatus.Idle:
                            otherMesh.SetSurfaceOverrideMaterial(otherMaterialIndex, otherMaterials[0]);
                            break;
                        case DoorStatus.Ok:
                            otherMesh.SetSurfaceOverrideMaterial(otherMaterialIndex, otherMaterials[1]);
                            break;
                        case DoorStatus.Warn:
                            otherMesh.SetSurfaceOverrideMaterial(otherMaterialIndex, otherMaterials[2]);
                            break;
                        case DoorStatus.Err:
                            otherMesh.SetSurfaceOverrideMaterial(otherMaterialIndex, otherMaterials[3]);
                            break;
                        case DoorStatus.Invalid:
                            otherMesh.SetSurfaceOverrideMaterial(otherMaterialIndex, otherMaterials[4]);
                            break;
                    }
                    prevStatus = status;
                }
            }
        }
        if (isKeycardCached)
        {
            if (HasTarget && iDoor != null)
            {
                DoorStatus status = iDoor.Status;
                if (force || prevStatus != status)
                {
                    switch (status)
                    {
                        default:
                        case DoorStatus.Idle:
                            keycardMesh.SetSurfaceOverrideMaterial(materialIndex, keycardMaterials[0]);
                            keycardLight.LightColor = keycardLightColors[0];
                            break;
                        case DoorStatus.Ok:
                            keycardMesh.SetSurfaceOverrideMaterial(materialIndex, keycardMaterials[1]);
                            keycardLight.LightColor = keycardLightColors[1];
                            break;
                        case DoorStatus.Warn:
                            keycardMesh.SetSurfaceOverrideMaterial(materialIndex, keycardMaterials[2]);
                            keycardLight.LightColor = keycardLightColors[2];
                            break;
                        case DoorStatus.Err:
                            keycardMesh.SetSurfaceOverrideMaterial(materialIndex, keycardMaterials[3]);
                            keycardLight.LightColor = keycardLightColors[3];
                            break;
                        case DoorStatus.Invalid:
                            keycardMesh.SetSurfaceOverrideMaterial(materialIndex, keycardMaterials[4]);
                            keycardLight.LightColor = keycardLightColors[3];
                            break;
                    }
                    prevStatus = status;
                }
            }
        }
        if (isButtonCached)
        {
            if (HasTarget && iDoor != null)
            {
                DoorStatus status = iDoor.Status;
                float pulse = iDoor.StatusPulse;
                if (force || prevStatus != status)
                {
                    switch (iDoor.Status)
                    {
                        default:
                        case DoorStatus.Idle:
                            buttonMesh.SetSurfaceOverrideMaterial(materialIndexButton, buttonMaterials[0]);
                            buttonMesh.SetSurfaceOverrideMaterial(materialIndexButton2, buttonMaterials2[0]);
                            break;
                        case DoorStatus.Ok:
                            buttonMesh.SetSurfaceOverrideMaterial(materialIndexButton, buttonMaterials[0]);
                            buttonMesh.SetSurfaceOverrideMaterial(materialIndexButton2, buttonMaterials2[1]);
                            break;
                        case DoorStatus.Warn:
                            buttonMesh.SetSurfaceOverrideMaterial(materialIndexButton, buttonMaterials[1]);
                            buttonMesh.SetSurfaceOverrideMaterial(materialIndexButton2, buttonMaterials2[2]);
                            break;
                        case DoorStatus.Err:
                            buttonMesh.SetSurfaceOverrideMaterial(materialIndexButton, buttonMaterials[2]);
                            buttonMesh.SetSurfaceOverrideMaterial(materialIndexButton2, buttonMaterials2[3]);
                            break;
                        case DoorStatus.Invalid:
                            buttonMesh.SetSurfaceOverrideMaterial(materialIndexButton, buttonMaterials[2]);
                            buttonMesh.SetSurfaceOverrideMaterial(materialIndexButton2, buttonMaterials2[4]);
                            break;
                    }
                    prevStatus = status;
                }
                if (force || !Mathf.IsEqualApprox(prevPulse, pulse))
                {
                    if (Mathf.IsZeroApprox(pulse))
                    {
                        buttonMesh.SetInstanceShaderParameter(shaderUniform, emission);
                        buttonMesh.SetInstanceShaderParameter(shaderUniformPulse, 0f);
                        // timer = 0d;
                    }
                    else
                    {
                        // timer += delta;
                        buttonMesh.SetInstanceShaderParameter(shaderUniform, emission);
                        buttonMesh.SetInstanceShaderParameter(shaderUniformPulse, 1f / pulse);
                    }
                    prevPulse = pulse;
                }
            }
        }
    }

    public void AreaEnter(Node3D body)
    {
        if (IsMultiplayerAuthority() && body is Keycard keycard && !IsInstanceValid(keycard.Item.Player))
        {
            door?.TryUseCard(keycard);
            timedDoor?.TryUseCard(keycard);
        }
    }

    public bool CanUse(IItemHolder holder, ItemObject item)
    {
        EventCanUse canUse = EventManager.GetInstance<EventCanUse>();
        canUse.Holder = holder;
        canUse.Item = item;
        canUse.Result = targetInteract?.CanUse(holder, item) ?? false;
        Emit(canUse);
        return canUse.Result;
    }

    public void Use(IItemHolder holder, ItemObject item)
    {
        EventUsing evtUsing = EventManager.GetInstance<EventUsing>();
        evtUsing.Holder = holder;
        evtUsing.Item = item;
        if (!Emit(evtUsing))
        {
            return;
        }
        if (HasTarget && animElevator != null && HasMeta("floor"))
        {
            animElevator.RpcId(MultiplayerPeer.TargetPeerServer, AnimElevator.MethodName.RpcUse, GetMeta("floor").AsUInt32());
        }
        else
        {
            targetInteract?.Use(holder, item);
        }
        if (IsInstanceValid(audio))
        {
            audio.Stream = pressSounds[GD.Randi() % pressSounds.Length];
            audio.Play();
        }
        EventUsed evtUsed = EventManager.GetInstance<EventUsed>();
        evtUsed.Item = item;
        evtUsed.Holder = holder;
        Emit(evtUsed);
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
        EventUseEnding evtEnding = EventManager.GetInstance<EventUseEnding>();
        evtEnding.Holder = holder;
        evtEnding.Item = item;
        if (!Emit(evtEnding))
        {
            return;
        }
        targetInteract?.UseEnd(holder, item);
        EventUseEnded evtEnded = EventManager.GetInstance<EventUseEnded>();
        evtEnded.Holder = holder;
        evtEnded.Item = item;
        Emit(evtEnded);
    }

    public bool Emit(IInteractableEvent evt)
    {
        evt.Interactable = this;
        return Emit(evt as IEvent);
    }

    public bool Emit(IEvent evt)
    {
        return EventManager.Emit(evt, this);
    }
}
