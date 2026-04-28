namespace TowerDefence;

public sealed class TopDownCameraController : Node, IFrameUpdate, IEventReceiver
{
    bool Moving;
    float X, Y = 1, Z = -8;
    Viewport? Viewport;

    protected override void InvalidateParent()
    {
        Viewport = this.FindParentOfType<Viewport>();
        base.InvalidateParent();
    }

    public void Update()
    {
        Viewport?.CameraMatrix = Matrix4x4.CreateLookAt(new(X, Y + 3, Z), new(X, Y, Z + 4), Vector3.UnitY);
    }

    public bool Event(ref SDL.Event evt)
    {
        const byte leftMouseButton = (byte) SDL.MouseButtonFlags.Left;
        var type = (SDL.EventType) evt.Type;

        if (type == SDL.EventType.MouseButtonDown && evt.Button.Button == leftMouseButton)
            Moving = true;
        else if (type == SDL.EventType.MouseButtonUp && evt.Button.Button == leftMouseButton)
            Moving = false;
        else if (type == SDL.EventType.MouseWheel)
            Y += evt.Wheel.IntegerY;
        else if (type == SDL.EventType.MouseMotion && Moving)
        {
            X += evt.Motion.XRel / 50f;
            Z += evt.Motion.YRel / 50f;
        }
        /*else*/
        return false;

        return true;
    }
}
