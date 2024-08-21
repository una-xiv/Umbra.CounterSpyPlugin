using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Umbra.Common;
using Umbra.Game;
using Umbra.Markers;

namespace Umbra.CounterSpyPlugin;

[Service]
internal sealed class CounterSpyMarker(
    CounterSpyRepository repository,
    IPlayer              player,
    IZoneManager         zoneManager
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
            new SelectMarkerConfigVariable(
                "VfxId",
                "Effect",
                "The visual effect to show on the player that is targeting you.",
                "None",
                new() {
                    { "", "None" },
                    { "vfx/common/eff/cmrz_castx1c.avfx", "Light" },
                    { "vfx/common/eff/levitate0f.avfx", "Levitate" },
                    { "vfx/common/eff/m0328sp10st0f.avfx", "Rotating Balls" },
                    { "vfx/common/eff/dkst_over_p0f.avfx", "Blue Aura" },
                    { "vfx/common/eff/st_akama_kega0j.avfx", "Red Swirls" }
                }
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
            new IntegerMarkerConfigVariable(
                "MarkerHeight",
                "Height of the marker relative to the target",
                "Specifies the height of the world marker relative to the position of the player or NPC that is targeting you. A value of 0 will place the marker at the feet of the target.",
                2,
                -10,
                10
            ),
            new BooleanMarkerConfigVariable(
                "PreviewMode",
                "Enable preview mode",
                "Shows the world marker on all players and NPCs that are near you, even if they are not targeting you.",
                false
            )
        ];
    }

    [OnTick]
    private void OnTick()
    {
        if (!zoneManager.HasCurrentZone
            || player.IsBetweenAreas
            || player.IsInCutscene
            || player.IsDead
            || player.IsOccupied
            || !GetConfigValue<bool>("Enabled")
           ) {
            RemoveAllMarkers();
            return;
        }

        repository.IsPreviewMode = GetConfigValue<bool>("PreviewMode");
        CounterSpyRenderer.VfxId = GetConfigValue<string>("VfxId");

        uint mapId = zoneManager.CurrentZone.Id;

        List<IGameObject> targets = repository.GetTargets(
            GetConfigValue<bool>("EnablePlayers"),
            GetConfigValue<bool>("EnableNPCs")
        );

        if (targets.Count == 0) {
            RemoveAllMarkers();
            return;
        }

        List<string> usedIds = [];

        uint    zoneId           = zoneManager.CurrentZone.Id;
        var     pIconId          = (uint)GetConfigValue<int>("PlayerIconId");
        var     nIconId          = (uint)GetConfigValue<int>("NPCIconId");
        var     markerHeight     = GetConfigValue<int>("MarkerHeight");
        var     showName         = GetConfigValue<bool>("ShowName");
        var     showTargetingYou = GetConfigValue<bool>("ShowTargetingYou");
        var     showOnCompass    = GetConfigValue<bool>("ShowOnCompass");
        Vector2 fadeDist         = new(0.1f, 1f);

        foreach (var obj in targets) {
            var key = $"CounterSpyMarker_{mapId}_{obj.GameObjectId:x8}";
            usedIds.Add(key);

            SetMarker(
                new() {
                    Key           = key,
                    MapId         = zoneId,
                    IconId        = obj.ObjectKind == ObjectKind.Player ? pIconId : nIconId,
                    Position      = obj.Position with { Y = obj.Position.Y + markerHeight },
                    Label         = showName ? obj.Name.TextValue : "",
                    SubLabel      = showTargetingYou ? "Targeting you" : null,
                    FadeDistance  = fadeDist,
                    ShowOnCompass = showOnCompass,
                }
            );
        }

        RemoveMarkersExcept(usedIds);
    }
}
