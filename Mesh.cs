namespace SdlSomething;

public interface IMesh : IDisposable
{
    int IndicesCount { get; }

    void PrepareFrame(nint commandBuffer);
    void GetBindings(out SDL.GPUBufferBinding vertices, out SDL.GPUBufferBinding indices);
}
public sealed class Mesh<T> : IMesh
    where T : unmanaged
{
    public GpuDevice Device => GpuVertices.Device;
    readonly ResizableGpuBuffer<T> GpuVertices;
    readonly ResizableGpuBuffer<Int16> GpuIndices;

    public int IndicesCount => ReadonlyIndices.Length;

    public T[] VerticesArr { set => WritableVertices = value; }
    public Int16[] IndicesArr { set => WritableIndices = value; }
    public ReadOnlyMemory<T> ReadonlyVertices => GpuVertices.ReadonlyData;
    public ReadOnlyMemory<Int16> ReadonlyIndices => GpuIndices.ReadonlyData;
    public ref Memory<T> WritableVertices => ref GpuVertices.WritableData;
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
    public void GetBindings(out SDL.GPUBufferBinding vertices, out SDL.GPUBufferBinding indices)
    {
        GpuVertices.GetBinding(out vertices);
        GpuIndices.GetBinding(out indices);
    }

    public void Dispose()
    {
        GpuVertices.Dispose();
        GpuIndices.Dispose();
    }
}
