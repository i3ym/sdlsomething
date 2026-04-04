namespace SdlSomething;

public interface IRenderGroup : IDisposable
{
    void PrepareFrame(nint commandBuffer);
    void BeginFrame(nint renderPass);
    void GetBindings(out SDL.GPUBufferBinding vertices, out SDL.GPUBufferBinding indices, out SDL.GPUBufferBinding instances, out uint indicesCount, out uint instanceCount);
}
public sealed class RenderGroup<TVertex, TInstance> : IRenderGroup
    where TVertex : unmanaged
    where TInstance : unmanaged
{
    readonly Mesh<TVertex> Mesh;
    readonly Material<TVertex> Material;
    readonly ResizableGpuBuffer<TInstance> InstanceData;

    public RenderGroup(Mesh<TVertex> mesh, Material<TVertex> material)
    {
        Mesh = mesh;
        Material = material;
        InstanceData = new(mesh.Device, SDL.GPUBufferUsageFlags.Vertex);
    }

    public ref TInstance DataFor(int index) => ref InstanceData.WritableData.Span[index];
    public Span<TInstance> GetData() => InstanceData.WritableData.Span;
    public void SetRange(TInstance[] instances) => InstanceData.WritableData = instances;
    public void SetRange(Memory<TInstance> instances) => InstanceData.WritableData = instances;

    public void PrepareFrame(nint commandBuffer)
    {
        Mesh.PrepareFrame(commandBuffer);
        InstanceData.PrepareFrame(commandBuffer);
    }
    public void BeginFrame(nint renderPass)
    {
        Material.BeginFrame(renderPass);
    }
    public void GetBindings(out SDL.GPUBufferBinding vertices, out SDL.GPUBufferBinding indices, out SDL.GPUBufferBinding instances, out uint indicesCount, out uint instanceCount)
    {
        Mesh.GetBindings(out vertices, out indices);
        InstanceData.GetBinding(out instances);
        indicesCount = (uint) Mesh.IndicesCount;
        instanceCount = (uint) InstanceData.Length;
    }

    public void Dispose() => InstanceData.Dispose();
}
