if (!SDL.Init(SDL.InitFlags.Video))
{
    SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize: {SDL.GetError()}");
    return;
}

using var device = new GpuDevice();
using var window = new Window(device);

// who cares about throughput, at least resizing does't kill the fps randomly (vsync 16+4?)
SDL.SetGPUAllowedFramesInFlight(device.Handle, 1);

var main = new TowerDefence.Main();

window.MainViewport.WorldChild = new TowerDefence.ViewModel(main, window.MainViewport);
window.Add(new SubViewport(window.Renderer)
{
    RelativeWidth = .3f,
    RelativeHeight = .3f,
    RelativeX = .1f,
    RelativeY = .1f,
    WorldChild = new TowerDefence.TestScene(window.MainViewport),
    ClearColor = new(.5f, .5f, 0, .5f),
});


var nt = DateTime.Now + TimeSpan.FromSeconds(1);
var f = 0;

var timeUpdate = TimeSpan.Zero;
var timeRender = TimeSpan.Zero;
var timeGpu = TimeSpan.Zero;


while (true)
{
    f++;
    if (DateTime.Now > nt)
    {
        var instances = window.InstancesCountDebug;
        Console.WriteLine($"fps: {f}, update: {timeUpdate.TotalMilliseconds}ms, render: {timeRender.TotalMilliseconds}ms, gpu: {timeGpu.TotalMilliseconds}ms, instances: {instances}");
        f = 0;
        nt = DateTime.Now + TimeSpan.FromSeconds(1);
    }

    while (SDL.PollEvent(out var e))
    {
        var type = (SDL.EventType) e.Type;

        if (type == SDL.EventType.Quit)
            goto end;

        if (NodeArchestrator.PropagateEvent(window, ref e))
            continue;
    }

    var start = Stopwatch.GetTimestamp();
    NodeArchestrator.PropagateUpdate(window);
    next(ref start, out timeUpdate);

    NodeArchestrator.PropagateRender2(window);
    next(ref start, out timeRender);

    window.Render();
    next(ref start, out timeGpu);


    static void next(ref long start, out TimeSpan end)
    {
        end = Stopwatch.GetElapsedTime(start);
        start = Stopwatch.GetTimestamp();
    }
}

end:;
SDL.Quit();
