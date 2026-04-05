namespace SdlSomething.TowerDefence;

// public sealed class SystemExecutor : IDisposable
// {
//     public Entity FixedPhaseStart { get; }
//     public Entity FixedPhase { get; }
//     public Entity FixedPhaseEnd { get; }
//     readonly Entity FixedStartPipeline;
//     readonly Entity FixedMainPipeline;
//     readonly Entity FixedEndPipeline;

//     public Entity FramePhase { get; }
//     readonly Entity FramePipeline;

//     public Entity RenderPhase { get; }
//     readonly Entity RenderPipeline;

//     public readonly World World;

//     public SystemExecutor(World world)
//     {
//         World = world;
//         world.Set(world);
//         world.Set(this);
//         OnDestroy(world.Dispose);

//         FixedPhaseStart = phase("FixedStart");
//         FixedPhase = phase("Fixed")
//             .DependsOn(FixedPhaseStart);
//         FixedPhaseEnd = phase("FixedEnd")
//             .DependsOn(FixedPhase);
//         FixedStartPipeline = pipeline("FixedStartOnly")
//             .With(FixedPhaseStart)
//             .Build();
//         FixedMainPipeline = pipeline("FixedOnly")
//             .With(FixedPhase)
//             .Build();
//         FixedEndPipeline = pipeline("FixedEndOnly")
//             .With(FixedPhaseEnd)
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

//     public void RunFrame()
//     {
//         RecordPhase("Frame", "begin");
//         World.RunPipeline(FramePipeline);
//         RecordPhase("Frame", "end");
//     }

//     public void RunFixed()
//     {
//         RecordPhase("FixedStart", "begin");
//         World.RunPipeline(FixedStartPipeline);
//         RecordPhase("FixedStart", "end");

//         RecordPhase("Fixed", "begin");
//         World.RunPipeline(FixedMainPipeline);
//         RecordPhase("Fixed", "end");

//         RecordPhase("FixedEnd", "begin");
//         World.RunPipeline(FixedEndPipeline);
//         RecordPhase("FixedEnd", "end");
//     }

//     public void RunRender()
//     {
//         RecordPhase("Render", "begin");
//         World.RunPipeline(RenderPipeline);
//         RecordPhase("Render", "end");
//     }

//     void RecordPhase(string phaseName, string transition)
//     {
//         WorldHistorySystem.Record(World, WorldHistoryEventKind.PipelinePhase, $"{phaseName} {transition}");
//     }

//     public IReadOnlyList<string> GetPhaseSystemNames(string phaseName)
//     {
//         var phase = phaseName switch
//         {
//             "FixedStart" => FixedPhaseStart,
//             "Fixed" => FixedPhase,
//             "FixedEnd" => FixedPhaseEnd,
//             "Frame" => FramePhase,
//             "Render" => RenderPhase,
//             _ => default,
//         };

//         if (phase == default)
//             return [];

//         var systems = new List<string>();
//         World.QueryBuilder()
//             .With(Ecs.System)
//             .With(phase)
//             .CacheKind(Flecs.NET.Bindings.flecs.ecs_query_cache_kind_t.EcsQueryCacheNone)
//             .Build()
//             .Each(entity => systems.Add(entity.ToString()));

//         systems.Sort(StringComparer.Ordinal);
//         return systems;
//     }

//     public static implicit operator World(SystemExecutor systemExecutor) => systemExecutor.World;

//     public void Dispose() => World.Dispose();
// }
