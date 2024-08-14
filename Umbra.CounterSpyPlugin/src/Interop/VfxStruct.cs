using System.Numerics;
using System.Runtime.InteropServices;

namespace Umbra.CounterSpyPlugin.Interop;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct VfxStruct
{
    [FieldOffset(0x38)] public byte    Flags;
    [FieldOffset(0x50)] public Vector3 Position;
    [FieldOffset(0x60)] public Quat    Rotation;
    [FieldOffset(0x70)] public Vector3 Scale;

    [FieldOffset(0x128)] public int ActorCaster;
    [FieldOffset(0x130)] public int ActorTarget;

    [FieldOffset(0x1B8)] public int StaticCaster;
    [FieldOffset(0x1C0)] public int StaticTarget;
}

[StructLayout( LayoutKind.Sequential )]
public struct Quat {
    public float X;
    public float Z;
    public float Y;
    public float W;
}
