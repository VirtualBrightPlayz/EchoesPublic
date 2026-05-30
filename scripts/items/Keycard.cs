using Godot;
using System.Collections.Generic;
using System.Linq;
using static Godot.HttpRequest;

[GlobalClass]
public partial class Keycard : WorldItem, ISpecificEventSource<IKeycardEvent>
{
    public enum CardAccessCategory : byte
    {
        Unknown = 0,
        Maintenance = 1,
        Research = 2,
        Security = 3,
        Admin = 4,
    }

    public const int ViewmodelEventUse = 1;
    public const int ViewmodelEventUseFail = 2;

    [Export]
    public MeshInstance3D cardMesh;
    [Export]
    public Node3D cardSpawnV;
    [Export]
    public Node3D cardSpawnH;
    [Export]
    public MeshInstance3D textMesh;

    public KeycardAccess[] access => (Item.Preset as KeycardPreset)?.Access;
    public Material worldMaterial => (Item.Preset as KeycardPreset)?.worldMaterial;
    public Material viewMaterial => (Item.Preset as KeycardPreset)?.viewMaterial;
    public PackedScene cardScene => (Item.Preset as KeycardPreset)?.cardScene;
    public bool isHorizAnims => (Item.Preset as KeycardPreset).isHorizAnims;

    [Export]
    [ExportGroup("Sync")]
    public string CardHolderName;
    private string lastHolderName;

    public bool CanAccess(IEnumerable<KeycardAccess> accesses)
    {
        EventKeycardCanAccess evt = EventManager.GetInstance<EventKeycardCanAccess>();
        evt.KeycardAccess = new List<KeycardAccess>(access).AsReadOnly();
        evt.Result = accesses.Any(x => CanAccess(x));
        Emit(evt);
        return evt.Result;
    }

    public bool CanAccess(KeycardAccess door)
    {
        bool result = false;
        for (int i = 0; i < access.Length; i++)
        {
            if (access[i].level >= door.level && access[i].cardType == door.cardType)
            {
                result = true;
                break;
            }
        }
        EventKeycardCanAccess evt = EventManager.GetInstance<EventKeycardCanAccess>();
        evt.KeycardAccess = new List<KeycardAccess>([door]).AsReadOnly();
        evt.Result = result;
        Emit(evt);
        return evt.Result;
    }

    public void OnUsed(IEnumerable<KeycardAccess> access)
    {
        EventItemUsed evt = EventManager.GetInstance<EventItemUsed>();
        Emit(evt);
        Rpc(MethodName.RpcItemModelEvent, CanAccess(access) ? ViewmodelEventUse : ViewmodelEventUseFail);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcItemModelEvent(int id)
    {
        if (Item.viewModel is KeycardViewmodel view)
        {
            view.ViewmodelEvent(id);
        }
    }

    private void SpawnCard()
    {
        // cardMesh.MaterialOverride = worldMaterial;
        if (IsInstanceValid(cardScene))
        {
            var card = cardScene.Instantiate<Node3D>();
            if (isHorizAnims)
                cardSpawnH.AddChild(card, true);
            else
                cardSpawnV.AddChild(card, true);
        }
        visuals = FindChildren("*", nameof(VisualInstance3D), owned: false);
    }

    public override void _Ready()
    {
        base._Ready();
        CallDeferred(MethodName.SpawnCard);
    }

    public bool Emit(IKeycardEvent evt)
    {
        evt.Keycard = this;
        evt.WorldItem = this;
        return Emit(evt as IWorldItemEvent);
    }
}