namespace SdlSomething.TowerDefence;

public sealed class Game
{
    readonly Renderer Renderer;

    public Game(Renderer renderer)
    {
        Renderer = renderer;


        {
            var floorMesh = createSubdividedPlane(100, 4);
            var material = new StandardMaterial(renderer.Device, renderer.Window);
            var floors = new RenderGroup<VertexPN, StandardMaterial.InstanceData>(floorMesh, material);
            floors.SetRange([new StandardMaterial.InstanceData(Matrix4x4.Identity, Vector4.One)]);
            renderer.MainViewport.World.Groups.Add(floors);
        }


        Mesh<VertexPN> createSubdividedPlane(float size, int subdivisions)
        {
            var planeCount = (int) Math.Pow(4, subdivisions);
            var vertices = new VertexPN[planeCount * 4];
            var indices = new Int16[planeCount * 6];

            var iPerCoord = MathF.Sqrt(planeCount);
            if (iPerCoord == 0) iPerCoord = 1;

            var planeSize = size / planeCount;
            for (var i = 0; i < planeCount; i++)
            {
                var x = ((int) (i % iPerCoord)) * planeSize;
                var z = ((int) (i / iPerCoord)) * planeSize;
                createPlane(vertices.AsSpan(i * 4, 4), planeSize, x, z);

                var inds = indices.AsSpan(i * 6, 6);
                inds[0] = (short) (0 + i * 4);
                inds[1] = (short) (1 + i * 4);
                inds[2] = (short) (2 + i * 4);
                inds[3] = (short) (0 + i * 4);
                inds[4] = (short) (2 + i * 4);
                inds[5] = (short) (3 + i * 4);
            }

            return new Mesh<VertexPN>(renderer.Device)
            {
                VerticesArr = vertices,
                IndicesArr = indices,
            };
        }
        static void createPlane(Span<VertexPN> result, float size, float x, float z)
        {
            var s = size;

            result[0] = new(x + 0, Random.Shared.NextSingle() * .1f, z + 0);
            result[1] = new(x + 0, Random.Shared.NextSingle() * .1f, z + s);
            result[2] = new(x + s, Random.Shared.NextSingle() * .1f, z + s);
            result[3] = new(x + s, Random.Shared.NextSingle() * .1f, z + 0);

            var a = Unsafe.As<VertexPN, Vector3>(ref result[0]);
            var b = Unsafe.As<VertexPN, Vector3>(ref result[1]);
            var c = Unsafe.As<VertexPN, Vector3>(ref result[2]);
            var edge1 = b - a;
            var edge2 = c - a;

            var normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));
            result[0] = new(x + 0, result[0].Y, z + 0, normal.X, normal.Y, normal.Z);
        }
    }

    public void Event(ref SDL.Event evt)
    {
        var type = (SDL.EventType) evt.Type;
        if (type == SDL.EventType.MouseMotion)
        {
            Console.WriteLine(evt.Motion.XRel + "/" + evt.Motion.YRel);
            X += evt.Motion.XRel / 10f;
            Z += evt.Motion.YRel / 10f;
        }
    }

    float X, Z;

    public void Update()
    {
        Renderer.MainViewport.CameraMatrix = Matrix4x4.CreateLookAt(new(X, 4, Z), new(X, 1, Z + 4), Vector3.UnitY);
    }
}
