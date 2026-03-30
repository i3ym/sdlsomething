struct VSInput
{
    float3 position : POSITION;
    float3 normal : NORMAL;
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
    float3 normal : TEXCOORD0;
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
    float4x4 modelMatrix = float4x4(
        input.transformCol0,
        input.transformCol1,
        input.transformCol2,
        input.transformCol3
    );
    output.position = mul(buffer.viewProj, mul(float4(input.position, 1.0f), modelMatrix));

    output.color = input.color;
    output.normal = mul((float3x3)modelMatrix, -input.normal);
    return output;
}
