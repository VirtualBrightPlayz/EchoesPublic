using System;
using System.Collections.Generic;
using Godot;

public class AudioBus : IDisposable
{
	public readonly string Name;
	public readonly bool IsTemp;

	public int Index
	{
		get
		{
			for (int i = 0; i < AudioServer.BusCount; i++)
			{
				if (AudioServer.GetBusName(i) == Name)
				{
					return i;
				}
			}
			return -1;
		}
	}

	public float VolumeDb
	{
		get => AudioServer.GetBusVolumeDb(Index);
		set => AudioServer.SetBusVolumeDb(Index, value);
	}

	public bool Mute
	{
		get => AudioServer.IsBusMute(Index);
		set => AudioServer.SetBusMute(Index, value);
	}

	public AudioBus()
	{
		AudioServer.AddBus();
		Name = Guid.NewGuid().ToString();
		IsTemp = true;
		AudioServer.SetBusName(AudioServer.BusCount - 1, Name);
	}

	public AudioBus(string name)
	{
		Name = name;
		IsTemp = false;
	}

	public void Dispose()
	{
		if (IsTemp)
			AudioServer.RemoveBus(Index);
	}

	public void SetParent(string name)
	{
		AudioServer.SetBusSend(Index, name);
	}

	public void AddEffect(AudioEffect effect, int at = -1)
	{
		AudioServer.AddBusEffect(Index, effect, at);
	}

	public void RemoveEffectAt(int effectIdx)
	{
		AudioServer.RemoveBusEffect(Index, effectIdx);
	}

	public AudioEffectInstance GetEffectInstance(int effectIdx)
	{
		return AudioServer.GetBusEffectInstance(Index, effectIdx);
	}

	public AudioEffect GetEffect(int effectIdx)
	{
		return AudioServer.GetBusEffect(Index, effectIdx);
	}
}
