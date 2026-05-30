using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using FragLabs.Audio.Codecs;
using Godot;
using FileAccess = Godot.FileAccess;

public partial class VoiceChat : Node
{
	public const int MaxBytes = 4000;
	public const int OpusSampleRate = 12_000;
	public const float OpusFrameSize = 60f;
	public const int Bitrate = 16_000;

	[Signal]
	public delegate void OnSpeakEventHandler(float[] pcm);
	[Signal]
	public delegate void OnCreateVoiceEventHandler(string bus);

	[Export]
	public NodePath CustomVoiceAudioPlayer;
	[Export]
	public bool Recording = false;
	[Export]
	public bool Listen = false;
	[Export]
	public bool DirectListen = false;
	[Export]
	public float BufferLength = 0.1f;
	private AudioEffectCapture capture;
	private AudioEffectSpectrumAnalyzerInstance analyzer;
	private AudioEffectRecord record;
	public VoiceMic mic;
	private AudioStreamGenerator generator;
	private AudioStreamGeneratorPlayback playback;
	private Queue<float> recieve_buffer = new Queue<float>();
	private bool prev_frame_recording = false;
	private Node voiceNode;
	private AudioStreamPlayer voice;
	private AudioStreamPlayer3D voice3d;
	private OpusDecoder decoder;
	private OpusEncoder encoder;
	private Queue<float> queue = new Queue<float>();
	private double last_speak;
	public bool IsSpeaking => Time.GetUnixTimeFromSystem() - last_speak <= 1d || Recording;
	private long discarded;
	private int localId;
	private int authorityId;
	private double playbackPosition;
	private int prevSkips;
	private ulong playPositionTicks;
	private Thread audioThread;
	private bool stopped = false;
	private System.Threading.Mutex mutex;
	private float volumedb = 0f;
	public float VolumeDb
	{
		get => volumedb;
		set
		{
			volumedb = value;
			if (Bus != null)
				Bus.VolumeDb = value;
		}
	}
	public AudioBus Bus { get; private set; }
	public float Loudness { get; private set; }
	public AudioEffectSpectrumAnalyzerInstance SpectrumAnalyzerInstance => analyzer;

	static VoiceChat()
	{
		NativeLibrary.SetDllImportResolver(typeof(OpusDecoder).Assembly, Resolver);
	}

	private static IntPtr Resolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
	{
		if (libraryName.Equals("opus") && !IInitScript.IsServerOnly)
		{
			if (OS.GetName().Equals("Windows"))
			{
				string path = Path.Combine(Directory.GetCurrentDirectory(), "opus.dll");
				return NativeLibrary.Load(path);
			}

			if (OS.GetName().Equals("Linux"))
			{
				string path = Path.Combine(Directory.GetCurrentDirectory(), "libopus.so");
				return NativeLibrary.Load(path);
			}

			if (OS.GetName().Equals("Android"))
			{
				var file = FileAccess.Open("res://libopus_android.so", FileAccess.ModeFlags.Read);
				var data = file.GetBuffer((long)file.GetLength());
				string path = Path.Combine(OS.GetUserDataDir(), "libopus_android.so");
				File.WriteAllBytes(path, data);
				return NativeLibrary.Load(path);
			}
		}

		return IntPtr.Zero;
	}

	public override void _Ready()
	{
		localId = Multiplayer.GetUniqueId();
		authorityId = GetMultiplayerAuthority();
		// if (GetMultiplayerAuthority() == Multiplayer.GetUniqueId())
		//     CreateMic();
		if (decoder != null)
		{
			DestroyVoice();
			CreateVoice(decoder?.OutputSamplingRate ?? OpusSampleRate);
		}
		stopped = false;
		mutex = new System.Threading.Mutex();
		// audioThread = new Thread(() => AudioThreadLoop());
		// audioThread.Name = "VoiceChat Thread";
		// audioThread.Start();
	}

	public override void _ExitTree()
	{
		stopped = true;
		DestroyMic();
		DestroyVoice();
		// audioThread.Join();
		mutex.Dispose();
		encoder?.Dispose();
		decoder?.Dispose();
	}

	public override void _Process(double delta)
	{
		localId = Multiplayer.GetUniqueId();
		authorityId = GetMultiplayerAuthority();
		if (Recording)
			CreateMic();
		else
			DestroyMic();

		ProcessMic(delta);
		TickAudio(delta);
	}

	private void TickAudio(double delta)
	{
		playbackPosition += delta;

		/*
		double delta2 = 0;

		if (voice != null && playbackPosition + delta2 >= BufferLength)
		{
			voice.Playing = true;
			playback = voice.GetStreamPlayback() as AudioStreamGeneratorPlayback;
			if (ProcessVoice(delta))
				playbackPosition = 0;
		}
		else if (voice3d != null && playbackPosition + delta2 >= BufferLength)
		{
			voice3d.Playing = true;
			playbackPosition = 0;
			playback = voice3d.GetStreamPlayback() as AudioStreamGeneratorPlayback;
			if (ProcessVoice(delta))
				playbackPosition = 0;
		}
		*/

		if (IsInstanceValid(analyzer))
		{
			var mag = analyzer.GetMagnitudeForFrequencyRange(1f, AudioServer.GetMixRate(), AudioEffectSpectrumAnalyzerInstance.MagnitudeMode.Average);
			Loudness = mag.Length();
		}
		else
			Loudness = 0f;
		ProcessVoice(delta);
	}

	private void AudioThreadLoop()
	{
		while (!stopped)
		{
			// break;
			int ms = 2;
			ProcessMic(ms / 1000d);
			// if (mutex.WaitOne(0))
			{
				// ProcessVoice(ms / 1000d);
				// mutex.ReleaseMutex();
			}
			Thread.Sleep(ms);
		}
	}

	private void CreateMic()
	{
		if (IsInstanceValid(mic))
			return;
		mic = new VoiceMic();
		AddChild(mic);
		int recordBusIndex = AudioServer.GetBusIndex(mic.Bus);
		for (int i = 0; i < AudioServer.GetBusEffectCount(recordBusIndex); i++)
		{
			if (AudioServer.GetBusEffect(recordBusIndex, i) is AudioEffectCapture c)
			{
				capture = c;
				// break;
			}
			if (AudioServer.GetBusEffect(recordBusIndex, i) is AudioEffectSpectrumAnalyzer && AudioServer.GetBusEffectInstance(recordBusIndex, i) is AudioEffectSpectrumAnalyzerInstance a)
			{
				analyzer = a;
			}
		}
		// capture.BufferLength = BufferLength;
	}

	private void DestroyMic()
	{
		if (mic != null)
		{
			mic.QueueFreeNow();
			mic = null;
			capture = null;
			record = null;
			// firFilter = null;
		}
	}

	private void CreateVoice(int sampleRate)
	{
		if (CustomVoiceAudioPlayer != null && !CustomVoiceAudioPlayer.IsEmpty)
		{
			Node player = GetNode(CustomVoiceAudioPlayer);
			if (player != null)
			{
				if (player is AudioStreamPlayer)
				{
					voice = player as AudioStreamPlayer;
					voice3d = null;
					voiceNode = null;
				}
				else if (player is AudioStreamPlayer3D)
				{
					voice = null;
					voice3d = player as AudioStreamPlayer3D;
					voiceNode = null;
				}
				else
				{
					Log.PrintErr($"Node {CustomVoiceAudioPlayer} is not any kind of AudioStreamPlayer!");
				}
			}
			else
			{
				Log.PrintErr($"Node {CustomVoiceAudioPlayer} does not exist!");
			}
		}
		else
		{
			voice = new AudioStreamPlayer();
			voice3d = null;
			voiceNode = voice;
			AddChild(voice);
		}

		Bus = new AudioBus();
		Bus.SetParent("Voice");
		Bus.VolumeDb = volumedb;
		Bus.AddEffect(new AudioEffectSpectrumAnalyzer()
		{
			BufferLength = 0.1f,
			TapBackPos = 0.1f,
			FftSize = AudioEffectSpectrumAnalyzer.FftSizeEnum.Size256,
		});
		/*
		Bus.AddEffect(new AudioEffectAmplify()
		{
			VolumeDb = 16f,
		});
		Bus.AddEffect(new AudioEffectCompressor()
		{
			Threshold = 0f,
			Ratio = 4,
		});
		*/
		EmitSignalOnCreateVoice(Bus.Name);
		if (!Recording)
			analyzer = (AudioEffectSpectrumAnalyzerInstance)Bus.GetEffectInstance(0);

		generator = new AudioStreamGenerator()
		{
			MixRate = sampleRate,
			BufferLength = BufferLength,
		};
		if (voice != null)
		{
			Bus.SetParent(voice.GetMeta("VoiceBus", "Voice").AsString());
			voice.Bus = Bus.Name;
			voice.Stream = generator;
			voice.Playing = true;
			playback = voice.GetStreamPlayback() as AudioStreamGeneratorPlayback;
			int frameCount = playback.GetFramesAvailable();
			for (int i = 0; i < frameCount; i++)
				playback.PushFrame(Vector2.Zero);
		}
		else if (voice3d != null)
		{
			Bus.SetParent(voice3d.GetMeta("VoiceBus", "Voice").AsString());
			voice3d.Bus = Bus.Name;
			voice3d.Stream = generator;
			voice3d.Playing = true;
			playback = voice3d.GetStreamPlayback() as AudioStreamGeneratorPlayback;
			int frameCount = playback.GetFramesAvailable();
			for (int i = 0; i < frameCount; i++)
				playback.PushFrame(Vector2.Zero);
		}
		playbackPosition = 0;
		prevSkips = 0;
		Bus.VolumeDb = volumedb;
	}

	public void SetVoice(Node node)
	{
		CustomVoiceAudioPlayer = node.GetPath();
		DestroyVoice();
		if (decoder != null)
			CreateVoice(decoder?.OutputSamplingRate ?? OpusSampleRate);
	}

	public Node GetVoice()
	{
		if (IsInstanceValid(voice))
			return voice;
		if (IsInstanceValid(voice3d))
			return voice3d;
		return null;
	}

	private void DestroyVoice()
	{
		if (playback == null)
			return;
		if (voiceNode != null)
			voiceNode.QueueFree();
		voiceNode = null;
		voice = null;
		voice3d = null;
		// generator.Free();
		playback = null;
		Bus?.Dispose();
		Bus = null;
		// Log.Print("Destroy Voice");
	}

	private void Speak(float[] data, int dataLength, int netId)
	{
		// Log.Print(authorityId, " ", netId);
		if (netId != authorityId)
		{
			return;
		}
		// if (playback == null)
		//     CreateVoice(decoder == null ? OpusSampleRate : decoder.OutputSamplingRate);
		last_speak = Time.GetUnixTimeFromSystem();
		for (int i = 0; i < dataLength; i++)
			recieve_buffer.Enqueue(data[i]);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	public void RpcSpeak(byte[] data, int encodedLength, int freq, int channels)
	{
		if (Multiplayer.GetRemoteSenderId() != authorityId)
		{
			return;
		}
		if (IsMultiplayerAuthority() && !Listen /*&& !Settings.User.VoiceDebug.HasFlag(Settings.VoiceDebugFlags.Listen)*/)
			return;
		last_speak = Time.GetUnixTimeFromSystem();
		if (IInitScript.IsServerOnly)
			return;
		if (decoder == null)
		{
			decoder = OpusDecoder.Create(freq, channels);
			decoder.MaxDataBytes = MaxBytes;
			DestroyVoice();
			CreateVoice(decoder.OutputSamplingRate);
		}
		float[] decodedDataBytes = decoder.DecodeFloat(data, encodedLength, out int decodedLength);
		float[] decodedData = new float[decodedLength];
		Array.Copy(decodedDataBytes, decodedData, decodedData.Length);
		Speak(decodedData, decodedLength, Multiplayer.GetRemoteSenderId());
	}

	private void SendRpcSpeak(byte[] data, int encodedLength, int freq, int channels)
	{
		last_speak = Time.GetUnixTimeFromSystem();
		// Log.Print(authorityId, " ", localId);
		Rpc(nameof(RpcSpeak), data, encodedLength, freq, channels);
	}

	private bool ProcessVoice(double delta)
	{
		if (playback == null)
			return false;
		if (playback.GetSkips() > prevSkips)
		{
			prevSkips = playback.GetSkips();
			/*
			for (int i = 0; i < generator.MixRate * BufferLength; i++)
			{
				playback.PushFrame(Vector2.Zero);
			}
			*/
			// Log.PrintS("Skips", playback.GetSkips());
		}
		if (playback.GetFramesAvailable() < 1)
		{
			// Log.Print(recieve_buffer.Count);
			return false;
		}

		{
			int len = Mathf.Min(playback.GetFramesAvailable(), recieve_buffer.Count);
			for (int i = 0; i < len; i++)
			{
				playback.PushFrame(new Vector2(recieve_buffer.Peek(), recieve_buffer.Peek()));
				recieve_buffer.Dequeue();
			}
		}
		return true;
	}

	private static int ResampleFrameCount(int frameCount, int sourceSampleRate, int targetSampleRate)
	{
		float factor = (float)targetSampleRate / sourceSampleRate;
		if (sourceSampleRate < targetSampleRate)
			return (int)MathF.Ceiling(frameCount * factor);
		else
			return (int)MathF.Floor(frameCount * factor);
	}

	public static float[] Resample(float[] data, int sourceSampleRate, int targetSampleRate)
	{
		if (sourceSampleRate == targetSampleRate)
			return data;
		int outputFrameCount = ResampleFrameCount(data.Length, sourceSampleRate, targetSampleRate);
		float[] pcm = new float[outputFrameCount];

		float sampleRateFactor = (float)sourceSampleRate / targetSampleRate;
		for (int i = 0; i < outputFrameCount; i++)
		{
			float inFrameIdx = i * sampleRateFactor;
			int inFrameFloor = (int)inFrameIdx;
			float inFrameFrac = inFrameIdx - inFrameFloor;

			if (inFrameFloor >= data.Length - 1)
			{
				pcm[i] = 0f;
			}
			else
			{
				pcm[i] = data[inFrameFloor] * (1f - inFrameFrac) + data[inFrameFloor + 1] * inFrameFrac;
			}
		}
		return pcm;
	}

	private void ProcessMic(double delta)
	{
		if (Recording && IsInstanceValid(mic))
		{
			var mag = analyzer.GetMagnitudeForFrequencyRange(1f, AudioServer.GetMixRate(), AudioEffectSpectrumAnalyzerInstance.MagnitudeMode.Average);
			Loudness = mag.Length();

			// if (capture == null)
			//     CreateMic();

			if (!prev_frame_recording)
			{
				capture.ClearBuffer();
				queue.Clear();
			}

			// if (capture.GetFramesAvailable() >= capture.BufferLength * 0.5f * OpusSampleRate)
			{
				// for (long i = discarded; i < capture.GetDiscardedFrames(); i++)
				{
					// queue.Enqueue(0f);
				}
				discarded = capture.GetDiscardedFrames();
				// Log.Print(discarded);

				Vector2[] stereo_data = capture.GetBuffer(capture.GetFramesAvailable());
				if (stereo_data.Length > 0)
				{
					float[] data = new float[stereo_data.Length];
					for (int i = 0; i < stereo_data.Length; i++)
					{
						float value = (stereo_data[i].X + stereo_data[i].Y) / 2f;
						data[i] = value;
					}

					{
						// DiscreteSignal signal = Operation.Resample(new DiscreteSignal((int)AudioServer.GetMixRate(), data, false), OpusSampleRate);
						// DiscreteSignal signal = Operation.Resample(new DiscreteSignal((int)AudioServer.GetInputMixRate(), data, false), OpusSampleRate, firFilter);
						// DiscreteSignal inputSignal = new DiscreteSignal((int)AudioServer.GetInputMixRate(), data, );
						// DiscreteSignal signal = Operation.Resample(inputSignal, OpusSampleRate);
						float[] signal = Resample(data, (int)AudioServer.GetMixRate(), OpusSampleRate);
						for (int i = 0; i < signal.Length; i++)
						{
							if (!Mathf.IsEqualApprox(signal[i], 0f, 0.00001f) /*|| !Settings.User.VoiceDebug.HasFlag(Settings.VoiceDebugFlags.RemoveZeros)*/)
								queue.Enqueue(signal[i]);
						}
					}
				}
			}

			int length = (int)(OpusFrameSize / 1000f * OpusSampleRate);
			while (queue.Count >= length)
			{
				int stereo_data_length = length;

				float[] data = new float[stereo_data_length];
				float max_value = 0.0f;
				float min_value = 0.0f;

				for (int i = 0; i < stereo_data_length; i++)
				{
					float value = queue.Dequeue();
					max_value = Mathf.Max(value, max_value);
					min_value = Mathf.Min(value, min_value);
					data[i] = value;
				}

				if (encoder == null)
				{
					encoder = OpusEncoder.Create(OpusSampleRate, 1, FragLabs.Audio.Codecs.Opus.Application.Voip);
					encoder.Bitrate = Bitrate;
					encoder.MaxDataBytes = MaxBytes;
				}

				byte[] encodedDataBytes = encoder.EncodeFloat(data, data.Length, out int encodedLength);
				byte[] encodedDataFinal = new byte[encodedLength];
				Array.Copy(encodedDataBytes, encodedDataFinal, encodedDataFinal.Length);

				if (DirectListen)
				{
					if (!IsInstanceValid(voiceNode))
					{
						CreateVoice(OpusSampleRate);
					}
					Speak(data, data.Length, authorityId);
				}

				EmitSignalOnSpeak(data);
				CallDeferred(nameof(SendRpcSpeak), encodedDataFinal, encodedLength, encoder.InputSamplingRate, encoder.InputChannels);
			}
		}
		else if (IsInstanceValid(mic))
		{
			capture.ClearBuffer();
		}
		else if (capture != null)
		{
			// capture.GetBuffer(capture.GetFramesAvailable());
			// DestroyMic();
		}
		prev_frame_recording = Recording;
	}
}
