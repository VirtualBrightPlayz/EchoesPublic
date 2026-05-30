using System;
using System.Linq;
using Godot;

[GlobalClass]
public partial class ObjectivePoint : Node3D
{
	[Export]
	public ObjectiveAsset objective;
	[Export]
	public ObjectiveAsset[] nextObjectives = Array.Empty<ObjectiveAsset>();

	public override void _EnterTree()
	{
		base._EnterTree();
		if (IsInstanceValid(ObjectivePointManager.Instance))
		{
			ObjectivePointManager.Instance.Points.Add(this);
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		if (IsInstanceValid(ObjectivePointManager.Instance))
		{
			ObjectivePointManager.Instance.Points.Remove(this);
		}
	}

	private void AddNext(NetworkPlayer player)
	{
		for (int i = 0; i < nextObjectives.Length; i++)
		{
			if (nextObjectives[i].isGlobal)
			{
				if (IsInstanceValid(ObjectivePointManager.Instance))
					ObjectivePointManager.Instance.activeObjectives.Add(nextObjectives[i].key);
			}
			else
			{
				if (IsInstanceValid(player))
					player.objectives.Add(nextObjectives[i].key);
			}
		}
	}

	private bool HasCompletedRequired(NetworkPlayer player)
	{
		for (int i = 0; i < objective.required.Length; i++)
		{
			if (objective.required[i].isGlobal)
			{
				if (IsInstanceValid(ObjectivePointManager.Instance) && !ObjectivePointManager.Instance.completeObjectives.Contains(objective.required[i].key))
				{
					return false;
				}
			}
			else
			{
				if (IsInstanceValid(player) && !player.completeObjectives.Contains(objective.required[i].key))
				{
					return false;
				}
			}
		}
		return true;
	}

	public void CL_CompleteObjective()
	{
		Rpc(MethodName.RpcCompleteObjective);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void RpcCompleteObjective()
	{
		var player = IPlayerList.List(this).PlayerList.FirstOrDefault(x => x.GetMultiplayerAuthority() == Multiplayer.GetRemoteSenderId());
		// TODO: anti-cheat
		SV_CompleteObjective(player);
	}

	public void SV_CompleteObjective(NetworkPlayer player)
	{
		if (objective.isGlobal)
		{
			if (IsInstanceValid(ObjectivePointManager.Instance))
			{
				if (ObjectivePointManager.Instance.CompleteObjective(objective))
				{
					AddNext(player);
				}
			}
		}
		else
		{
			if (player.objectives.Remove(objective.key))
			{
				player.completeObjectives.Add(objective.key);
				AddNext(player);
			}
		}
	}
}
