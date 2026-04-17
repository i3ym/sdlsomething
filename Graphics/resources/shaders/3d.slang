struct VSInput
{
    [vk::location(0)] float3 position;
#ifdef HAS_NORMAL
    [vk::location(1)] float3 normal;
#endif
#ifdef HAS_COLOR
    [vk::location(2)] float4 color;
#endif

#ifdef HAS_INSTANCE_TRANSFORM
    [vk::location(3)] float4 transformCol0;
    [vk::location(4)] float4 transformCol1;
    [vk::location(5)] float4 transformCol2;
    [vk::location(6)] float4 transformCol3;
#endif
#ifdef HAS_INSTANCE_COLOR
    [vk::location(7)] float4 instanceColor;
#endif
};

struct VSOutput
{
    float4 position : SV_Position;
#ifdef HAS_NORMAL
    [vk::location(1)] nointerpolation float3 normal;
#endif
#ifdef HAS_ANY_COLOR
    [vk::location(2)] nointerpolation float4 color;
#endif
};

struct VertUniformData
{
    float4x4 viewProj;
};
[vk::binding(0, 1)]
ConstantBuffer<VertUniformData> vertBuffer;

[shader("vertex")]
VSOutput main(VSInput input)
{
    VSOutput output;
    #ifdef HAS_INSTANCE_TRANSFORM
        float4x4 modelMatrix = float4x4(input.transformCol0, input.transformCol1, input.transformCol2, input.transformCol3);
        output.position = mul(vertBuffer.viewProj, mul(float4(input.position, 1.0f), modelMatrix));
        #ifdef HAS_NORMAL
            output.normal = mul((float3x3)modelMatrix, -input.normal);
        #endif
    #else
        output.position = mul(vertBuffer.viewProj, float4(input.position, 1.0f));
        #ifdef HAS_NORMAL
            output.normal = -input.normal;
        #endif
    #endif

    #ifdef HAS_ANY_COLOR
        float4 color;
        #ifdef HAS_COLOR
            color = input.color;
        #else
            color = float4(1, 1, 1, 1);
        #endif

        #ifdef HAS_INSTANCE_COLOR
            color *= input.instanceColor;
        #endif

        output.color = color;
    #endif

    return output;
}


struct FragUniformData
{
    float3 sunDirection;
    float _align1;
};
[vk::binding(0, 3)]
ConstantBuffer<FragUniformData> fragBuffer;

[shader("fragment")]
float4 main(VSOutput input) : SV_Target
{
    #ifdef HAS_ANY_COLOR
        float4 color = input.color;
    #else
        float4 color = float4(1, 1, 1, 1);
    #endif

    #ifdef HAS_NORMAL
        float light = max(dot(input.normal, fragBuffer.sunDirection), 0.1);
        color *= light;
    #endif

    return color;
}
