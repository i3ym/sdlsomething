namespace TowerDefence.Towers;

public sealed class TowerNormal1 : TowerProcessor, IFrameRender
{
    readonly Standard3DInstanceDataTC Instances;

    public TowerNormal1(GpuDevice device, Main game) : base(game)
    {
        Instances = new(device);

        var mesh = StandardMeshBuilder.NewNCI()
            .Cube()
            .BuildNCI(device);
        Add(new Standard3DRenderGroup(mesh, Instances));
    }

    public void UpdateRender() => Game.World.Render<TowerPosition>(Instances);
}
