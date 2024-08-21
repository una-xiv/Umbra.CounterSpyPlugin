using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Umbra.Common;
using Umbra.Game;

namespace Umbra.CounterSpyPlugin;

[Service]
internal sealed class CounterSpyRepository(
    IClientState clientState,
    IObjectTable objectTable,
    IPlayer      player
) : IDisposable
{
    private readonly Dictionary<ulong, IGameObject> _objects = [];

    public bool IsPreviewMode { get; set; }

    public List<IGameObject> GetTargets(bool players, bool npcs)
    {
        lock (_objects) {
            return _objects
                .Values.Where(
                    obj => (players && obj.ObjectKind == ObjectKind.Player)
                        || (npcs && obj.ObjectKind == ObjectKind.BattleNpc)
                )
                .ToList();
        }
    }

    public bool IsTargetingYou(ulong id)
    {
        lock (_objects) {
            return _objects.ContainsKey(id);
        }
    }

    [OnTick]
    private void OnTick()
    {
        if (null == clientState.LocalPlayer) return;

        lock (_objects) {
            if (player.IsBetweenAreas || player.IsInCutscene) {
                _objects.Clear();
                return;
            }

            ulong       playerId  = clientState.LocalPlayer.GameObjectId;
            Vector3     playerPos = clientState.LocalPlayer.Position;
            List<ulong> ids       = [];

            // Sort objects by distance to player.
            List<IGameObject> objects =
                objectTable.ToArray().OrderBy(p => Vector3.Distance(playerPos, p.Position)).ToList();

            foreach (var obj in objects) {
                if (obj.IsValid()
                    && (obj.ObjectKind is ObjectKind.Player or ObjectKind.BattleNpc)
                    && obj.TargetObjectId != obj.GameObjectId
                    && obj.GameObjectId != playerId
                    && (IsPreviewMode || obj.TargetObjectId == playerId)
                    && !obj.IsDead
                   ) {
                    ids.Add(obj.GameObjectId);
                    _objects[obj.GameObjectId] = obj;
                }
            }

            foreach (ulong obj in _objects.Keys.ToArray()) {
                if (!ids.Contains(obj)) {
                    _objects.Remove(obj);
                }
            }
        }
    }

    public void Dispose()
    {
        _objects.Clear();
    }
}
