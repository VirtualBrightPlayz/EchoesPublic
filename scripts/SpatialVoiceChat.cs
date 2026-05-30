using Godot;

public partial class SpatialVoiceChat : Node
{
	public enum ReverbPreset { None, SmallRoom, MediumRoom, LargeRoom, Hall, Cave, Custom }

	[Export] public float MinReverbDistance = 5.0f;
	[Export] public float MaxReverbDistance = 20.0f;

	// Close range 
	[Export] public ReverbPreset ClosePreset = ReverbPreset.None;
	[Export] public float CloseReverbRoomSize, CloseReverbDamping, CloseReverbWet, ClosePredelayMsec, ClosePredelayFeedback;

	// Far range 
	[Export] public ReverbPreset FarPreset = ReverbPreset.MediumRoom;
	[Export] public float FarReverbRoomSize, FarReverbDamping, FarReverbWet, FarPredelayMsec, FarPredelayFeedback;

	private AudioEffectReverb _reverbEffect;
	private NetworkPlayer _speaker;

	public override void _Ready()
	{
		_speaker = GetParentOrNull<NetworkPlayer>();
		
		if (!IsInstanceValid(_speaker))
		{
			GD.PushError("SpatialVoiceChat must be child of a NetworkPlayer");
			return;
		}

		_speaker.voiceChat.OnCreateVoice += VoiceCreated;
		if (_speaker.voiceChat.Bus != null)
		{
			VoiceCreated(_speaker.voiceChat.Bus.Name);
		}
	}

	public void VoiceCreated(string busName)
	{
		AudioBus bus = new AudioBus(busName);

		// Find or create reverb effect
		if (!IsInstanceValid(_reverbEffect))
			_reverbEffect = new AudioEffectReverb();
		bus.AddEffect(_reverbEffect);

		ApplyPresetSettings();

		ApplySettings(
			CloseReverbRoomSize, 
			CloseReverbDamping, 
			CloseReverbWet,
			ClosePredelayMsec, 
			ClosePredelayFeedback
		);
	}

	private void ApplyPresetSettings()
	{
		switch (ClosePreset)
		{
			case ReverbPreset.None: 
				SetClosePreset(0,0,0,0,0); 
				break;
			case ReverbPreset.SmallRoom: 
				SetClosePreset(0.3f,0.4f,0.1f,15f,0.1f); 
				break;
			case ReverbPreset.MediumRoom: 
				SetClosePreset(0.5f,0.5f,0.2f,20f,0.2f); 
				break;
			case ReverbPreset.LargeRoom: 
				SetClosePreset(0.7f,0.6f,0.3f,25f,0.3f); 
				break;
			case ReverbPreset.Hall: 
				SetClosePreset(0.9f,0.7f,0.4f,30f,0.4f); 
				break;
			case ReverbPreset.Cave: 
				SetClosePreset(1f,0.9f,0.5f,50f,0.5f); 
				break;
		}
		
		switch (FarPreset)
		{
			case ReverbPreset.SmallRoom: 
				SetFarPreset(0.3f,0.4f,0.2f,15f,0.2f); 
				break;
			case ReverbPreset.MediumRoom: 
				SetFarPreset(0.5f,0.5f,0.3f,20f,0.3f); 
				break;
			case ReverbPreset.LargeRoom: 
				SetFarPreset(0.7f,0.6f,0.4f,25f,0.4f); 
				break;
			case ReverbPreset.Hall: 
				SetFarPreset(0.9f,0.7f,0.6f,30f,0.5f); 
				break;
			case ReverbPreset.Cave: 
				SetFarPreset(1f,0.9f,0.8f,50f,0.6f); 
				break;
		}
	}

	private void SetClosePreset(float r, float d, float w, float p, float pf) 
	{ 
		if (ClosePreset != ReverbPreset.Custom) 
		{
			CloseReverbRoomSize = r; 
			CloseReverbDamping = d; 
			CloseReverbWet = w; 
			ClosePredelayMsec = p; 
			ClosePredelayFeedback = pf; 
		}
	}

	private void SetFarPreset(float r, float d, float w, float p, float pf) 
	{ 
		if (FarPreset != ReverbPreset.Custom) 
		{
			FarReverbRoomSize = r; 
			FarReverbDamping = d; 
			FarReverbWet = w; 
			FarPredelayMsec = p; 
			FarPredelayFeedback = pf; 
		}
	}

	public override void _Process(double delta)
	{
		// get listener if missing
		if (!IsInstanceValid(NetworkPlayer.LocalInstance))
			return;
		var _listener = NetworkPlayer.LocalInstance.ActiveController.Camera;
		
		// Validate critical components
		if (!IsInstanceValid(_listener) || 
			!IsInstanceValid(_speaker) || 
			!IsInstanceValid(_reverbEffect))
			return;
		
		if (_speaker.voiceChat.GetVoice().GetMeta(Intercom.IntercomName, false).AsBool())
		{
			ApplySettings(0, 0, 0, 0, 0);
			return;
		}
		
		float distance = _listener.GlobalPosition.DistanceTo(_speaker.ActiveController.View.GlobalPosition);
		
		if (distance <= MinReverbDistance)
		{
			ApplySettings(
				CloseReverbRoomSize,
				CloseReverbDamping,
				CloseReverbWet,
				ClosePredelayMsec,
				ClosePredelayFeedback
			);
		}
		else if (distance >= MaxReverbDistance)
		{
			ApplySettings(
				FarReverbRoomSize,
				FarReverbDamping,
				FarReverbWet,
				FarPredelayMsec,
				FarPredelayFeedback
			);
		}
		else
		{
			float t = (distance - MinReverbDistance) / (MaxReverbDistance - MinReverbDistance);
			ApplySettings(
				Mathf.Lerp(CloseReverbRoomSize, FarReverbRoomSize, t),
				Mathf.Lerp(CloseReverbDamping, FarReverbDamping, t),
				Mathf.Lerp(CloseReverbWet, FarReverbWet, t),
				Mathf.Lerp(ClosePredelayMsec, FarPredelayMsec, t),
				Mathf.Lerp(ClosePredelayFeedback, FarPredelayFeedback, t)
			);
		}
	}

	private void ApplySettings(float rs, float d, float w, float pm, float pf)
	{
		_reverbEffect.RoomSize = rs;
		_reverbEffect.Damping = d;
		_reverbEffect.Wet = w;
		_reverbEffect.PredelayMsec = pm;
		_reverbEffect.PredelayFeedback = pf;
	}
}
