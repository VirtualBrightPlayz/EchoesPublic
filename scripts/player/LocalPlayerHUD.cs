using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class LocalPlayerHUD : Control
{
    [Export]
    public NetworkPlayer Player;
    [Export]
    public TextureRect Crosshair;
    // [Export]
    // public TextureRect handIcon;
    [Export]
    public Control[] handIcons = [];
    [Export]
    public Color handIconColor = Colors.White;
    [Export]
    public float handIconSpeed = 10f;
    [Export]
    public Texture2D pointIcon;
    [Export]
    public Texture2D useIcon;
    [Export]
    public Texture2D useGrabbingIcon;
    [Export]
    public Texture2D grabIcon;
    [Export]
    public Texture2D grabbingIcon;
    [Export]
    public Label useLabel;
    [Export]
    public Theme mainTheme;
    [Export]
    public TextureRect mouse;

    [Export]
    public ChaseMusic chaseMusicScript;

    [ExportGroup("Health")]
    [Export]
    public ProgressBar healthBar;
    [Export]
    public TextureRect healthIcon;
    [Export]
    public TextureRect healthOverlay;

    public enum UIElementVisibilityState
    {
        Invisible,
        FadeIn,
        Visible,
        FadeOut,
    }

    [ExportGroup("Healthbar Fadeout")]
    [Export]
    public UIElementVisibilityState DefaultUiElementVisibilityState = UIElementVisibilityState.FadeOut;

    private UIElementVisibilityState _hpBarVisibilityState = UIElementVisibilityState.Invisible;

    private double healthBarTimer = 0.0f;

    private double oldHealth = 0.0f;

    private Tween healthBarTween;
    
    [Export]
    private bool doHealthBarFade = true;

    [Export]
    private double healthBarFadeInSpeed = 0.1f;

    [Export]
    private double healthBarFadeOutSpeed = 0.5f;

    [Export]
    private double healthBarRemainsVisibleFor = 2.0f;

    [Export]
    private double healthBarMaximumFadeIn = 1f;

    [Export]
    private double healthBarMaximumFadeOut = 0f;

    [ExportGroup("Flashbang")]
    [Export]
    public UIElementVisibilityState DefaultFlashbangVisibilityState = UIElementVisibilityState.Invisible;

    private UIElementVisibilityState _flashbangState = UIElementVisibilityState.Invisible;

    private double _flashbangTimer = 0.0f;

    private double _flashDuration = 0.0f;

    private double _oldFlashDuration = 0.0f;
    
    private Tween _flashbangTween;

    [Export]
    private double _flashBangFadeOutDuration = 1.0f;

    [Export]
    private double _flashBangFadeInDuration = 0.01f;
    
    [Export]
    private ColorRect _flashBangEffect;

    [ExportGroup("Hide UI Controls")]
    [Export]
    private Control[] _hideableControls = new Control[0];

    private bool _controlsHidden = false;

    public int handIconState = 0;
    
    public void Flash(double duration)
    {
        _flashbangTimer = duration;
        _flashDuration = duration;
    }

    public void Unflash()
    {
        _flashDuration = 0.0f;
        _flashbangTimer = 0.0f;
        _flashbangState = UIElementVisibilityState.FadeOut;
    }
    
    [ExportGroup("Radiation")]
    [Export]
    public ProgressBar radiationBar;
    [Export]
    public TextureRect radiationIcon;
    [Export]
    public Label radiationLabel;
    [Export]
    public Color radiationColorGood;
    [Export]
    public Color radiationColorBad;
    [Export]
    public Color radiationColorWarn;
    [ExportGroup("Role Intro")]
    [Export]
    public Control roleThemedRoot;
    [Export]
    public Control roleIntroRoot;
    [Export]
    public AnimationPlayer roleIntroAnim;
    [Export]
    public Label roleIntroLabel;
    [Export]
    public RichTextLabel roleLabel;
    [Export]
    public RichTextLabel roleObjective;
    [ExportGroup("Effects")]
    [Export]
    public EffectOverlayManager EffectOverlayManager;
    [ExportGroup("Inventory")]
    [Export]
    public Control inventoryMenu;
    [Export]
    public Camera3D invCamera;
    [Export]
    public float invCamHeightRatio = 0.5f;
    [Export]
    public float invCamDist = 1f;
    // public RichTextLabel inventoryText => inventoryMenu.GetNodeOrNull("RadialMenu/CenterNode").GetChild<RichTextLabel>(0);
    [Export]
    public Label ammoLabel;
    [ExportGroup("Sprint")]
    [Export]
    public ProgressBar SprintBar;
    [Export]
    public Control SprintBarRoot;
    [Export]
    public Control SprintBarRootIn;
    [Export]
    public Control SprintBarRootOut;
    [ExportGroup("Chase Music")]
    [Export]
    public AudioStreamPlayer chaseMusic;
    [ExportGroup("Player List")]
    [Export]
    public Control playerListRoot;
    [Export]
    public RichTextLabel playerInfoLabel;
    [Export]
    public float playerInfoMaxRange = 2f;
    [Export]
    public float playerInfoLerpSpeed = 2f;
    [Export]
    public Curve playerInfoCurve;

    [ExportGroup("Menu")]
    [Export]
    public Control menuRoot;
    [Export]
    public Control menuContainer;
    [Export]
    public Button menuReturn;
    [Export]
    public Button menuExit;
    [Export]
    public Button SettingsOpen;
    [Export]
    public Control SettingsControl;

    [ExportGroup("Mic")]
    [Export]
    public Control micRoot;
    [Export]
    public TextureRect micBG;
    [Export]
    public TextureRect micFG;

    [ExportGroup("Nuke")]
    [Export]
    public ColorRect nukeOverlay;
    [Export]
    public float GlitchEffectAmount
    {
        get => ((ShaderMaterial)nukeOverlay.Material).GetShaderParameter("amount").AsSingle();
        set => ((ShaderMaterial)nukeOverlay.Material).SetShaderParameter("amount", value);
    }

    [ExportGroup("Role GUI")]
    [Export]
    public Control roleGuiRoot;
    [Export]
    public AnimationPlayer hitmarkerAnim;
    [Export]
    public RichTextLabel roleNameText;

    public PlayerRole Role => Player?.Role;
    public Color RoleColor => Role?.RoleColor ?? Colors.White;
    public float Health => Player?.Health ?? 0f;
    public float Min_Health => 0f;
    public float Max_Health => Player?.MaxHealth ?? 1f;
    private double lerpHealth = 1.0f;
    private int spawning = 0;

    public Control roleGUI;

    public bool MicOn
    {
        get => micRoot.Visible;
        set => micRoot.Visible = value;
    }
    public float MicAmount
    {
        get => ((ShaderMaterial)micFG.Material).GetShaderParameter("progress").AsSingle();
        set => ((ShaderMaterial)micFG.Material).SetShaderParameter("progress", value);
    }

    public bool InventoryMenuVisible => inventoryMenu.Visible;

    private MouseButtonMask lastButtonMask;
    private bool _paused;

    private ButtonInputFlags debugHideUI = ButtonInputFlags.None;
    private ButtonInputFlags buttonPlayerListUI = ButtonInputFlags.None;
    private ButtonInputFlags buttonPause = ButtonInputFlags.None;

    private PlayerHud hudUtil;

    public List<int> itemSerialList = new List<int>();

    public override void _EnterTree()
    {
        chaseMusic.VolumeDb = Mathf.LinearToDb(0f);
        menuContainer.Visible = false;
        menuRoot.Visible = false;
        nukeOverlay.Visible = false;
        _hpBarVisibilityState = DefaultUiElementVisibilityState;
        _flashbangState = DefaultFlashbangVisibilityState;
    }

    public override void _Ready()
    {
        hudUtil = new PlayerHud();
        Player.OnLocalSpawned += OnSpawn;
        chaseMusic.VolumeDb = Mathf.LinearToDb(0f);
        menuRoot.Visible = IsMultiplayerAuthority();
        nukeOverlay.Visible = IsMultiplayerAuthority();
        menuReturn.Pressed += CloseMenu;
        menuExit.Pressed += ExitGame;
        SettingsOpen.Pressed += SettingsOpen_Pressed;
        UpdateMenu();
        Visible = IsMultiplayerAuthority();
    }

    private void SettingsOpen_Pressed()
    {
        if (SettingsControl.Visible)
        {
            return;
        }
        SettingsControl.Visible = true;
    }

    public override void _ExitTree()
    {
        Player.OnLocalSpawned -= OnSpawn;
        menuReturn.Pressed -= CloseMenu;
        menuExit.Pressed -= ExitGame;
    }

    public double flashbangTimer = 0f;
    public double flashbangOpacity = 0f;
    private Tween flashbangTweener;
    
    
    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority())
        {
            return;
        }

        InputManager.UpdateInput(Player, LocalPlayerInput.debug_player_hide_ui, ref debugHideUI);
        if (debugHideUI.HasFlag(ButtonInputFlags.JustPressed))
        {
            // rotate bool other way
            _controlsHidden = !_controlsHidden;
            foreach (var control in _hideableControls)
            {
                // if it is the same value as the bool
                if (control.Visible == !_controlsHidden)
                {
                    continue;
                }
                control.Visible = !_controlsHidden;
            }

            if (_controlsHidden)
            {
                DebugText.Instance.Hider = this;
            }
            else
            {
                DebugText.Instance.Hider = null;
            }
            DebugText.Instance.HideDebugInfo = _controlsHidden;
        }
        
        roleThemedRoot.Visible = !IsInstanceValid(RoundManager.Instance) || (RoundManager.Instance.state == RoundManager.RoundState.InGame && Player.Role.team != TeamID.Dead);
        ammoLabel.Visible = false;

        nukeOverlay.Visible = GlitchEffectAmount > 0f;
        lastButtonMask = Input.GetMouseButtonMask();

        if (IsInstanceValid(_flashBangEffect))
        {
            if (_oldFlashDuration != _flashDuration)
            {
                _flashbangState = UIElementVisibilityState.FadeIn;
                _oldFlashDuration = _flashDuration;
                _flashbangTimer = 0f;
            }
            else
            {
                if (_flashbangState == UIElementVisibilityState.Visible)
                {
                    _flashbangTimer += delta;
                    if (_flashbangTimer >= _flashDuration)
                    {
                        _flashDuration = 0.0f;
                        _flashbangTimer = 0.0f;
                        _flashbangState = UIElementVisibilityState.FadeOut;
                    }
                }
            }
        }
        
        
        if(doHealthBarFade && IsInstanceValid(healthBar))
        {
            if (oldHealth != Health)
            {
                healthBarTimer = 0.0f;
                _hpBarVisibilityState = UIElementVisibilityState.FadeIn;
                oldHealth = Health;
            }
            else
            {
                if (_hpBarVisibilityState == UIElementVisibilityState.Visible)
                {
                    healthBarTimer += delta;
                    if (healthBarTimer >= healthBarRemainsVisibleFor)
                    {
                        _hpBarVisibilityState = UIElementVisibilityState.FadeOut;
                    }
                }
            }

            Color healthBarColor = new Color(healthBar.Modulate.R, healthBar.Modulate.G, healthBar.Modulate.B);
            switch (_hpBarVisibilityState)
            {
                case UIElementVisibilityState.Invisible:
                    healthBarColor.A = (float)healthBarMaximumFadeOut;
                    healthBar.Modulate = healthBarColor;
                    break;
                case UIElementVisibilityState.Visible:
                    healthBarColor.A = (float)healthBarMaximumFadeIn;
                    healthBar.Modulate = healthBarColor;
                    break;
                case UIElementVisibilityState.FadeIn:
                    if (healthBar.Modulate.A >= healthBarMaximumFadeIn)
                    {
                        _hpBarVisibilityState = UIElementVisibilityState.Visible;
                        break;
                    }
                    healthBarColor.A = (float)healthBarMaximumFadeIn;
                    if (!IsInstanceValid(healthBarTween))
                    {
                        healthBarTween = CreateTween();
                        healthBarTween.TweenProperty(healthBar, new NodePath(CanvasItem.PropertyName.Modulate), healthBarColor, healthBarFadeInSpeed).SetTrans(Tween.TransitionType.Linear);
                    }
                    else
                    {
                        if (!healthBarTween.IsValid())
                        {
                            healthBarTween = CreateTween();
                            healthBarTween.TweenProperty(healthBar, new NodePath(CanvasItem.PropertyName.Modulate), healthBarColor, healthBarFadeInSpeed).SetTrans(Tween.TransitionType.Linear);
                        }
                        else
                        {
                            if (!healthBarTween.IsRunning())
                            {
                                var tweener =healthBarTween.TweenProperty(healthBar, new NodePath(CanvasItem.PropertyName.Modulate),
                                    healthBarColor, healthBarFadeInSpeed);
                                if (IsInstanceValid(tweener))
                                {
                                    tweener.SetTrans(Tween.TransitionType.Linear);
                                }
                            }
                        }
                    }
                    break;
                case UIElementVisibilityState.FadeOut:
                    if (healthBar.Modulate.A <= healthBarMaximumFadeOut)
                    {
                        _hpBarVisibilityState = UIElementVisibilityState.Invisible;
                        break;
                    }
                    healthBarColor.A = (float)healthBarMaximumFadeOut;
                    if (!IsInstanceValid(healthBarTween))
                    {
                        healthBarTween = CreateTween();
                        healthBarTween.TweenProperty(healthBar, new NodePath(CanvasItem.PropertyName.Modulate), healthBarColor, healthBarFadeOutSpeed).SetTrans(Tween.TransitionType.Linear);
                    }
                    else
                    {
                        if (!healthBarTween.IsValid())
                        {
                            healthBarTween = CreateTween();
                            healthBarTween.TweenProperty(healthBar, new NodePath(CanvasItem.PropertyName.Modulate), healthBarColor, healthBarFadeOutSpeed).SetTrans(Tween.TransitionType.Linear);
                        }
                        else
                        {
                            if (!healthBarTween.IsRunning())
                            {
                                var tweener = healthBarTween.TweenProperty(healthBar, new NodePath(CanvasItem.PropertyName.Modulate),
                                    healthBarColor, healthBarFadeOutSpeed);
                                if (IsInstanceValid(tweener))
                                {
                                    tweener.SetTrans(Tween.TransitionType.Linear);
                                }
                            }
                        }
                    }
                    break;
            }
        }

        if (IsInstanceValid(_flashBangEffect))
        {
            Color flashColor = new Color(_flashBangEffect.Color.R, _flashBangEffect.Color.G, _flashBangEffect.Color.B);
            switch (_flashbangState)
            {
                case UIElementVisibilityState.Invisible:
                    flashColor.A = 0.0f;
                    _flashBangEffect.Color = flashColor;
                    break;
                case UIElementVisibilityState.Visible:
                    flashColor.A = 1.0f;
                    _flashBangEffect.Color = flashColor;
                    break;
                case UIElementVisibilityState.FadeIn:
                    if (_flashBangEffect.Color.A >= 1.0f)
                    {
                        _flashbangState = UIElementVisibilityState.Visible;
                        break;
                    }
                    flashColor.A = 1.0f;
                    if (!IsInstanceValid(_flashbangTween))
                    {
                        _flashbangTween = CreateTween();
                        _flashbangTween.TweenProperty(_flashBangEffect, new NodePath(ColorRect.PropertyName.Color), flashColor, _flashBangFadeInDuration).SetTrans(Tween.TransitionType.Linear);
                    }
                    else
                    {
                        if (!_flashbangTween.IsValid())
                        {
                            _flashbangTween = CreateTween();
                            _flashbangTween.TweenProperty(_flashBangEffect, new NodePath(ColorRect.PropertyName.Color), flashColor, _flashBangFadeInDuration).SetTrans(Tween.TransitionType.Linear);
                        }
                        else
                        {
                            if (!_flashbangTween.IsRunning())
                            {
                                var tweener =_flashbangTween.TweenProperty(_flashBangEffect, new NodePath(ColorRect.PropertyName.Color),
                                    flashColor, _flashBangFadeInDuration);
                                if (IsInstanceValid(tweener))
                                {
                                    tweener.SetTrans(Tween.TransitionType.Linear);
                                }
                            }
                        }
                    }
                    break;
                case UIElementVisibilityState.FadeOut:
                    if (_flashBangEffect.Color.A <= 0.0f)
                    {
                        _flashbangState = UIElementVisibilityState.Invisible;
                        break;
                    }
                    flashColor.A = 0.0f;
                    if (!IsInstanceValid(_flashbangTween))
                    {
                        _flashbangTween = CreateTween();
                        _flashbangTween.TweenProperty(_flashBangEffect, new NodePath(ColorRect.PropertyName.Color), flashColor, _flashBangFadeOutDuration).SetTrans(Tween.TransitionType.Linear);
                    }
                    else
                    {
                        if (!_flashbangTween.IsValid())
                        {
                            _flashbangTween = CreateTween();
                            _flashbangTween.TweenProperty(_flashBangEffect, new NodePath(ColorRect.PropertyName.Color), flashColor, _flashBangFadeOutDuration).SetTrans(Tween.TransitionType.Linear);
                        }
                        else
                        {
                            if (!_flashbangTween.IsRunning())
                            {
                                var tweener = _flashbangTween.TweenProperty(_flashBangEffect, new NodePath(ColorRect.PropertyName.Color),
                                    flashColor, _flashBangFadeOutDuration);
                                if (IsInstanceValid(tweener))
                                {
                                    tweener.SetTrans(Tween.TransitionType.Linear);
                                }
                            }
                        }
                    }
                    break;
            }
        }
        
        
        Color roleColor = new Color(RoleColor.R, RoleColor.G, RoleColor.B, healthBar.Modulate.A);

        healthBar.Modulate = roleColor;
        healthIcon.Modulate = roleColor;

        lerpHealth = Mathf.MoveToward(lerpHealth, Health, Max_Health * delta * 1.6f);
        lerpHealth = Health;
        healthBar.MaxValue = Max_Health;
        healthBar.MinValue = Min_Health;
        healthBar.Value = lerpHealth;

        if (IsInstanceValid(Crosshair))
        {
            if (_controlsHidden)
            {
                foreach (var item in handIcons)
                {
                    item.Visible = false;
                }
            }
            else
            {
                foreach (var item in handIcons)
                {
                    item.Visible = true;
                }
            }
            float lerpColor = (float)delta * handIconSpeed;
            for (int i = 0; i < handIcons.Length; i++)
            {
                if (i == handIconState)
                {
                    handIcons[i].SelfModulate = handIcons[i].SelfModulate.Lerp(handIconColor, lerpColor);
                }
                else
                {
                    handIcons[i].SelfModulate = handIcons[i].SelfModulate.Lerp(Colors.Transparent, lerpColor);
                }
            }
        }

        if (IsInstanceValid(healthOverlay))
        {
            healthOverlay.Visible = true;
            healthOverlay.SelfModulate = new Color(1f, 1f, 1f, 1f - Health / Max_Health);
        }

        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerListName, ref buttonPlayerListUI);
        if (buttonPlayerListUI.HasFlag(ButtonInputFlags.JustPressed))
            TogglePlayerList();

        InputManager.UpdateInput(Player, LocalPlayerInput.MenuPause, ref buttonPause);
        if (buttonPause.HasFlag(ButtonInputFlags.JustPressed))
        {
            _paused = !_paused;
            playerListRoot.Visible = false;
            inventoryMenu.Visible = false;
            UpdateMenu();
        }

        if (IsInstanceValid(mouse))
        {
            mouse.Visible = false;
        }

        UpdateInventoryCamera();
        CallDeferred(MethodName.UpdateInventoryCamera);
    }

    public void UpdateInventoryCamera()
    {
        if (IsInstanceValid(invCamera))
        {
            var pos = Player.ActiveController.Root.GlobalPosition.Lerp(Player.ActiveController.Camera.GlobalPosition, invCamHeightRatio);
            Aabb? aabb2 = Player.model.GetAabb();
            if (aabb2.HasValue)
            {
                Aabb aabb = aabb2.Value.Abs();
                Vector3 center = aabb.GetCenter();
                float halfHeight = aabb.Size.Y / 2f;
                invCamera.Fov = Mathf.RadToDeg(Mathf.Atan(halfHeight) * 2f);
                invCamera.Size = aabb.Size.Y;
                invCamera.LookAtFromPosition(center - Player.ActiveController.Root.GlobalBasis.Z.Normalized() * (aabb.Size.Z * 2f), center);
            }
            invCamera.Visible = inventoryMenu.Visible;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        playerInfoLabel.Visible = !Multiplayer.HasMultiplayerPeer() || IsMultiplayerAuthority();
        if (Multiplayer.HasMultiplayerPeer() && !IsMultiplayerAuthority())
            return;

        UpdateInventoryCamera();
        CallDeferred(MethodName.UpdateInventoryCamera);

        /*
        if (Input.IsKeyPressed(Key.Key1))
        {
            var list = Player.InventoryEquipped;
            foreach (var item in list)
            {
                item.GripsRelease();
            }
            if (IsInstanceValid(inventorySlots[0].itemObj))
            {
                inventorySlots[0].itemObj.GripGrab(Player);
            }
        }
        if (Input.IsKeyPressed(Key.Key2))
        {
            var list = Player.InventoryEquipped;
            foreach (var item in list)
            {
                item.GripsRelease();
            }
            if (IsInstanceValid(inventorySlots[1].itemObj))
            {
                inventorySlots[1].itemObj.GripGrab(Player);
            }
        }
        if (Input.IsKeyPressed(Key.Key3))
        {
            var list = Player.InventoryEquipped;
            foreach (var item in list)
            {
                item.GripsRelease();
            }
            if (IsInstanceValid(inventorySlots[2].itemObj))
            {
                inventorySlots[2].itemObj.GripGrab(Player);
            }
        }
        if (Input.IsKeyPressed(Key.Key4))
        {
            var list = Player.InventoryEquipped;
            foreach (var item in list)
            {
                item.GripsRelease();
            }
            if (IsInstanceValid(inventorySlots[3].itemObj))
            {
                inventorySlots[3].itemObj.GripGrab(Player);
            }
        }
        */

        if (IsInstanceValid(Player) && Player.ActiveController != null)
        {
            Vector3 fwd = Player.ActiveController.Camera.GlobalTransform.Basis * Vector3.Forward;
            fwd = fwd.Normalized();
            var exclude = new Godot.Collections.Array<Rid>();
            if (Player.ActiveController is PhysicsBody3D ch)
                exclude.Add(ch.GetRid());
            if (Player.ActiveController is SpectatorController spec && IsInstanceValid(spec.OtherPlayer) && spec.OtherPlayer.ActiveController is PhysicsBody3D ch2)
                exclude.Add(ch2.GetRid());
            PhysicsRayQueryParameters3D ray = PhysicsRayQueryParameters3D.Create(Player.ActiveController.Camera.GlobalPosition + fwd * 0.01f, Player.ActiveController.Camera.GlobalPosition + fwd * playerInfoMaxRange, ItemManager.Instance.playerLayer, exclude);
            var output = Player.GetWorld3D().DirectSpaceState.IntersectRay(ray);
            if (output.Count > 0)
            {
                var obj = output["collider"].AsGodotObject();
                var pos = output["position"].AsVector3();
                if (obj is IPlayerController ctrl && IsInstanceValid(ctrl.Player))
                {
                    var dist = pos.DistanceTo(Player.ActiveController.Camera.GlobalPosition);
                    playerInfoLabel.Text = $"[center]{ctrl.Player.AttackerDisplayName}\n{ctrl.Player.Role.RichDisplayName}[/center]";
                    playerInfoLabel.Modulate = Colors.White * playerInfoCurve.Sample(Mathf.Clamp(dist / playerInfoMaxRange, 0f, 1f));
                }
                else if (obj.HasMeta(Ragdoll.META_NAME))
                {
                    var ragdoll = obj.GetMeta(Ragdoll.META_NAME).As<Ragdoll>();
                    var dist = pos.DistanceTo(Player.ActiveController.Camera.GlobalPosition);
                    playerInfoLabel.Text = $"[center]{ragdoll.playerName}'s body.\nThey were {ragdoll.Role.RichDisplayName}.\nCause of death: {DamageUtils.TranslateType(ragdoll.Type)}[/center]";
                    playerInfoLabel.Modulate = Colors.White * playerInfoCurve.Sample(Mathf.Clamp(dist / playerInfoMaxRange, 0f, 1f));
                }
                else
                {
                    playerInfoLabel.Modulate = playerInfoLabel.Modulate.Lerp(Colors.Transparent, (float)delta * playerInfoLerpSpeed);
                }
            }
            else
            {
                playerInfoLabel.Modulate = playerInfoLabel.Modulate.Lerp(Colors.Transparent, (float)delta * playerInfoLerpSpeed);
            }
        }
        else
        {
            playerInfoLabel.Modulate = playerInfoLabel.Modulate.Lerp(Colors.Transparent, (float)delta * playerInfoLerpSpeed);
            playerInfoLabel.Text = string.Empty;
        }
    }

    public void OnHit(DamageInfo info, Node target)
    {
        hitmarkerAnim.Stop();
        hitmarkerAnim.Play("hit");
        if (IsInstanceValid(target) && target is BasePlayer plr && (plr.Health - info.Amount <= 0f || plr.Role.team == TeamID.Dead))
        {
            StatusRpcManager.Instance.PlayerKilled(plr);
        }
    }

    public void CloseMenu()
    {
        _paused = false;
        UpdateMenu();
    }

    public async void UpdateMenu()
    {
        // Player.Inputs.Cursor.Set(CursorManager.CursorType.Pause, _paused);
        InputManager.Instance.ChangeActionSet(_paused ? "Menu" : "InGame");
        menuContainer.Visible = _paused;
        var tween = CreateTween();
        tween.TweenProperty(menuRoot, new NodePath(CanvasItem.PropertyName.Modulate), _paused ? Colors.White : Colors.Transparent, 0.15d);
        await ToSignal(tween, Tween.SignalName.Finished);
        menuRoot.Visible = menuContainer.Visible;
    }

    public void ExitGame()
    {
        NetworkManager.Instance.Shutdown();
    }

    public void TogglePlayerList()
    {
        SetPlayerListVisible(!playerListRoot.Visible);
    }

    public void SetPlayerListVisible(bool val)
    {
        playerListRoot.Visible = val;
        InputManager.Instance.ChangeActionSet(val ? "Inventory" : "InGame");
    }

    public void ToggleInventoryGui()
    {
        if (inventoryMenu.Visible)
            CloseInventoryGui();
        else
            OpenInventoryGui();
    }

    public void CloseInventoryGui()
    {
        if (InputManager.Instance.CurrentSet.ResourceName == "Inventory")
        {
            InputManager.Instance.ChangeActionSet("InGame");
        }
        inventoryMenu.Visible = false;
    }

    public void OpenInventoryGui()
    {
        InputManager.Instance.ChangeActionSet("Inventory");
        inventoryMenu.Visible = true;
    }

    #region SprintBar
    public Tween sprintBarTween;

    public void SprintBarSpawnIn()
    {
        if (sprintBarTween.IsValid())
        {
            sprintBarTween.Kill();
        }
        var tween = CreateTween();
        SprintBarRoot.Visible = true;
        tween.Finished += OnSprintTweenFinish;
        // tween.TweenProperty(SprintBarRoot, new NodePath(Control.PropertyName.Position), SprintBarRootIn.Position, 0.5d).From(SprintBarRootOut.Position);
        tween.TweenProperty(SprintBarRoot, new NodePath(CanvasItem.PropertyName.Modulate), Colors.White, 0.5d).From(Colors.Transparent);
        sprintBarTween = tween;
    }

    public void SprintBarSpawnOut()
    {
        if (sprintBarTween != null && sprintBarTween.IsValid())
        {
            sprintBarTween.Kill();
        }
        var tween = CreateTween();
        SprintBarRoot.Visible = true;
        tween.Finished += OnSprintTweenFinish;
        // tween.TweenProperty(SprintBarRoot, new NodePath(Control.PropertyName.Position), SprintBarRootOut.Position, 0.5d).From(SprintBarRootIn.Position);
        tween.TweenProperty(SprintBarRoot, new NodePath(CanvasItem.PropertyName.Modulate), Colors.Transparent, 0.5d).From(Colors.White);
        sprintBarTween = tween;
    }

    public void OnSprintTweenFinish()
    {
        SprintBarRoot.Visible = !SprintBarRoot.Modulate.IsEqualApprox(Colors.Transparent);
    }
    #endregion

    public void RoleUpdated()
    {
        CloseInventoryGui();
        if (IsInstanceValid(roleGUI))
            roleGUI.QueueFree();
        if (!IsInstanceValid(Player.Role) || !IsInstanceValid(Player.Role.GUIScene))
            return;
        roleGUI = Player.Role.GUIScene.Instantiate<Control>();
        roleGuiRoot.AddChild(roleGUI);
        healthBarTimer = 0.0f;
        _hpBarVisibilityState = UIElementVisibilityState.Visible;
        oldHealth = 0.0f;
    }

    public void UpdateThemeColors(string item, Color color)
    {
        foreach (var stylebox in Theme.GetStyleboxList(item))
        {
            if (Theme.GetStylebox(stylebox, item) is StyleBoxFlat flat)
            {
                flat.BorderColor = color;
            }
        }
    }

    public void OnSpawn(PlayerRole role)
    {
        if (IsInstanceValid(roleNameText))
        {
            roleNameText.Text = $"[center]{Tr("ROLE_INTRO")}\n{role.RichDisplayName}[/center]";
        }

        Theme = (Theme)mainTheme.Duplicate(true);

        var roleColor = role?.RoleColor ?? Colors.White;
        UpdateThemeColors(nameof(Button), roleColor);
        UpdateThemeColors(nameof(PanelContainer), roleColor);
        UpdateThemeColors(nameof(CheckBox), roleColor);

        chaseMusic.VolumeDb = Mathf.LinearToDb(0f);

        // handIcon.Visible = false;
        handIconState = 0;
        SprintBarSpawnOut();

        roleIntroRoot.Visible = false;
        // if (role == RoleID.Spectator)
        {
            return;
        }

        hudUtil.SpawnIntro(this, role, roleIntroRoot, roleThemedRoot, roleIntroLabel, roleLabel, roleObjective);
    }
}
