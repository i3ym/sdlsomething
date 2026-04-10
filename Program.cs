global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
global using System.Numerics;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using SDL3;
global using SdlSomething.Graphics;
global using static SdlSomething.Graphics.SdlUtils;
using SdlSomething.TowerDefence;


BlockAllocator.Test();
return;

if (!SDL.Init(SDL.InitFlags.Video))
{
    SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize: {SDL.GetError()}");
    return;
}
using var window = new Window();
using var device = new GpuDevice();
window.ClaimGPU(device);

var depthStencilFormat = GetStencilFormat(device);


var renderer = new Renderer(window, device);

var main = new SdlSomething.TowerDefence.Main();
var game = new SdlSomething.TowerDefence.ViewModel(main, renderer);

var nt = DateTime.Now + TimeSpan.FromSeconds(1);
var f = 0;

var timeUpdate = TimeSpan.Zero;
var timeRender = TimeSpan.Zero;
var timeGpu = TimeSpan.Zero;

var loop = true;
while (loop)
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
        {
            loop = false;
            break;
        }

        if (type == SDL.EventType.WindowResized)
            renderer.Resize((uint) e.Window.Data1, (uint) e.Window.Data2);

        game.Event(ref e);
    }

    var start = Stopwatch.GetTimestamp();
    game.Update();
    timeUpdate = Stopwatch.GetElapsedTime(start);
    start = Stopwatch.GetTimestamp();

    game.Render();
    timeRender = Stopwatch.GetElapsedTime(start);

    renderer.Render(ref start);
    timeGpu = Stopwatch.GetElapsedTime(start);
}

SDL.Quit();
