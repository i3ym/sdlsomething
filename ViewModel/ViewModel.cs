namespace TowerDefence;

public sealed class ViewModel
{
    readonly Standard3DInstanceDataTC Towers, Enemies;
    readonly Standard3DMeshC LinesMesh;

    readonly Main Game;
    readonly Renderer Renderer;

    public ViewModel(Main game, Renderer renderer)
    {
        Renderer = renderer;
        Game = game;

        renderer.MainViewport.World.Groups.Add(new Standard3DRenderGroup(PrimitiveMeshes.Cube(renderer.Device), Towers = new(renderer.Device), renderer.Window));
        renderer.MainViewport.World.Groups.Add(new Standard3DRenderGroup(PrimitiveMeshes.Sphere(renderer.Device), Enemies = new(renderer.Device), renderer.Window));
        renderer.MainViewport.World.Groups.Add(new Standard3DRenderGroup(LinesMesh = new(renderer.Device), null, renderer.Window, new() { PrimitiveType = SDL.GPUPrimitiveType.LineList }));
    }

    public void Event(ref SDL.Event evt)
    {

    }

    long LastFrameTime;
    public void Update()
    {
        var now = Stopwatch.GetTimestamp();
        if (LastFrameTime == 0) ProcessGameTick(0);
        else ProcessGameTick((now - LastFrameTime) / (float) Stopwatch.Frequency);
        LastFrameTime = now;

        Renderer.MainViewport.CameraMatrix = Matrix4x4.CreateLookAt(new(MathF.Sin(Tick / 200f) * 20, 10, MathF.Cos(Tick / 200f) * 20), new(0, 1, 0), Vector3.UnitY);
        // Renderer.MainViewport.CameraMatrix = Matrix4x4.CreateLookAt(new(MathF.Sin(500 / 200f) * 5, 3, MathF.Cos(500 / 200f) * 5), new(0, 1, 0), Vector3.UnitY);
    }

    float TargetFixedDt = 1 / 60f;
    float TimeDilation = 1;
    float FixedAccumulator = 0;
    int Tick = 0;
    void ProcessGameTick(float dt)
    {
        // systemExecutor.World.Set(new FixedGameTime());
        // systemExecutor.World.Set(new FixedInterpolationAlpha(0));

        var dtime = dt * TimeDilation;
        FixedAccumulator += dtime;

        var fixedDt = TargetFixedDt;
        while (FixedAccumulator >= fixedDt)
        {
            Tick++;
            // systemExecutor.World.Set(new FixedGameTime(fixedDt, ++Tick));
            // systemExecutor.RunFixed();
            Game.FixedTick(Tick);

            FixedAccumulator -= fixedDt;
        }

        // var alpha = fixedDt <= 0 ? 0 : Math.Clamp(FixedAccumulator / fixedDt, 0, 1);
        // systemExecutor.World.Set(new FixedInterpolationAlpha(alpha));
    }

    public void Render()
    {
        RenderFrom<TowerPosition>(Game.World, Towers);
        RenderFrom<EnemyPosition>(Game.World, Enemies);

        RenderField(Game.World.Singleton<Field>().Paths, LinesMesh);
    }

    static void RenderField(List<ImmutableArray<Vec2Fixed>> field, Standard3DMeshC mesh)
    {
        var count = field.Sum(c => c.Length) * 2;

        var verts = mesh.Vertices.GetWritableSpan(count);
        var colors = mesh.Colors.GetWritableSpan(count);

        var i = 0;
        var color = new Vector4(1, 0, 1, 1);
        foreach (var path in field)
        {
            for (var j = 0; j < path.Length - 1; j++)
            {
                verts[i] = new Vector3(path[j].X.ToFloat(), 0, path[j].Y.ToFloat());
                verts[i + 1] = new Vector3(path[j + 1].X.ToFloat(), 0, path[j + 1].Y.ToFloat());

                colors[i] = color;
                colors[i + 1] = color;

                i += 2;
            }
        }
    }

    static void RenderFrom<T>(Ekaes world, Standard3DInstanceDataTC storage)
        where T : unmanaged, IPosition =>
        RenderFrom(world.Component<T>(), storage);

    static void RenderFrom<T>(EkaesSet<T> set, Standard3DInstanceDataTC storage)
        where T : unmanaged, IPosition
    {
        var transforms = storage.Transform.GetWritableSpan(set.Count);
        var colors = storage.Color.GetWritableSpan(set.Count);

        var i = 0;
        foreach (ref readonly var pos in set)
        {
            transforms[i] = Matrix4x4.CreateTranslation(pos.Value.Position.X.ToFloat(), 0, pos.Value.Position.Y.ToFloat());
            colors[i] = new(1, 1, 1, 1);

            i++;
        }
    }
}
