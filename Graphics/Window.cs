namespace SdlSomething;

public class Window : Node, IEventReceiver, IHasSize, IDisposable
{
    public int InstancesCountDebug => Renderer.GetInstancesCount();

    uint IHasSize.RealX => 0;
    uint IHasSize.RealY => 0;
    public uint RealWidth { get; internal set; }
    public uint RealHeight { get; internal set; }

    public nint Handle { get; }

    public Renderer Renderer { get; }
    public Viewport MainViewport { get; }

    public Window(GpuDevice device)
    {
        RealWidth = 1920;
        RealHeight = 1080;

        Handle = SDL.CreateWindow("WAAA", (int) RealWidth, (int) RealHeight, SDL.WindowFlags.Resizable);
        if (!SDL.ClaimWindowForGPUDevice(device.Handle, Handle))
            throw new Exception(SDL.GetError());

        Renderer = new Renderer(this, device);
        MainViewport = Add(new MainViewport(Renderer));
    }

    void Resize(uint w, uint h)
    {
        RealWidth = w;
        RealHeight = h;
        GuiObject.PropagateLayoutInvalidation(this);
    }

    public bool Event(ref SDL.Event evt)
    {
        var type = (SDL.EventType) evt.Type;

        if (type == SDL.EventType.WindowResized)
            Resize((uint) evt.Window.Data1, (uint) evt.Window.Data2);

        if (Renderer.Event(ref evt))
            return true;

        return false;
    }

    public void Render() => Renderer.Render();

    public (uint w, uint h) GetSize()
    {
        SDL.GetWindowSizeInPixels(Handle, out var ww, out var hh);
        return ((uint) ww, (uint) hh);
    }

    public void Dispose() => SDL.DestroyWindow(Handle);
}
