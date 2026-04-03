global using System.Diagnostics.CodeAnalysis;
global using System.Numerics;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using SDL3;
global using static SdlSomething.SdlUtils;
using SdlSomething;

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

{
    const int count = 1000;
    var _cubeMesh = new Mesh<VertexPN>(device)
    {
        VerticesArr = [
            // bottom
            new(1, 0, 0, 0, -1, 0), // 0
            new(1, 0, 1, 0, -1, 0), // 1
            new(0, 0, 1, 0, -1, 0), // 2
            new(0, 0, 0, 0, -1, 0), // 3

            // top
            new(0, 1, 0, 0, 1, 0), // 4
            new(0, 1, 1, 0, 1, 0), // 5
            new(1, 1, 1, 0, 1, 0), // 6
            new(1, 1, 0, 0, 1, 0), // 7

            // left
            new(0, 0, 1, -1, 0, 0), // 8
            new(0, 1, 1, -1, 0, 0), // 9
            new(0, 1, 0, -1, 0, 0), // 10
            new(0, 0, 0, -1, 0, 0), // 11

            // right
            new(1, 0, 0, 1, 0, 0), // 14
            new(1, 1, 0, 1, 0, 0), // 12
            new(1, 1, 1, 1, 0, 0), // 13
            new(1, 0, 1, 1, 0, 0), // 15

            // front
            new(0, 0, 0, 0, 0, 1), // 16
            new(0, 1, 0, 0, 0, 1), // 17
            new(1, 1, 0, 0, 0, 1), // 18
            new(1, 0, 0, 0, 0, 1), // 19

            // back
            new(1, 0, 1, 0, 0, -1), // 20
            new(1, 1, 1, 0, 0, -1), // 21
            new(0, 1, 1, 0, 0, -1), // 22
            new(0, 0, 1, 0, 0, -1), // 23
        ],
        IndicesArr = [
            0, 1, 2, 0, 2, 3,
            4, 5, 6, 4, 6, 7,
            8, 9, 10, 8, 10, 11,
            12, 13, 14, 12, 14, 15,
            16, 17, 18, 16, 18, 19,
            20, 21, 22, 20, 22, 23,
        ],
    };
    var material = new StandardMaterial(device, window);
    var cubes = new RenderGroup<VertexPN, StandardMaterial.InstanceData>(_cubeMesh, material);
    renderer.MainViewport.World.Groups.Add(cubes);

    var instances = Enumerable.Range(0, count)
        .SelectMany(x =>
            Enumerable.Range(0, count)
                .Select(y => new StandardMaterial.InstanceData(Matrix4x4.CreateTranslation(x + (x * .1f) - count / 2, -2, y + (y * .1f) - count / 2), new Vector4(MathF.Sin(y / 2f) / 2 + .5f, MathF.Cos(x / 3f) / 2 + .5f, 1, 1)))
        );
    cubes.SetRange(instances.ToArray());
}

var game = new Game();

var nt = DateTime.Now + TimeSpan.FromSeconds(1);
var f = 0;

var frame = 0L;
var loop = true;
while (loop)
{
    frame++;

    f++;
    if (DateTime.Now > nt)
    {
        Console.WriteLine(f);
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

    renderer.MainViewport.CameraMatrix = Matrix4x4.CreateLookAt(new(MathF.Sin(frame / 130f) * 3 + .5f, 1, MathF.Cos(frame / 130f) * 3 + .5f), new(0, 0, 0), Vector3.UnitY);

    game.Update();
    renderer.Render();
}

SDL.Quit();


public readonly struct VertexPN
{
    public readonly float X, Y, Z;
    public readonly float NX, NY, NZ;

    public VertexPN(float x, float y, float z, float nx, float ny, float nz)
    {
        X = x;
        Y = y;
        Z = z;
        NX = nx;
        NY = ny;
        NZ = nz;
    }
}
