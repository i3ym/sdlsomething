namespace SdlSomething.TowerDefence;

public sealed class ViewModel
{
    readonly InstancedRenderGroup<VertexPN, StandardMaterial.InstanceData> Cubes;
    StandardMaterial.InstanceData[]? Towers;

    readonly InstancedRenderGroup<VertexPN, StandardMaterial.InstanceData> Spheres;
    StandardMaterial.InstanceData[]? Enemies;

    readonly NoIndexMesh<LineMaterial.Vertex> LinesMesh;
    readonly NonInstancedRenderGroup<LineMaterial.Vertex> Lines;
    LineMaterial.Vertex[]? LineVertices;

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

        LinesMesh = new(renderer.Device);
        Lines = new(LinesMesh, new LineMaterial(renderer.Device, renderer.Window));
        renderer.MainViewport.World.Groups.Add(Lines);
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

        var list = new List<StandardMaterial.InstanceData>();
        foreach (ref readonly var pos in Game.World.Component<TowerPosition>())
            list.Add(new StandardMaterial.InstanceData(Matrix4x4.CreateTranslation(pos.Value.Position.X.ToFloat(), 0, pos.Value.Position.Y.ToFloat()), Vector4.One));

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

        RenderField(LinesMesh, Game.World.Singleton<Field>().Paths, ref LineVertices);
    }

    static void RenderField(NoIndexMesh<LineMaterial.Vertex> mesh, List<ImmutableArray<Vec2Fixed>> field, ref LineMaterial.Vertex[]? storage)
    {
        var count = field.Sum(c => c.Length) * 2;
        if (storage is null || storage.Length < count)
            Array.Resize(ref storage, BytesExtensions.EnsureArrayLength(64, storage?.Length ?? 0, count));

        var i = 0;
        var (r, g, b) = (1, 0, 1);
        foreach (var path in field)
        {
            for (var j = 0; j < path.Length - 1; j++)
            {
                storage[i] = new LineMaterial.Vertex(path[j].X.ToFloat(), 0, path[j].Y.ToFloat(), r, g, b);
                storage[i + 1] = new LineMaterial.Vertex(path[j + 1].X.ToFloat(), 0, path[j + 1].Y.ToFloat(), r, g, b);

                i += 2;
            }
        }

        mesh.WritableVertices = storage.AsMemory(0, i);
    }

    static void RenderFrom<T>(InstancedRenderGroup<VertexPN, StandardMaterial.InstanceData> renderGroup, Ekaes world, ref StandardMaterial.InstanceData[]? storage)
        where T : unmanaged, IPosition =>
        RenderFrom(renderGroup, world.Component<T>(), ref storage);

    static void RenderFrom<T>(InstancedRenderGroup<VertexPN, StandardMaterial.InstanceData> renderGroup, EkaesSet<T> set, ref StandardMaterial.InstanceData[]? storage)
        where T : unmanaged, IPosition
    {
        if (storage is null || storage.Length < set.Count)
            Array.Resize(ref storage, BytesExtensions.EnsureArrayLength(64, storage?.Length ?? 0, set.Count));

        var i = 0;
        foreach (ref readonly var pos in set)
            storage[i++] = new StandardMaterial.InstanceData(Matrix4x4.CreateTranslation(pos.Value.Position.X.ToFloat(), 0, pos.Value.Position.Y.ToFloat()), Vector4.One);

        renderGroup.SetRange(storage.AsMemory(0, set.Count));
    }
}
