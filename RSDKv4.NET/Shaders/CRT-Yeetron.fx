
#include "Macros.fxh"

#define RSDK_PI     3.14159                 // PI
#define viewSizeHD  720                     // how tall viewSize.y has to be before it simulates the dimming effect
#define intencity   float3(1.1, 0.9, 0.9)   // how much to "dim" the screen when simulating a CRT effect

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

void SpriteVertexShader(
	inout float4 color    : COLOR0,
	inout float2 texCoord : TEXCOORD0,
	inout float4 position : SV_Position)
{
	position = mul(position, MatrixTransform);
}


float4 SpritePixelShader(
	float4 position : SV_Position,
	float4 color : COLOR0,
	float2 texCoord : TEXCOORD0) : SV_Target0
{
	float2 viewPos = floor((textureSize.xy / pixelSize.xy) * texCoord.xy * viewSize.xy) + 0.5;
	float intencityPos = frac((viewPos.y * 3.0 + viewPos.x) * 0.166667);

	float4 scanlineIntencity;
	if (intencityPos < 0.333)
		scanlineIntencity.rgb = intencity.xyz;
	else if (intencityPos < 0.666)
		scanlineIntencity.rgb = intencity.zxy;
	else
		scanlineIntencity.rgb = intencity.yzx;

	float2 pixelPos = texCoord.xy * textureSize.xy;
	float2 roundedPixelPos = floor(pixelPos.xy);

	scanlineIntencity.a = clamp(abs(sin(pixelPos.y * RSDK_PI)) + 0.25, 0.5, 1.0);
	pixelPos.xy = frac(pixelPos.xy) + -0.5;

	float2 invTexPos = -texCoord.xy * textureSize.xy + (roundedPixelPos + 0.5);

	float2 newTexPos;
	newTexPos.x = clamp(-abs(invTexPos.x * 0.5) + 1.5, 0.8, 1.25);
	newTexPos.y = clamp(-abs(invTexPos.y * 2.0) + 1.25, 0.5, 1.0);

	float2 colorMod;
	colorMod.x = newTexPos.x * newTexPos.y;
	colorMod.y = newTexPos.x * ((scanlineIntencity.a + newTexPos.y) * 0.5);

	scanlineIntencity.a *= newTexPos.x;

	float2 texPos = ((pixelPos.xy + -clamp(pixelPos.xy, -0.25, 0.25)) * 2.0 + roundedPixelPos + 0.5) / textureSize.xy;
	float4 texColor = SAMPLE_TEXTURE(Texture, texPos.xy);

	float3 blendedColor;
	blendedColor.r = scanlineIntencity.a * texColor.r;
	blendedColor.gb = colorMod.xy * texColor.gb;

	float4 outColor;
	outColor.rgb = viewSize.y >= viewSizeHD ? (scanlineIntencity.rgb * blendedColor.rgb) : blendedColor.rgb;
	outColor.a = texColor.a;

	return outColor;
}


technique SpriteBatch
{
	pass
	{
		VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
		PixelShader = compile PS_SHADERMODEL SpritePixelShader();
	}
}
