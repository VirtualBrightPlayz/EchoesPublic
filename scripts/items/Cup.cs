using System;
using System.Buffers.Binary;
using System.IO;
using Godot;

[GlobalClass]
public partial class Cup : WorldItem
{
    public const int ViewmodelEventDrink = 1;

    [Export]
    public Color color = Colors.White;
    [Export]
    public bool empty = true;
    [Export]
    public string drinkName = string.Empty;
    [Export]
    public bool emission = false;
    [Export]
    public bool damage = false;
    [Export]
    public bool heal = false;
    [Export]
    public float damageAmount = 0f;
    [Export]
    public float healAmount = 0f;
    [Export]
    public int soundIndex = -1;
    [Export]
    public Timer destroyTimer;
    [Export]
    public PackedScene cupPropPrefab;

    /*
    public override void PrimaryPress(NetworkPlayer player)
    {
        if (!player.Multiplayer.IsServer())
            return;
        if (empty)
            return;
        empty = true;
        player.Rpc(nameof(BasePlayer.SV_ItemModelEvent), ViewmodelEventDrink);
        if (soundIndex != -1)
        {
            player.Rpc(nameof(BasePlayer.CL_PlaySoundAt3D), soundIndex, player.PlayerPosition);
        }
        if (heal)
        {
            player.Heal(new HealInfo(healAmount));
        }
        if (damage)
        {
            player.Damage(new DamageInfo(damageAmount));
        }
    }
    */

    public override void _Ready()
    {
        base._Ready();
        destroyTimer.Timeout += Destroy;
    }

    private void Destroy()
    {
        Item.QueueFree();
        if (IsInstanceValid(cupPropPrefab))
        {
            var node = cupPropPrefab.Instantiate<Node3D>();
            node.Position = GlobalPosition;
            node.Rotation = GlobalRotation;
            ItemManager.Instance.PropSpawnNode.AddChild(node);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcDrink()
    {
        if (IsMultiplayerAuthority() && Item.SenderIsPlayer)
        {
            if (empty)
                return;
            empty = true;
            destroyTimer.Start();
            Rpc(MethodName.RpcOnDrink);
            BasePlayer player = Item.Player;
            if (soundIndex != -1)
            {
                player.Rpc(nameof(BasePlayer.CL_PlaySound3D), soundIndex);
            }
            if (heal)
            {
                player.Heal(new HealInfo(healAmount));
            }
            if (damage)
            {
                player.Damage(new DamageInfo(damageAmount));
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcOnDrink()
    {
        if (Item.viewModel is ViewmodelCup view)
        {
            view.ViewmodelEvent(ViewmodelEventDrink);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        MeshInstance3D inst = GetNodeOrNull<MeshInstance3D>("%Liquid");
        inst.Visible = !empty;
        if (inst.MaterialOverride is StandardMaterial3D mat)
        {
            mat.AlbedoColor = color;
            mat.EmissionEnabled = emission;
            mat.Emission = color;
        }
        else if (inst.MaterialOverride is ShaderMaterial shMat)
        {
            shMat.SetShaderParameter("liquid_color", color);
            shMat.SetShaderParameter("foam_color", color);
        }
        if (Item.PrimaryHolder != null && Item.Player.HasAuthority && Item.PrimaryHolder.InputPrimary.HasFlag(ButtonInputFlags.JustPressed) && !empty)
        {
            Rpc(MethodName.RpcDrink);
        }
    }

    public void FromDrink(SCP294Drink drink)
    {
        empty = false;
        drinkName = drink.ResourceName;
        emission = drink.emission;
        color = drink.color;
        damage = drink.damage;
        heal = drink.heal;
        damageAmount = drink.damageAmount;
        healAmount = drink.healAmount;
        soundIndex = ItemManager.Instance.Data.Sounds.IndexOf(drink.drinkSound);
    }

    public override string ToString()
    {
        return $"Cup of {drinkName}";
    }
}