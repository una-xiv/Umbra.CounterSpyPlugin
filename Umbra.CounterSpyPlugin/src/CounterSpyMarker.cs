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
        ];
    }

    [OnTick]
    private void OnTick()
    {
        if (!zoneManager.HasCurrentZone) {
            RemoveAllMarkers();
            return;
        }

        if (null == clientState.LocalPlayer) return;
        ulong localPlayerId = clientState.LocalPlayer.GameObjectId;

        List<string> usedIds = [];

        foreach (var obj in objectTable) {
            if (!obj.IsValid()
                || obj.IsDead
                || obj.ObjectKind != ObjectKind.Player
                || obj.GameObjectId == localPlayerId
                || obj.TargetObjectId != localPlayerId)
                continue;

            var key = $"CounterSpyMarker_{obj.GameObjectId}";
            usedIds.Add(key);

            SetMarker(
                new() {
                    Key           = key,
                    MapId         = zoneManager.CurrentZone.Id,
                    IconId        = 60407u,
                    Position      = obj.Position with { Y = obj.Position.Y + 2f },
                    SubLabel      = "Targeting you",
                    FadeDistance  = new(0.1f, 1f),
                    ShowOnCompass = GetConfigValue<bool>("ShowOnCompass"),
                }
            );
        }

        RemoveMarkersExcept(usedIds);
    }
}
