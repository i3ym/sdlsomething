namespace SdlSomething.TowerDefence;

public sealed class ViewModel
{
    readonly RenderGroup<VertexPN, StandardMaterial.InstanceData> Cubes;
    StandardMaterial.InstanceData[]? Towers;

    readonly RenderGroup<VertexPN, StandardMaterial.InstanceData> Spheres;
    StandardMaterial.InstanceData[]? Enemies;

    readonly Main Game;
    readonly Renderer Renderer;

    public ViewModel(Main game, Renderer renderer)
    {
        Renderer = renderer;
        Game = game;

        Cubes = new(PrimitiveMeshes.Cube(renderer.Device), new StandardMaterial(renderer.Device, renderer.Window));
        renderer.MainViewport.World.Groups.Add(Cubes);

        Spheres = new(PrimitiveMeshes.Sphere(renderer.Device), new StandardMaterial(renderer.Device, renderer.Window));
        renderer.MainViewport.World.Groups.Add(Spheres);
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

        Renderer.MainViewport.CameraMatrix = Matrix4x4.CreateLookAt(new(MathF.Sin(Tick / 200f) * 40, 30, MathF.Cos(Tick / 200f) * 40), new(0, 1, 0), Vector3.UnitY);
        // Renderer.MainViewport.CameraMatrix = Matrix4x4.CreateLookAt(new(MathF.Sin(500 / 200f) * 5, 3, MathF.Cos(500 / 200f) * 5), new(0, 1, 0), Vector3.UnitY);

        var list = new List<StandardMaterial.InstanceData>();
        foreach (ref readonly var pos in Game.World.Component<TowerPosition>())
            list.Add(new StandardMaterial.InstanceData(Matrix4x4.CreateTranslation(pos.Value.X.ToFloat(), 0, pos.Value.Y.ToFloat()), Vector4.One));

        Cubes.SetRange([.. list]);
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
        RenderFrom<TowerPosition>(Cubes, Game.World, ref Towers);
        RenderFrom<EnemyPosition>(Spheres, Game.World, ref Enemies);
    }


    static void RenderFrom<T>(RenderGroup<VertexPN, StandardMaterial.InstanceData> renderGroup, Ekaes world, ref StandardMaterial.InstanceData[]? storage)
        where T : unmanaged, IPosition =>
        RenderFrom(renderGroup, world.Component<T>(), ref storage);

    static void RenderFrom<T>(RenderGroup<VertexPN, StandardMaterial.InstanceData> renderGroup, EkaesSet<T> set, ref StandardMaterial.InstanceData[]? storage)
        where T : unmanaged, IPosition
    {
        if (storage is null || storage.Length < set.Count)
            Array.Resize(ref storage, BytesExtensions.EnsureArrayLength(64, storage?.Length ?? 0, set.Count));

        var enemyHealths = set.World.Component<EnemyHealth>();

        var i = 0;
        foreach (ref readonly var pos in set)
        {
            var albedo = Vector4.One;
            if (enemyHealths.TryGet(pos.Entity, out var health))
            {
                var p = health.Health / 4f;
                albedo = new Vector4(1, p, p, 1);
            }

            storage[i++] = new StandardMaterial.InstanceData(Matrix4x4.CreateTranslation(pos.Value.X.ToFloat(), 0, pos.Value.Y.ToFloat()), albedo);
        }

        renderGroup.SetRange(storage.AsMemory(0, set.Count));
    }
}
