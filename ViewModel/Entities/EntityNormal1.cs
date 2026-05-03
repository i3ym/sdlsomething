namespace TowerDefence.Entities;

public sealed class EntityNormal1 : EntityProcessor, IFrameRender
{
    readonly Standard3DInstanceDataTC Instances;

    public EntityNormal1(GpuDevice device, Main game) : base(game)
    {
        Instances = new(device);

        var mesh = StandardMeshBuilder.NewNCI()
            .Cube(new(.5f, 1, .5f))
            .BuildNCI(device);
        Add(new Standard3DRenderGroup(mesh, Instances));
    }

    public void UpdateRender() => Game.World.Render<EnemyPosition>(Instances);
}
