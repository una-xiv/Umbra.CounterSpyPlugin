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
    public static string VfxId { get; set; } = "";

    private readonly Dictionary<ulong, nint> _vfxList = [];

    private string _lastVfxId = "";

    [OnDraw]
    private void OnDraw()
    {
        if (string.IsNullOrEmpty(VfxId)) return;

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
            if (obj == null || string.IsNullOrEmpty(VfxId) || VfxId != _lastVfxId) {
                vfx.RemoveVfx(ptr);
                _vfxList.Remove(id);
                continue;
            }

            if (!repository.IsTargetingYou(id)) {
                vfx.RemoveVfx(ptr);
                _vfxList.Remove(id);
            }
        }

        _lastVfxId = VfxId;
    }

    private void SpawnVfx(IGameObject player)
    {
        if (_vfxList.ContainsKey(player.GameObjectId)) return;
        if (false == VfxId.StartsWith("vfx/common/eff/")) return;

        nint ptr = vfx.PlayVfx(VfxId, player);
        if (ptr == 0) return;

        Logger.Info($"Playing VFX for {player.Name}");

        _vfxList[player.GameObjectId] = ptr;
    }
}
