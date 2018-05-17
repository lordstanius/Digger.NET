using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

public static class StdLib
{
    static StdLib()
    {
        var memset = new DynamicMethod("_memset", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard,
            null, new[] { typeof(IntPtr), typeof(byte), typeof(int) }, typeof(StdLib), true);

        var generator = memset.GetILGenerator();
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Ldarg_2);
        generator.Emit(OpCodes.Initblk);
        generator.Emit(OpCodes.Ret);

        var memcpy = new DynamicMethod("_memcpy", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard,
            null, new[] { typeof(IntPtr), typeof(IntPtr), typeof(int) }, typeof(StdLib), true);

        generator = memcpy.GetILGenerator();
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Ldarg_2);
        generator.Emit(OpCodes.Cpblk);
        generator.Emit(OpCodes.Ret);

        _memsetDelegate = (Action<IntPtr, byte, int>)memset.CreateDelegate(typeof(Action<IntPtr, byte, int>));
        _memcpyDelegate = (Action<IntPtr, IntPtr, int>)memcpy.CreateDelegate(typeof(Action<IntPtr, IntPtr, int>));
    }

    private static Action<IntPtr, byte, int> _memsetDelegate;
    private static Action<IntPtr, IntPtr, int> _memcpyDelegate;

    public static void MemSet(byte[] array, byte what, int length)
    {
        var gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
        _memsetDelegate(gcHandle.AddrOfPinnedObject(), what, length);
        gcHandle.Free();
    }

    public static void MemSet(IntPtr destination, byte what, int lenght)
    {
        _memsetDelegate.Invoke(destination, what, lenght);
    }

    public static void MemCpy(IntPtr destination, IntPtr source, int lenght)
    {
        _memcpyDelegate.Invoke(destination, source, lenght);
    }

    public static T ToStruct<T>(this IntPtr ptr) where T: struct
    {
        return (T)Marshal.PtrToStructure(ptr, typeof(T));
    }

    public static IntPtr ToPointer<T>(this T structure) where T: struct
    {
        IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
        Marshal.StructureToPtr(structure, ptr, false);

        return ptr;
    }

    public static unsafe void MemCpy(IntPtr destination, uint[] source)
    {
        fixed (void* pSource = &source[0])
        {
            _memcpyDelegate.Invoke(destination, new IntPtr(pSource), source.Length * sizeof(uint));
        }
    }
}
