
#include "Macros.fxh"

#define viewSizeHD  720                     // how tall viewSize.y has to be before it simulates the dimming effect
#define intencity   float3(1.2, 0.9, 0.9)   // how much to "dim" the screen when simulating a CRT effect
#define brightness  1.25                    // the brightness multipler of the colors

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
	float2 texelPos = (textureSize.xy / pixelSize.xy) * texCoord.xy;
	float4 size = (pixelSize.xy / textureSize.xy).xyxy * texelPos.xyxy;
	float2 exp = size.zw * textureSize.xy + -floor(size.zw * textureSize.xy) + -0.5;

	float4 factor = pow(2, pow(-exp.x + float4(-1, 1, -2, 2), 2) * -3);
	float  factor2 = pow(2, pow(exp.x, 2) * -3); // used for the same stuff as 'factor', just doesn't fit in a float4 :)

	float3 power;
	power.x = pow(2, pow(exp.y, 2) * -8);
	power.y = pow(2, pow(-exp.y + -1, 2) * -8);
	power.z = pow(2, pow(-exp.y + 1, 2) * -8);

	float2 viewPos = floor(texelPos.xy * viewSize.xy) + 0.5;
	float intencityPos = frac((viewPos.y * 3.0 + viewPos.x) * 0.166667);

	float4 scanlineIntencity;
	if (intencityPos < 0.333)
		scanlineIntencity.rgb = intencity.xyz;
	else if (intencityPos < 0.666)
		scanlineIntencity.rgb = intencity.zxy;
	else
		scanlineIntencity.rgb = intencity.yzx;

	float3 color1 = SAMPLE_TEXTURE(Texture, (floor(size.zw * textureSize.xy + float2(1, -1)) + 0.5) / textureSize.xy).rgb * factor.y * brightness;
	float3 color2 = SAMPLE_TEXTURE(Texture, (floor(size.zw * textureSize.xy + float2(-2,  0)) + 0.5) / textureSize.xy).rgb * factor.z * brightness;
	float3 color3 = SAMPLE_TEXTURE(Texture, (floor(size.zw * textureSize.xy + float2(-1,  0)) + 0.5) / textureSize.xy).rgb * factor.x * brightness;
	float3 color4 = SAMPLE_TEXTURE(Texture, (floor(size.zw * textureSize.xy + float2(1,  0)) + 0.5) / textureSize.xy).rgb * factor.y * brightness;
	float3 color5 = SAMPLE_TEXTURE(Texture, (floor(size.zw * textureSize.xy + 0) + 0.5) / textureSize.xy).rgb * factor2 * brightness;
	float3 color6 = SAMPLE_TEXTURE(Texture, (floor(size.zw * textureSize.xy + float2(-1,  1)) + 0.5) / textureSize.xy).rgb * factor.x * brightness;
	float3 color7 = SAMPLE_TEXTURE(Texture, (floor(size.zw * textureSize.xy + float2(2,  0)) + 0.5) / textureSize.xy).rgb * factor.w * brightness;
	float3 color8 = SAMPLE_TEXTURE(Texture, (floor(size.zw * textureSize.xy + -1) + 0.5) / textureSize.xy).rgb * factor.x * brightness;
	float3 color9 = SAMPLE_TEXTURE(Texture, (floor(size.zw * textureSize.xy + float2(0, -1)) + 0.5) / textureSize.xy).rgb * factor2 * brightness;
	float3 color10 = SAMPLE_TEXTURE(Texture, (floor(size.zw * textureSize.xy + 1) + 0.5) / textureSize.xy).rgb * factor.y * brightness;
	float3 color11 = SAMPLE_TEXTURE(Texture, (floor(size.xy * textureSize.xy + float2(0,  1)) + 0.5) / textureSize.xy).rgb * factor2 * brightness;

	float3 final =
		power.x * (color2 + color3 + color4 + color5 + color7) / (factor.z + factor.x + factor.y + factor2 + factor.w) +
		power.y * (color1 + color8 + color9) / (factor.y + factor.x + factor2) +
		power.z * (color10 + color6 + color11) / (factor.y + factor.x + factor2);

	float4 outColor;
	outColor.rgb = viewSizeHD < viewSize.y ? (scanlineIntencity.rgb * final.rgb) : final.rgb;
	outColor.a = 1.0;

#if defined(RETRO_REV02) 
	outColor *= screenDim.x;
#endif
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
