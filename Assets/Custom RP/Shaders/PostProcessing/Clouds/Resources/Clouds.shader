Shader "Hidden/JTRP/PP/Clouds"
{

    SubShader
    {
        
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        HLSLINCLUDE
        #include "UnityCG.cginc"
        #include "Assets/Custom RP/Tools/TransformTools.hlsl"
        #include "Assets/Custom RP/Tools/CommonTools.hlsl"
        #include "Assets/Custom RP/Tools/JTRPCommonFunc.hlsl"
        // Texture3D<float4>
        TEXTURE3D(NoiseTex);    SAMPLER(sampler_NoiseTex);
        TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
Texture3D<float4> DetailNoiseTex;
            Texture2D<float4> WeatherMap;
            Texture2D<float4> BlueNoise;
            
            SAMPLER (samplerNoiseTex);
            SAMPLER (samplerDetailNoiseTex);
            SAMPLER (samplerWeatherMap);
            SAMPLER (samplerBlueNoise);

            //TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);
        CBUFFER_START(UnityPerMaterial)
        float densityOffset;
        float time;
        float baseSpeed;
        float darknessThreshold;;
        float densityMultiplier;

        // Shape settings
        float4 params;
        int3 mapSize;
        //float densityOffset;
        float scale;
        float detailNoiseScale;
        float detailNoiseWeight;
        float3 detailWeights;
        float4 shapeNoiseWeights;
        float4 phaseParams;

        // March settings
        int numStepsLight;
        float rayOffsetStrength;

        //float3 boundsMin;
        //float3 boundsMax;

        float3 shapeOffset;
        float3 detailOffset;

        // Light settings
        float lightAbsorptionTowardSun;
        float lightAbsorptionThroughCloud;
        
        //float4 _LightColor0;
        float4 colA;
        float4 colB;

        // Animation settings
        float timeScale;
        //float baseSpeed;
        float detailSpeed;

        // Debug settings:
        int debugViewMode; // 0 = off; 1 = shape tex; 2 = detail tex; 3 = weathermap
        float debugNoiseSliceDepth;
        float4 debugChannelWeight;
        float debugTileAmount;
        float viewerSize;
        
        float3 boundsMin;
        float3 boundsMax;
        float4 _LightColor0;
        CBUFFER_END
        
        struct Attributes
        {
            float3 positionOS : POSITION;
            float2 uv         : TEXCOORD0;
        };

        struct v2f
        {
            float4 positionCS : SV_POSITION;
            float2 uv         : TEXCOORD0;
            float3 camDirWS   : TEXCOORD1;
        };

        v2f Vert(Attributes input)
        {
            v2f output;
            output.uv = input.uv;
            output.positionCS = TransformObjectToHClip(input.positionOS);
            float3 camDirVS = mul(unity_CameraInvProjection, float4(input.uv * 2 - 1, 0, -1));
            output.camDirWS = mul(unity_CameraToWorld, float4(camDirVS, 0));

            return output;
        }
        
        float GetRandomStepSize(float2 uv)
        {
            return NoiseTex.SampleLevel(sampler_NoiseTex, float3(uv, 0), 0).r * 5;
        }
        
        float2 GetInteractInfo(float3 boxPoint1, float3 boxPoint2, float3 camPosWS, float3 camDirWS)
        {
            float3 t1 = (boxPoint1 - camPosWS) / camDirWS;
            float3 t2 = (boxPoint2 - camPosWS) / camDirWS;

            float3 tmax = max(t1, t2);
            float3 tmin = min(t1, t2);

            float tin = max(tmin.x, max(tmin.y, tmin.z));
            float tout = min(tmax.x, min(tmax.y, tmax.z));

            float dst2Box = max(0, tin);
            float dstInBox = max(0, tout - dst2Box);

            return float2(dst2Box, dstInBox);
        }
        
        float sampleDensity(float3 rayPos)
        {
                // Constants:
                const int mipLevel = 0;
                const float baseScale = 1/1000.0;
                const float offsetSpeed = 1/100.0;

                // Calculate texture sample positions
                float time = _Time.x * timeScale;
                float3 size = boundsMax - boundsMin;
                float3 boundsCentre = (boundsMin+boundsMax) * .5;
                float3 samplePos = rayPos * baseScale + densityOffset * 0.01 + float3(time,time*0.1,time*0.2) * baseSpeed;
                float3 uvw = (size * .5 + rayPos) * baseScale * scale;
                float3 shapeSamplePos = uvw + shapeOffset * offsetSpeed + float3(time,time*0.1,time*0.2) * baseSpeed;

                // Calculate falloff at along x/z edges of the cloud container
                const float containerEdgeFadeDst = 50;
                float dstFromEdgeX = min(containerEdgeFadeDst, min(rayPos.x - boundsMin.x, boundsMax.x - rayPos.x));
                float dstFromEdgeZ = min(containerEdgeFadeDst, min(rayPos.z - boundsMin.z, boundsMax.z - rayPos.z));
                float edgeWeight = min(dstFromEdgeZ,dstFromEdgeX)/containerEdgeFadeDst;
                
                // Calculate height gradient from weather map
                float2 weatherUV = (size.xz * .5 + (rayPos.xz-boundsCentre.xz)) / max(size.x,size.z);
                float weatherMap = WeatherMap.SampleLevel(samplerWeatherMap, weatherUV, mipLevel).x;
                float gMin = remap(weatherMap.x,0,1,0.1,0.5);
                float gMax = remap(weatherMap.x,0,1,gMin,0.9);
                float heightPercent = (rayPos.y - boundsMin.y) / size.y;
                float heightGradient = saturate(remap(heightPercent, 0.0, gMin, 0, 1)) * saturate(remap(heightPercent, 1, gMax, 0, 1));
                heightGradient *= edgeWeight;

                // Calculate base shape density
                float4 shapeNoise = NoiseTex.SampleLevel(samplerNoiseTex, samplePos, 0);
                float4 normalizedShapeWeights = shapeNoiseWeights / dot(shapeNoiseWeights, 1);
                float shapeFBM = dot(shapeNoise, normalizedShapeWeights) * heightGradient;
                float baseShapeDensity = shapeFBM + densityOffset * .1;
return shapeNoise;
                // Save sampling from detail tex if shape density <= 0
                if (baseShapeDensity > 0) {
                    // Sample detail noise
                    float3 detailSamplePos = uvw*detailNoiseScale + detailOffset * offsetSpeed + float3(time*.4,-time,time*0.1)*detailSpeed;
                    float4 detailNoise = DetailNoiseTex.SampleLevel(samplerDetailNoiseTex, detailSamplePos, mipLevel);
                    float3 normalizedDetailWeights = detailWeights / dot(detailWeights, 1);
                    float detailFBM = dot(detailNoise, normalizedDetailWeights);

                    // Subtract detail noise from base shape (weighted by inverse density so that edges get eroded more than centre)
                    float oneMinusShape = 1 - shapeFBM;
                    float detailErodeWeight = oneMinusShape * oneMinusShape * oneMinusShape;
                    float cloudDensity = baseShapeDensity - (1-detailFBM) * detailErodeWeight * detailNoiseWeight;
    
                    return cloudDensity * densityMultiplier * 0.1;
                }
                return 0;
        }

        float4 DrawNoise(float2 samplePos)
        {
            float4 noiseColor = NoiseTex.SampleLevel(sampler_NoiseTex, float3(samplePos, debugNoiseSliceDepth), 0);
            float grayNoise = dot(noiseColor, 1);
            return grayNoise;
        }
        
        float GetDensity(float3 enterPos, float3 camDirWS, float distance, float2 uv)
        {
            float stepSize = GetRandomStepSize(uv);
            float dstTravelled = 0.0f;
            float finalDensity = 0.0f;
            while(dstTravelled < distance)
            {
                float3 rayPos = enterPos + dstTravelled * camDirWS;
                float density = sampleDensity(rayPos);
                finalDensity += max(0, density - darknessThreshold) * densityMultiplier;
                dstTravelled += stepSize;
            }
            return finalDensity;
        }
        ENDHLSL
        
        Pass
        {
/*            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment frag

            float2 squareUV(float2 uv) {
                float width = _ScreenParams.x;
                float height =_ScreenParams.y;
                //float minDim = min(width, height);
                float scale = 1000;
                float x = uv.x * width;
                float y = uv.y * height;
                return float2 (x/scale, y/scale);
            }

            // Returns (dstToBox, dstInsideBox). If ray misses box, dstInsideBox will be zero
            float2 rayBoxDst(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 invRaydir) {
                // Adapted from: http://jcgt.org/published/0007/03/04/
                float3 t0 = (boundsMin - rayOrigin) * invRaydir;
                float3 t1 = (boundsMax - rayOrigin) * invRaydir;
                float3 tmin = min(t0, t1);
                float3 tmax = max(t0, t1);
                
                float dstA = max(max(tmin.x, tmin.y), tmin.z);
                float dstB = min(tmax.x, min(tmax.y, tmax.z));

                // CASE 1: ray intersects box from outside (0 <= dstA <= dstB)
                // dstA is dst to nearest intersection, dstB dst to far intersection

                // CASE 2: ray intersects box from inside (dstA < 0 < dstB)
                // dstA is the dst to intersection behind the ray, dstB is dst to forward intersection

                // CASE 3: ray misses box (dstA > dstB)

                float dstToBox = max(0, dstA);
                float dstInsideBox = max(0, dstB - dstToBox);
                return float2(dstToBox, dstInsideBox);
            }

            // Henyey-Greenstein
            float hg(float a, float g) {
                float g2 = g*g;
                return (1-g2) / (4*3.1415*pow(1+g2-2*g*(a), 1.5));
            }

            float phase(float a) {
                float blend = .5;
                float hgBlend = hg(a,phaseParams.x) * (1-blend) + hg(a,-phaseParams.y) * blend;
                return phaseParams.z + hgBlend*phaseParams.w;
            }

            float beer(float d) {
                float beer = exp(-d);
                return beer;
            }

            float remap01(float v, float low, float high) {
                return (v-low)/(high-low);
            }
            
            // Calculate proportion of light that reaches the given point from the lightsource
            float lightmarch(float3 p) {
                float3 dirToLight = _WorldSpaceLightPos0.xyz;
                float dstInsideBox = rayBoxDst(boundsMin, boundsMax, p, 1/dirToLight).y;
                
                float transmittance = 1;
                float stepSize = dstInsideBox/numStepsLight;
                p += dirToLight * stepSize * .5;
                float totalDensity = 0;

                for (int step = 0; step < numStepsLight; step ++) {
                    float density = sampleDensity(p);
                    totalDensity += max(0, density * stepSize);
                    p += dirToLight * stepSize;
                }

                transmittance = beer(totalDensity*lightAbsorptionTowardSun);

                float clampedTransmittance = darknessThreshold + transmittance * (1-darknessThreshold);
                return clampedTransmittance;
            }

            float4 debugDrawNoise(float2 uv) {

                float4 channels = 0;
                float3 samplePos = float3(uv.x,uv.y, debugNoiseSliceDepth);

                if (debugViewMode == 1) {
                    channels = NoiseTex.SampleLevel(samplerNoiseTex, samplePos, 0);
                }
                else if (debugViewMode == 2) {
                    channels = DetailNoiseTex.SampleLevel(samplerDetailNoiseTex, samplePos, 0);
                }
                else if (debugViewMode == 3) {
                    channels = WeatherMap.SampleLevel(samplerWeatherMap, samplePos.xy, 0);
                }
                return dot(channels, 1);
            }

          
            float4 frag (v2f i) : SV_Target
            {

                if (debugViewMode != 0) {
                    float width = _ScreenParams.x;
                    float height =_ScreenParams.y;
                    float minDim = min(width, height);
                    float x = i.uv.x * width;
                    float y = (1-i.uv.y) * height;

                    if (x < minDim*viewerSize && y < minDim*viewerSize) {
                        return debugDrawNoise(float2(x/(minDim*viewerSize)*debugTileAmount, y/(minDim*viewerSize)*debugTileAmount));
                    }
                }
                
                // Create ray
                float3 rayPos = _WorldSpaceCameraPos;
                float viewLength = length(i.camDirWS);
                float3 rayDir = i.camDirWS / viewLength;
                
                // Depth and cloud container intersection info:
                float nonlin_depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv);
                float depth = LinearEyeDepth(nonlin_depth) * viewLength;
                float2 rayToContainerInfo = rayBoxDst(boundsMin, boundsMax, rayPos, 1/rayDir);
                float dstToBox = rayToContainerInfo.x;
                float dstInsideBox = rayToContainerInfo.y;

                // point of intersection with the cloud container
                float3 entryPoint = rayPos + rayDir * dstToBox;

                // random starting offset (makes low-res results noisy rather than jagged/glitchy, which is nicer)
                float randomOffset = BlueNoise.SampleLevel(samplerBlueNoise, squareUV(i.uv*3), 0);
                randomOffset *= rayOffsetStrength;
                
                // Phase function makes clouds brighter around sun
                float cosAngle = dot(rayDir, _WorldSpaceLightPos0.xyz);
                float phaseVal = phase(cosAngle);

                float dstTravelled = randomOffset;
                // 改 float dstLimit = min(depth-dstToBox, dstInsideBox);
                float dstLimit = dstInsideBox;
                
                // March through volume:
                float stepSize = NoiseTex.SampleLevel(samplerNoiseTex, float3(i.uv, 1), 0).r * 5;
                float transmittance = 1;
                float3 lightEnergy = 0;

                while (dstTravelled < dstLimit) {
                    rayPos = entryPoint + rayDir * dstTravelled;
                    float density = sampleDensity(rayPos);
                    
                    if (density > 0) {

                        float lightTransmittance = lightmarch(rayPos);
                        lightEnergy += density * stepSize * transmittance * lightTransmittance * phaseVal;
                        transmittance *= exp(-density * stepSize * lightAbsorptionThroughCloud);
                    
                        // Early exit
                        if (transmittance < 0.01) {
                            break;
                        }
                    }
                    dstTravelled += stepSize;
                }
                
                // Composite sky + background
                float3 skyColBase = lerp(colA,colB, sqrt(abs(saturate(rayDir.y))));
                float3 backgroundCol = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, i.uv, 0);
                float dstFog = 1-exp(-max(0,depth) * 8 * .0001);
                float3 sky = dstFog * skyColBase;
                backgroundCol = backgroundCol * (1-dstFog) + sky;

                // Sun
                float focusedEyeCos = pow(saturate(cosAngle), params.x);
                float sun = saturate(hg(focusedEyeCos, .9995)) * transmittance;
                
                // Add clouds
                float density = GetDensity(entryPoint, i.camDirWS, dstInsideBox, i.uv);
                float3 cloudCol = density * _LightColor0.xyz;
                float3 col = backgroundCol * transmittance + cloudCol;
                // 改 col = saturate(col) * (1-sun) + _LightColor0.xyz * sun;
                col = saturate(col);
                return float4(col,1);
            }
            ENDHLSL*/
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag(v2f input) : SV_Target
            {
                // Show noiseTex in editor
                if (debugViewMode != 0) {
                    float width = _ScreenParams.x;
                    float height =_ScreenParams.y;
                    float minDim = min(width, height);
                    float x = input.uv.x * width;
                    float y = (1-input.uv.y) * height;

                    if (x < minDim*viewerSize && y < minDim*viewerSize)
                    {
                        float2 samplePos = float2(x/(minDim*viewerSize)*debugTileAmount, y/(minDim*viewerSize)*debugTileAmount);
                        return DrawNoise(samplePos);
                    }
                }
                // Get background Color
                float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Get cloudsColor
                float2 interactInfo = GetInteractInfo(boundsMin, boundsMax, _WorldSpaceCameraPos, input.camDirWS);
                float dst2Box = interactInfo.x;
                float dstInBox = interactInfo.y;
                
                float3 enterPos = _WorldSpaceCameraPos + dst2Box * input.camDirWS;
                float distance = dstInBox;
                float density = GetDensity(enterPos, input.camDirWS, distance, input.uv);
                float3 cloudsColor = density * _LightColor0.xyz;

                // Add them together
                float4 finalColor = saturate(float4(baseColor.xyz + cloudsColor, baseColor.a));
                return finalColor;
            }
            
            ENDHLSL
        }
    }
}