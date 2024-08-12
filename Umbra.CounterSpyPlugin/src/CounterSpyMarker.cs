using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;
using Umbra.Common;
using Umbra.Game;
using Umbra.Markers;

namespace Umbra.CounterSpyPlugin;

[Service]
internal sealed class CounterSpyMarker(
    IObjectTable objectTable,
    IClientState clientState,
    IPlayer      player,
    IZoneManager zoneManager
) : WorldMarkerFactory
{
    public override string Id          => "Umbra_CounterSpyMarker";
    public override string Name        => "Counter Spy Marker";
    public override string Description => "Shows world markers on players and NPCs that are targeting you.";

    public override List<IMarkerConfigVariable> GetConfigVariables()
    {
        return [
            ..DefaultStateConfigVariables,
            new BooleanMarkerConfigVariable(
                "EnablePlayers",
                "Show markers on players targeting you",
                "Show world markers on players that are targeting your character.",
                true
            ),
            new BooleanMarkerConfigVariable(
                "EnableNPCs",
                "Show markers on NPCs targeting you",
                "Show world markers on NPCs that are targeting your character.",
                false
            ),
            new BooleanMarkerConfigVariable(
                "ShowName",
                "Show name",
                "Show the name of the player or NPC that is targeting you on the world marker.",
                false
            ),
            new BooleanMarkerConfigVariable(
                "ShowTargetingYou",
                "Show the text 'Targeting you' on the world marker.",
                null,
                true
            ),
            new IntegerMarkerConfigVariable(
                "PlayerIconId",
                "Icon ID for players targeting you",
                "The icon ID to use for the world marker. Use value 0 to disable the icon. Type \"/xldata icons\" in the chat to access the icon browser.",
                60407,
                0
            ),
            new IntegerMarkerConfigVariable(
                "NPCIconId",
                "Icon ID for NPCs targeting you",
                "The icon ID to use for the world marker. Use value 0 to disable the icon. Type \"/xldata icons\" in the chat to access the icon browser.",
                61510,
                0
            ),
        ];
    }

    [OnTick]
    private void OnTick()
    {
        if (null == clientState.LocalPlayer
            || !zoneManager.HasCurrentZone
            || player.IsBetweenAreas
            || player.IsInCutscene
            || player.IsDead
            || player.IsOccupied
            || !GetConfigValue<bool>("Enabled")
        ) {
            RemoveAllMarkers();
            return;
        }

        ulong localPlayerId = clientState.LocalPlayer.GameObjectId;
        uint  mapId         = zoneManager.CurrentZone.Id;

        List<string> usedIds = [];

        foreach (var obj in objectTable) {
            if (!obj.IsValid()
                || obj.IsDead
                || obj.GameObjectId == localPlayerId
                || obj.TargetObjectId != localPlayerId
                || obj.ObjectKind is not (ObjectKind.Player or ObjectKind.BattleNpc)
            ) continue;

            switch (obj.ObjectKind) {
                case ObjectKind.Player when !GetConfigValue<bool>("EnablePlayers"):
                case ObjectKind.BattleNpc when !GetConfigValue<bool>("EnableNPCs"):
                    continue;
            }

            var key = $"CounterSpyMarker_{mapId}_{obj.GameObjectId}";
            usedIds.Add(key);

            int iconId = obj.ObjectKind == ObjectKind.Player
                ? GetConfigValue<int>("PlayerIconId")
                : GetConfigValue<int>("NPCIconId");

            SetMarker(
                new() {
                    Key           = key,
                    MapId         = zoneManager.CurrentZone.Id,
                    IconId        = (uint)iconId,
                    Position      = obj.Position with { Y = obj.Position.Y + 2f },
                    Label         = GetConfigValue<bool>("ShowName") ? obj.Name.TextValue : "",
                    SubLabel      = GetConfigValue<bool>("ShowTargetingYou") ? "Targeting you" : null,
                    FadeDistance  = new(0.1f, 1f),
                    ShowOnCompass = GetConfigValue<bool>("ShowOnCompass"),
                }
            );
        }

        RemoveMarkersExcept(usedIds);
    }
}
