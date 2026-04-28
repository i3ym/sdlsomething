namespace SdlSomething;

public abstract class RenderGroup : Node
{
    public abstract int InstancesCount { get; }

    protected override void PreInvalidateParent()
    {
        this.FindParentOfType<RenderWorld>()?.RemoveGroup(this);
        base.PreInvalidateParent();
    }
    protected override void InvalidateParent()
    {
        this.FindParentOfType<RenderWorld>()?.AddGroup(this);
        base.InvalidateParent();
    }

    public abstract void PrepareFrame(nint commandBuffer);
    public abstract void RenderFrame(nint renderPass);
    public virtual void Dispose() { }
}
