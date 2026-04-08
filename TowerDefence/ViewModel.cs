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

    int Frame = 0;
    public void Update()
    {
        Game.Update();

        Frame++;
        Renderer.MainViewport.CameraMatrix = Matrix4x4.CreateLookAt(new(MathF.Sin(Frame / 200f) * 5, 3, MathF.Cos(Frame / 200f) * 5), new(0, 1, 0), Vector3.UnitY);
        // Renderer.MainViewport.CameraMatrix = Matrix4x4.CreateLookAt(new(MathF.Sin(500 / 200f) * 5, 3, MathF.Cos(500 / 200f) * 5), new(0, 1, 0), Vector3.UnitY);

        var list = new List<StandardMaterial.InstanceData>();
        foreach (ref readonly var pos in Game.World.Component<TowerPosition>())
            list.Add(new StandardMaterial.InstanceData(Matrix4x4.CreateTranslation(pos.Value.X.ToFloat(), 0, pos.Value.Y.ToFloat()), Vector4.One));

        Cubes.SetRange([.. list]);
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
        while (storage is null || storage.Length < set.Count)
            Array.Resize(ref storage, Math.Max((storage?.Length ?? 0) * 2, 64));

        var i = 0;
        foreach (ref readonly var pos in set)
        {
            var albedo = Vector4.One;
            if (pos.Entity.Fat(set).Has<EnemyHealth>())
            {
                var p = pos.Entity.Fat(set).Get<EnemyHealth>().Health / 4f;
                albedo = new Vector4(1, p, p, 1);
            }

            storage[i++] = new StandardMaterial.InstanceData(Matrix4x4.CreateTranslation(pos.Value.X.ToFloat(), 0, pos.Value.Y.ToFloat()), albedo);
        }

        renderGroup.SetRange(storage.AsMemory(0, set.Count));
    }
}
