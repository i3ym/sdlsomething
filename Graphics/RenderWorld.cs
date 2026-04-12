namespace SdlSomething.Graphics;

public sealed class RenderWorld
{
    public Vector3 SunDirection { get; set; } = new Vector3(-.5f, -1, -.67f);
    public List<IRenderGroup> Groups { get; } = [];

    public void PrepareFrame(nint commandBuffer)
    {
        foreach (var group in Groups)
            group.PrepareFrame(commandBuffer);
    }
    public void Render(nint commandBuffer, nint renderPass)
    {
        SDL.PushGPUFragmentUniformData(commandBuffer, 0, StructureToPointer(SunDirection), sizeof(float) * 3);

        foreach (var group in Groups)
            group.RenderFrame(renderPass);
    }
}
