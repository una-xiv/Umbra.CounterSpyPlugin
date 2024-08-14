using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling;
using Umbra.Common;
using Umbra.Game;
using Umbra.Widgets;

namespace Umbra.CounterSpyPlugin;

[ToolbarWidget(
    "Umbra_CounterSpyWidget",
    "Counter Spy Widget",
    "Shows a list of players and NPCs that are targeting you."
)]
public class CounterSpyWidget(
    WidgetInfo                  info,
    string?                     guid         = null,
    Dictionary<string, object>? configValues = null
) : DefaultToolbarWidget(info, guid, configValues)
{
    public override MenuPopup Popup { get; } = new();

    private Dictionary<string, List<string>> _menuItems = [];

    private CounterSpyRepository Repository    { get; } = Framework.Service<CounterSpyRepository>();
    private IPlayer              Player        { get; } = Framework.Service<IPlayer>();
    private ITargetManager       TargetManager { get; } = Framework.Service<ITargetManager>();

    protected override void Initialize()
    {
        Popup.AddGroup("Players", "Players");
        Popup.AddGroup("NPCs",    "NPCs");

        _menuItems["Players"] = [];
        _menuItems["NPCs"]    = [];
    }

    protected override void OnUpdate()
    {
        var               showPlayers = GetConfigValue<bool>("ShowPlayers");
        var               showNpcs    = GetConfigValue<bool>("ShowNPCs");
        bool              showIcons   = GetConfigValue<string>("DisplayMode") != "TextOnly";
        List<IGameObject> playerList  = Repository.GetTargets(showPlayers, false);
        List<IGameObject> npcList     = Repository.GetTargets(false,       showNpcs);
        bool              isEmpty     = playerList.Count == 0 && npcList.Count == 0;

        uint iconId = playerList.Count > 0
            ? (uint)GetConfigValue<int>("PlayerIconId")
            : npcList.Count > 0
                ? (uint)GetConfigValue<int>("NPCIconId")
                : 0u;

        SetIcon(iconId);

        Node.Style.IsVisible = !(isEmpty && GetConfigValue<bool>("HideIfEmpty"));

        if (playerList.Count == 0 && npcList.Count == 0) {
            SetLabel("No targets");
            SetIcon(null);
            return;
        }

        var label        = "";
        var playersLabel = "";
        var npcsLabel    = "";

        if (playerList.Count > 0) {
            playersLabel = $"Players: {playerList.Count}";
        }

        if (npcList.Count > 0) {
            npcsLabel = $"NPCs {npcList.Count}";
        }

        label = $"{playersLabel} {npcsLabel}";
        SetLabel(label.Trim());

        UpdateMenuItems(playerList, "Players");
        UpdateMenuItems(npcList,    "NPCs");

        base.OnUpdate();
    }

    private void UpdateMenuItems(List<IGameObject> list, string group)
    {
        foreach (var obj in list) {
            var   id   = $"obj_{obj.GameObjectId}";
            float d    = Vector3.Distance(Player.Position, obj.Position);
            var   dist = $"{d:N0} yalms";

            if (Popup.HasButton(id)) {
                Popup.SetButtonAltLabel(id, dist);
                Popup.SetButtonDisabled(id, d > 50);

                if (obj is IPlayerCharacter player) {
                    Popup.SetButtonIcon(id, player.ClassJob.Id + 62000);
                }

                continue;
            }

            _menuItems[group].Add(id);
            Popup.AddButton(
                id,
                obj.Name.TextValue,
                obj.ObjectIndex,
                null,
                dist,
                groupId: group,
                onClick: () => TargetManager.Target = obj
            );
        }

        foreach (string id in _menuItems[group].ToArray()) {
            if (list.Find(obj => $"obj_{obj.GameObjectId}" == id) == null) {
                _menuItems[group].Remove(id);
                Popup.RemoveButton(id);
            }
        }
    }

    protected override IEnumerable<IWidgetConfigVariable> GetConfigVariables()
    {
        return [
            new BooleanWidgetConfigVariable(
                "HideIfEmpty",
                "Hide the widget if nothing targets you.",
                "Hide the widget if there are no players or NPCs are currently targeting you.",
                true
            ),
            new BooleanWidgetConfigVariable(
                "ShowPlayers",
                "Show players",
                "Include player characters in the list of entities currently targeting you.",
                true
            ),
            new BooleanWidgetConfigVariable(
                "ShowNPCs",
                "Show NPCs",
                "Include non-player characters in the list of entities currently targeting you.",
                false
            ),
            new IntegerWidgetConfigVariable(
                "PlayerIconId",
                "Icon ID for players targeting you",
                "The icon ID to use for the world marker. Use value 0 to disable the icon. Type \"/xldata icons\" in the chat to access the icon browser.",
                60407,
                0
            ),
            new IntegerWidgetConfigVariable(
                "NPCIconId",
                "Icon ID for NPCs targeting you",
                "The icon ID to use for the world marker. Use value 0 to disable the icon. Type \"/xldata icons\" in the chat to access the icon browser.",
                61510,
                0
            ),
            ..DefaultToolbarWidgetConfigVariables,
            ..SingleLabelTextOffsetVariables
        ];
    }
}
