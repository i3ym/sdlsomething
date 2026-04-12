namespace SdlSomething.Graphics;

public interface IRenderGroup : IDisposable
{
    int InstancesCount { get; }

    void PrepareFrame(nint commandBuffer);
    void RenderFrame(nint renderPass);
}
public sealed class NonInstancedRenderGroup<TVertex> : IRenderGroup
    where TVertex : unmanaged
{
    int IRenderGroup.InstancesCount => 1;
    readonly IMesh<TVertex> Mesh;
    readonly NonInstancedMaterial<TVertex> Material;

    public NonInstancedRenderGroup(IMesh<TVertex> mesh, NonInstancedMaterial<TVertex> material)
    {
        Mesh = mesh;
        Material = material;
    }

    public void PrepareFrame(nint commandBuffer)
    {
        Mesh.PrepareFrame(commandBuffer);
    }
    public void RenderFrame(nint renderPass)
    {
        Material.BeginFrame(renderPass);
        Mesh.RenderFrame(renderPass, out var vertb);

        SDL.BindGPUVertexBuffers(renderPass, 0, SpanToPointer([vertb]), 1);

        var indicesCount = Mesh.IndicesCount;
        if (indicesCount == 0) SDL.DrawGPUPrimitives(renderPass, (uint) Mesh.VerticesCount, 1, 0, 0);
        else SDL.DrawGPUIndexedPrimitives(renderPass, (uint) indicesCount, 1, 0, 0, 0);
    }

    public void Dispose() { }
}

public sealed class InstancedRenderGroup<TVertex, TInstance> : IRenderGroup
    where TVertex : unmanaged
    where TInstance : unmanaged
{
    public int InstancesCount => InstanceData.Length;
    readonly IMesh<TVertex> Mesh;
    readonly InstancedMaterial<TVertex, TInstance> Material;
    readonly ResizableGpuBuffer<TInstance> InstanceData;

    public InstancedRenderGroup(IMesh<TVertex> mesh, InstancedMaterial<TVertex, TInstance> material)
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
    public void RenderFrame(nint renderPass)
    {
        if (InstanceData.Length == 0) return;

        Material.BeginFrame(renderPass);
        Mesh.RenderFrame(renderPass, out var vertb);
        InstanceData.GetBinding(out var instab);

        SDL.BindGPUVertexBuffers(renderPass, 0, SpanToPointer([vertb, instab]), 2);

        var indicesCount = Mesh.IndicesCount;
        if (indicesCount == 0) SDL.DrawGPUPrimitives(renderPass, (uint) Mesh.VerticesCount, (uint) InstanceData.Length, 0, 0);
        else SDL.DrawGPUIndexedPrimitives(renderPass, (uint) indicesCount, (uint) InstanceData.Length, 0, 0, 0);
    }

    public void Dispose() => InstanceData.Dispose();
}
