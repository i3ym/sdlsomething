namespace SdlSomething;

public interface IRenderGroup : IDisposable
{
    int InstancesCount { get; }

    void PrepareFrame(nint commandBuffer);

    void RenderFrame(nint renderPass);
    void RenderShadow(nint renderPass);
}
public sealed class RenderWorld
{
    public Vector3 SunDirection { get; set; } = Vector3.Normalize(new Vector3(-.5f, -1, -.67f));
    public List<IRenderGroup> Groups { get; } = [];

    int F = 0;
    public void PrepareFrame(nint commandBuffer)
    {
        F++;
        SunDirection = Vector3.Normalize(new Vector3(MathF.Sin(F / 200f), -1, MathF.Cos(F / 200f)));

        foreach (var group in Groups)
            group.PrepareFrame(commandBuffer);
    }
    public void Render(nint commandBuffer, nint renderPass)
    {
        SDL.PushGPUFragmentUniformData(commandBuffer, 0, StructureToPointer(SunDirection), sizeof(float) * 3);

        foreach (var group in Groups)
            group.RenderFrame(renderPass);
    }
    public void RenderShadow(nint commandBuffer, nint renderPass)
    {
        SDL.PushGPUFragmentUniformData(commandBuffer, 0, StructureToPointer(SunDirection), sizeof(float) * 3);

        foreach (var group in Groups)
            group.RenderShadow(renderPass);
    }
}
