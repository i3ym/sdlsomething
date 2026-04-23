using Slangc.NET;

namespace SdlSomething;

public static class GraphicsPipeline
{
    public readonly struct Config
    {
        public nint VertexShader { get; }
        public nint FragmentShader { get; }

        public SDL.GPUPrimitiveType PrimitiveType { get; init; } = SDL.GPUPrimitiveType.TriangleList;
        public SDL.GPUGraphicsPipelineCreateInfo Init { get; init; }

        public required bool Blending { get; init; }
        public required bool Depth { get; init; }
        public required bool BackfaceCulling { get; init; }

        public Config((nint vert, nint frag) shaders) : this(shaders.vert, shaders.frag) { }
        public Config(nint vert, nint frag)
        {
            VertexShader = vert;
            FragmentShader = frag;
        }
    }

    public static nint Create(GpuDevice device, Window window, in Config config)
    {
        var colorTargets = new[]
        {
            new SDL.GPUColorTargetDescription()
            {
                Format = SDL.GetGPUSwapchainTextureFormat(device.Handle, window.Handle),
            },
        };

        var info = config.Init;
        info.PrimitiveType = config.PrimitiveType;
        info.VertexShader = config.VertexShader;
        info.FragmentShader = config.FragmentShader;

        if (config.Depth)
        {
            info.TargetInfo.HasDepthStencilTarget = true;
            info.TargetInfo.DepthStencilFormat = GetStencilFormat(device);

            ref var ds = ref info.DepthStencilState;
            ds.EnableDepthTest = true;
            ds.EnableDepthWrite = true;
            ds.CompareOp = SDL.GPUCompareOp.Less;
            ds.WriteMask = 0xFF;
        }
        if (config.Blending)
        {
            ref var bs = ref colorTargets[0].BlendState;

            bs.EnableBlend = true;
            bs.ColorWriteMask = SDL.GPUColorComponentFlags.R
                | SDL.GPUColorComponentFlags.G
                | SDL.GPUColorComponentFlags.B
                | SDL.GPUColorComponentFlags.A;

            // (src * srcA) + (dst * (1 - srcA))
            bs.SrcColorBlendFactor = SDL.GPUBlendFactor.SrcAlpha;
            bs.DstColorBlendFactor = SDL.GPUBlendFactor.OneMinusSrcAlpha;
            bs.ColorBlendOp = SDL.GPUBlendOp.Add;

            // outA = (srcA * 1) + (dstA * 0)
            bs.SrcAlphaBlendFactor = SDL.GPUBlendFactor.One;
            bs.DstAlphaBlendFactor = SDL.GPUBlendFactor.Zero;
            bs.AlphaBlendOp = SDL.GPUBlendOp.Add;
        }
        if (config.BackfaceCulling)
            info.RasterizerState.CullMode = SDL.GPUCullMode.Back;


        info.TargetInfo.NumColorTargets = (uint) colorTargets.Length;
        info.TargetInfo.ColorTargetDescriptions = SpanToPointer(colorTargets);


        var pipeline = SDL.CreateGPUGraphicsPipeline(device.Handle, info);
        SDL.ReleaseGPUShader(device.Handle, info.VertexShader);
        SDL.ReleaseGPUShader(device.Handle, info.FragmentShader);

        return pipeline;
    }

    public static nint CompileShader(string name, GpuDevice device, SDL.GPUShaderStage stage) =>
        CompileShader(name, device, stage, []);
    public static nint CompileShader(string name, GpuDevice device, SDL.GPUShaderStage stage, IReadOnlyCollection<string> additionalargs, string? description = null)
    {
        description ??= (additionalargs.Count == 0 ? null : string.Join(" ", additionalargs));
        Console.WriteLine($"Compiling {stage} shader {name}" + (description is null ? null : $" ({description})"));

        var args = (string[]) [
            $"resources/shaders/{name}.slang",
            "-target", "spirv",
            "-matrix-layout-column-major",
            .. additionalargs,
        ];
        var shader = SlangCompiler.CompileWithReflection(args, out var reflection);

        var uniformSpace = stage == SDL.GPUShaderStage.Vertex ? 1 : 3;
        var uniformBuffers = reflection.Parameters
            .Count(c => c.Type.Kind == SlangTypeKind.ConstantBuffer && c.Bindings.Single().Space == uniformSpace);


        var info = new SDL.GPUShaderCreateInfo()
        {
            Code = StructureToPointer(in MemoryMarshal.GetReference(shader)),
            CodeSize = (nuint) shader.Length,
            Entrypoint = "main",
            Format = SDL.GPUShaderFormat.SPIRV,
            Stage = stage,
            NumSamplers = 0,
            NumStorageBuffers = 0,
            NumStorageTextures = 0,
            NumUniformBuffers = (uint) uniformBuffers,
        };

        return SDL.CreateGPUShader(device.Handle, info);
    }

    public static (nint vert, nint frag) CompileShaders(string name, GpuDevice device) =>
        CompileShaders(name, device, []);
    public static (nint vert, nint frag) CompileShaders(string name, GpuDevice device, IReadOnlyCollection<string> additionalargs, string? description = null)
    {
        return (
            CompileShader(name, device, SDL.GPUShaderStage.Vertex, additionalargs, description),
            CompileShader(name, device, SDL.GPUShaderStage.Fragment, additionalargs, description)
        );
    }
}
