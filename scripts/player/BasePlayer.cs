using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Godot;

public abstract partial class BasePlayer : Node3D, IInputSource, IDamageSource, IItemHolder, IFlammable, ISpecificEventSource<IPlayerDamageableEvent>
{
    public Func<float[], float> ModifierCalculator => (floats) =>
    {
        if (!floats.Any())
        {
            return 1f;
        }
        float result = 0f;
        foreach (float f in floats)
        {
            result += f;
        }
        return result / floats.Count();
    };

    [Export]
    public TransformSync xformSync;
    [Export]
    public TransformSync viewSync;
    [Export]
    public GameSound hitSound;
    [Export]
    public bool Godmode = false;
    [Export(PropertyHint.Layers3DRender)]
    public uint modelLocalCullFlags = uint.MinValue;
    [ExportGroup("Sync")]
    public Vector3 PlayerPosition => ActiveController != null ? ActiveController.Root.GlobalPosition : Vector3.Zero;
    public Vector3 PlayerRotation => ActiveController != null ? new Vector3(ActiveController.View.Rotation.X, ActiveController.Root.GlobalRotation.Y, ActiveController.Root.GlobalRotation.Z) : Vector3.Zero;
    [Export]
    public Node3D[] HandSyncTargets = new Node3D[3];
    [Export]
    public TransformSync[] HandSyncs = new TransformSync[3];
    [Export]
    public float Health;
    [Obsolete]
    public int RoleIndex => Enum.TryParse(RoleId, out RoleID role) ? (int)role : 0;
    [Export]
    public string RoleId
    {
        get => _roleIndex;
        set
        {
            _roleIndex = value;
            CL_Spawn(value);
        }
    }
    private string _roleIndex;
    [Export]
    public float lerpSpeed = 1f;
    [Export]
    public bool IsVR = false;
    [Export]
    public PlayerStatusEffectManager statusEffectManager;

    [Export]
    public FireWorldEffects FireEffect
    {
        get
        {
            if (ActiveController is FPController fpController && IsInstanceValid(fpController.FireEffect))
            {
                return fpController.FireEffect;
            }
            return null;
        }
        set
        {
            if (ActiveController is FPController fpController && IsInstanceValid(fpController.FireEffect))
            {
                fpController.FireEffect = value;
            }
        }
    }

    public float Amount
    {
        get
        {
            if (!IsInstanceValid(FireEffect))
            {
                return 0f;
            }
            return FireEffect.FireAmount;
        }
        set
        {
            if (!IsInstanceValid(FireEffect))
            {
                return;
            }
            FireEffect.FireAmount = value;
        }
    }

    public bool OnFire => statusEffectManager.IsActive(EffectType.Burn);

    [Export] 
    public float Flammability { get; set; } = 10f;
    
    public void Ignite(float duration)
    {
        this.statusEffectManager.EnableEffect(EffectType.Burn, duration, 1f);
    }
    
    public void Extinguish()
    {
        statusEffectManager.DisableEffect(EffectType.Burn);
    }

    public bool TryGetAbility<T>(out T ability) where T : BaseAbility
    {
        if (ActiveController == null)
        {
            ability = null;
            return false;
        }
        ability = Abilities.OfType<T>().FirstOrDefault(x => x.CanProcess());
        return IsInstanceValid(ability);
    }

    public bool TryGetAbility(Type type, out BaseAbility ability)
    {
        if (ActiveController == null)
        {
            ability = null;
            return false;
        }
        ability = Abilities.FirstOrDefault(x => x.GetType() == type && x.CanProcess());
        return IsInstanceValid(ability);
    }

#region Items

    public Godot.Collections.Array<int> EnabledStatusEffects
    {
        get
        {
            return new Godot.Collections.Array<int>(statusEffectManager.GetEffects(x => x.Active).Select(x => (int)x.Type));
        }
    }

#endregion

    ConcurrentQueue<IHealthModifier> damageQueue = new();

    public override void _EnterTree()
    {
        base._EnterTree();
        controllerSpawner.SetMultiplayerAuthority((int)MultiplayerPeer.TargetPeerServer);
        controllerSpawner.SpawnFunction = Callable.From<Variant, Node>(_SpawnController);
        modelSpawner.SetMultiplayerAuthority((int)MultiplayerPeer.TargetPeerServer);
        modelSpawner.SpawnFunction = Callable.From<Variant, Node>(_SpawnModel);
        abilitySpawner.SetMultiplayerAuthority((int)MultiplayerPeer.TargetPeerServer);
        abilitySpawner.SpawnFunction = Callable.From<Variant, Node>(_SpawnAbility);
    }

    public override void _Ready()
    {
        base._Ready();
        TryChangeRole();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        UpdateInputs();
        CallDeferred(MethodName.AddUpDamage);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        ControllerUpdate();
    }

    public void UpdateInputs()
    {
        if (IsLocalPlayer)
        {
            InputPrimary = InputManager.UpdateInput(GetDigitalActionData(LocalPlayerInput.PlayerPrimary), InputPrimary);
        }
    }

    private void AddUpDamage()
    {
        DamageInfo lastHit = null;
        Dictionary<IDamageSource, List<DamageInfo>> appliedHits = new Dictionary<IDamageSource, List<DamageInfo>>();
        while (damageQueue.TryDequeue(out IHealthModifier mod))
        {
            if (IsInstanceValid((GodotObject)mod) && mod is DamageInfo dmg && IsInstanceValid((Node)dmg.Source))
            {
                if (!appliedHits.ContainsKey(dmg.Source))
                    appliedHits.Add(dmg.Source, new List<DamageInfo>());
                appliedHits[dmg.Source].Add(dmg);
            }
            if (IsInstanceValid((GodotObject)mod) && mod is HealInfo heal)
            {
                Health += heal.Amount;
                if (Health > MaxHealth)
                {
                    Health = MaxHealth;
                }
                EventPlayerHealed healed = EventManager.GetInstance<EventPlayerHealed>();
                healed.Modifier = heal;
                healed.Healer = heal.Source;
                healed.Victim = ActiveControllerNode as IHealth;
                Emit(healed);
            }
        }
        foreach (var kvp in appliedHits)
        {
            float amount = 0f;
            foreach (var dmg in kvp.Value)
            {
                lastHit = dmg;
                amount += dmg.Amount;
                EventPlayerDamaged damaged = EventManager.GetInstance<EventPlayerDamaged>();
                damaged.Modifier = dmg;
                damaged.Victim = dmg.Target;
                damaged.Damager = dmg.Source;
                Emit(damaged);
            }
            Health -= amount;
        }
        if (Health <= 0f)
        {
            if (lastHit != null)
            {
                Kill(lastHit);
            }
            else
            {
                Kill(new DamageInfo());
            }
        }
    }

#region Health

    public Action<DamageInfo> OnDamaged = (_) => { };
    public Action<DamageInfo> OnKilled = (_) => { };

    public virtual void Damage(DamageInfo info)
    {
        if (!Multiplayer.IsServer())
        {
            Log.PrintWarn("Trying to deal damage not on the server.");
            return;
        }
        //we are already dead.
        if (Health <= 0f)
        {
            return;
        }
        if (Godmode)
            return;
        if (Array.IndexOf(Role.ImmuneDamageTypes, info.TypeOfDamage) != -1)
        {
            return;
        }
        if (!Settings.Server.FriendlyFire && info.Team != TeamID.Dead && info.Team == Role.team && info.Source != this)
        {
            return;
        }
        float[] effects = statusEffectManager.GetEffects(x => x.Active && x is IDamageModifier).Cast<IDamageModifier>().Where(x => x.ActiveForType(info.TypeOfDamage)).Select(x => x.DamageMultiplierForType(info.TypeOfDamage)).ToArray();
        float mult = ModifierCalculator(effects);
        info.Amount *= mult;
        if (ActiveController is FPController fpCont)
        {
            info.Target = fpCont;
        }
        EventPlayerDamaging damaging = EventManager.GetInstance<EventPlayerDamaging>();
        damaging.Damager = info.Source;
        damaging.Victim = info.Target;
        damaging.Modifier = info;
        if (!Emit(damaging))
        {
            return;
        }
        OnDamaged?.Invoke(info);
        foreach (var modifer in info.Modifiers)
        {
            modifer.Apply(this);
        }
        damageQueue.Enqueue(info);
    }

    public void Heal(HealInfo info)
    {
        if (!Multiplayer.IsServer())
            return;
        float[] effects = statusEffectManager.GetEffects(x => x.Active && x is IHealingModifier).Cast<IHealingModifier>().Where(x => x.HealingModifierActiveForSource(info.Source)).Select(x => x.HealingMultiplierForSource(info.Source)).ToArray();
        float mult = ModifierCalculator(effects);
        info.Amount *= mult;
        EventPlayerHealing healing = EventManager.GetInstance<EventPlayerHealing>();
        healing.Healer = info.Source;
        healing.Victim = ActiveControllerNode as IHealth;
        healing.Modifier = info;
        if (!Emit(healing))
        {
            return;
        }
        damageQueue.Enqueue(info);
    }

    public void Kill(DamageInfo info)
    {
        if (!Multiplayer.IsServer())
            return;
        if (Godmode)
            return;
        //why?
        Godmode = true;
        //huh? if it does more damage than our health, make it do 2 times our health? Why?
        if (info.Amount <= Health)
        {
            info.Amount = Health * 2;
        }
        EventPlayerDying dying = EventManager.GetInstance<EventPlayerDying>();
        dying.Modifier = info;
        dying.Victim = info.Target;
        dying.Damager = info.Source;
        if (!Emit(dying))
        {
            return;
        }

        if (TryGetAbility(out InventoryAbility inventory))
        {
            inventory.SV_DropAll();
        }
        if (RoleController is IPlayerControllerExt ext)
        {
            ext.OnKilled(info);
        }
        
        if (Role.team != TeamID.Dead)
        {
            RpcId(GetMultiplayerAuthority(), MethodName.SV_Killed, DamageInfo.ToNetwork(info));
            SV_Spawn(RoleID.Spectator, PlayerPosition);
        }

        EventPlayerDeath dead = EventManager.GetInstance<EventPlayerDeath>();
        dead.Modifier = info;
        dead.Victim = info.Target;
        dead.Damager = info.Source;
        Emit(dead);
    }

#endregion

#region IItemHolder

    public bool IsSocket => false;
    public ButtonInputFlags InputPrimary { get; set; } = ButtonInputFlags.None;
    public CollisionObject3D MainCollider => (CollisionObject3D)RoleController; // HACK: this assumes the playercontroller is a physics body.
    public Transform3D HolderTransform => RoleController.Root.GlobalTransform;
    public Transform3D AimTransform => RoleController.Camera.GlobalTransform;
    public bool IsAimingDown => GetDigitalActionData(LocalPlayerInput.PlayerSecondary);

    public BasePlayer GetPlayer() => this;

#endregion

#region IDamageSource

    public abstract string AttackerDisplayName { get; }
    public NodePath AbsolutePath => GetPath();

#endregion

    public GameData Data => IInitScript.Instance.Data;
    public GameEffect[] Effects => Data.Effects;

#region Roles

    public PlayerRole Role => Data.RoleLookup.TryGetValue(RoleId, out PlayerRole val) ? val : null;
    public float MaxHealth => Role?.MaxHealth ?? 1f;
    public string _prevRoleIndex;

    public void SV_Spawn(RoleID role, Vector3? spawnpoint = null) => SV_Spawn(role.ToString(), spawnpoint);

    public virtual void SV_Spawn(string roleId, Vector3? spawnpoint = null)
    {
        RoleId = roleId;
        Health = MaxHealth;
        Godmode = false;
    }

    public virtual void CL_Spawn(string roleId)
    {
        // OnEquipmentChanged = () => { };
    }

    public virtual void TryChangeRole()
    {
        if (_prevRoleIndex != RoleId && controllerSpawner.IsMultiplayerAuthority())
        {
            var pos = Vector3.Zero;
            var rot = Vector3.Zero;
            if (controllerRoot.GetChildCount() != 0)
            {
                pos = PlayerPosition;
                rot = PlayerRotation;
                foreach (var item in controllerRoot.GetChildren())
                {
                    if (IsInstanceValid(item))
                    {
                        item.QueueFreeNow();
                    }
                }
            }
            if (IsInstanceValid(model))
            {
                model.QueueFreeNow();
            }
            foreach (var item in Abilities)
            {
                item.QueueFreeNow();
            }

            if (ActiveController != null)
            {
                ActiveController.Player = null;
            }

            Godot.Collections.Array arr = [Role.Controller, pos, rot];
            Node3D ctrl = (Node3D)controllerSpawner.Spawn(arr);
            SV_SetActiveController(null);
            PlayerModel mdl = (PlayerModel)modelSpawner.Spawn(Role.Model);
            foreach (var item in Role.Abilities)
            {
                BaseAbility ability = (BaseAbility)abilitySpawner.Spawn(item);
            }

            xformSync.SetTargetNode(RoleController.Root);
            viewSync.SetTargetNode(RoleController.View);

            // CallDeferred(MethodName.CL_Spawn, RoleIndex);
        }
        _prevRoleIndex = RoleId;
    }

    public virtual Node _SpawnController(Variant data)
    {
        Godot.Collections.Array arr = data.AsGodotArray();
        PackedScene scene = Data.PlayerControllers[arr[0].AsString()];
        Node3D node = scene.Instantiate<Node3D>();
        node.Position = arr[1].AsVector3();
        node.Rotation = new Vector3(0f, arr[2].AsVector3().Y, 0f);
        node.SetMultiplayerAuthority(GetMultiplayerAuthority());
        if (node is IPlayerController ctrl)
        {
            ctrl.Player = this;
        }
        xformSync.AfterTeleport();
        viewSync.AfterTeleport();
        return node;
    }

    public virtual Node _SpawnAbility(Variant data)
    {
        PlayerAbility ability = Data.AbilityLookup[data.AsString()];
        BaseAbility node = ability.Scene.Instantiate<BaseAbility>();
        node.Player = this;
        node.AbilityDef = ability;
        node.SetMultiplayerAuthority(GetMultiplayerAuthority());
        return node;
    }

    public virtual Node _SpawnModel(Variant data)
    {
        PlayerModel node = Data.PlayerModels[data.AsString()].Instantiate<PlayerModel>();
        node.Player = this;
        node.SetMultiplayerAuthority(GetMultiplayerAuthority());
        node.GlobalTransform = RoleController.Root.GlobalTransform;
        return node;
    }

#endregion

#region Controllers/Model/Abilities

    [Export] public MultiplayerSpawner controllerSpawner;
    [Export] public Node controllerRoot;
    [Export] public MultiplayerSpawner modelSpawner;
    [Export] public Node modelRoot;
    [Export] public MultiplayerSpawner abilitySpawner;
    [Export] public Node abilityRoot;
    [Export] public NodePath activeControllerPath;

    public IEnumerable<BaseAbility> Abilities => abilityRoot.GetChildren().OfType<BaseAbility>();

    public PlayerModel model => modelRoot.GetChildOrNull<PlayerModel>(0);

    public IPlayerController RoleController => controllerRoot.GetChildOrNull<IPlayerController>(0);

    public Node ActiveControllerNode => GetNodeOrNull(activeControllerPath);
    public IPlayerController ActiveController => IsInstanceValid(ActiveControllerNode) && ActiveControllerNode is IPlayerController ctrl ? ctrl : null;

    public void SV_SetActiveController(IPlayerController controller)
    {
        if (controller == null)
        {
            activeControllerPath = GetPathTo(RoleController.Root);
        }
        else
        {
            activeControllerPath = GetPathTo(controller.Root);
        }
    }

    public void ControllerUpdate()
    {
        if (RoleController != null)
        {
            xformSync.SetTargetNode(RoleController.Root);
            viewSync.SetTargetNode(RoleController.View);
        }
        if (IsLocalPlayer && ActiveController != null)
        {
            ActiveController.Camera.MakeCurrent();
        }
    }

#endregion

    public abstract bool HasAuthority { get; }
    public abstract bool IsServer { get; }
    public abstract bool IsLocalPlayer { get; }

#region Input

    public abstract void ChangeActionSet(string next);
    public abstract Vector2 GetStickPadActionData(string action);
    public abstract float GetAnalogActionData(string action);
    public abstract bool GetDigitalActionData(string action);

#endregion

#region Position/Rotation

    public void ApplyRotation(Vector3 rotation)
    {
        if (ActiveController != null)
        {
            ActiveController.Root.GlobalRotation = rotation with { X = 0f };
            ActiveController.View.Rotation = rotation with { Y = 0f, Z = 0f };
        }
    }

    public virtual void ForceTeleport(Vector3 position, Vector3 rotation)
    {
        if (ActiveController != null)
        {
            ActiveController.Root.ResetPhysicsInterpolation();
            ActiveController.Root.GlobalPosition = position;
            ActiveController.Root.GlobalRotation = rotation with { X = 0f };
            ActiveController.View.Rotation = rotation with { Y = 0f, Z = 0f };
        }
    }

    public virtual void ForceTeleportPosition(Vector3 position)
    {
        if (ActiveController != null)
        {
            ActiveController.Root.ResetPhysicsInterpolation();
            ActiveController.Root.GlobalPosition = position;
        }
    }

#endregion

#region RPCs
    public bool CallerIsServer => !Multiplayer.HasMultiplayerPeer() || Multiplayer.GetRemoteSenderId() == 1;
    public bool CallerHasAuthority => !Multiplayer.HasMultiplayerPeer() || Multiplayer.GetRemoteSenderId() == GetMultiplayerAuthority();

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void SV_PlaySound3D(int soundId)
    {
        if (CallerHasAuthority)
        {
            Rpc(nameof(CL_PlaySound3D), soundId);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void CL_PlaySound3D(int soundId)
    {
        if (CallerIsServer)
        {
            Data.Sounds[soundId].PlayOneShot3D(ActiveController.Root);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void CL_PlaySoundAt3D(int soundId, Vector3 pos)
    {
        if (CallerIsServer)
        {
            Data.Sounds[soundId].PlayOneShotAt3D(this, pos);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void SV_PlayEffect3D(int effectId)
    {
        if (CallerHasAuthority)
        {
            Rpc(nameof(CL_PlayEffect3D), effectId);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void CL_PlayEffect3D(int effectId)
    {
        if (CallerIsServer)
        {
            Effects[effectId].PlayOneShot3D(ActiveController.Camera, true);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void SV_PlayEffectAt3D(int effectId, Vector3 position, Vector3 normal)
    {
        if (CallerHasAuthority)
        {
            Rpc(nameof(CL_PlayEffectAt3D), effectId, position, normal);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void CL_PlayEffectAt3D(int effectId, Vector3 position, Vector3 normal)
    {
        if (CallerIsServer)
        {
            Vector3 rot = Basis.LookingAt(normal, normal.IsEqualApprox(Vector3.Up) || normal.IsEqualApprox(Vector3.Down) ? normal.Cross(Vector3.Forward) : Vector3.Up).GetEuler();
            Effects[effectId].PlayOneShotAt3D(ItemManager.Instance.SpawnNode, position, rot, true);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void SV_Killed(Godot.Collections.Dictionary dict)
    {
        if (CallerIsServer && HasAuthority)
        {
            DamageInfo info = new DamageInfo(dict);
            OnKilled?.Invoke(info);
            EventPlayerDeath death = EventManager.GetInstance<EventPlayerDeath>();
            death.Damager = info.Source;
            death.Victim = info.Target;
            death.Modifier = info;
            Emit(death);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void CL_ShowHint(string hintText, double time)
    {
        if (CallerIsServer)
        {
            if (IsInstanceValid(RoleHintUI.Instance))
            {
                RoleHintUI.Instance.QueueHint(hintText, string.Empty, null, time);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void CL_ClearHintQueue()
    {
        if (CallerIsServer)
        {
            if (IsInstanceValid(RoleHintUI.Instance))
            {
                RoleHintUI.Instance.ClearQueue();
            }
        }
    }

    public virtual bool Emit(IPlayerDamageableEvent evt)
    {
        evt.Player = this;
        if (evt.Damager == null)
        {
            evt.Damager = this;
        }
        if (evt.Victim == null)
        {
            evt.Victim = ActiveController as IHealth;
        }
        return Emit(evt as IEvent);
    }

    public virtual bool Emit(IEvent evt)
    {
        return EventManager.Emit(evt, this);
    }
    #endregion

}