using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

public partial class DebugFPController : CharacterBody3D
{
    [Export]
    public float Speed = 5.0f;

    public float EffectiveSpeed
    {
        get
        {
            return Speed;
        }
    }

    [Export]
    public float Accel = 100.0f;

    public float EffectiveAccel
    {
        get
        {
            return Accel;
        }
    }

    [Export]
    public float SprintMultiplier = 2.0f;

    public float EffectiveSprintMultiplier
    {
        get
        {
            return SprintMultiplier;
        }
    }

    [Export]
    public float SneakMultiplier = 0.25f;

    public float EffectiveSneakMultiplier
    {
        get
        {
            return SneakMultiplier;
        }
    }

    [Export]
    public float JumpVelocity = 5.0f;

    public float EffectiveJumpVelocity
    {
        get
        {
            return JumpVelocity;
        }
    }

    [Export]
    public float GravityMulti = 1.0f;

    public float EffectiveGravityMultiplier
    {
        get
        {
            return GravityMulti;
        }
    }

    public float EffectiveStaminaIncreaseMultiplier
    {
        get
        {
            return 1f;
        }
    }

    public float EffectiveStaminaDecreaseMultiplier
    {
        get
        {
            return 1f;
        }
    }

    public float EffectiveMaxStaminaMultiplier
    {
        get
        {
            return 1f;
        }
    }

    public float EffectiveStaminaPauseTimerMultiplier
    {
        get
        {
            return 1f;
        }
    }

    [Export]
    public Node3D head;
    [Export]
    public Camera3D camera;
    [Export]
    public bool Noclip = false;
    [Export]
    public LocalPlayerInput Inputs;

    public Node3D View => head;
    public Camera3D Camera => camera;

    protected bool hasSprinted;
    protected bool sprinting;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = 1f;

    public IInteractable curInteract;

    public override void _EnterTree()
    {
    }

    public override void _Ready()
    {
        gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
        camera.Current = true;
    }

    public override void _Process(double delta)
    {
		Inputs.Tick(delta);

		if (Inputs.PauseMenu.HasFlag(ButtonInputFlags.JustPressed))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
        }

        if (Inputs.AdminMenu.HasFlag(ButtonInputFlags.JustPressed))
            GetTree().Quit();

        if (Inputs.NoClip.HasFlag(ButtonInputFlags.JustPressed))
            Noclip = !Noclip;

        ApplyRotation(delta);
		Inputs.Flush();
	}

    public override void _PhysicsProcess(double delta)
    {
        if (Noclip)
            NoclipMovement(delta);
        else
            Movement(delta);
        HandleInputs(delta);
    }

    public virtual void ApplyRotation(double delta)
    {
        var mouseMotion = Inputs.MouseMotion;

        Rotation += new Vector3(0, mouseMotion.X, 0);
        head.Rotation += new Vector3(mouseMotion.Y, 0, 0);
        head.Rotation = new Vector3(Mathf.Clamp(head.Rotation.X, -1.55f, 1.55f), head.Rotation.Y, head.Rotation.Z);
    }

    public virtual void HandleInputs(double delta)
    {
    }

    public virtual float GetSpeedMultiplier()
    {
        bool isSneaking = Inputs.Crouch.HasFlag(ButtonInputFlags.Pressed);
        bool isSprinting = CanSprint();
        float multiplier = isSneaking ? EffectiveSneakMultiplier : isSprinting ? EffectiveSprintMultiplier : 1.0f;
        return multiplier;
    }

    public virtual bool CanSprint()
    {
        return Inputs.Sprint.HasFlag(ButtonInputFlags.Pressed);
    }

    public virtual Vector3 GetMovementVelocity(Vector3 vel, double delta)
    {
        Vector3 velocity = vel;

        // Handle Jump.
        if (Inputs.Jump.HasFlag(ButtonInputFlags.JustPressed) && IsOnFloor())
            velocity.Y = EffectiveJumpVelocity;


        float multiplier = GetSpeedMultiplier();
        //Not needed. EffectiveSpeed does this math already.
        //if (Player is NetworkPlayer plr)
        //    multiplier *= plr.GetMovementEffectsMultiplier();

        Vector2 inputDir = Inputs.MovementDirection;
        Vector3 direction = (Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        float y = velocity.Y;
        if (direction.IsZeroApprox())
        {
            velocity = velocity.MoveToward(Vector3.Zero, EffectiveAccel * (float)delta);
        }
        else
        {
            velocity = velocity.MoveToward(direction * EffectiveSpeed * multiplier, EffectiveAccel * (float)delta);
        }
        velocity.Y = y;

        return velocity;
    }

    public virtual void NoclipMovement(double delta)
    {
        float multiplier = GetSpeedMultiplier();
        Vector2 inputDir = Inputs.MovementDirection;
        Vector3 direction = (camera.GlobalBasis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized() * EffectiveSpeed * 2f * multiplier;

        Velocity = Vector3.Zero;
        GlobalPosition += direction * (float)delta;
    }

    public virtual void Movement(double delta)
    {
        bool wasOnFloor = IsOnFloor();
        Vector3 velocity = Velocity;

        // Add the gravity.
        if (!IsOnFloor())
            velocity.Y -= gravity * (float)delta * EffectiveGravityMultiplier;
        else
            velocity.Y = 0f;

        velocity = GetMovementVelocity(velocity, delta);

        Velocity = velocity;

        Vector3 val = this.MoveAndStairs(delta, velocity, UpDirection, FloorSnapLength);
        GlobalPosition += val;

        MoveAndSlide();
    }
}
