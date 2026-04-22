using Slangc.NET;

namespace SdlSomething;

public static class GraphicsPipeline
{
    public readonly struct Config()
    {
        public required nint VertexShader { get; init; }
        public required nint FragmentShader { get; init; }

        public SDL.GPUPrimitiveType PrimitiveType { get; init; } = SDL.GPUPrimitiveType.TriangleList;
        public SDL.GPUGraphicsPipelineCreateInfo Init { get; init; }

        public bool EnableBlending { get; init; } = false;
        public bool DepthEnabled { get; init; } = true;
        public bool BackfaceCulling { get; init; } = true;

        [SetsRequiredMembers]
        public Config((nint vert, nint frag) shaders) : this() => (VertexShader, FragmentShader) = shaders;
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

        if (config.DepthEnabled)
        {
            info.TargetInfo.HasDepthStencilTarget = true;
            info.TargetInfo.DepthStencilFormat = GetStencilFormat(device);

            info.DepthStencilState = new SDL.GPUDepthStencilState()
            {
                EnableDepthTest = true,
                EnableDepthWrite = true,
                CompareOp = SDL.GPUCompareOp.Less,
                WriteMask = 0xFF,
            };
        }
        if (config.EnableBlending)
        {
            colorTargets[0].BlendState = new SDL.GPUColorTargetBlendState()
            {
                EnableBlend = true,
                ColorWriteMask = SDL.GPUColorComponentFlags.R
                    | SDL.GPUColorComponentFlags.G
                    | SDL.GPUColorComponentFlags.B
                    | SDL.GPUColorComponentFlags.A,

                // (src * srcA) + (dst * (1 - srcA))
                SrcColorBlendFactor = SDL.GPUBlendFactor.SrcAlpha,
                DstColorBlendFactor = SDL.GPUBlendFactor.OneMinusSrcAlpha,
                ColorBlendOp = SDL.GPUBlendOp.Add,

                // outA = (srcA * 1) + (dstA * 0)
                SrcAlphaBlendFactor = SDL.GPUBlendFactor.One,
                DstAlphaBlendFactor = SDL.GPUBlendFactor.Zero,
                AlphaBlendOp = SDL.GPUBlendOp.Add,
            };
        }
        if (config.BackfaceCulling)
            info.RasterizerState.CullMode = SDL.GPUCullMode.Back;


        info.TargetInfo = new SDL.GPUGraphicsPipelineTargetInfo()
        {
            NumColorTargets = (uint) colorTargets.Length,
            ColorTargetDescriptions = SpanToPointer(colorTargets),
        };


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
