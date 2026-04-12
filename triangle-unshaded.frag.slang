struct VSOutput
{
    float4 color : COLOR;
};

[shader("fragment")]
float4 main(VSOutput input) : SV_Target
{
    return input.color;
}
