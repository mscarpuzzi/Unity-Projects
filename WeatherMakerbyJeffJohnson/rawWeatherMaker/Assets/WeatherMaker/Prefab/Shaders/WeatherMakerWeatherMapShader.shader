//
// Weather Maker for Unity
// (c) 2016 Digital Ruby, LLC
// Source code may be used for personal or commercial projects.
// Source code may NOT be redistributed or sold.
// 
// *** A NOTE ABOUT PIRACY ***
// 
// If you got this asset from a pirate site, please consider buying it from the Unity asset store at https://www.assetstore.unity3d.com/en/#!/content/60955?aid=1011lGnL. This asset is only legally available from the Unity Asset Store.
// 
// I'm a single indie dev supporting my family by spending hundreds and thousands of hours on this and other assets. It's very offensive, rude and just plain evil to steal when I (and many others) put so much hard work into the software.
// 
// Thank you.
//
// *** END NOTE ABOUT PIRACY ***
//

Shader "WeatherMaker/WeatherMakerWeatherMapShader"
{
	Properties
	{
		[Header(Cloud Coverage)]
		_CloudCoverageFrequency("Frequency", Range(0.1, 64.0)) = 6.0
		_CloudCoverageRotation("Rotation", Vector) = (0.0, 0.0, 0.0, 0.0)
		_CloudCoverageVelocity("Velocity", Vector) = (0.01, 0.01, 0.01)
		_CloudCoverageOffset("Offset", Vector) = (0.0, 0.0, 0.0)
		_CloudCoverageMultiplier("Multiplier", Range(0.0, 100.0)) = 1.0
		_CloudCoverageAdder("Adder", Range(-1.0, 1.0)) = 0.0
		_CloudCoveragePower("Power", Range(0.0, 16.0)) = 1.0
		_CloudCoverageProfileInfluence("Profile influence", Range(0.0, 1.0)) = 1.0
		[NoScaleOffset] _CloudCoverageTexture("Coverage Texture", 2D) = "black" {} // additional cloud coverage
		_CloudCoverageTextureMultiplier("Coverage texture multiplier", Range(0.0, 1.0)) = 0.0
		_CloudCoverageTextureScale("Coverage texture scale", Range(0.0, 1.0)) = 1.0

		[Header(Cloud Type)]
		_CloudTypeFrequency("Frequency", Range(0.1, 64.0)) = 6.0
		_CloudTypeRotation("Rotation", Vector) = (0.0, 0.0, 0.0, 0.0)
		_CloudTypeVelocity("Velocity", Vector) = (0.01, 0.01, 0.01)
		_CloudTypeOffset("Offset", Vector) = (0.0, 0.0, 0.0)
		_CloudTypeMultiplier("Multiplier", Range(0.0, 100.0)) = 1.0
		_CloudTypeAdder("Adder", Range(-1.0, 1.0)) = 0.0
		_CloudTypePower("Power", Range(0.0, 16.0)) = 1.0
		_CloudTypeProfileInfluence("Profile influence", Range(0.0, 1.0)) = 1.0
		_CloudTypeCoveragePower("Coverage Power", Range(0.0, 1.0)) = 0.3
		[NoScaleOffset] _CloudTypeTexture("Type Texture", 2D) = "black" {} // additional cloud type
		_CloudTypeTextureMultiplier("Type texture multiplier", Range(0.0, 1.0)) = 0.0
		_CloudTypeTextureScale("Type texture scale", Range(0.0, 1.0)) = 1.0

		[Header(Other)]
		[NoScaleOffset] _MainTex("Mask Texture", 2D) = "white" {} // mask texture
		_MaskVelocity("Mask Velocity", Vector) = (0.0, 0.0, 0.0, 0.0)
		_MaskOffset("Mask Offset", Vector) = (0.0, 0.0, 0.0, 0.0)
	}
	SubShader
	{
		Tags { }
		LOD 100
		Blend One Zero
		Fog { Mode Off }
		ZWrite On
		ZTest Always

		CGINCLUDE

		#pragma target 3.5
		#pragma exclude_renderers gles
		#pragma exclude_renderers d3d9
		
		#define WEATHER_MAKER_ENABLE_TEXTURE_DEFINES

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
		};

		v2f vert(appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = v.uv;
			return o;
		}

		ENDCG

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "WeatherMakerCloudVolumetricShaderInclude.cginc"

#if defined(WEATHER_MAKER_ENABLE_VOLUMETRIC_CLOUDS)

			uniform float2 _MaskOffset;

			float WeatherMapNoise(float2 P, float sum)
			{

#define noiseFunc simplexNoise2D

				static const float weights[3] = { 0.5, 0.35, 0.15 };
				sum += (noiseFunc(P) * weights[0]);
				sum += (noiseFunc(P * 2.0) * weights[1]);
				sum += (noiseFunc(P * 4.0) * weights[2]);

#undef noiseFunc

				return saturate(sum);
			}

			// weather map space sample pos, velocity is already scaled
			inline float3 GetSamplePos(float2 uv, float3 velocity, float freq, float2 rotation)
			{
				uv -= 0.5;
				uv = RotateUV(uv, rotation.x, rotation.y);
				float2 xyPos = uv + RotateUV(weatherMapCameraPos.xz, rotation.x, rotation.y);
				float3 pos = velocity + float3(xyPos * _WeatherMakerWeatherMapScale.xy * freq, 0.0);
				return pos;
			}

			inline float SampleTexture(sampler2D samp, float2 uv, float2 rotation, float scale, float multiplier)
			{
				UNITY_BRANCH
				if (scale == 0.0f || multiplier <= 0.0f)
				{
					return 0.0f;
				}
				else
				{
					uv = RotateUV(uv, rotation.x, rotation.y) * scale;
					return tex2Dlod(samp, float4(uv, 0.0, 0.0)).a * multiplier;
				}
			}

#endif

			
			fixed4 frag (v2f i) : SV_Target
			{

#if defined(WEATHER_MAKER_ENABLE_VOLUMETRIC_CLOUDS)

				float3 samp;
				float value;
				fixed cloudCoverage;
				fixed cloudType;
				 
				samp = GetSamplePos(i.uv, cloudCoverageVelocity, _CloudCoverageFrequency, _CloudCoverageRotation);

				UNITY_BRANCH
                if (cloudCoverageIsMin)
                {
                    value = 0.0;
                }
				else if (cloudCoverageIsMax)
                {
                    value = 1.0;
                }
				else
				{
					float value2 = SampleTexture(_CloudCoverageTexture, i.uv, _CloudCoverageRotation, cloudCoverageTextureScale, cloudCoverageTextureMultiplier);
					value = saturate(((WeatherMapNoise(samp, cloudCoverageInfluence + value2) + _CloudCoverageAdder) * cloudCoverageInfluence2));
				}
				cloudCoverage = pow(value, _CloudCoveragePower);

				samp = GetSamplePos(i.uv, cloudTypeVelocity, _CloudTypeFrequency, _CloudTypeRotation);
                UNITY_BRANCH
                if (cloudTypeIsMin)
                {
                    value = 0.0;
                }
                else if (cloudTypeIsMax)
                {
                    value = 1.0;
                }
                else
                {
					float value2 = SampleTexture(_CloudTypeTexture, i.uv, _CloudTypeRotation, cloudTypeTextureScale, cloudTypeTextureMultiplier);
				    value = saturate(((WeatherMapNoise(samp, cloudTypeInfluence + value2) + _CloudTypeAdder) * cloudTypeInfluence2));
                }
				cloudType = pow(cloudCoverage, _CloudTypeCoveragePower) * pow(value, _CloudTypePower);

				fixed mask = tex2Dlod(_MainTex, float4(i.uv.xy + _MaskOffset, 0.0, 0.0)).a;

				// r = cloud coverage
				// g = precipitation (unused currently, 0)
				// b = cloud type
				// a = unused (1)
				return fixed4(cloudCoverage * mask * (cloudCoverage > VOLUMETRIC_MINIMUM_COVERAGE_FOR_CLOUD), 0.0, cloudType * mask, 1.0);

#else

				return fixed4Zero;

#endif

			}

			ENDCG
		}
	}

	Fallback Off
}
