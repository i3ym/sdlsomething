namespace SdlSomething;

public static class SdlUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe nint StructureToPointer<T>(in T structure)
        where T : unmanaged =>
        (nint) Unsafe.AsPointer(in structure);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe nint SpanToPointer<T>(ReadOnlySpan<T> span)
        where T : unmanaged =>
        (nint) Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));

    public static SDL.GPUTextureFormat GetStencilFormat(GpuDevice device)
    {
        ReadOnlySpan<SDL.GPUTextureFormat> depthFormats = [
            SDL.GPUTextureFormat.D24UnormS8Uint,
            SDL.GPUTextureFormat.D32FloatS8Uint,
        ];
        foreach (var format in depthFormats)
        {
            if (!SDL.GPUTextureSupportsFormat(device.Handle, format, SDL.GPUTextureType.TextureType2D, SDL.GPUTextureUsageFlags.DepthStencilTarget))
                continue;

            return format;
        }

        throw new NotSupportedException("Stencil formats not supported!");
    }
}
