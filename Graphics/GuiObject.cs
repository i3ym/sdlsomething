namespace SdlSomething;

public interface IHasSize
{
    uint RealX { get; }
    uint RealY { get; }
    uint RealWidth { get; }
    uint RealHeight { get; }
}
public class GuiObject : Node, IHasSize
{
    public uint RealX { get; private set; }
    public uint RealY { get; private set; }
    public uint RealWidth { get; private set; }
    public uint RealHeight { get; private set; }

    public uint X { get; set; }
    public uint Y { get; set; }
    public uint Width { get; set; }
    public uint Height { get; set; }

    public float RelativeX { get; set; }
    public float RelativeY { get; set; }
    public float RelativeWidth { get; set; }
    public float RelativeHeight { get; set; }

    protected override void InvalidateParent() => RecalculateSize();

    void RecalculateSize()
    {
        var parent = this.FindParentOfType<IHasSize>();
        if (parent is null) return;

        RealX = parent.RealX + X + (uint) (parent.RealWidth * RelativeX);
        RealY = parent.RealY + Y + (uint) (parent.RealHeight * RelativeY);
        RealWidth = Width + (uint) (parent.RealWidth * RelativeWidth);
        RealHeight = Height + (uint) (parent.RealHeight * RelativeHeight);
    }

    internal static void PropagateLayoutInvalidation(Node node)
    {
        if (node is GuiObject gui)
            gui.RecalculateSize();

        foreach (var child in node.Children)
            PropagateLayoutInvalidation(child);
    }
}
