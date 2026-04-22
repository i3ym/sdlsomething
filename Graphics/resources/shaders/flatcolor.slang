struct VSOutput
{
    float4 position : SV_Position;
};

[shader("vertex")]
VSOutput main(uint vid : SV_VertexID)
{
    VSOutput output;
    output.position = float4(float2((vid << 1) & 2, vid & 2) * float2(2.0, -2.0) + float2(-1.0, 1.0), 0.0, 1.0);

    return output;
}


struct FragUniformData
{
    float4 color;
};
[vk::binding(0, 3)]
ConstantBuffer<FragUniformData> fragBuffer;

[shader("fragment")]
float4 main(VSOutput input) : SV_Target
{
    return fragBuffer.color;
}
