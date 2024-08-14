using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Umbra.Common;

namespace Umbra.CounterSpyPlugin.Interop;

[Service]
internal unsafe class VfxManager : IDisposable
{
    public const string ActorVfxCreateSig =
        "40 53 55 56 57 48 81 EC ?? ?? ?? ?? 0F 29 B4 24 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B6 AC 24 ?? ?? ?? ?? 0F 28 F3 49 8B F8";

    public const string ActorVfxRemoveSig = "0F 11 48 10 48 8D 05";

    public delegate IntPtr ActorVfxCreateDelegate(
        string path, IntPtr a2, IntPtr a3, float a4, char a5, ushort a6, char a7
    );

    public ActorVfxCreateDelegate ActorVfxCreate;

    public delegate IntPtr ActorVfxRemoveDelegate(IntPtr vfx, char a2);

    public ActorVfxRemoveDelegate ActorVfxRemove;

    // ======== ACTOR HOOKS =============
    public Hook<ActorVfxCreateDelegate>? ActorVfxCreateHook { get; private set; }

    public Hook<ActorVfxRemoveDelegate>? ActorVfxRemoveHook { get; private set; }

    private readonly List<nint> _vfxActors = [];

    public VfxManager(IGameInteropProvider interop, ISigScanner sigScanner)
    {
        nint actorVfxCreateAddress     = sigScanner.ScanText(ActorVfxCreateSig);
        nint actorVfxRemoveAddressTemp = sigScanner.ScanText(ActorVfxRemoveSig) + 7;

        nint actorVfxRemoveAddress =
            Marshal.ReadIntPtr(actorVfxRemoveAddressTemp + Marshal.ReadInt32(actorVfxRemoveAddressTemp) + 4);

        ActorVfxCreate = Marshal.GetDelegateForFunctionPointer<ActorVfxCreateDelegate>(actorVfxCreateAddress);
        ActorVfxRemove = Marshal.GetDelegateForFunctionPointer<ActorVfxRemoveDelegate>(actorVfxRemoveAddress);

        ActorVfxCreateHook = interop.HookFromAddress<ActorVfxCreateDelegate>(actorVfxCreateAddress, ActorVfxNewDetour);

        ActorVfxRemoveHook =
            interop.HookFromAddress<ActorVfxRemoveDelegate>(actorVfxRemoveAddress, ActorVfxRemoveDetour);

        ActorVfxCreateHook?.Enable();
        ActorVfxRemoveHook?.Enable();
    }

    public void Dispose()
    {
        foreach (nint ptr in _vfxActors) {
            ActorVfxRemoveHook?.Original(ptr, (char)0);
        }

        ActorVfxCreateHook?.Dispose();
        ActorVfxRemoveHook?.Dispose();
    }

    public IntPtr PlayVfx(string path, IGameObject obj)
    {
        if (ActorVfxCreateHook == null) return 0;

        var go = GameObjectManager.Instance()->Objects.GetObjectByGameObjectId(obj.GameObjectId);

        if (go == null) {
            Logger.Info("GameObject not found");
            return 0;
        }

        nint actor = Framework.Service<IObjectTable>().GetObjectAddress(obj.ObjectIndex);
        nint ptr   = ActorVfxCreateHook.Original(path, actor, actor, -1, (char)0, 0, (char)0);

        if (ptr > 0) {
            _vfxActors.Add(ptr);
        }

        return ptr;
    }

    public void RemoveVfx(nint ptr)
    {
        if (ActorVfxRemoveHook == null) return;

        ActorVfxRemoveHook.Original(ptr, (char)0);
        _vfxActors.Remove(ptr);
        Logger.Info($"Removed VFX: {ptr:X8}");
    }

    private IntPtr ActorVfxNewDetour(string path, IntPtr a2, IntPtr a3, float a4, char a5, ushort a6, char a7)
    {
        if (ActorVfxCreateHook == null) return 0;

        IntPtr vfx = ActorVfxCreateHook.Original(path, a2, a3, a4, a5, a6, a7);

        return vfx;
    }

    private IntPtr ActorVfxRemoveDetour(IntPtr vfx, char a2)
    {
        return ActorVfxRemoveHook == null ? 0 : ActorVfxRemoveHook.Original(vfx, a2);
    }
}
