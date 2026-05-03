namespace SdlSomething;

public interface IEventReceiver
{
    /// <summary> Process a window event </summary>
    /// <returns> True to stop event propagation, false to continue. </returns>
    bool Event(ref SDL.Event evt);
}
public interface IFrameUpdate
{
    void Update();
}
public interface IFrameRender
{
    void UpdateRender();
}

public class Node
{
    public Node? Parent { get; private set; }

    public IReadOnlyList<Node> Children => _Children;
    readonly List<Node> _Children = [];

    public T Add<T>(T child)
        where T : Node
    {
        if (child.Parent is not null)
            throw new InvalidOperationException("Node already has a parent");

        PropagateParentPreInvalidation(child);
        _Children.Add(child);
        child.Parent = this;
        PropagateParentInvalidation(child);

        return child;
    }
    public void Remove(Node child)
    {
        PropagateParentPreInvalidation(child);
        _Children.Remove(child);
        child.Parent = null;
        PropagateParentInvalidation(child);
    }

    static void PropagateParentPreInvalidation(Node node)
    {
        node.PreInvalidateParent();

        foreach (var child in node.Children)
            PropagateParentPreInvalidation(child);
    }
    static void PropagateParentInvalidation(Node node)
    {
        node.InvalidateParent();

        foreach (var child in node.Children)
            PropagateParentInvalidation(child);
    }
    protected virtual void PreInvalidateParent() { }
    protected virtual void InvalidateParent() { }
}
public static class NodeArchestrator
{
    public static void PropagateUpdate(Node node)
    {
        if (node is IFrameUpdate updatable)
            updatable.Update();

        foreach (var child in node.Children)
            PropagateUpdate(child);
    }

    public static void PropagateRender2(Node node)
    {
        if (node is IFrameRender renderable)
            renderable.UpdateRender();

        foreach (var child in node.Children)
            PropagateRender2(child);
    }

    public static bool PropagateEvent(Node node, ref SDL.Event e)
    {
        if (node is IEventReceiver receiver)
            if (receiver.Event(ref e))
                return true;

        foreach (var child in node.Children)
            if (PropagateEvent(child, ref e))
                return true;

        return false;
    }
}

public static class NodeExtensions
{
    public static T WithChild<T>(this T node, Node child)
        where T : Node
    {
        node.Add(child);
        return node;
    }
    public static T? FindParentOfType<T>(this Node node)
        where T : class
    {
        var parent = node.Parent;
        while (parent is not null)
        {
            if (parent is T t)
                return t;

            parent = parent?.Parent;
        }

        return null;
    }
}
