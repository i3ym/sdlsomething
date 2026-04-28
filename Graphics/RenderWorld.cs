namespace SdlSomething;

public class RenderWorld : Node
{
    public int InstancesCount => Groups.Sum(c => c.InstancesCount);

    public Vector3 SunDirection { get; set; } = new Vector3(-.5f, -1, -.67f);
    readonly List<RenderGroup> Groups = [];

    internal void AddGroup(RenderGroup group) => Groups.Add(group);
    internal void RemoveGroup(RenderGroup group) => Groups.Remove(group);

    internal void PrepareFrame(nint commandBuffer)
    {
        foreach (var group in Groups)
            group.PrepareFrame(commandBuffer);
    }
    internal void Render(nint commandBuffer, nint renderPass)
    {
        SDL.PushGPUFragmentUniformData(commandBuffer, 0, StructureToPointer(SunDirection), sizeof(float) * 3);

        foreach (var group in Groups)
            group.RenderFrame(renderPass);
    }
}
