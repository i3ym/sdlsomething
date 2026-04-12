namespace SdlSomething.Graphics;

public interface IMesh : IDisposable
{
    GpuDevice Device { get; }

    /// <summary> Mesh indices count, or 0 if non-indexed </summary>
    int IndicesCount { get; }
    int VerticesCount { get; }

    void PrepareFrame(nint commandBuffer);
    void RenderFrame(nint renderPass, out SDL.GPUBufferBinding vertices);
}
public interface IMesh<TVertex> : IMesh
    where TVertex : unmanaged;

public sealed class NoIndexMesh<TVertex> : IMesh<TVertex>
    where TVertex : unmanaged
{
    public GpuDevice Device => GpuVertices.Device;
    readonly ResizableGpuBuffer<TVertex> GpuVertices;

    int IMesh.IndicesCount => 0;
    public int VerticesCount => ReadonlyVertices.Length;

    public TVertex[] VerticesArr { set => WritableVertices = value; }
    public ReadOnlyMemory<TVertex> ReadonlyVertices => GpuVertices.ReadonlyData;
    public ref Memory<TVertex> WritableVertices => ref GpuVertices.WritableData;

    public NoIndexMesh(GpuDevice device) => GpuVertices = new(device, SDL.GPUBufferUsageFlags.Vertex);

    public void PrepareFrame(nint commandBuffer) => GpuVertices.PrepareFrame(commandBuffer);
    public void RenderFrame(nint renderPass, out SDL.GPUBufferBinding vertices) => GpuVertices.GetBinding(out vertices);

    public void Dispose() => GpuVertices.Dispose();
}
public sealed class Mesh<TVertex> : IMesh<TVertex>
    where TVertex : unmanaged
{
    public GpuDevice Device => GpuVertices.Device;
    readonly ResizableGpuBuffer<TVertex> GpuVertices;
    readonly ResizableGpuBuffer<Int16> GpuIndices;

    public int IndicesCount => ReadonlyIndices.Length;
    public int VerticesCount => ReadonlyVertices.Length;

    public TVertex[] VerticesArr { set => WritableVertices = value; }
    public Int16[] IndicesArr { set => WritableIndices = value; }
    public ReadOnlyMemory<TVertex> ReadonlyVertices => GpuVertices.ReadonlyData;
    public ReadOnlyMemory<Int16> ReadonlyIndices => GpuIndices.ReadonlyData;
    public ref Memory<TVertex> WritableVertices => ref GpuVertices.WritableData;
    public ref Memory<Int16> WritableIndices => ref GpuIndices.WritableData;

    public Mesh(GpuDevice device)
    {
        GpuVertices = new(device, SDL.GPUBufferUsageFlags.Vertex);
        GpuIndices = new(device, SDL.GPUBufferUsageFlags.Index);
    }

    public void PrepareFrame(nint commandBuffer)
    {
        GpuVertices.PrepareFrame(commandBuffer);
        GpuIndices.PrepareFrame(commandBuffer);
    }
    public void RenderFrame(nint renderPass, out SDL.GPUBufferBinding vertices)
    {
        GpuVertices.GetBinding(out vertices);

        GpuIndices.GetBinding(out var indices);
        SDL.BindGPUIndexBuffer(renderPass, indices, SDL.GPUIndexElementSize.IndexElementSize16Bit);
    }

    public void Dispose()
    {
        GpuVertices.Dispose();
        GpuIndices.Dispose();
    }
}

