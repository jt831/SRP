/*#ifndef LIGHT_PASS_INCLUDED
#define LIGHT_PASS_INCLUDED
#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomLight)
int _DirectionalLightCount;
float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalShadowData[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT];
float4x4 _TransformWorldToShadowMapMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct DirectionalShadowData
{
    float shadowStrength;
    int shadowedLightI;
};
struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
};
DirectionalShadowData GetDirectionalShadowData (int i)
{
    DirectionalShadowData data;
    data.shadowStrength = _DirectionalShadowData[i].x;
    data.shadowedLightI = _DirectionalShadowData[i].y;
    return data;
}
float GetDirectionalShadowAttenuation (DirectionalShadowData data, float3 positionWS, float3 normalWS)
{
    if (data.shadowStrength <= 0.0f) return 1.0f;
    float4 test = (positionWS + normalWS, 1.0f);
    float3 positionSTS =
        mul(_TransformWorldToShadowMapMatrices[data.shadowedLightI], test).xyz;
    float shadowWeight =
        SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
    return shadowWeight;
}
Light SetupLight(int i, float3 positionWS, float normalWS)
{
    Light light;
    light.color = _DirectionalLightColors[i].rgb;
    light.direction = _DirectionalLightDirections[i].xyz;
    light.attenuation = GetDirectionalShadowAttenuation(GetDirectionalShadowData(i), positionWS, normalWS);
    return light;
}

#endif*/
#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomLight)
int _DirectionalLightCount;
float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightShadowData[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT];
float4x4 _TransformWorldToShadowMapMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct Light {
    float3 color;
    float3 direction;
    float attenuation;
};

struct DirectionalShadowData {
    float shadowStrength;
    int shadowedLightI;
};

DirectionalShadowData GetDirectionalShadowData (int lightIndex) {
    DirectionalShadowData data;
    data.shadowStrength = _DirectionalLightShadowData[lightIndex].x;
    data.shadowedLightI = _DirectionalLightShadowData[lightIndex].y;
    return data;
}

/*float GetDirectionalShadowAttenuation (DirectionalShadowData directional, float3 positionWS, float3 normalWS) {
    if (directional.strength <= 0.0) {
        return 1.0;
    }
    float3 normalBias = normalWS;
    float3 positionSTS =
        mul(_TransformWorldToShadowMapMatrices[directional.tileIndex],float4(positionWS + normalBias, 1.0)).xyz;
    float shadow = SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
	
    return lerp(1.0, shadow, directional.strength);
}*/

float GetDirectionalShadowAttenuation (DirectionalShadowData data, float3 positionWS, float3 normalWS)
{
    if (data.shadowStrength <= 0.0f) return 1.0f;
    // 
    float4 position = float4(positionWS + normalWS, 1.0f);
    float3 positionSTS =
        mul(_TransformWorldToShadowMapMatrices[data.shadowedLightI], position).xyz;
    float shadowWeight =
        SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
    return lerp(1.0, shadowWeight, data.shadowStrength);
}

Light SetupLight(int i, float3 positionWS, float3 normalWS)
{
    Light light;
    light.color = _DirectionalLightColors[i].rgb;
    light.direction = _DirectionalLightDirections[i].xyz;
    light.attenuation = GetDirectionalShadowAttenuation(GetDirectionalShadowData(i), positionWS, normalWS);
    return light;
}

float3 GetFinalColor(Light light, float3 normalWS)
{
    return saturate(dot(normalWS, light.direction)) * light.color * light.attenuation;
}
#endif