using System;
using Godot;

public class PropDamageModifier : DamageInfoModifier
{
    public PropDamageModifier()
    {
    }

    public override bool Active { get; set; } = true;
    
    public override void Apply(Node node)
    {
        if (node is BasePlayer player)
        {
            int idx = player.Data.Sounds.IndexOf(player.hitSound);
            if (idx != -1)
                player.Rpc(BasePlayer.MethodName.CL_PlaySoundAt3D, idx, player.PlayerPosition);
        }
    }

    public override void ApplyPostMortem(Node node)
    {
    }
}