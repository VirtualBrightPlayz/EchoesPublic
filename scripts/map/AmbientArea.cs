using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

[GlobalClass]
public partial class AmbientArea : Area3D
{
    [Export]
    public DirectionalLight3D dirLight;
    [Export]
    public Godot.Environment env;
    [Export]
    public AmbientProfile profile;
    [Export]
    public Node enableWhenInside;
    [Export]
    public float speed = 15f;
    [Export]
    public float FarZ = 0f;

    private Node3D foundNode = null;
    private HashSet<Node3D> foundNodes = new HashSet<Node3D>();

    public override void _EnterTree()
    {
        BodyEntered += OnEnter;
        BodyExited += OnExit;
        AreaEntered += OnEnter;
        AreaExited += OnExit;
    }

    public override void _ExitTree()
    {
        BodyEntered -= OnEnter;
        BodyExited -= OnExit;
        AreaEntered -= OnEnter;
        AreaExited -= OnExit;
    }

    private void OnEnter(Node3D body)
    {
        foundNodes.Add(body);
        if (IsInstanceValid(foundNode))
            return;
        if (body is IPlayerController player /*&& IsInstanceValid(player.Player) && player.Player.IsLocalPlayer*/)
        {
            if (IsInstanceValid(player.Player) && player.Player.IsLocalPlayer)
            {
                foundNode = body;
            }
        }
        else
        {
            var par = body.GetParent();
            if (IsInstanceValid(par) && par is Camera3D cam && cam.Current)
            {
                foundNode = body;
            }
        }
    }

    private void OnExit(Node3D body)
    {
        foundNodes.Remove(body);
        if (foundNode == body)
        {
            foundNode = null;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        bool found = IsInstanceValid(foundNode) || foundNodes.OfType<IPlayerController>().Any(x => IsInstanceValid(x.Player) && x.Player.IsLocalPlayer);
        if (!IsInstanceValid(NetworkPlayer.LocalInstance))
        {
            if (dirLight != null)
                dirLight.Visible = false;
            return;
        }
        // found = NetworkPlayer.LocalInstance.ActiveController.Camera

        if (dirLight != null)
        {
            dirLight.Visible = found;
        }
        if (env != null)
        {
            if (found)
            {
                if (IsInstanceValid(MenuManager.Instance) && MenuManager.Instance.GetEnv() != env)
                    MenuManager.Instance.SetEnv(env);
            }
            /*else if (MenuManager.Instance.GetEnv() == env)
            {
                MenuManager.Instance.SetEnv(RoundManager.Instance.environment);
            }*/
        }
        if (IsInstanceValid(enableWhenInside))
        {
            enableWhenInside.ProcessMode = found ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        }
        if (NetworkPlayer.LocalInstance.ActiveController != null
            && IsInstanceValid(NetworkPlayer.LocalInstance.ActiveController.Camera)
            && found)
        {
            NetworkPlayer.LocalInstance.ActiveController.Camera.Far = FarZ > 1f ? FarZ : 100f;
        }
    }
}
