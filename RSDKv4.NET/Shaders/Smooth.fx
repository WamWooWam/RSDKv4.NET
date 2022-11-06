#include "Macros.fxh"


DECLARE_TEXTURE(Texture, 0);


BEGIN_CONSTANTS
MATRIX_CONSTANTS

float4x4 MatrixTransform;

END_CONSTANTS

BEGIN_CONSTANTS

float2 pixelSize;		// internal game resolution (usually 424x240 or smth)
float2 textureSize;		// size of the internal framebuffer texture
float2 viewSize;		// window viewport size

END_CONSTANTS


float4 SpritePixelShader(float4 position : SV_Position, 
                         float4 color : COLOR0, 
                         float2 texCoord : TEXCOORD0) : SV_Target0
{
    // Adapted from https://github.com/rsn8887/Sharp-Bilinear-Shaders/releases, used in RetroArch 
	
    float2 texel = texCoord.xy * float4(textureSize, 1.0 / textureSize).xy;

    float2 texelFloored = floor(texel);
    float2 s            = frac(texel);
    float2 regionRange  = 0.5 - 0.5 / 2.0;

    float2 centerDist   = s - 0.5;
    float2 f            = (centerDist - clamp(centerDist, -regionRange, regionRange)) * 2.0 + 0.5;

    float2 modTexel = texelFloored + f;
	
    float4 outColor = SAMPLE_TEXTURE(Texture, modTexel / textureSize.xy);
	return outColor;
}


technique SpriteBatch
{
    pass
    {
        PixelShader = compile ps_2_0 SpritePixelShader();
    }
}
