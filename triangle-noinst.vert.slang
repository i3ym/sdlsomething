struct VSInput
{
    float3 position : POSITION;
    float4 color : COLOR;
};

struct VSOutput
{
    float4 position : SV_Position;
    float4 color : COLOR;
};

struct UniformData
{
    float4x4 viewProj;
};
[[vk::binding(0, 1)]]
ConstantBuffer<UniformData> buffer;

[shader("vertex")]
VSOutput main(VSInput input)
{
    VSOutput output;
    output.position = mul(buffer.viewProj, float4(input.position, 1.0f));

    output.color = input.color;
    return output;
}
