using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Tomlyn;
using Tomlyn.Model;
using static TomlExtensions;

[GlobalClass]
public partial class PlayerRole : Resource
{
    // TODO: Make Icons for roles!
    [Export]
    public string file;
    [Export]
    public Color RoleColor = Colors.White;
    [Export]
    public float MaxHealth = 1f;
    [Export]
    public string DisplayName;
    public string PlainDisplayName => Tr(DisplayName);
    public string RichDisplayNameUpper => $"[color=#{RoleColor.ToHtml()}]{Tr(DisplayName).ToUpper()}[/color]";
    public string RichDisplayName => $"[color=#{RoleColor.ToHtml()}]{Tr(DisplayName)}[/color]";
    [Export]
    public string Objective;
    [Export]
    public string HintText;
    [Export]
    public double HintTime;
    [Export]
    public Texture2D HintIcon;
    [Export]
    public string DiscordActivityImage;
    [Export]
    public TeamID team;
    [Export]
    public int spawnStage = -1;
    [Export]
    public bool canUseIntercom = true;
    [Export]
    public bool proxChat = true;
    [Export]
    public bool globalTeamChat = false;
    [Export]
    public int mainChatChannel = 0;
    [Export]
    public int altChatChannel = 0;
    [Export]
    public int proxChatChannel = 0;
    [Export]
    public int[] listeningChatChannels = [0];
    [Export]
    public ItemPreset[] StartItems = Array.Empty<ItemPreset>();
    [Export]
    public string[] StartItemNames = [];
    [Export]
    public int[] StartAmmo = new int[3];
    public GameSound SpawnAudio => string.IsNullOrEmpty(SpawnAudioName) ? null : IInitScript.Instance.Data.Sounds.FirstOrDefault(x => x.ResourceName == SpawnAudioName);
    [Export]
    public string SpawnAudioName = "SpawnBell";
    [Export]
    public PackedScene ControllerScene;
    [Export]
    public PackedScene GUIScene;
    [Export]
    public AmbientProfile Ambient;

    [ExportGroup("Chase Music")]
    [Export]
    public double ChaseMusicTime = 60d;
    [Export]
    public string SpottedAudioName;
    public GameSound SpottedAudio => IInitScript.Instance.Data.TryGetSound(SpottedAudioName, out GameSound snd) ? snd : null;
    [Export]
    public string PreChaseAudioName;
    public GameSound PreChaseAudio => IInitScript.Instance.Data.TryGetSound(PreChaseAudioName, out GameSound snd) ? snd : null;
    [Export]
    public string ChaseAudioName;
    public GameSound ChaseAudio => IInitScript.Instance.Data.TryGetSound(ChaseAudioName, out GameSound snd) ? snd : null;
    [Export]
    public string ChaseLayerAudioName;
    public GameSound ChaseLayerAudio => IInitScript.Instance.Data.TryGetSound(ChaseLayerAudioName, out GameSound snd) ? snd : null;
    [Export]
    public string PostChaseAudioName;
    public GameSound PostChaseAudio => IInitScript.Instance.Data.TryGetSound(PostChaseAudioName, out GameSound snd) ? snd : null;
    private AudioStreamInteractive chaseMusicInst;
    public AudioStream ChaseMusicStream
    {
        get
        {
            if (IsInstanceValid(PreChaseAudio) && IsInstanceValid(ChaseAudio) && IsInstanceValid(PostChaseAudio))
            {
                if (!IsInstanceValid(chaseMusicInst))
                {
                    chaseMusicInst = new AudioStreamInteractive();
                    chaseMusicInst.ClipCount = 3;
                    chaseMusicInst.AddTransition((int)AudioStreamInteractive.ClipAny, (int)AudioStreamInteractive.ClipAny, AudioStreamInteractive.TransitionFromTime.NextBeat, AudioStreamInteractive.TransitionToTime.Start, AudioStreamInteractive.FadeMode.Automatic, 1f);
                    chaseMusicInst.SetClipName(0, "PreChase");
                    chaseMusicInst.SetClipStream(0, PreChaseAudio?.Stream);
                    chaseMusicInst.SetClipName(1, "Chase");
                    if (IsInstanceValid(ChaseLayerAudio))
                    {
                        var chaseStream = new AudioStreamSynchronized();
                        chaseStream.StreamCount = 2;
                        chaseStream.SetSyncStream(0, ChaseAudio?.Stream);
                        chaseStream.SetSyncStream(1, ChaseLayerAudio?.Stream);
                        chaseMusicInst.SetClipStream(1, chaseStream);
                    }
                    else
                    {
                        chaseMusicInst.SetClipStream(1, ChaseAudio?.Stream);
                    }
                    chaseMusicInst.SetClipName(2, "PostChase");
                    chaseMusicInst.SetClipStream(2, PostChaseAudio?.Stream);
                }
            }
            else
            {
                return ChaseAudio?.Stream;
            }
            return chaseMusicInst;
        }
    }

    [ExportGroup("Extras")]
    [Export]
    public string Controller = "Dead";
    [Export]
    public string Model = "None";
    [Export]
    public string[] Abilities = [];
    [Export]
    public string RagdollEffectName;
    public GameEffect RagdollEffect => IInitScript.Instance.Data.Effects.FirstOrDefault(x => x.ResourceName == RagdollEffectName);
    // [Export]
    // public string[] ImmuneDamageTypeNames = [];
    public DamageType[] ImmuneDamageTypes = [];

    [ExportGroup("Movement")]
    [Export]
    public float Speed = 5f;
    [Export]
    public float Acceleration = 100f;
    [Export]
    public float SprintSpeedMultiplier = 2f;
    [Export]
    public float CrouchSpeedMultiplier = 0.25f;
    [Export]
    public float JumpVelocity = 5.0f;
    [Export]
    public float GravityMultiplier = 5.0f;
    [Export]
    public float CameraHeight = 2.2f;
    [Export]
    public float CollisionHeight = 2.5f;
    [Export]
    public string DefaultFootsteps = "concrete";
    [Export]
    public bool ForceDefaultFootsteps = false;
    [Export]
    public float FootstepInterval = 0.6f;

    [ExportGroup("Sprint")]
    [Export]
    public float SprintStaminaDecreaseRate = 0.3f;
    [Export]
    public float SprintStaminaIncreaseRate = 0.3f;
    [Export]
    public float MaxSprintStamina = 5.0f;
    [Export]
    public float SprintStaminaPause = 1.0f;
    [Export]
    public bool OverrideNoSCPSprint = false;

    [ExportGroup("Stats")]
    [Export]
    public PlayerSkillFlags SkillFlags = 0;
    [Export]
    public bool CanCapturePoints = false;

    public void ApplyGunSkills(ref float fireRate, ref Rect2 recoil, ref Vector2 spread)
    {
        return;
        if (!SkillFlags.HasFlag(PlayerSkillFlags.GunSmith))
        {
            fireRate *= 1.5f;
            recoil = recoil.Grow(2f);
            spread *= 2f;
        }
    }

    public void ApplyThrowSkills(Vector3 fromPosition, ref Vector3 velocity)
    {
        return;
        if (SkillFlags.HasFlag(PlayerSkillFlags.Bomber))
        {
            velocity *= 2f;
        }
    }

    public bool ReadFromFile()
    {
        PlayerRole role = this;
        FileAccess fs = FileAccess.Open(file, FileAccess.ModeFlags.Read);
        if (!IsInstanceValid(fs))
        {
            return false;
        }
        TomlTable cfg = TomlSerializer.Deserialize<TomlTable>(fs.GetAsText());
        fs.Close();
        // role
        if (TryGetTable(cfg, "role", out TomlTable roleCfg))
        {
            role.ResourceName = (string)roleCfg["name"];
            role.DisplayName = (string)roleCfg["display_name"];
            role.team = Enum.Parse<TeamID>((string)roleCfg["team"]);
            if (TryGetValue(roleCfg, "ui_color", out string color))
            {
                role.RoleColor = Color.FromString(color, role.RoleColor);
            }
            GetValueOptional(roleCfg, "controller", ref role.Controller);
            GetValueOptional(roleCfg, "model", ref role.Model);
            GetArrayOptional(roleCfg, "abilities", ref role.Abilities);
            GetValueOptional(roleCfg, "ragdoll_effect", ref role.RagdollEffectName);
            // voice
            if (TryGetTable(roleCfg, "voice", out TomlTable voiceCfg))
            {
                GetValueOptional(voiceCfg, "can_use_intercom", ref role.canUseIntercom);
                GetValueOptional(voiceCfg, "proximity_chat", ref role.proxChat);
                GetValueOptional(voiceCfg, "global_team_chat", ref role.globalTeamChat);
                GetValueOptional(voiceCfg, "main_chat_channel", ref role.mainChatChannel);
                GetValueOptional(voiceCfg, "alt_chat_channel", ref role.altChatChannel);
                GetValueOptional(voiceCfg, "proximity_chat_channel", ref role.proxChatChannel);
                GetArrayOptional(voiceCfg, "listening_chat_channels", ref role.listeningChatChannels);
            }
            // intro
            if (TryGetTable(roleCfg, "intro", out TomlTable introCfg))
            {
                GetValueOptional(introCfg, "objective_text", ref role.Objective);
                GetValueOptional(introCfg, "hint_text", ref role.HintText);
                GetValueOptional(introCfg, "hint_time", ref role.HintTime);
                GetValueOptional(introCfg, "spawn_audio", ref role.SpawnAudioName);
            }
            // chase music
            if (TryGetTable(roleCfg, "chase_music", out TomlTable chaseMusicCfg))
            {
                GetValueOptional(chaseMusicCfg, "chase_time", ref role.ChaseMusicTime);
                GetValueOptional(chaseMusicCfg, "spotted_audio", ref role.SpottedAudioName);
                GetValueOptional(chaseMusicCfg, "pre_chase_audio", ref role.PreChaseAudioName);
                GetValueOptional(chaseMusicCfg, "chase_audio", ref role.ChaseAudioName);
                GetValueOptional(chaseMusicCfg, "chase_layer_audio", ref role.ChaseLayerAudioName);
                GetValueOptional(chaseMusicCfg, "post_chase_audio", ref role.PostChaseAudioName);
            }
            // stats
            if (TryGetTable(roleCfg, "stats", out TomlTable statsCfg))
            {
                GetValueOptional(statsCfg, "max_health", ref role.MaxHealth);
                GetValueOptional(statsCfg, "spawn_stage", ref role.spawnStage);
                GetValueOptional(statsCfg, "can_capture_points", ref role.CanCapturePoints);
                GetArrayOptional(statsCfg, "start_items", ref role.StartItemNames);
                GetArrayOptional(statsCfg, "start_ammo", ref role.StartAmmo);
                GetArrayOptional(statsCfg, "immune_damage_types", ref role.ImmuneDamageTypes);
                // movement stats
                if (TryGetTable(statsCfg, "movement", out TomlTable movementCfg))
                {
                    GetValueOptional(movementCfg, "speed", ref role.Speed);
                    GetValueOptional(movementCfg, "acceleration", ref role.Acceleration);
                    GetValueOptional(movementCfg, "sprint_speed_multiplier", ref role.SprintSpeedMultiplier);
                    GetValueOptional(movementCfg, "crouch_speed_multiplier", ref role.CrouchSpeedMultiplier);
                    GetValueOptional(movementCfg, "jump_velocity", ref role.JumpVelocity);
                    GetValueOptional(movementCfg, "gravity_multiplier", ref role.GravityMultiplier);
                    GetValueOptional(movementCfg, "camera_height", ref role.CameraHeight);
                    GetValueOptional(movementCfg, "collision_height", ref role.CollisionHeight);
                    GetValueOptional(movementCfg, "default_footsteps", ref role.DefaultFootsteps);
                    GetValueOptional(movementCfg, "force_default_footsteps", ref role.ForceDefaultFootsteps);
                    GetValueOptional(movementCfg, "footstep_interval", ref role.FootstepInterval);
                }
                if (TryGetTable(statsCfg, "stamina", out TomlTable staminaCfg))
                {
                    GetValueOptional(staminaCfg, "decrease_rate", ref role.SprintStaminaDecreaseRate);
                    GetValueOptional(staminaCfg, "increase_rate", ref role.SprintStaminaIncreaseRate);
                    GetValueOptional(staminaCfg, "max_value", ref role.MaxSprintStamina);
                    GetValueOptional(staminaCfg, "pause_time", ref role.SprintStaminaPause);
                }
            }
            return true;
        }
        return false;
    }
}

[Flags]
public enum PlayerSkillFlags : int
{
    Medic = 1,
    GunSmith = 2,
    Bomber = 4,
    Hacker = 8,
    Navigator = 16,
}
