struct VSOutput
{
    float4 color : COLOR;
    float3 normal : TEXCOORD0;
};

struct UniformData
{
    float3 sunDirection;
    float _;
};
[[vk::binding(0, 3)]]
ConstantBuffer<UniformData> buffer;

[shader("fragment")]
float4 main(VSOutput input) : SV_Target
{
    float light = max(dot(input.normal, buffer.sunDirection), 0.1);
    return input.color * light;
}
