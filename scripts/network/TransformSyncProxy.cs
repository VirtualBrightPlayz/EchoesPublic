using Godot;

public static class TransformSyncProxy
{
    public static StringName IsAuthorityName = "is_authority";
    public static StringName TargetNodeName = "target_node";
    public static StringName SendRelativeToName = "send_relative_to";
    public static StringName AfterTeleportName = "after_teleport";
    public static StringName SendToAllName = "send_to_all";
    public static StringName SendToPeerName = "send_to_peer";
    public static StringName AuthorityIdName = "authority_id";
    public static StringName SyncIntervalName = "sync_interval_ticks";
    public static StringName SyncTransformName = "sync_transform";

    /*
    public static void SyncSetSyncTransform(this Node sync, bool value)
    {
        sync.Set(SyncTransformName, value);
    }

    public static bool SyncGetSyncTransform(this Node sync)
    {
        return sync.Get(SyncTransformName).AsBool();
    }

    public static bool SyncIsAuthority(this Node sync)
    {
        if (GodotObject.IsInstanceValid(sync))
            return sync.Call(IsAuthorityName).AsBool();
        Log.PrintWarn("Sync not valid in SyncIsAuthority.");
        return true;
    }

    public static void SyncSetTargetNode(this Node sync, NodePath path)
    {
        // if (GodotObject.IsInstanceValid(sync))
            sync.Set(TargetNodeName, path);
    }

    public static ulong SyncGetSyncInterval(this Node sync)
    {
        // if (GodotObject.IsInstanceValid(sync))
            return sync.Get(SyncIntervalName).AsUInt64();
    }

    public static void SyncSetSyncInterval(this Node sync, ulong ticks)
    {
        // if (GodotObject.IsInstanceValid(sync))
            sync.Set(SyncIntervalName, ticks);
    }

    public static void SyncSendRelativeTo(this Node sync, NodePath path)
    {
        // if (GodotObject.IsInstanceValid(sync))
            sync.Call(SendRelativeToName, path);
    }

    public static void SyncSetAuthorityId(this Node sync, long id)
    {
        // if (GodotObject.IsInstanceValid(sync))
            sync.Set(AuthorityIdName, id);
    }

    public static void SyncAfterTeleport(this Node sync)
    {
        // if (GodotObject.IsInstanceValid(sync))
            sync.Call(AfterTeleportName);
    }

    public static void SyncSendToAll(this Node sync)
    {
        if (!sync.IsNodeReady())
        {
            GD.PrintErr($"{sync.GetPath()} NOT READY");
        }
        // if (GodotObject.IsInstanceValid(sync))
            sync.Call(SendToAllName);
    }

    public static void SyncSendToPeer(this Node sync, long id)
    {
        if (!sync.IsNodeReady())
        {
            GD.PrintErr($"{sync.GetPath()} NOT READY");
        }
        // if (GodotObject.IsInstanceValid(sync))
            sync.Call(SendToPeerName, id);
    }
    */
}