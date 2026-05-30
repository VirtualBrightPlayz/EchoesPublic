using Godot;
using System;
using System.Linq;

public partial class LocalPlayerHUDVR : Control
{
    [Export]
    public NetworkPlayer Player;
    [Export]
    public CanvasLayer uiMenuLayer;
    [Export]
    public Theme mainTheme;
    [Export]
    public SubViewport handUI;
    [Export]
    public SubViewport viewUI;
    [Export]
    public SubViewport menuViewport;
    [Export]
    public Node3D menuRoot;
    [Export]
    public Control menuRootUI;
    [Export]
    public WorldCanvas menuCanvas;

    [ExportGroup("VR GUI")]
    [Export]
    public MenuVRHand menuHandLeft;
    [Export]
    public MenuVRHand menuHandRight;
    [Export]
    public MeshInstance3D blinkOverlay;

    [ExportGroup("Health")]
    [Export]
    public ProgressBar healthBar;
    [Export]
    public TextureRect healthIcon;

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

    [ExportGroup("Inventory")]
    [Export]
    public Node3D inventoryMenu;
    [Export]
    public InventorySocket[] inventorySlots;

    [ExportGroup("Sprint")]
    [Export]
    public ProgressBar SprintBar;
    [Export]
    public Control SprintBarRoot;

    [ExportGroup("Player List")]
    [Export]
    public Control playerListRoot;
    [Export]
    public ItemList playerListItems;
    [Export]
    public RichTextLabel playerListLabel;
    [Export]
    public RichTextLabel playerInfoLabel;
    [Export]
    public float playerInfoMaxRange = 2f;
    [Export]
    public float playerInfoLerpSpeed = 2f;
    [Export]
    public Curve playerInfoCurve;

    [ExportGroup("Mic")]
    [Export]
    public Control micRoot;
    [Export]
    public TextureRect micBG;
    [Export]
    public TextureRect micFG;

    [ExportGroup("Role GUI")]
    [Export]
    public Control roleGuiRoot;

    public PlayerRole Role => Player?.Role;
    public Color RoleColor => Role?.RoleColor ?? Colors.White;
    public float Health => (Player?.ActiveController is SpectatorController spec ? spec.OtherPlayer?.Health : Player?.Health)?? 0f;
    public float Min_Health => 0f;
    public float Max_Health => (Player?.ActiveController is SpectatorController spec ? spec.OtherPlayer?.MaxHealth : Player?.MaxHealth)?? 1f;
    private double lerpHealth = 1.0f;
    private int spawning = 0;
    private IPlayerList _playerList;

    public Control roleGUI;

    public bool InventoryMenuVisible => inventoryMenu.Visible;

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

    private ButtonInputFlags buttonPlayerListUI = ButtonInputFlags.None;
    private ButtonInputFlags buttonPause = ButtonInputFlags.None;

    private PlayerHud hudUtil;
    private bool _paused;

    public override void _EnterTree()
    {
        Player.OnLocalSpawned += OnSpawn;
    }

    public override void _Ready()
    {
        CloseInventoryGui();
        hudUtil = new PlayerHud();
        _playerList = IPlayerList.List(this);
        bool isVr = IInitScript.Instance.IsXR;
        handUI.RenderTargetUpdateMode = isVr ? SubViewport.UpdateMode.Always : SubViewport.UpdateMode.Disabled;
        viewUI.RenderTargetUpdateMode = isVr ? SubViewport.UpdateMode.Always : SubViewport.UpdateMode.Disabled;
        _paused = false;
        UpdateMenu();
        if (isVr)
        {
            uiMenuLayer.CallDeferred(MethodName.Reparent, menuViewport);
        }
    }

    public override void _ExitTree()
    {
        Player.OnLocalSpawned -= OnSpawn;
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority())
        {
            return;
        }
        if (!InputManager.Instance.IsVR)
            return;
        // roleIntroRoot.Visible = !IsInstanceValid(RoundManager.Instance) || (RoundManager.Instance.state == RoundManager.RoundState.InGame || RoundManager.Instance.state == RoundManager.RoundState.End);
        roleThemedRoot.Visible = !IsInstanceValid(RoundManager.Instance) || (RoundManager.Instance.state == RoundManager.RoundState.InGame && Player.Role.team != TeamID.Dead);

        lerpHealth = Mathf.MoveToward(lerpHealth, Health, Max_Health * delta * 1.6f);
        lerpHealth = Health;
        healthBar.MaxValue = Max_Health;
        healthBar.MinValue = Min_Health;
        healthBar.Value = lerpHealth;
        healthBar.Modulate = RoleColor;
        healthIcon.Modulate = RoleColor;

        if (playerListLabel != null && IsInstanceValid(RoundManager.Instance))
        {
            playerListLabel.Text = RoundManager.Instance.ServerSettings.Name;
        }
        if (playerListItems != null && _playerList != null)
        {
            playerListItems.Clear();
            foreach (var plr in _playerList.PlayerList)
            {
                playerListItems.AddItem(plr.username);
            }
        }

        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerListName, ref buttonPlayerListUI);
        if (IsInstanceValid(playerListRoot) && buttonPlayerListUI.HasFlag(ButtonInputFlags.JustPressed))
            TogglePlayerList();

        InputManager.UpdateInput(Player, LocalPlayerInput.MenuPause, ref buttonPause);
        if (buttonPause.HasFlag(ButtonInputFlags.JustPressed))
        {
            _paused = !_paused;
            UpdateMenu();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateInventory();
        if (!IsMultiplayerAuthority())
            return;
        if (!InputManager.Instance.IsVR)
            return;

        if (IsInstanceValid(blinkOverlay) && IsInstanceValid(RoundManager.Instance))
        {
            blinkOverlay.Visible = Player.Role.team != TeamID.Dead && Player.Role.team != TeamID.SCP && RoundManager.Instance.IsBlinking && _playerList.PlayerList.Any(x => x.TryGetAbility(out StatueAbility statue) && statue.IsSeenBy(Player.ActiveController.Root));
        }
        else if (IsInstanceValid(blinkOverlay))
            blinkOverlay.Visible = false;

        if (Player != null && Player.ActiveController != null && IsInstanceValid(playerInfoLabel))
        {
            Vector3 fwd = Player.ActiveController.Camera.GlobalTransform.Basis * Vector3.Forward;
            fwd = fwd.Normalized();
            var exclude = new Godot.Collections.Array<Rid>();
            if (Player.ActiveController is PhysicsBody3D ch)
                exclude.Add(ch.GetRid());
            if (Player.ActiveController is SpectatorController spec && spec.OtherPlayer.ActiveController is PhysicsBody3D ch2)
                exclude.Add(ch2.GetRid());
            PhysicsRayQueryParameters3D ray = PhysicsRayQueryParameters3D.Create(Player.ActiveController.Camera.GlobalPosition + fwd * 0.01f, Player.ActiveController.Camera.GlobalPosition + fwd * playerInfoMaxRange, ItemManager.Instance.attackLayer, exclude);
            var output = Player.GetWorld3D().DirectSpaceState.IntersectRay(ray);
            if (output.Count > 0)
            {
                var obj = output["collider"].AsGodotObject();
                var pos = output["position"].AsVector3();
                if (obj is IPlayerController ctrl)
                {
                    var dist = pos.DistanceTo(Player.ActiveController.Camera.GlobalPosition);
                    playerInfoLabel.Text = $"[center]{ctrl.Player.AttackerDisplayName}\n{ctrl.Player.Role.RichDisplayName}[/center]";
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
        else if (IsInstanceValid(playerInfoLabel))
        {
            playerInfoLabel.Modulate = playerInfoLabel.Modulate.Lerp(Colors.Transparent, (float)delta * playerInfoLerpSpeed);
            playerInfoLabel.Text = string.Empty;
        }
    }

    public void UpdateMenu()
    {
        menuRoot.Visible = _paused;
        // menuRootUI.Visible = _paused;
        if (IsInstanceValid(Player) && IsInstanceValid(VRPlayerOrigin.Instance))
            menuRoot.GlobalRotation = VRPlayerOrigin.Instance.head.GlobalRotation;
    }

    public void ExitGame()
    {
        NetworkManager.Instance.Shutdown();
    }

    #region Inventory
    public void ToggleInventoryGui()
    {
        UpdateInventoryTextures();
        inventoryMenu.Visible = !inventoryMenu.Visible;
    }

    public void CloseInventoryGui()
    {
        inventoryMenu.Visible = false;
    }

    public void UpdateInventoryTextures()
    {
        bool hasInv = Player.TryGetAbility(out InventoryAbility inventory);
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (hasInv && i < inventory.Inventory.Length && (inventory.Inventory[i].PrimaryHolder == null || inventory.Inventory[i].PrimaryHolder is InventorySocket))
                inventorySlots[i].PickupWorldItem(inventory.Inventory[i].Serial, true);
            else
                inventorySlots[i].PickupWorldItem(-1, true);
        }
    }

    public void UpdateInventory()
    {
        if (IsInstanceValid(Player) && Player.ActiveController != null && IsInstanceValid(VRPlayerOrigin.Instance))
        {
            inventoryMenu.GlobalTransform = Player.ActiveController.Root.GlobalTransform;
            inventoryMenu.GlobalPosition = VRPlayerOrigin.Instance.head.GlobalTransform.Origin + Player.ActiveController.Root.GlobalBasis.Orthonormalized() * new Vector3(0f, 0f, -0.75f);
        }
    }
    #endregion

    #region Player List
    public void TogglePlayerList()
    {
        SetPlayerListVisible(!playerListRoot.Visible);
    }

    public void SetPlayerListVisible(bool val)
    {
        playerListRoot.Visible = val;
    }
    #endregion

    public void RoleUpdated()
    {
        CloseInventoryGui();
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
        Theme = (Theme)mainTheme.Duplicate(true);

        var roleColor = role?.RoleColor ?? Colors.White;
        UpdateThemeColors(nameof(Button), roleColor);
        UpdateThemeColors(nameof(PanelContainer), roleColor);
        UpdateThemeColors(nameof(CheckBox), roleColor);

        roleIntroRoot.Visible = false;
    }
}
