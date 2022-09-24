#ifndef LIGHT_PASS_INCLUDED
#define LIGHT_PASS_INCLUDED
struct Light
{
    float3 position;
    float3 color;
    float3 direction;
};

float4 GetSurfaceWithLight(Light light, float3 surfaceNormalWS, float4 surfaceColor)
{
    float4 finalColor = surfaceColor;
    float halfLambert = (dot(light.direction, surfaceNormalWS) * 0.5) + 0.5;
    finalColor.xyz = surfaceColor.xyz * light.color * halfLambert;
    return finalColor;
}
#endif