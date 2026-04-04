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
public sealed class Mesh<TVertex, TData> : IDisposable
    where TVertex : unmanaged
    where TData : unmanaged
{
    public GpuDevice Device => GpuVertices.Device;
    readonly ResizableGpuBuffer<TVertex> GpuVertices;
    readonly ResizableGpuBuffer<TData> GpuData;
    readonly ResizableGpuBuffer<Int16> GpuIndices;

    public int IndicesCount => ReadonlyIndices.Length;

    public TVertex[] VerticesArr { set => WritableVertices = value; }
    public TData[] DataArr { set => WritableData = value; }
    public Int16[] IndicesArr { set => WritableIndices = value; }
    public ReadOnlyMemory<TVertex> ReadonlyVertices => GpuVertices.ReadonlyData;
    public ReadOnlyMemory<TData> ReadonlyData => GpuData.ReadonlyData;
    public ReadOnlyMemory<Int16> ReadonlyIndices => GpuIndices.ReadonlyData;
    public ref Memory<TVertex> WritableVertices => ref GpuVertices.WritableData;
    public ref Memory<TData> WritableData => ref GpuData.WritableData;
    public ref Memory<Int16> WritableIndices => ref GpuIndices.WritableData;

    public Mesh(GpuDevice device)
    {
        GpuVertices = new(device, SDL.GPUBufferUsageFlags.Vertex);
        GpuData = new(device, SDL.GPUBufferUsageFlags.Vertex | SDL.GPUBufferUsageFlags.GraphicsStorageRead);
        GpuIndices = new(device, SDL.GPUBufferUsageFlags.Index);
    }

    public void PrepareFrame(nint commandBuffer)
    {
        GpuVertices.PrepareFrame(commandBuffer);
        GpuData.PrepareFrame(commandBuffer);
        GpuIndices.PrepareFrame(commandBuffer);
    }
    public void GetBindings(out SDL.GPUBufferBinding vertices, out SDL.GPUBufferBinding data, out SDL.GPUBufferBinding indices)
    {
        GpuVertices.GetBinding(out vertices);
        GpuData.GetBinding(out data);
        GpuIndices.GetBinding(out indices);
    }

    public void Dispose()
    {
        GpuVertices.Dispose();
        GpuData.Dispose();
        GpuIndices.Dispose();
    }
}
