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

Shader "WeatherMaker/WeatherMakerFullScreenAlphaShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_AlphaDepthFade("Blur depth fade", Range(0.0, 1.0)) = 1.0
		_AlphaDepthPower("Blur depth power", Range(0.0, 4.0)) = 1.0
		_AlphaDitherLevel("Alpha dither level", Range(0.0, 1.0)) = 0.0004
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest [_ZTest]
		Blend [_SrcBlendMode][_DstBlendMode]

		CGINCLUDE

		#pragma target 3.5
		#pragma exclude_renderers gles
		#pragma exclude_renderers d3d9
		
		#define WEATHER_MAKER_ENABLE_TEXTURE_DEFINES

		ENDCG

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing

			#define WEATHER_MAKER_IS_FULL_SCREEN_EFFECT

			#include "WeatherMakerCoreShaderInclude.cginc"

			UNITY_DECLARE_DEPTH_TEXTURE(_WeatherMakerTemporaryDepthTexture);

			float _AlphaDepthFade;
			float _AlphaDepthPower;
			float _AlphaDitherLevel;

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				WM_BASE_VERTEX_TO_FRAG
			};
	 
			v2f vert(appdata_base v)
			{
				WM_INSTANCE_VERT(v, v2f, o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = AdjustFullScreenUV(v.texcoord.xy);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				WM_INSTANCE_FRAG(i);

				float sourceDepth = (UNITY_SAMPLE_DEPTH(SAMPLE_DEPTH_TEXTURE(_WeatherMakerTemporaryDepthTexture, i.uv))); // already linear 0 - 1
				float sceneDepth = WM_LINEAR_DEPTH_01(UNITY_SAMPLE_DEPTH(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv)));
				float multiplier = lerp(pow(saturate(((sceneDepth * _ProjectionParams.z) - (sourceDepth * _ProjectionParams.z)) * _AlphaDepthFade), _AlphaDepthPower), 1.0, sceneDepth >= 1.0);

				UNITY_BRANCH
				if (sceneDepth < sourceDepth - 0.001)
				{
					// occluded pixel
					return fixed4Zero;
				}

				fixed4 col = WM_SAMPLE_FULL_SCREEN_TEXTURE(_MainTex, i.uv);
				col *= multiplier;

				if (col.a > 0.0)
				{
					ApplyDither(col.rgb, i.uv.xy, _AlphaDitherLevel);
				}

				return col;
			}

			ENDCG
		}
	}
}
