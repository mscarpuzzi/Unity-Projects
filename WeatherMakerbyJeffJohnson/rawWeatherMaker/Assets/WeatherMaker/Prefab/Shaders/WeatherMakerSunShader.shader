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

Shader "WeatherMaker/WeatherMakerSunShader"
{
	Properties
	{
		_MieG("Mie g", Range(0.0, 1.0)) = 0.98
	}
		SubShader
	{
		Tags { "Queue" = "AlphaTest+51" }
		Cull Front Lighting Off ZWrite Off ZTest LEqual
		Blend One One

		CGINCLUDE

		#pragma target 3.5
		#pragma exclude_renderers gles
		#pragma exclude_renderers d3d9

		#define WEATHER_MAKER_ENABLE_TEXTURE_DEFINES

		#include "WeatherMakerSkyShaderInclude.cginc"

		#pragma fragmentoption ARB_precision_hint_fastest
		#pragma glsl_no_auto_normalization
		#pragma multi_compile_instancing

		uniform fixed _MieG;
		static const fixed _MieG2 = _MieG * _MieG;

		struct v2fSun
		{
			float4 vertex : SV_POSITION;
			float3 ray : NORMAL;
			float2 uv : TEXCOORD0;
			WM_BASE_VERTEX_TO_FRAG
		};

		v2fSun vert(appdata_base v)
		{
			WM_INSTANCE_VERT(v, v2fSun, o);
			o.vertex = UnityObjectToClipPosFarPlane(v.vertex);
			o.ray = -WorldSpaceViewDir(v.vertex);
			o.uv = v.texcoord.xy;
			return o;
		}

		fixed4 fragBase(v2fSun i)
		{
			WM_INSTANCE_FRAG(i);

			i.ray = normalize(i.ray);

			float eyeCos = dot(_WeatherMakerSunDirectionDown, i.ray);
			fixed miePhase = GetMiePhase(_WeatherMakerSunVar1.x, _MieG, _MieG2, eyeCos, eyeCos * eyeCos, 1.18);

			//float eyeCos = dot(_WeatherMakerSunDirectionUp, i.ray);
			//float miePhase = pow(1.0 - _MieG, 2.0) / (4.0 * PI * pow(max(0.0, 1.0 + _MieG2 - 2.0 * _MieG * eyeCos), 1.5));

			// ramp up sun intensity quickly, but not instantly, this ensures the sun doesn't pop into view all of a sudden
			fixed sunIntensityMultiplier = min(1.0, _WeatherMakerSunColor.a * _WeatherMakerSunColor.a * 4.0);
			fixed yFade = saturate((i.ray.y + _WeatherMakerSkyYOffset2D) * 4.0);
			fixed4 sunColor = fixed4((_WeatherMakerSunColor.rgb * _WeatherMakerSunTintColor.rgb) * miePhase * _WeatherMakerSunTintColor.a * sunIntensityMultiplier * yFade, miePhase);
			ApplyDither(sunColor.rgb, i.uv, _WeatherMakerSkyDitherLevel);
			return sunColor;
		}
			
		fixed4 frag (v2fSun i) : SV_TARGET
		{
			return fragBase(i);
		}

		ENDCG

		Pass
		{
			Tags { }

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			ENDCG
		}
	}

	FallBack Off
}
