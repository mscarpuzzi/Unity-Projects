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

Shader "WeatherMaker/WeatherMakerDownsampleDepthBufferShader"
{
	Properties
	{
	}
	Subshader
	{
		Cull Off Lighting Off ZWrite Off ZTest Always Fog { Mode Off } Blend One Zero

		CGINCLUDE

		#pragma target 3.5
		#pragma exclude_renderers gles
		#pragma exclude_renderers d3d9

		#define WEATHER_MAKER_ENABLE_TEXTURE_DEFINES
		#define WEATHER_MAKER_IS_FULL_SCREEN_EFFECT
		#define DEPTH_SAMPLE_MODE 0 // 0 = max, 1 = min, 2 = alternate max/min
		
		#include "WeatherMakerCoreShaderInclude.cginc"

		uniform float _DownsampleDepthScale;

		static const float2 texelSizeDepth = _CameraDepthTexture_TexelSize.xy * _DownsampleDepthScale;
		static const float2 tapsDepth1 = (float2(-0.4, -0.4) * texelSizeDepth);
		static const float2 tapsDepth2 = (float2(0.4, -0.4) * texelSizeDepth);
		static const float2 tapsDepth3 = (float2(-0.4, 0.4) * texelSizeDepth);
		static const float2 tapsDepth4 = (float2(0.4, 0.4) * texelSizeDepth);

#define SAMPLE_DEPTH_4(source, i) \
		float depth1 = UNITY_SAMPLE_DEPTH(SAMPLE_DEPTH_TEXTURE(source, i.uv.xy + tapsDepth1)); \
		float depth2 = UNITY_SAMPLE_DEPTH(SAMPLE_DEPTH_TEXTURE(source, i.uv.xy + tapsDepth2)); \
		float depth3 = UNITY_SAMPLE_DEPTH(SAMPLE_DEPTH_TEXTURE(source, i.uv.xy + tapsDepth3)); \
		float depth4 = UNITY_SAMPLE_DEPTH(SAMPLE_DEPTH_TEXTURE(source, i.uv.xy + tapsDepth4));

#define SAMPLE_DEPTH_4_01(source, i) \
		float depth1 = WM_LINEAR_DEPTH_01(UNITY_SAMPLE_DEPTH(SAMPLE_DEPTH_TEXTURE(source, i.uv.xy + tapsDepth1))); \
		float depth2 = WM_LINEAR_DEPTH_01(UNITY_SAMPLE_DEPTH(SAMPLE_DEPTH_TEXTURE(source, i.uv.xy + tapsDepth2))); \
		float depth3 = WM_LINEAR_DEPTH_01(UNITY_SAMPLE_DEPTH(SAMPLE_DEPTH_TEXTURE(source, i.uv.xy + tapsDepth3))); \
		float depth4 = WM_LINEAR_DEPTH_01(UNITY_SAMPLE_DEPTH(SAMPLE_DEPTH_TEXTURE(source, i.uv.xy + tapsDepth4)));

#if DEPTH_SAMPLE_MODE == 0 // max

#define DownsampleDepth(source, i, texelSize) \
		SAMPLE_DEPTH_4(source, i); \
		return (max(depth1, max(depth2, max(depth3, depth4))));

#define DownsampleDepth01(source, i, texelSize) \
		SAMPLE_DEPTH_4_01(source, i); \
		return (max(depth1, max(depth2, max(depth3, depth4))));

#elif DEPTH_SAMPLE_MODE == 1 // min

#define DownsampleDepth(source, i, texelSize) \
		SAMPLE_DEPTH_4(source, i); \
		return (min(depth1, min(depth2, min(depth3, depth4))));

#define DownsampleDepth01(source, i, texelSize) \
		SAMPLE_DEPTH_4_01(source, i); \
		return (min(depth1, min(depth2, min(depth3, depth4))));

#elif DEPTH_SAMPLE_MODE == 2 // checkerboard

#define DownsampleDepth(source, i, texelSize) \
		SAMPLE_DEPTH_4(source, i); \
		float minDepth = min(min(depth1, depth2), min(depth3, depth4)); \
		float maxDepth = max(max(depth1, depth2), max(depth3, depth4)); \
		int2 position = floor(i.uv.xy * texelSize.zw); \
		int index = fmod(position.x + position.y, 2.0); \
		return lerp(maxDepth, minDepth, index);

#define DownsampleDepth01(source, i, texelSize) \
		SAMPLE_DEPTH_4_01(source, i); \
		float minDepth = min(min(depth1, depth2), min(depth3, depth4)); \
		float maxDepth = max(max(depth1, depth2), max(depth3, depth4)); \
		int2 position = floor(i.uv.xy * texelSize.zw); \
		int index = fmod(position.x + position.y, 2.0); \
		return lerp(maxDepth, minDepth, index);

#else

#error Invalid downsample mode

#endif

		wm_full_screen_fragment_vertex_uv vert(wm_full_screen_vertex v)
		{
			WM_INSTANCE_VERT(v, wm_full_screen_fragment_vertex_uv, o);
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = AdjustFullScreenUV(v.uv);
			return o;
		}

		float4 frag1(wm_full_screen_fragment_vertex_uv i) : SV_Target
		{
			WM_INSTANCE_FRAG(i);
			return WM_LINEAR_DEPTH_01(UNITY_SAMPLE_DEPTH(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv)));
		}

		float4 frag2(wm_full_screen_fragment_vertex_uv i) : SV_Target
		{
			WM_INSTANCE_FRAG(i);

#if UNITY_VERSION >= 201901 && UNITY_VERSION < 201903

			// Unity 2019.1 and 2019.2 depth buffer are upside down in VR
			i.uv.y = lerp(i.uv.y, 1.0 - i.uv.y, _WeatherMakerVREnabled);

#endif

			DownsampleDepth01(_CameraDepthTexture, i, _CameraDepthTextureHalf_TexelSize);
		}

		float4 frag3(wm_full_screen_fragment_vertex_uv i) : SV_Target
		{
			WM_INSTANCE_FRAG(i);
			DownsampleDepth(_CameraDepthTextureHalf, i, _CameraDepthTextureQuarter_TexelSize);
		}

		float4 frag4(wm_full_screen_fragment_vertex_uv i) : SV_Target
		{
			WM_INSTANCE_FRAG(i);
			DownsampleDepth(_CameraDepthTextureQuarter, i, _CameraDepthTextureEighth_TexelSize);
		}

		float4 frag5(wm_full_screen_fragment_vertex_uv i) : SV_Target
		{
			WM_INSTANCE_FRAG(i);
			DownsampleDepth(_CameraDepthTextureEighth, i, _CameraDepthTextureSixteenth_TexelSize);
		}

		ENDCG

		Pass // straight up copy
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag1
			#pragma multi_compile_instancing

			ENDCG
		}

		Pass // copy full depth texture to half
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag2
			#pragma multi_compile_instancing

			ENDCG
		}

		Pass // copy half depth texture to quarter
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag3
			#pragma multi_compile_instancing

			ENDCG
		}

		Pass // copy quarter depth texture to eighth
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag4
			#pragma multi_compile_instancing

			ENDCG
		}

		Pass // copy eighth depth texture to sixteenth
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag5
			#pragma multi_compile_instancing

			ENDCG
		}
	}

	Fallback Off
}