global using System.Diagnostics.CodeAnalysis;
global using SDL3;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SdlSomething;

if (!SDL.Init(SDL.InitFlags.Video))
{
    SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize: {SDL.GetError()}");
    return;
}
var window = SDL.CreateWindow("SDL3 Create Window", 800, 600, SDL.WindowFlags.Resizable);
var device = new GpuDevice();
if (!SDL.ClaimWindowForGPUDevice(device.Handle, window))
    throw new Exception(SDL.GetError());



using var vb = new GpuBuffer<Vertex>(device, 4, SDL.GPUBufferUsageFlags.Vertex);
using var vtb = new GpuTransferBuffer<Vertex>(vb);

using var ib = new GpuBuffer<Int16>(device, 6, SDL.GPUBufferUsageFlags.Index);
using var itb = new GpuTransferBuffer<Int16>(ib);

static nint loadShader(GpuDevice device, string path, SDL.GPUShaderStage stage)
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
var vertexShader = loadShader(device, "/home/i3ym/workspace/Projects/_test/sdlsomething/triangle.vert.spv", SDL.GPUShaderStage.Vertex);
var fragmentShader = loadShader(device, "/home/i3ym/workspace/Projects/_test/sdlsomething/triangle.frag.spv", SDL.GPUShaderStage.Fragment);

SDL.GPUTextureFormat depthStencilFormat = default;
{
    ReadOnlySpan<SDL.GPUTextureFormat> depthFormats = [
        SDL.GPUTextureFormat.D24UnormS8Uint,
        SDL.GPUTextureFormat.D32FloatS8Uint,
    ];
    foreach (var format in depthFormats)
    {
        if (!SDL.GPUTextureSupportsFormat(device.Handle, format, SDL.GPUTextureType.TextureType2D, SDL.GPUTextureUsageFlags.DepthStencilTarget))
            continue;

        depthStencilFormat = format;
        break;
    }
    if (depthStencilFormat == default)
        throw new NotSupportedException("Stencil formats not supported!");
}

var pipelineInfo = new SDL.GPUGraphicsPipelineCreateInfo()
{
    VertexShader = vertexShader,
    FragmentShader = fragmentShader,
    PrimitiveType = SDL.GPUPrimitiveType.TriangleList,
    TargetInfo = new SDL.GPUGraphicsPipelineTargetInfo()
    {
        HasDepthStencilTarget = true,
        DepthStencilFormat = depthStencilFormat,

        NumColorTargets = 1,
        ColorTargetDescriptions = SpanToPointer([
            new SDL.GPUColorTargetDescription()
            {
                Format = SDL.GetGPUSwapchainTextureFormat(device.Handle, window),
            },
        ]),
    },
    DepthStencilState = new SDL.GPUDepthStencilState
    {
        EnableDepthTest = true,
        EnableDepthWrite = true,
        CompareOp = SDL.GPUCompareOp.Less,
        WriteMask = 0xFF,
    },
    RasterizerState = new()
    {
        CullMode = SDL.GPUCullMode.Back,
    },

    VertexInputState = new()
    {
        NumVertexBuffers = 1,
        VertexBufferDescriptions = SpanToPointer([
            new SDL.GPUVertexBufferDescription()
            {
                Slot = 0,
                InputRate = SDL.GPUVertexInputRate.Vertex,
                Pitch = (uint) Unsafe.SizeOf<Vertex>(),
            },
        ]),

        NumVertexAttributes = 2,
        VertexAttributes = SpanToPointer([
            new SDL.GPUVertexAttribute()
            {
                BufferSlot = 0,
                Location = 0,
                Format = SDL.GPUVertexElementFormat.Float3,
                Offset = 0,
            },
            new SDL.GPUVertexAttribute()
            {
                BufferSlot = 0,
                Location = 1,
                Format = SDL.GPUVertexElementFormat.Float4,
                Offset = sizeof(float) * 3,
            },
        ]),
    },
};
var graphicsPipeline = SDL.CreateGPUGraphicsPipeline(device.Handle, pipelineInfo);
SDL.ReleaseGPUShader(device.Handle, vertexShader);
SDL.ReleaseGPUShader(device.Handle, fragmentShader);


uint w, h;
{
    SDL.GetWindowSizeInPixels(window, out var ww, out var hh);
    w = (uint) ww;
    h = (uint) hh;
}
var depthStencilTexture = SDL.CreateGPUTexture(
    device.Handle,
    new SDL.GPUTextureCreateInfo()
    {
        Type = SDL.GPUTextureType.TextureType2D,
        Width = w,
        Height = h,
        LayerCountOrDepth = 1,
        NumLevels = 1,
        SampleCount = SDL.GPUSampleCount.SampleCount1,
        Format = depthStencilFormat,
        Usage = SDL.GPUTextureUsageFlags.DepthStencilTarget,
    }
);

using var cubeVertexBuffer = GpuBuffer.Create<Vertex>(device, SDL.GPUBufferUsageFlags.Vertex, [
    new(0, 0, 0, 0, 1, 1, 1), // 0
    new(1, 0, 0, 1, 0, 1, 1), // 1
    new(0, 1, 0, 1, 1, 0, 1), // 2
    new(1, 1, 0, 1, 1, 1, 1), // 3
    new(0, 0, 1, 1, 1, 1, 1), // 4
    new(1, 0, 1, 1, 1, 1, 1), // 5
    new(0, 1, 1, 1, 1, 1, 1), // 6
    new(1, 1, 1, 1, 1, 1, 1), // 7

    new(0, 0, 2, 1, 0, 0, 1),
    new(1, 0, 2, 1, 0, 1, 1),
    new(1, 1, 2, 1, 0, 0, 1),
]);
using var cubeIndexBuffer = GpuBuffer.Create<Int16>(device, SDL.GPUBufferUsageFlags.Index, [
    0, 2, 3, 0, 3, 1, // back
    5, 7, 6, 5, 6, 4, // front
    1, 3, 7, 1, 7, 5, // right
    4, 6, 2, 4, 2, 0, // left
    2, 6, 7, 2, 7, 3, // top
    1, 5, 4, 1, 4, 0, // bottom

    9, 8, 10,
]);


var frame = 0L;
var loop = true;
while (loop)
{
    frame++;

    while (SDL.PollEvent(out var e))
    {
        if ((SDL.EventType) e.Type == SDL.EventType.Quit)
        {
            loop = false;
        }
    }


    var commandBuffer = SDL.AcquireGPUCommandBuffer(device.Handle);

    // var ft = frame / 100f;
    // ft += MathF.Sin(frame / 50f);
    // vtb.WriteAndCopy(commandBuffer, [
    //     new(MathF.Sin(ft + MathF.PI * 2 / 4 * 0), MathF.Cos(ft + MathF.PI * 2 / 4 * 0), 0, 1, 0, 0, 1),
    //     new(MathF.Sin(ft + MathF.PI * 2 / 4 * 1), MathF.Cos(ft + MathF.PI * 2 / 4 * 1), 0, 1, 1, 0, 1),
    //     new(MathF.Sin(ft + MathF.PI * 2 / 4 * 2), MathF.Cos(ft + MathF.PI * 2 / 4 * 2), 0, 1, 0, 1, 1),

    //     // new(MathF.Sin(ft + MathF.PI * 2 / 4 * 0), MathF.Cos(ft + MathF.PI * 2 / 4 * 0), 0, 1, 0, 0, 1),
    //     // new(MathF.Sin(ft + MathF.PI * 2 / 4 * 2), MathF.Cos(ft + MathF.PI * 2 / 4 * 2), 0, 1, 0, 1, 1),
    //     new(MathF.Sin(ft + MathF.PI * 2 / 4 * 3), MathF.Cos(ft + MathF.PI * 2 / 4 * 3), 0, 1, 0, 1, 1),
    // ]);
    // itb.WriteAndCopy(commandBuffer, [0, 1, 2, 0, 2, 3]);

    SDL.WaitAndAcquireGPUSwapchainTexture(commandBuffer, window, out var swapchainTexture, out w, out h);
    if (swapchainTexture == nint.Zero)
    {
        SDL.SubmitGPUCommandBuffer(commandBuffer);
        continue;
    }

    var colorTarget = new SDL.GPUColorTargetInfo()
    {
        Texture = swapchainTexture,
        ClearColor = new SDL.FColor(0 / 255f, 0 / 255f, 0 / 255f, 255 / 255f),
        LoadOp = SDL.GPULoadOp.Clear,
        StoreOp = SDL.GPUStoreOp.Store,
    };
    var stencil = new SDL.GPUDepthStencilTargetInfo()
    {
        Texture = depthStencilTexture,
        Cycle = 0,
        ClearDepth = 1,
        ClearStencil = 0,
        LoadOp = SDL.GPULoadOp.Clear,
        StoreOp = SDL.GPUStoreOp.DontCare,
        StencilLoadOp = SDL.GPULoadOp.Clear,
        StencilStoreOp = SDL.GPUStoreOp.DontCare,
    };
    var renderPass = SDL.BeginGPURenderPass(commandBuffer, StructureToPointer(colorTarget), 1, StructureToPointer(stencil));

    SDL.BindGPUGraphicsPipeline(renderPass, graphicsPipeline);

    var matrix = Matrix4x4.CreateLookAt(new(MathF.Sin(frame / 130f) * 3 + .5f, MathF.Sin(frame / 100f) * 1, MathF.Cos(frame / 130f) * 3 + .5f), new(0, 0, 0), Vector3.UnitY)
        * Matrix4x4.CreatePerspectiveFieldOfView(90 * (MathF.PI / 180), 2f / 1, .01f, 100f);

    SDL.SetGPUStencilReference(renderPass, 0);

    SDL.BindGPUVertexBuffers(renderPass, 0, StructureToPointer(new SDL.GPUBufferBinding() { Buffer = cubeVertexBuffer.Handle }), 1);
    SDL.BindGPUIndexBuffer(renderPass, new SDL.GPUBufferBinding() { Buffer = cubeIndexBuffer.Handle }, SDL.GPUIndexElementSize.IndexElementSize16Bit);
    SDL.PushGPUVertexUniformData(commandBuffer, 0, StructureToPointer(in matrix), sizeof(float) * 4 * 4);
    SDL.DrawGPUIndexedPrimitives(renderPass, (uint) cubeIndexBuffer.Length, 1, 0, 0, 0);

    SDL.EndGPURenderPass(renderPass);
    SDL.SubmitGPUCommandBuffer(commandBuffer);
}

SDL.DestroyGPUDevice(device.Handle);
SDL.DestroyWindow(window);

SDL.Quit();


static unsafe nint StructureToPointer<T>(in T structure) where T : unmanaged => (nint) Unsafe.AsPointer(in structure);
static unsafe nint SpanToPointer<T>(ReadOnlySpan<T> span) where T : unmanaged => (nint) Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));

readonly struct Vertex
{
    public readonly float X, Y, Z;
    public readonly float R, G, B, A;

    public Vertex(float x, float y, float z, float r, float g, float b, float a)
    {
        X = x;
        Y = y;
        Z = z;
        R = r;
        G = g;
        B = b;
        A = a;
    }
};
