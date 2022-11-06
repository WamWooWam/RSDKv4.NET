#include "Macros.fxh"


DECLARE_TEXTURE(Texture, 0);
DECLARE_TEXTURE(Palette, 1);


BEGIN_CONSTANTS
MATRIX_CONSTANTS

float4x4 MatrixTransform;

END_CONSTANTS


void SpriteVertexShader(inout float4 color    : COLOR0,
                        inout float2 texCoord : TEXCOORD0,
                        inout float4 position : SV_Position)
{
    position = mul(position, MatrixTransform);
}


float4 SpritePixelShader(float4 color : COLOR0,
    float2 texCoord : TEXCOORD0) : SV_Target0
{
    float paletteIndex = SAMPLE_TEXTURE(Texture, texCoord).a * 255.0;
    return SAMPLE_TEXTURE(Palette, float2((paletteIndex % 16.0) / 16.0, (paletteIndex / 256.0))) * color;
}


technique SpriteBatch
{
    pass
    {
        VertexShader = compile vs_2_0 SpriteVertexShader();
        PixelShader = compile ps_2_0 SpritePixelShader();
    }
}
