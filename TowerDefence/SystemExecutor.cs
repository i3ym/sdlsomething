namespace SdlSomething.TowerDefence;

// public sealed class SystemExecutor
// {
//     public Entity FixedPhaseStart { get; }
//     public Entity FixedPhase { get; }
//     public Entity FixedPhaseEnd { get; }
//     readonly Entity FixedPipeline;

//     public Entity FramePhase { get; }
//     readonly Entity FramePipeline;

//     public Entity RenderPhase { get; }
//     readonly Entity RenderPipeline;

//     public World World { get; }

//     public SystemExecutor(World world)
//     {
//         World = world;
//         world.Set(world);

//         FixedPhaseStart = phase("FixedStart");
//         FixedPhase = phase("Fixed")
//             .DependsOn(FixedPhaseStart);
//         FixedPhaseEnd = phase("FixedEnd")
//             .DependsOn(FixedPhase);
//         FixedPipeline = pipeline("Fixed")
//             .With(FixedPhaseStart)
//             .Or().With(FixedPhase)
//             .Or().With(FixedPhaseEnd)
//             .Build();

//         FramePhase = phase("Frame");
//         FramePipeline = pipeline("Frame")
//             .With(FramePhase)
//             .Build();

//         RenderPhase = phase("Render");
//         RenderPipeline = pipeline("Render")
//             .With(RenderPhase)
//             .Build();

//         Entity phase(string name)
//         {
//             return world.Entity(name)
//                 .Add(Ecs.Phase);
//         }
//         PipelineBuilder pipeline(string name)
//         {
//             return world.Pipeline(name)
//                 .With(Ecs.System)
//                 .With(Ecs.Phase).Cascade(Ecs.DependsOn)
//                 .Without(Ecs.Disabled).Up(Ecs.DependsOn)
//                 .Without(Ecs.Disabled).Up(Ecs.ChildOf);
//         }
//     }

//     public void RunFrame() => World.RunPipeline(FramePipeline);
//     public void RunFixed() => World.RunPipeline(FixedPipeline);
//     public void RunRender() => World.RunPipeline(RenderPipeline);

//     public void Dispose() => World.Dispose();

//     public static implicit operator World(SystemExecutor systemExecutor) => systemExecutor.World;
// }
