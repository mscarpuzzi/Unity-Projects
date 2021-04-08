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

Shader "WeatherMaker/WeatherMakerCloudShadowShader"
{
	Properties
	{
		[Header(Shadow)]
		_CloudShadowMapAdder("Adder", Range(-1.0, 1.0)) = -0.4
		_CloudShadowMapMultiplier("Multiplier", Range(0.01, 10.0)) = 4.0
		_CloudShadowMapPower("Power", Range(0.0, 256.0)) = 1.0
		_CloudShadowDetails("Shadow Details", Int) = 1
		_WeatherMakerCloudVolumetricShadowDither("Dither", Range(0.0, 1.0)) = 0.05
		[NoScaleOffset] _WeatherMakerCloudShadowDetailTexture("Detail Texture", 2D) = "white" {}
		_WeatherMakerCloudShadowDetailScale("Detail Scale", Range(0.0, 1.0)) = 0.0001
		_WeatherMakerCloudShadowDetailIntensity("Detail Intensity", Range(0.0, 10.0)) = 1.0
		_WeatherMakerCloudShadowDetailFalloff("Detail Falloff", Range(0.0, 1.0)) = 0.0
		_WeatherMakerCloudShadowDistanceFade("Distance Fade", Range(0.0, 4.0)) = 1.75
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always BlendOp [_BlendOp]

		CGINCLUDE

		#pragma target 3.5
		#pragma exclude_renderers gles
		#pragma exclude_renderers d3d9

		#pragma fragmentoption ARB_precision_hint_fastest
		#pragma glsl_no_auto_normalization
		#pragma multi_compile_instancing

		#define WEATHER_MAKER_ENABLE_TEXTURE_DEFINES
		#define WEATHER_MAKER_IS_FULL_SCREEN_EFFECT

		#include "WeatherMakerCloudVolumetricShaderInclude.cginc"

		struct v2fCloudShadow
		{
			float4 vertex : SV_POSITION;
			float2 uv : TEXCOORD0;
			float3 rayDir : TEXCOORD1;
		};

		uniform int _CloudShadowDetails;

		struct cloud_shadow_fragment
		{
			float4 vertex : SV_POSITION;
			float4 worldPos : TEXCOORD0;
			float2 uv : TEXCOORD1;
			WM_BASE_VERTEX_TO_FRAG
		};

		cloud_shadow_fragment cloudShadowVertexShader(wm_full_screen_fragment_vertex_uv v)
		{
			WM_INSTANCE_VERT(v, cloud_shadow_fragment, o);
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = v.uv;
			o.worldPos.x = lerp(WEATHER_MAKER_CAMERA_POS.x - _ProjectionParams.z, WEATHER_MAKER_CAMERA_POS.x + _ProjectionParams.z, v.uv.x);
			o.worldPos.y = 0.0;
			o.worldPos.z = lerp(WEATHER_MAKER_CAMERA_POS.z - _ProjectionParams.z, WEATHER_MAKER_CAMERA_POS.z + _ProjectionParams.z, v.uv.y);
			o.worldPos.w = 0.0;
			return o;
		}

		ENDCG
		
		// screen space cloud shadows
		Pass
		{
			CGPROGRAM

			#pragma vertex full_screen_vertex_shader
			#pragma fragment shadowFrag

			float4 shadowFrag(wm_full_screen_fragment i) : SV_Target
			{
				// screen shadows
				WM_INSTANCE_FRAG(i);


#if UNITY_VERSION >= 201901 && UNITY_VERSION < 201903

				// Unity 2019.1 and 2019.2 depth buffer are upside down in VR
				i.uv.y = lerp(i.uv.y, 1.0 - i.uv.y, _WeatherMakerVREnabled);

#endif

				float depth = GetDepth01(i.uv);
				UNITY_BRANCH
				if (depth < 1.0)
				{
					float3 worldPos = WEATHER_MAKER_CAMERA_POS + (depth * i.forwardLine);
					float existingShadow = WM_SAMPLE_FULL_SCREEN_TEXTURE(_MainTex5, i.uv.xy).r;
					return ComputeCloudShadowStrength(worldPos, 0, existingShadow, _CloudShadowDetails, 1.0);
					//return ComputeCloudShadowStrengthTexture(worldPos, 0, existingShadow, 0);
				}
				else
				{
					return 1.0;
				}
			}

			ENDCG
		}

		// cloud shadow texture pass
		Pass
		{
			Blend One Zero

			CGPROGRAM

			#pragma vertex cloudShadowVertexShader
			#pragma fragment frag
			#pragma multi_compile_instancing

			fixed4 frag(cloud_shadow_fragment i) : SV_Target
			{ 
				WM_INSTANCE_FRAG(i);

				float shadowStrength = ComputeCloudShadowStrength(i.worldPos.xyz, 0, 1.0, false, 0.0);
				return fixed4(shadowStrength, shadowStrength, shadowStrength, shadowStrength);
			}

			ENDCG
		}

		// cloud shadow texture blur pass
		Pass
		{
			Blend One Zero

			CGPROGRAM

			#pragma vertex cloudShadowVertexShader
			#pragma fragment frag
			#pragma multi_compile_instancing

			fixed4 frag(cloud_shadow_fragment i) : SV_Target
			{ 
				WM_INSTANCE_FRAG(i);

				static const float4 offsets = float4
				(
					_MainTex4_TexelSize.x * 0.2,
					_MainTex4_TexelSize.x * 0.4,
					_MainTex4_TexelSize.y * 0.2,
					_MainTex4_TexelSize.y * 0.4
				);
				static const float4 offsets2 = float4
				(
					_MainTex4_TexelSize.x * 0.3,
					_MainTex4_TexelSize.x * 0.6,
					_MainTex4_TexelSize.y * 0.3,
					_MainTex4_TexelSize.y * 0.6
				);

				fixed c;
				GaussianBlur17TapR(c, _MainTex2, i.uv.xy, offsets);
				fixed c2;
				GaussianBlur17TapR(c2, _MainTex2, i.uv.xy, offsets2);
				float shadow = (c + c2) * 0.5;

				// add a tiny bit that way if people are taking floor or square of shadow, completely unshadowed areas are more pronounced
				shadow = min(weatherMakerGlobalShadow, shadow + 0.01);
				return shadow;
			}

			ENDCG
		}
	}

	Fallback Off
}