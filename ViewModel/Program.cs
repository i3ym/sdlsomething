if (!SDL.Init(SDL.InitFlags.Video))
{
    SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize: {SDL.GetError()}");
    return;
}
using var window = new Window();
using var device = new GpuDevice();
window.ClaimGPU(device);


using var renderer = new Renderer(window, device);

var main = new TowerDefence.Main();
var game = new TowerDefence.TestScene(renderer);
var game2 = new TowerDefence.ViewModel(main, renderer);

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
        var instances = renderer.MainViewport.World.Groups.Sum(c => c.InstancesCount);
        Console.WriteLine($"fps: {f}, update: {timeUpdate.TotalMilliseconds}ms, render: {timeRender.TotalMilliseconds}ms, gpu: {timeGpu.TotalMilliseconds}ms, instances: {instances}");
        f = 0;
        nt = DateTime.Now + TimeSpan.FromSeconds(1);
    }

    while (SDL.PollEvent(out var e))
    {
        var type = (SDL.EventType) e.Type;

        if (type == SDL.EventType.Quit)
            goto end;

        if (renderer.Event(ref e)) continue;
        if (game.Event(ref e)) continue;
    }

    var start = Stopwatch.GetTimestamp();
    game.Update();
    game2.Update();
    timeUpdate = Stopwatch.GetElapsedTime(start);
    start = Stopwatch.GetTimestamp();

    game.Render();
    game2.Render();
    timeRender = Stopwatch.GetElapsedTime(start);

    renderer.Render(ref start);
    timeGpu = Stopwatch.GetElapsedTime(start);
}

end:;
SDL.Quit();
