using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class PhysicsProp3D : RigidbodySync, IHealth, IFlammable
{
    public static List<PhysicsProp3D> PhysicsProps = new List<PhysicsProp3D>();

    [Export]
    public PropCategory Category = PropCategory.Other;
    
    [ExportGroup("Prop Parents")]
    [Export]
    private TransformSyncMode _postDetachSyncMode = TransformSyncMode.Position | TransformSyncMode.Rotation;

    [Export]
    private bool _freezeUntilDetached = false;
    
    [Export]
    private bool _doGravityWhenAttached = true;
    
    [Export]
    private bool _allowGrabPostDetatch = true;
    
    [Export]
    public PhysicsProp3D Parent;

    /// <summary>
    /// Used to find the root of the prop, if this is null, the global root will be used instead.
    /// </summary>
    [Export]
    private Node3D _root;

    [Export]
    private bool _detatchOnParentDeath = true;
    
    [Export]
    private bool _sharesHpWithParent = true;
    
    private List<PhysicsProp3D> _children = new List<PhysicsProp3D>();

    public void RegisterChild(PhysicsProp3D child)
    {
        // child.Reparent(this);
        _children.Add(child);
    }
    
    public event Action OnDeathEvent;
    
    [ExportGroup("Sounds")]
    [Export]
    public Godot.Collections.Dictionary<SoundType, Godot.Collections.Array<AudioStream>> Sounds { get; set; } = new Godot.Collections.Dictionary<SoundType, Godot.Collections.Array<AudioStream>>();

    [Export]
    public AudioStreamPlayer3D PropAudio { get; set; }

    [Export]
    public int MaxCollisionCalls { get; set; } = 5;

    [Export]
    public bool PlaySounds { get; set; } = false;
    
    [ExportGroup("Damage")]
    [Export]
    public bool AllowDamage { get; set; } = false;
    
    [Export]
    public float Health { get; set; } = -1;
    
    [Export]
    public float MaxHealth { get; set; } = -1;
    
    [ExportGroup("Death Actions")]
    [Export] 
    public DeathAction OnDeath { get; set; } = DeathAction.None;
    
    [Export]
    public float FadeOutSpeed { get; set; } = 2.0f;

    [Export]
    public MeshInstance3D Mesh;

    [Export]
    public Node[] DestroyOnDeath;
    
    [ExportGroup("Extra")]
    [Export]
    public bool Scp106CanPassThrough { get; set; } = true;

    private GodotObject[] prevCollisions;
    private GodotObject[] collisions;
    private float[] collisionVols;

    private bool _hadParent = false;

    public bool IsDead { get; set; } = false;

    public ulong spawnTimestamp = 0;

    public override void _EnterTree()
    {
        base._EnterTree();
        spawnTimestamp = Time.GetTicksMsec();
        _oldPosition = Position;
        _oldRotation = Rotation;
        _oldScale = Scale;
        PhysicsProps.Add(this);
        if (IsInstanceValid(Parent))
        {
            SetupParent();
        }
        SetMeta(IHealth.MetaName, this);
        // CollisionLayer = (1 << 0) | (1 << 7) | (1 << 8);
        // CollisionMask = (1 << 0) | (1 << 1) | (1 << 7);
        if ((CollisionLayer & (1 << 0)) != 0)
        {
            CollisionMask |= (1 << 1); // always collide with players
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        PhysicsProps.Remove(this);
        BodyEntered -= OnCollide;
    }

    public override void _Ready()
    {
        base._Ready();
        if (PlaySounds)
        {
            ContactMonitor = true;
            // MaxContactsReported = MaxCollisionCalls;
            prevCollisions = new GodotObject[MaxContactsReported];
            collisions = new GodotObject[MaxContactsReported];
            collisionVols = new float[MaxContactsReported];
        }
        BodyEntered += OnCollide;
        if (IsInstanceValid(ItemManager.Instance) && !IsInstanceValid(Parent))
            ItemManager.Instance.RespawnPropIfNeeded(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (fireTimeLeft >= 0.0d)
        {
            fireTimeLeft -= delta;
        }
        else if(OnFire)
        {
            Extinguish();
        }
    }

    public void PlayRandomSound(SoundType type, float volume)
    {
        if (IsMultiplayerAuthority())
            Rpc(MethodName.RpcPlaySound, (int)type, volume);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcPlaySound(int typeid, float volume)
    {
        SoundType type = (SoundType)typeid;
        if (Sounds.ContainsKey(type) && Sounds[type].Count > 0)
        {
            PlaySound(Sounds[type][(int)GD.Randi() % Sounds[type].Count], volume);
        }
    }

    private void PlaySound(AudioStream stream, float volumedb)
    {
        if (PlaySounds && IsInstanceValid(PropAudio))
        {
            if (!PropAudio.HasStreamPlayback())
            {
                PropAudio.Stream = new AudioStreamPolyphonic();
                PropAudio.Play();
            }
            if (PropAudio.HasStreamPlayback() && PropAudio.GetStreamPlayback() is AudioStreamPlaybackPolyphonic playback)
            {
                playback.PlayStream(stream, volumeDb: Mathf.Clamp(volumedb, -80f, 0f));
            }
        }
    }
    
    private void OnCollide(Node other)
    {
        if (PlaySounds)
        {
            // PlayRandomSound(SoundType.Collide);
        }
    }

    private float _oldGravityScale;

    private Vector3 _oldPosition;
    
    private Vector3 _oldRotation;
    
    private Vector3 _oldScale;
    
    private void SetupParent()
    {
        Parent.RegisterChild(this);
        Parent.OnDeathEvent += OnParentDeath;
        if (!_doGravityWhenAttached)
        {
            _oldGravityScale = GetGravityScale();
            SetGravityScale(0f);
        }
        // _hadParent = true;
    }

    public override void _Joined(long id)
    {
        base._Joined(id);
        if (_hadParent && IsInstanceValid(Parent) && IsMultiplayerAuthority())
        {
            // RpcId(id, PhysicsProp3D.MethodName.RpcDetached);
        }
        if (IsMultiplayerAuthority() && IsDead)
        {
            RpcId(id, MethodName.RpcKill);
        }
    }

    public override void DamageOther(IHealth health, DamageInfo info)
    {
        info.AddModifier(new PropDamageModifier());
        base.DamageOther(health, info);
    }

    public override void UpdateState(PhysicsDirectBodyState3D state)
    {
        base.UpdateState(state);
        if (Sleeping)
            return;
        if (IsMultiplayerAuthority() && PlaySounds)
        {
            for (int i = 0; i < collisions.Length; i++)
            {
                prevCollisions[i] = collisions[i];
                collisions[i] = null;
            }
            int j = 0;
            for (int i = 0; i < state.GetContactCount(); i++)
            {
                GodotObject collider = state.GetContactColliderObject(i);
                Vector3 vel = state.GetContactLocalVelocityAtPosition(i);
                Vector3 norm = state.GetContactLocalNormal(i);
                if (vel.Length() >= 1f)
                {
                    float vol = vel.Length() / 10f;
                    collisions[j] = collider;
                    collisionVols[j] = Mathf.LinearToDb(vol);
                    j++;
                }
            }
            for (int i = 0; i < collisions.Length; i++)
            {
                int idx = Array.IndexOf(prevCollisions, collisions[i]);
                if (idx == -1)
                {
                    PlayRandomSound(SoundType.Collide, collisionVols[i]);
                }
            }
        }
        if (IsMultiplayerAuthority())
        {
            for (int i = 0; i < state.GetContactCount(); i++)
            {
                UpdateCollisionWith(state, i);
            }
        }
    }

    public virtual void UpdateCollisionWith(PhysicsDirectBodyState3D state, int i)
    {
        GodotObject collider = state.GetContactColliderObject(i);
        IHealth hp = IHealth.GetHealth(collider);
        Vector3 vel = state.GetContactLocalVelocityAtPosition(i);
        Vector3 norm = state.GetContactLocalNormal(i);
        if (hp != null && hp is not IPlayerController)
        {
            float len = vel.Length();
            if (len >= 5f)
            {
                DamageOther(hp, new DamageInfo(Mass * 5f, this, DamageType.Crushed));
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    private void RpcKill()
    {
        Kill(lastDamage);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    private void RpcDetached()
    {
        Detach();
    }

    private void OnParentDeath()
    {
        if (_detatchOnParentDeath)
        {
            Detach();
        }
    }
    
    public void Attach(PhysicsProp3D parent)
    {
        this.Parent = parent;
        // Reparent(parent);
    }

    public void Detach()
    {
        if (!_doGravityWhenAttached)
        {
            SetGravityScale(_oldGravityScale);
        }
        this.Parent = null;
        if (DestroyOnDeath != null)
        {
            foreach (var node in DestroyOnDeath)
            {
                if (IsInstanceValid(node))
                {
                    node.QueueFree();
                }
            }
        }
    }
    
    public virtual void Spawn(HealInfo info)
    {
        if (_sharesHpWithParent)
        {
            Parent.Spawn(info);
        }
        Health = MaxHealth;
        IsDead = false;

        foreach (int ownerId in GetShapeOwners())
        {
            if (ShapeOwnerGetOwner((uint)ownerId) is CollisionShape3D shape3D)
            {
                shape3D.Disabled = false;
            }
        }
        Visible = true;
        UpdateFreezeState();
    }

    public DamageInfo lastDamage;
    
    public virtual void Damage(DamageInfo info)
    {
        if (_sharesHpWithParent && IsInstanceValid(Parent))
        {
            Parent.Damage(info);
            return;
        }
        if (!AllowDamage)
        {
            return;
        }
        //Already dead, can't deal damage.
        if (Health <= 0f)
        {
            return;
        }
        Health = (Health - info.Amount);
        lastDamage = info;
        if (Health <= 0f)
        {
            Kill(info);
        }
        else
        {
            if (PlaySounds)
            {
                PlayRandomSound(SoundType.Damage, 0f);
            }
        }
    }

    public virtual void Heal(HealInfo info)
    {
        if (_sharesHpWithParent && IsInstanceValid(Parent))
        {
            Parent.Heal(info);
            return;
        }
        if (!AllowDamage)
        {
            return;
        }
        Health = Mathf.Clamp(info.Amount, 0, MaxHealth);
    }

    public void RecursiveKill(DamageInfo info)
    {
        foreach (PhysicsProp3D p in _children)
        {
            if(!IsInstanceValid(p))
            {
                return;
            }
            p.RecursiveKill(info);
        }
        Kill(info);
    }

    public void RecursiveHeal(HealInfo info)
    {
        foreach (PhysicsProp3D p in _children)
        {
            if(!IsInstanceValid(p))
            {
                return;
            }
            p.RecursiveHeal(info);
        }
        Heal(info);
    }

    public void RecursiveDamage(DamageInfo info)
    {
        foreach (PhysicsProp3D p in _children)
        {
            if(!IsInstanceValid(p))
            {
                return;
            }
            p.RecursiveDamage(info);
        }
        Damage(info);
    }
    
    public virtual void Kill(DamageInfo info)
    {
        if (_sharesHpWithParent && IsInstanceValid(Parent))
        {
            Parent.Kill(info);
            return;
        }
        if (!AllowDamage)
        {
            return;
        }

        lastDamage = info;
        Health = 0f;
        IsDead = true;

        if (IsMultiplayerAuthority())
            Rpc(MethodName.RpcKill);

        if (PlaySounds)
        {
            PlayRandomSound(SoundType.Break, 0f);
        }

        switch (OnDeath)
        {
            case DeathAction.None:
                break;
            case DeathAction.Vanish:
                Vanish();
                break;
            case DeathAction.FadeOut:
                FadeOut();
                break;
            case DeathAction.Shatter:
                if (Shatter()) return;
                break;
            case DeathAction.Detach:
                DetachOnDeath();
                break;
            case DeathAction.Custom:
                OnCustomDeath(info);
                break;
        }
        if (DestroyOnDeath != null)
        {
            foreach (var node in DestroyOnDeath)
            {
                if (IsInstanceValid(node))
                {
                    node.QueueFree();
                }
            }
        }
    }

    public virtual void DetachOnDeath()
    {
        if (IsInstanceValid(Parent))
        {
            Detach();
        }

        Heal(new HealInfo(MaxHealth));
    }

    public virtual bool Shatter()
    {
        foreach (PhysicsProp3D p in _children)
        {
            if(!IsInstanceValid(p))
            {
                return true;
            }
            p.Detach();
        }

        return false;
    }

    public virtual void FadeOut()
    {
        if (!IsInstanceValid(Mesh))
        {
            throw new InvalidOperationException(
                "No Mesh is set for prop which is set up to fade out on death.");
        }
        canGrab = false;
        xformSync.SetClaimant((int)MultiplayerPeer.TargetPeerBroadcast);
        Tween tween = CreateTween();
        tween.SetParallel();
        for (int i = 0; i < Mesh.GetSurfaceOverrideMaterialCount(); i++)
        {
            BaseMaterial3D material = Mesh.GetSurfaceOverrideMaterial(i) as BaseMaterial3D;
            if (!IsInstanceValid(material))
            {
                //TODO: LOG
            }
            else
            {
                Color color = material.AlbedoColor;
                color.A = 0.0f;
                tween.TweenProperty(Mesh.GetSurfaceOverrideMaterial(i), new NodePath(BaseMaterial3D.PropertyName.AlbedoColor), color, FadeOutSpeed).SetTrans(Tween.TransitionType.Linear);
            }
        }
        tween.Finished += Vanish;
    }

    public virtual void Vanish()
    {
        canGrab = false;
        xformSync.SetClaimant((int)MultiplayerPeer.TargetPeerBroadcast);
        foreach (int ownerId in GetShapeOwners())
        {
            if (ShapeOwnerGetOwner((uint)ownerId) is CollisionShape3D shape3D)
            {
                shape3D.Disabled = true;
            }
        }
        Visible = false;
        UpdateFreezeState();
    }

    public override void UpdateFreezeState()
    {
        base.UpdateFreezeState();
        return;
        if (IsMultiplayerAuthority() && IsDead && AllowDamage)
        {
            Freeze = true;
        }
        else
        {
            base.UpdateFreezeState();
        }
    }

    public virtual void OnCustomDeath(DamageInfo info)
    {
        
    }
    
    [Flags]
    public enum TransformSyncMode : byte
    {
        Position = 2,
        Rotation = 4,
        Scale = 8,
    }
    
    public enum DeathAction
    {
        None,
        FadeOut,
        Vanish,
        Shatter,
        Detach,
        Custom,
    }

    public enum SoundType
    {
        Collide,
        Damage,
        Break,
    }

    [Export]
    public FireWorldEffects FireEffect { get; set; }

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

    public bool OnFire
    {
        get
        {
            if (!IsInstanceValid(FireEffect))
            {
                return false;
            }
            return FireEffect.Enabled;
        }
    }
    
    [Export]
    public float Flammability { get; set; }

    private double fireTimeLeft;
    
    public void Ignite(float duration)
    {
        fireTimeLeft = duration;
        if (!IsInstanceValid(FireEffect))
        {
            return;
        }
        FireEffect.Enabled = true;
    }

    public void Extinguish()
    {
        fireTimeLeft = 0d;
        if (!IsInstanceValid(FireEffect))
        {
            return;
        }
        FireEffect.Enabled = false;
    }
}

public enum PropCategory
{
    Other,
    Containment_Breach,
    Furniture,
    Office,
    Small_Props,
    Industrial,
    Container,
    Functional,
    Decoration,
    Stock,
    Food,
    Garbage,
    Janitorial,
    Laboratory,
    Custom,
}
