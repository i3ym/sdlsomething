namespace SdlSomething;

public static class SdlUtils
{
    public static unsafe nint StructureToPointer<T>(in T structure)
        where T : unmanaged =>
        (nint) Unsafe.AsPointer(in structure);

    public static unsafe nint SpanToPointer<T>(ReadOnlySpan<T> span)
        where T : unmanaged =>
        (nint) Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));

    public static nint LoadShader(GpuDevice device, string path, SDL.GPUShaderStage stage)
    {
        var codeptr = SDL.LoadFile(path, out var codeSize);
        var info = new SDL.GPUShaderCreateInfo()
        {
            Code = codeptr,
            CodeSize = codeSize,
            Entrypoint = "main",
            Format = SDL.GPUShaderFormat.SPIRV,
            Stage = stage,
            NumSamplers = 0,
            NumStorageBuffers = 0,
            NumStorageTextures = 0,
            NumUniformBuffers = 1,
        };

        var shader = SDL.CreateGPUShader(device.Handle, info);
        SDL.Free(codeptr);

        return shader;
    }

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
