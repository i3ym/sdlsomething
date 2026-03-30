struct VSInput
{
    float3 position : POSITION;
    float4 color : COLOR;

    float4 transformCol0 : TEXCOORD0;
    float4 transformCol1 : TEXCOORD1;
    float4 transformCol2 : TEXCOORD2;
    float4 transformCol3 : TEXCOORD3;
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
[[vk::binding(0, 1)]] ConstantBuffer<UniformData> buffer;

[shader("vertex")]
VSOutput main(VSInput input)
{
    VSOutput output;
    output.position = mul(buffer.viewProj, float4(input.position, 1.0f));
    float4x4 instanceMatrix = float4x4(
        input.transformCol0,
        input.transformCol1,
        input.transformCol2,
        input.transformCol3
    );
    output.position = mul(buffer.viewProj, mul(float4(input.position, 1.0f), instanceMatrix));

    output.color = input.color;
    return output;
}
