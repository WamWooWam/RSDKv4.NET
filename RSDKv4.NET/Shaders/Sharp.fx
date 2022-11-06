
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
    float2 viewScale = frac((1.0 / pixelSize) * viewSize) - 0.01;

    // if viewSize is an integer scale of pixelSize (within a small margin of error)
    if (viewScale.x < 0 && viewScale.y < 0) {
        // just get the pixel at this fragment with no filtering
        return SAMPLE_TEXTURE(Texture, texCoord);
    }

    // otherwise, it's not pixel perfect... do a bit of pixel filtering
    // we have to do it manually here since the engine samples this shader using the "point" filter, rather than "linear"

    float2 adjacent;
    adjacent.x = abs(ddx(texCoord.x));
    adjacent.y = abs(ddy(texCoord.y));

    float4 texPos;
    texPos.zw = adjacent.yx * 0.500501 + texCoord.yx;
    texPos.xy = -adjacent.xy * 0.500501 + texCoord.xy;

    float2 texSize  = 1.0 / textureSize.yx;
    float2 texCoord1 = clamp(texSize.xy * round(texCoord.yx / texSize.xy), texPos.yx, texPos.zw);
    
    float4 blendFactor;
    blendFactor.xy = -texPos.xy +  texCoord1.yx;
    blendFactor.zw =  texPos.zw + -texCoord1.xy;

    float strength = adjacent.x * adjacent.y * 0.500501 * 2.002;

    float4 filteredColor = 
        ((blendFactor.x * blendFactor.y) / strength) * SAMPLE_TEXTURE(Texture, texPos.xy) + 
        ((blendFactor.z * blendFactor.w) / strength) * SAMPLE_TEXTURE(Texture, texPos.wz) + 
        ((blendFactor.z * blendFactor.x) / strength) * SAMPLE_TEXTURE(Texture, texPos.xz) +
        ((blendFactor.w * blendFactor.y) / strength) * SAMPLE_TEXTURE(Texture, texPos.wy); 
    
    return filteredColor;
}


technique SpriteBatch
{
    pass
    {
        PixelShader = compile ps_3_0 SpritePixelShader();
    }
}
