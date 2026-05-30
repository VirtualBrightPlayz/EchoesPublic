using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class SCP294 : StaticBody3D, IInteractable
{
    [Export]
    public Node3D marker;
    [Export]
    public Label3D label;
    [Export]
    public AudioStreamPlayer3D audioPlayer;
    [Export]
    public AudioStream[] dispenseAudio = new AudioStream[0];
    [Export]
    public AudioStream coinDropAudio;
    [Export]
    public SCP294Drink[] drinks = new SCP294Drink[0];
    [Export]
    public ItemPreset cup;
    [Export]
    public Node3D cupSpawn;
    [Export] public Control uiRoot;
    [Export] public LineEdit uiLineEdit;
    [Export] public int Credits = 0;

    public Task task = Task.CompletedTask;

    public Vector3 WorldInteractPosition => marker?.GlobalPosition ?? GlobalPosition;
    public IInteractable.InteractType ActionType => IInteractable.InteractType.Grab;

    public bool CanUse(IItemHolder holder, ItemObject item) => holder.GetPlayer().TryGetAbility(out InventoryAbility _);

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcUse(string cup)
    {
        var multiplayerRemoteSenderId = Multiplayer.GetRemoteSenderId();
        var plr = IPlayerList.List(this).PlayerList.FirstOrDefault(x => multiplayerRemoteSenderId == x.GetMultiplayerAuthority());
        if (IsInstanceValid(plr) && plr.TryGetAbility(out InventoryAbility _) && IsMultiplayerAuthority())
        {
            string text = cup.Replace("cup of ", "");
            if (task.IsCompleted)
            {
                if (Credits > 0)
                    task = DispenseCup(text);
                else
                    task = DispenseError();
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void RpcAddCredit()
    {
        var multiplayerRemoteSenderId = Multiplayer.GetRemoteSenderId();
        var plr = IPlayerList.List(this).PlayerList.FirstOrDefault(x => multiplayerRemoteSenderId == x.GetMultiplayerAuthority());
        if (IsInstanceValid(plr) && plr.TryGetAbility(out InventoryAbility inv) && IsMultiplayerAuthority())
        {
            var coin = inv.InventoryEquipped.FirstOrDefault(x => x.Preset.ResourceName == "Coin");
            if (IsInstanceValid(coin))
            {
                coin.QueueFreeNow();
                Credits++;
                Rpc(nameof(RpcDispenseSound), -1);
            }
        }
    }

    private async Task DispenseError()
    {
        Rpc(nameof(RpcSetText), "Insert Coin");
        await ToSignal(GetTree().CreateTimer(4d), SceneTreeTimer.SignalName.Timeout);
        Rpc(nameof(RpcSetText), string.Empty);
    }

    private async Task DispenseCup(string text)
    {
        var drink = drinks.FirstOrDefault(x => x.phrases.Any(y => text == y));
        for (int i = 0; i < text.Length; i+=3)
        {
            Rpc(nameof(RpcSetText), text.Remove(i));
            await ToSignal(GetTree().CreateTimer(0.5d), SceneTreeTimer.SignalName.Timeout);
        }
        Rpc(nameof(RpcSetText), text);
        await ToSignal(GetTree().CreateTimer(1d), SceneTreeTimer.SignalName.Timeout);
        if (IsInstanceValid(drink))
        {
            Credits--;
            Rpc(nameof(RpcDispenseSound), (int)drink.dispenseSound);
            Rpc(nameof(RpcSetText), "Dispensing...");
            double time = drink.dispenseSound > SCP294Drink.DispenseSound.Fill ? 8d : 4d;
            await ToSignal(GetTree().CreateTimer(time), SceneTreeTimer.SignalName.Timeout);
            Cup cupItem = (Cup)cup.SpawnNew().model;
            cupItem.FromDrink(drink);
            cupItem.Item.SV_Teleport(cupSpawn.GlobalPosition, cupSpawn.GlobalRotation);
            cupItem.Item.ModelEnabled = true;
            Rpc(nameof(RpcSetText), "Enjoy!");
            await ToSignal(GetTree().CreateTimer(4d), SceneTreeTimer.SignalName.Timeout);
            Rpc(nameof(RpcSetText), string.Empty);
        }
        else
        {
            Rpc(nameof(RpcSetText), "Out of range");
            await ToSignal(GetTree().CreateTimer(4d), SceneTreeTimer.SignalName.Timeout);
            Rpc(nameof(RpcSetText), string.Empty);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcSetText(string text)
    {
        label.Text = text;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcDispenseSound(int sound)
    {
        if (sound == -1)
        {
            audioPlayer.Stream = coinDropAudio;
            audioPlayer.Play();
            return;
        }
        audioPlayer.Stream = dispenseAudio[sound];
        audioPlayer.Play();
    }

    public void _LineSubmit(string text)
    {
        Log.Print(text);
        Rpc(nameof(RpcUse), text);
        uiLineEdit.ReleaseFocus();
    }

    private void _LostFocus(bool on)
    {
        if (on)
            return;
        uiRoot.Hide();
        InputManager.Instance.ChangeActionSet("InGame");
    }

    public override void _Ready()
    {
        uiLineEdit.TextSubmitted += _LineSubmit;
        uiLineEdit.EditingToggled += _LostFocus;
    }

    public void Use(IItemHolder holder, ItemObject item)
    {
        if (holder.GetPlayer().IsLocalPlayer)
        {
            if (IsInstanceValid(item) && item.Preset.ResourceName == "Coin") // TODO: this is janky
            {
                Rpc(nameof(RpcAddCredit));
                return;
            }
            string text = VoskProcessor.Instance.recentText;
            // VoskProcessor.Instance.recentText = string.Empty;
            if (!text.StartsWith("cup of "))
            {
                uiRoot.Show();
                uiLineEdit.Text = string.Empty;
                uiLineEdit.CallDeferred(Control.MethodName.GrabFocus);
                uiLineEdit.CallDeferred(Control.MethodName.GrabClickFocus);
                InputManager.Instance.ChangeActionSet("Menu");
                return;
            }
            Log.Print(text);
            Rpc(nameof(RpcUse), text);
        }
    }

    public void UseEnd(IItemHolder holder, ItemObject item)
    {
    }
}