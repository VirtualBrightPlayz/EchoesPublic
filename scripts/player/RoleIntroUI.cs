using System;
using System.Globalization;
using System.Text;
using Godot;

public partial class RoleIntroUI : Control
{
    public static StringName TextName = "text";

    [Export]
    public AnimationPlayer animPlayer;
    [Export]
    public RichTextLabel deathText;
    [Export]
    public Node introTextLines;
    [Export]
    public RichTextLabel roleNameText;
    [Export]
    public RichTextLabel roleObjectiveText;
    [Export]
    public RichTextLabel roleHintText;
    [Export] public StringName animName;
    [Export] public StringName roundStartAnimName;
    [Export] public StringName spawnAnimName;
    [Export] public StringName endRoundAnimName;

    public override void _EnterTree()
    {
        // RoundManager.Instance.EventOnRoundStart += OnRoundStart;
        // RoundManager.Instance.EventOnRoundEnd += OnRoundEnd;
        NetworkPlayer.LocalInstance.OnLocalSpawned += OnSpawned;
        NetworkPlayer.LocalInstance.OnKilled += OnKilled;
    }

    public override void _ExitTree()
    {
        // RoundManager.Instance.EventOnRoundStart -= OnRoundStart;
        // RoundManager.Instance.EventOnRoundEnd -= OnRoundEnd;
        NetworkPlayer.LocalInstance.OnLocalSpawned -= OnSpawned;
        NetworkPlayer.LocalInstance.OnKilled -= OnKilled;
    }

    public static string AddOrdinal(int num)
    {
        if (num <= 0)
            return num.ToString();

        switch (num % 100)
        {
            case 11:
            case 12:
            case 13:
                return num + "th";
        }

        switch (num % 10)
        {
            case 1:
                return num + "st";
            case 2:
                return num + "nd";
            case 3:
                return num + "rd";
            default:
                return num + "th";
        }
    }

    private void OnRoundEnd(string winner)
    {
        NetworkPlayer player = NetworkPlayer.LocalInstance;
        string text = winner;
        deathText.Text = text;
        animPlayer.Play(endRoundAnimName);
        if (IsInstanceValid(MenuManager.Instance))
        {
            AudioServer.SetBusEffectEnabled(MenuManager.Instance.menuBus.Index, MenuManager.Instance.eq6EffectEffect, false);
        }
    }

    private void OnRoundStart()
    {
        NetworkPlayer player = NetworkPlayer.LocalInstance;
        string fmt = "Subject: {0}\nDate: {2}\nTime: {3}";
        DateTime datetime = DateTime.Now;
        string month = datetime.ToString("MMMM", CultureInfo.InvariantCulture);
        string date = AddOrdinal(datetime.Day);
        string time = datetime.ToString("HH:mm", CultureInfo.InvariantCulture);
        string text = string.Format(fmt, player.username, player.Role.PlainDisplayName, month + ' ' + date, time);
        introTextLines.Set(TextName, text);
        animPlayer.Play(roundStartAnimName);
        if (IsInstanceValid(MenuManager.Instance))
        {
            AudioServer.SetBusEffectEnabled(MenuManager.Instance.menuBus.Index, MenuManager.Instance.eq6EffectEffect, false);
        }
    }

    private void OnSpawned(PlayerRole role)
    {
        if (role.team == TeamID.Dead)
            return;
        NetworkPlayer player = NetworkPlayer.LocalInstance;
        roleNameText.Text = player.Role.PlainDisplayName;
        roleNameText.SelfModulate = player.Role.RoleColor;
        roleObjectiveText.Text = "[i]" + InputSettings.TranslateSlow(Tr(player.Role.Objective)) + "[/i]";
        // roleObjectiveText.SelfModulate = player.Role.RoleColor;
        roleHintText.Text = InputSettings.TranslateSlow(Tr(player.Role.HintText));
        animPlayer.Play(spawnAnimName);
        if (IsInstanceValid(MenuManager.Instance))
        {
            AudioServer.SetBusEffectEnabled(MenuManager.Instance.menuBus.Index, MenuManager.Instance.eq6EffectEffect, false);
        }
    }

    private void OnKilled(DamageInfo info)
    {
        NetworkPlayer player = NetworkPlayer.LocalInstance;
        string cause = DamageUtils.TranslateDeathMsg(info.TypeOfDamage);
        string src = info.Source?.AttackerDisplayName ?? string.Empty;
        string type = DamageUtils.TranslateType(info.TypeOfDamage);
        string fmt = MainMenuUI.TranslateText("DEATH_MSG_TEXT");
        string text = string.Format(fmt, player.username, cause, src, type);
        deathText.Text = text;
        // deathText.Text = MainMenuUI.TranslateText("ROLE_OBJ_SPECTATOR");
        animPlayer.Play(animName);
        if (IsInstanceValid(MenuManager.Instance))
        {
            Tween tween = CreateTween();
            AudioServer.SetBusEffectEnabled(MenuManager.Instance.menuBus.Index, MenuManager.Instance.eq6EffectEffect, true);
            tween.TweenMethod(Callable.From<float>(TweenEq), -24, 0f, 7d);
            tween.TweenCallback(Callable.From(ResetEq)).SetDelay(10d);
        }
    }

    private void ResetEq()
    {
        if (IsInstanceValid(MenuManager.Instance))
        {
            AudioServer.SetBusEffectEnabled(MenuManager.Instance.menuBus.Index, MenuManager.Instance.eq6EffectEffect, false);
        }
    }

    private void TweenEq(float volume)
    {
        if (IsInstanceValid(MenuManager.Instance))
        {
            AudioEffect effect = AudioServer.GetBusEffect(MenuManager.Instance.menuBus.Index, MenuManager.Instance.eq6EffectEffect);
            if (effect is AudioEffectEQ6 eq6)
            {
                eq6.SetBandGainDb(0, volume * 0.5f);
                eq6.SetBandGainDb(1, volume * 0.5f);
                eq6.SetBandGainDb(2, volume * 0.5f);
                eq6.SetBandGainDb(3, volume * 0.5f);
                eq6.SetBandGainDb(4, volume);
                eq6.SetBandGainDb(5, volume);
            }
        }
    }
}