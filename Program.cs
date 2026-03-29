global using System.Diagnostics.CodeAnalysis;
global using SDL3;
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



var vb = new GpuBuffer<Vertex>(device, 4, SDL.GPUBufferUsageFlags.Vertex);
var vtb = new GpuTransferBuffer<Vertex>(vb);

var ib = new GpuBuffer<Int16>(device, 6, SDL.GPUBufferUsageFlags.Index);
var itb = new GpuTransferBuffer<Int16>(ib);

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
        NumUniformBuffers = 0,
    };

    var shader = SDL.CreateGPUShader(device.Handle, info);
    SDL.Free(codeptr);

    return shader;
}
var vertexShader = loadShader(device, "/home/i3ym/workspace/Projects/_test/sdlsomething/triangle.vert.spv", SDL.GPUShaderStage.Vertex);
var fragmentShader = loadShader(device, "/home/i3ym/workspace/Projects/_test/sdlsomething/triangle.frag.spv", SDL.GPUShaderStage.Fragment);

SDL.GPUGraphicsPipelineCreateInfo pipelineInfo;
{
    pipelineInfo = new SDL.GPUGraphicsPipelineCreateInfo()
    {
        VertexShader = vertexShader,
        FragmentShader = fragmentShader,
        PrimitiveType = SDL.GPUPrimitiveType.TriangleList,
    };

    var vertexBufferDescriptions = new[]
    {
        new SDL.GPUVertexBufferDescription()
        {
            Slot = 0,
            InputRate = SDL.GPUVertexInputRate.Vertex,
            Pitch = (uint) Unsafe.SizeOf<Vertex>(),
        },
    };

    pipelineInfo.VertexInputState.NumVertexBuffers = (uint) vertexBufferDescriptions.Length;
    pipelineInfo.VertexInputState.VertexBufferDescriptions = SDL.StructureArrayToPointer(vertexBufferDescriptions);

    // describe the vertex attribute
    var vertexAttributes = new[]
    {
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
    };

    pipelineInfo.VertexInputState.NumVertexAttributes = (uint) vertexAttributes.Length;
    pipelineInfo.VertexInputState.VertexAttributes = SDL.StructureArrayToPointer(vertexAttributes);

    // describe the color target
    var colorTargetDescriptions = new[]
    {
        new SDL.GPUColorTargetDescription()
        {
            Format = SDL.GetGPUSwapchainTextureFormat(device.Handle, window),
        },
    };

    pipelineInfo.TargetInfo.NumColorTargets = (uint) colorTargetDescriptions.Length;
    pipelineInfo.TargetInfo.ColorTargetDescriptions = SDL.StructureArrayToPointer(colorTargetDescriptions);
}

var graphicsPipeline = SDL.CreateGPUGraphicsPipeline(device.Handle, pipelineInfo);
SDL.ReleaseGPUShader(device.Handle, vertexShader);
SDL.ReleaseGPUShader(device.Handle, fragmentShader);


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

    var ft = frame / 100f;
    ft += MathF.Sin(frame / 50f);
    vtb.WriteAndCopy(commandBuffer, [
        new(MathF.Sin(ft + MathF.PI * 2 / 4 * 0), MathF.Cos(ft + MathF.PI * 2 / 4 * 0), 0, 1, 0, 0, 1),
        new(MathF.Sin(ft + MathF.PI * 2 / 4 * 1), MathF.Cos(ft + MathF.PI * 2 / 4 * 1), 0, 1, 1, 0, 1),
        new(MathF.Sin(ft + MathF.PI * 2 / 4 * 2), MathF.Cos(ft + MathF.PI * 2 / 4 * 2), 0, 1, 0, 1, 1),

        // new(MathF.Sin(ft + MathF.PI * 2 / 4 * 0), MathF.Cos(ft + MathF.PI * 2 / 4 * 0), 0, 1, 0, 0, 1),
        // new(MathF.Sin(ft + MathF.PI * 2 / 4 * 2), MathF.Cos(ft + MathF.PI * 2 / 4 * 2), 0, 1, 0, 1, 1),
        new(MathF.Sin(ft + MathF.PI * 2 / 4 * 3), MathF.Cos(ft + MathF.PI * 2 / 4 * 3), 0, 1, 0, 1, 1),
    ]);
    itb.WriteAndCopy(commandBuffer, [0, 1, 2, 0, 2, 3]);

    SDL.WaitAndAcquireGPUSwapchainTexture(commandBuffer, window, out var swapchainTexture, out var w, out var h);
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
    var renderPass = SDL.BeginGPURenderPass(commandBuffer, StructureToPointer(colorTarget), 1, nint.Zero);

    SDL.BindGPUGraphicsPipeline(renderPass, graphicsPipeline);

    SDL.BindGPUVertexBuffers(renderPass, 0, StructureToPointer(new SDL.GPUBufferBinding() { Buffer = vb.Handle }), 1);
    SDL.BindGPUIndexBuffer(renderPass, new SDL.GPUBufferBinding() { Buffer = ib.Handle }, SDL.GPUIndexElementSize.IndexElementSize16Bit);
    SDL.DrawGPUIndexedPrimitives(renderPass, 6, 1, 0, 0, 0);

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
