using Godot;

public partial class SpectatorController : CharacterBody3D, IPlayerController
{
    [Export]
    public Node3D head;
    [Export]
    public Camera3D camera;
    [Export]
    public Label label;
    [Export]
    public float Speed = 5.0f;
    [Export]
    public float Accel = 100.0f;
    public int playerIndex = -1;
    public int PreviousIndex = -1;

    public NetworkPlayer OtherPlayer =>
        playerIndex == -1
            ? null
            : IPlayerList.List(this).PlayerList[
                Mathf.Clamp(playerIndex, 0, IPlayerList.List(this).PlayerList.Count - 1)
            ];

    public NetworkPlayer PreviousPlayer =>
        PreviousIndex == -1
            ? null
            : IPlayerList.List(this).PlayerList[
                Mathf.Clamp(PreviousIndex, 0, IPlayerList.List(this).PlayerList.Count - 1)
            ];
    
    public bool nextDirection = false;
    public bool freeCam = false;
    [Export]
    public Timer textTimer;
    [Export]
    public float viewOffset;
    [Export]
    public Area3D area;

    public int camIndex = 0;

    public Vector2 MovementDirection => Player.GetStickPadActionData(LocalPlayerInput.PlayerMove);
    public Vector2 MouseMotion => Player.GetStickPadActionData(LocalPlayerInput.PlayerCamera) * Settings.User.MouseSensitivity;
    public ButtonInputFlags ButtonUse = ButtonInputFlags.None;
    public ButtonInputFlags ButtonPrimary = ButtonInputFlags.None;
    public ButtonInputFlags ButtonSecondary = ButtonInputFlags.None;
    public ButtonInputFlags ButtonJump = ButtonInputFlags.None;
    public ButtonInputFlags ButtonSprint = ButtonInputFlags.None;
    public ButtonInputFlags ButtonCrouch = ButtonInputFlags.None;

    public Node3D Floor => this;
    public Node3D View => head;
    public Camera3D Camera => camera;
    public BasePlayer Player { get; set; }

    public override void _EnterTree()
    {
        SetMultiplayerAuthority(GetParent().GetMultiplayerAuthority());
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(OtherPlayer) && IsInstanceValid(OtherPlayer.model))
            OtherPlayer.model.SetWorldModelVisible(true);

        if (IsInstanceValid(PreviousPlayer) && IsInstanceValid(PreviousPlayer.model))
            PreviousPlayer.model.SetWorldModelVisible(true);

        if (!Multiplayer.HasMultiplayerPeer())
            return;
        if (IsMultiplayerAuthority())
        {
            // Player.Inputs.Cursor.Set(CursorManager.CursorType.Spectator, false);
            var list = IPlayerList.List(this).PlayerList;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].ActiveController != null)
                    list[i].ActiveController.Root.Visible = true;
            }
        }
    }

    public override void _Ready()
    {
        if (Player.IsLocalPlayer)
        {
            camera.Current = true;
            // Player.Inputs.Cursor.Set(CursorManager.CursorType.Spectator, true);
            // camera.GlobalPosition += Vector3.Up;
            camera.Position = Vector3.Zero;
            camIndex = GD.RandRange(0, int.MaxValue);
        }

        // SetProcess(IsMultiplayerAuthority());
        freeCam = true;
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority())
            return;
        ApplyRotation(delta);

        if ((IsInstanceValid(RoundManager.Instance) && RoundManager.Instance.state == RoundManager.RoundState.WaitingForPlayers) || IPlayerList.List(this).PlayerList.Count <= 1)
        {
            var nodes = GetTree().GetNodesInGroup("spectate_cam");
            if (nodes.Count != 0)
            {
                var node = (Node3D)nodes[camIndex % nodes.Count];
                if (IsInstanceValid(node))
                {
                    camera.GlobalPosition = node.GlobalPosition;
                    camera.GlobalRotation = node.GlobalRotation;
                }
            }
        }
        else if (freeCam)
        {
            camera.Transform = Transform3D.Identity;
        }
        else if (IsInstanceValid(OtherPlayer))
        {
            var cam = OtherPlayer.ActiveController.Camera;
            var xform = cam.GlobalTransform;
            camera.GlobalTransform = xform.Translated(OtherPlayer.ActiveController.Root.GlobalBasis.Z * viewOffset);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateInputs();
        if (!IsMultiplayerAuthority())
            return;

        var list = IPlayerList.List(this).PlayerList;

        if (ButtonPrimary.HasFlag(ButtonInputFlags.JustPressed) /*|| (nextDirection && OtherPlayer.RoleIndex == (int)RoleID.Spectator)*/)
        {
            nextDirection = true;
            Next();
            for (int i = 1; i < list.Count && OtherPlayer.Role.team == TeamID.Dead; i++)
            {
                Next();
            }
        }
        if (ButtonSecondary.HasFlag(ButtonInputFlags.JustPressed) /*|| (!nextDirection && OtherPlayer.RoleIndex == (int)RoleID.Spectator)*/)
        {
            nextDirection = false;
            Prev();
            for (int i = 1; i < list.Count && OtherPlayer.Role.team == TeamID.Dead; i++)
            {
                Prev();
            }
        }

        Color c = label.Modulate;
        if (freeCam)
        {
            c.A = Mathf.Clamp((float)textTimer.TimeLeft, 0f, 1f);
        }
        else
        {
            if (IsInstanceValid(OtherPlayer))
            {
                label.Text = $"{OtherPlayer.username} ({Tr(OtherPlayer.Role.DisplayName)}, {OtherPlayer.Health:0} HP)";
                label.Modulate = OtherPlayer.Role.RoleColor;
            }
        }
        label.Modulate = c;

        if (ButtonUse.HasFlag(ButtonInputFlags.JustPressed))
        {
            freeCam = !freeCam;
            OnChangePlayer();
        }

        if (freeCam)
        {
            Vector2 inputDir = MovementDirection;
            float updownDir = 0f;
            if (ButtonCrouch.HasFlag(ButtonInputFlags.Pressed))
                updownDir -= 1f;
            if (ButtonJump.HasFlag(ButtonInputFlags.Pressed))
                updownDir += 1f;
            Vector3 direction = (GlobalBasis * new Vector3(inputDir.X, updownDir, inputDir.Y)).Normalized();
            Velocity = Velocity.MoveToward(direction * Player.Role.Speed, Player.Role.Acceleration * (float)delta);
            MoveAndSlide();
        }
        else if (IsInstanceValid(OtherPlayer))
        {
            if (OtherPlayer.Role.team == TeamID.Dead || !MovementDirection.IsZeroApprox())
            {
                freeCam = true;
                OnChangePlayer();
            }
        }
    }

    public virtual void UpdateInputs()
    {
        if (!Player.IsLocalPlayer)
        {
            return;
        }
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerUse, ref ButtonUse);
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerPrimary, ref ButtonPrimary);
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerSecondary, ref ButtonSecondary);
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerJump, ref ButtonJump);
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerSprint, ref ButtonSprint);
        InputManager.UpdateInput(Player, LocalPlayerInput.PlayerSneak, ref ButtonCrouch);
    }

    public void ApplyRotation(double delta)
    {
        var mouseMotion = MouseMotion;

        Rotation += new Vector3(0, mouseMotion.X, 0);
        if (!InputManager.Instance.IsVR)
        {
            head.Rotation += new Vector3(mouseMotion.Y, 0, 0);
            head.Rotation = new Vector3(Mathf.Clamp(head.Rotation.X, -1.55f, 1.55f), head.Rotation.Y, head.Rotation.Z);
        }
    }

    public void NextCamera()
    {
        ResetPhysicsInterpolation();
        camIndex++;
    }

    public void Next()
    {
        var list = IPlayerList.List(this).PlayerList;
        int count = list.Count;

        PreviousIndex = playerIndex;
        playerIndex = (playerIndex + 1 + count) % count;
        freeCam = false;
        OnChangePlayer();
    }

    public void Prev()
    {
        var list = IPlayerList.List(this).PlayerList;
        int count = list.Count;

        PreviousIndex = playerIndex;
        playerIndex = (playerIndex - 1 + count) % count;
        freeCam = false;
        OnChangePlayer();
    }

    public void OnChangePlayer()
    {
        if (freeCam)
        {
            if (IsInstanceValid(OtherPlayer) && IsInstanceValid(OtherPlayer.model))
                OtherPlayer.model.SetWorldModelVisible(true);

            if (IsInstanceValid(PreviousPlayer) && IsInstanceValid(PreviousPlayer.model))
                PreviousPlayer.model.SetWorldModelVisible(true);

            ResetPhysicsInterpolation();
            return;
        }

        if (IsInstanceValid(PreviousPlayer) && PreviousPlayer.Role.team != TeamID.Dead)
        {
            if (IsInstanceValid(PreviousPlayer.model))
                PreviousPlayer.model.SetWorldModelVisible(true);
        }

        if (IsInstanceValid(OtherPlayer) && OtherPlayer.Role.team != TeamID.Dead)
        {
            if (IsInstanceValid(OtherPlayer.model))
                OtherPlayer.model.SetWorldModelVisible(false);

            GlobalPosition = OtherPlayer.PlayerPosition;
            GlobalRotation = Vector3.Up * OtherPlayer.PlayerRotation.Y;
            label.Text = $"{OtherPlayer.username} ({Tr(OtherPlayer.Role.DisplayName)}, {OtherPlayer.Health:0} HP)";
            label.Modulate = OtherPlayer.Role.RoleColor;
            textTimer.Start();
        }

        ResetPhysicsInterpolation();
    }

    public void OnDamaged(DamageInfo info)
    {
    }

    public void OnKilled(DamageInfo info)
    {
    }
}
