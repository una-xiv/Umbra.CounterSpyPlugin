using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Umbra.Common;
using Umbra.CounterSpyPlugin.Interop;

namespace Umbra.CounterSpyPlugin;

[Service]
internal sealed class CounterSpyRenderer(
    CounterSpyRepository repository,
    VfxManager           vfx
)
{
    public static bool Enabled { get; set; } = true;

    private readonly Dictionary<ulong, nint> _vfxList = [];

    [OnDraw]
    private void OnDraw()
    {
        if (!Enabled) return;

        foreach (var obj in repository.GetTargets(true, true)) {
            if (obj is IPlayerCharacter p && !_vfxList.ContainsKey(obj.GameObjectId)) {
                SpawnVfx(p);
            }
        }
    }

    [OnTick]
    private unsafe void OnTick()
    {
        foreach ((ulong id, nint ptr) in _vfxList) {
            var s = (VfxStruct*)ptr;
            if (s == null) continue;

            GameObject* obj = GameObjectManager.Instance()->Objects.GetObjectByGameObjectId(id);
            if (obj == null || !Enabled) {
                vfx.RemoveVfx(ptr);
                _vfxList.Remove(id);
                continue;
            }

            if (!repository.IsTargetingYou(id)) {
                vfx.RemoveVfx(ptr);
                _vfxList.Remove(id);
            }
        }
    }

    private void SpawnVfx(IGameObject player)
    {
        if (_vfxList.ContainsKey(player.GameObjectId)) return;

        nint ptr = vfx.PlayVfx("vfx/common/eff/sta_death00_m1.avfx", player);
        if (ptr == 0) return;

        Logger.Info($"Playing VFX for {player.Name}");

        _vfxList[player.GameObjectId] = ptr;
    }
}
