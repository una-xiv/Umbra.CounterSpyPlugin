using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
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
) : StandardToolbarWidget(info, guid, configValues)
{
    public override MenuPopup Popup { get; } = new();

    protected override StandardWidgetFeatures Features =>
        StandardWidgetFeatures.Text |
        StandardWidgetFeatures.Icon;

    private readonly Dictionary<string, Dictionary<string, MenuPopup.Button>> _menuItems   = [];
    private readonly MenuPopup.Group                                          _playerGroup = new("Players");
    private readonly MenuPopup.Group                                          _npcGroup    = new("NPCs");

    private CounterSpyRepository Repository    { get; } = Framework.Service<CounterSpyRepository>();
    private IPlayer              Player        { get; } = Framework.Service<IPlayer>();
    private ITargetManager       TargetManager { get; } = Framework.Service<ITargetManager>();

    protected override void OnLoad()
    {
        Popup.Add(_playerGroup);
        Popup.Add(_npcGroup);

        _menuItems["Players"] = [];
        _menuItems["NPCs"]    = [];
    }

    protected override void OnDraw()
    {
        var showPlayers = GetConfigValue<bool>("ShowPlayers");
        var showNpcs    = GetConfigValue<bool>("ShowNPCs");
        var playerList  = Repository.GetTargets(showPlayers, false);
        var npcList     = Repository.GetTargets(false, showNpcs);
        var isEmpty     = playerList.Count == 0 && npcList.Count == 0;

        var iconId = playerList.Count > 0
            ? (uint)GetConfigValue<int>("PlayerIconId")
            : npcList.Count > 0
                ? (uint)GetConfigValue<int>("NPCIconId")
                : 0u;

        SetGameIconId(iconId);

        IsVisible = !(isEmpty && GetConfigValue<bool>("HideIfEmpty"));
        if (!IsVisible) return;

        if (playerList.Count == 0 && npcList.Count == 0)
        {
            SetText("No targets");
            ClearIcon();
            return;
        }

        var playersLabel = "";
        var npcLabel     = "";

        if (playerList.Count > 0)
        {
            playersLabel = $"Players: {playerList.Count}";
        }

        if (npcList.Count > 0)
        {
            npcLabel = $"NPCs {npcList.Count}";
        }

        var label = $"{playersLabel} {npcLabel}";
        SetText(label.Trim());

        UpdateMenuItems(playerList, _playerGroup);
        UpdateMenuItems(npcList, _npcGroup);
    }

    private void UpdateMenuItems(List<IGameObject> list, MenuPopup.Group group)
    {
        if (!_menuItems.ContainsKey(group.Label!)) _menuItems[group.Label!] = [];

        List<string> usedIds = [];

        foreach (var obj in list)
        {
            var   id   = $"obj_{obj.GameObjectId}";
            float d    = Vector3.Distance(Player.Position, obj.Position);
            var   dist = $"{d:N0} yalms";

            usedIds.Add(id);

            if (!_menuItems[group.Label!].ContainsKey(id))
            {
                _menuItems[group.Label!][id] = new MenuPopup.Button(obj.Name.TextValue)
                {
                    IsDisabled = d > 50,
                    Icon       = obj is IPlayerCharacter p ? p.ClassJob.RowId + 62000 : null,
                    AltText    = dist,
                    SortIndex  = obj.ObjectIndex,
                    OnClick    = () => TargetManager.Target = obj,
                };
            }

            var button = _menuItems[group.Label!][id];
            group.Add(button);
        }

        foreach (var (id, btn) in _menuItems[group.Label!].ToDictionary())
        {
            if (!usedIds.Contains(id))
            {
                group.Remove(btn);
                _menuItems[group.Label!].Remove(id);
            }
        }
    }

    protected override IEnumerable<IWidgetConfigVariable> GetConfigVariables()
    {
        return
        [
            ..base.GetConfigVariables(),

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
        ];
    }
}