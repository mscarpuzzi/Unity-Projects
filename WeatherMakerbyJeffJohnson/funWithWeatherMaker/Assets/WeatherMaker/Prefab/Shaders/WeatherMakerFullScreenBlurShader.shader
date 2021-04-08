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

// http://rastergrid.com/blog/2010/09/efficient-gaussian-blur-with-linear-sampling/
// _MainTex must be bilinear

Shader "WeatherMaker/WeatherMakerFullScreenBlurShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "red" {}
		_BlurDepthMin("Blur depth min", Range(0.0, 10.0)) = 0.1
		_BlurAlphaMin("Blur alpha min", Range(0.0, 10.0)) = 1.2
		_BlurDitherLevel("Blur dither level", Range(0.0, 1.0)) = 0.0004
		_BlurDepthFade("Blur depth fade", Range(0.0, 1.0)) = 1.0
		_BlurDepthPower("Blur depth power", Range(0.0, 4.0)) = 1.0
		_BlurNeedsSourceDepth("Blur needs source depth", Int) = 1
		_Blur7("Blur 7 tap, 0 for 17 tap", Int) = 0
		_BlendOp("Blend Op", Int) = 0
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest[_ZTest]
		BlendOp[_BlendOp]
		Blend[_SrcBlendMode][_DstBlendMode]

		CGINCLUDE

		#pragma target 3.5
		#pragma exclude_renderers gles
		#pragma exclude_renderers d3d9
		#pragma fragmentoption ARB_precision_hint_fastest
		#pragma multi_compile_instancing

		#define WEATHER_MAKER_IS_FULL_SCREEN_EFFECT
		#define WEATHER_MAKER_ENABLE_TEXTURE_DEFINES

		#include "WeatherMakerCoreShaderInclude.cginc"

		UNITY_DECLARE_DEPTH_TEXTURE(_WeatherMakerTemporaryDepthTexture);

		float _BlurDepthMin;
		float _BlurAlphaMin;
		float _BlurDitherLevel;
		float _BlurDepthFade;
		float _BlurDepthPower;
		int _Blur7;
		int _BlurNeedsSourceDepth;

		struct v2f
		{
			float4 vertex : SV_POSITION;
			float2 uv : TEXCOORD0;
			float4 offsets : TEXCOORD1;

			WM_BASE_VERTEX_TO_FRAG
		};

		v2f vert (appdata_base v)
		{
			WM_INSTANCE_VERT(v, v2f, o);
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uv = AdjustFullScreenUV(v.texcoord);

			if (_Blur7 == 1.0)
			{
				// take top left 3 and bottom right 3 plus center pixel average
				o.offsets = float4(_MainTex_TexelSize.x * 0.333333, _MainTex_TexelSize.y * 0.333333, 0.0, 0.0);
			}
			else
			{
				// (0.4,-1.2) , (-1.2,-0.4) , (1.2,0.4) and (-0.4,1.2).
				o.offsets = float4
				(
					_MainTex_TexelSize.x * 0.4,
					_MainTex_TexelSize.x * 1.2,
					_MainTex_TexelSize.y * 0.4,
					_MainTex_TexelSize.y * 1.2
				);
			}

			return o;
		}

		ENDCG

		// optimized blur
		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			fixed4 frag(v2f i) : SV_Target
			{
				WM_INSTANCE_FRAG(i);

				float sceneDepth = WM_LINEAR_DEPTH_01(UNITY_SAMPLE_DEPTH(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv)));
				float sourceDepth = (_BlurNeedsSourceDepth ? (UNITY_SAMPLE_DEPTH(SAMPLE_DEPTH_TEXTURE(_WeatherMakerTemporaryDepthTexture, i.uv))) : sceneDepth); // already linear 0 - 1
				float multiplier = (_BlurNeedsSourceDepth ? lerp(pow(saturate(((sceneDepth * _ProjectionParams.z) - (sourceDepth * _ProjectionParams.z)) * _BlurDepthFade), _BlurDepthPower), 1.0, sceneDepth >= 1.0) : 1.0);

				//return fixed4(sourceDepth.rrr - sceneDepth.rrr, 1.0);

				UNITY_BRANCH
				if (_BlurNeedsSourceDepth && sceneDepth < sourceDepth - 0.01)
				{
					// occluded pixel
					return fixed4Zero;
				}

				fixed4 col = WM_SAMPLE_FULL_SCREEN_TEXTURE(_MainTex, i.uv);
				float depth1 = (WM_SAMPLE_DEPTH_DOWNSAMPLED_01(i.uv));

				UNITY_BRANCH
				if (_Blur7)
				{
					// 7 tap approximation with 2 texture lookups
					float2 uv1 = float2(i.uv.x - i.offsets.x, i.uv.y - i.offsets.y);
					float2 uv2 = float2(i.uv.x + i.offsets.x, i.uv.y + i.offsets.y);
					float depth2 = (WM_SAMPLE_DEPTH_DOWNSAMPLED_01(uv1));
					float depth3 = (WM_SAMPLE_DEPTH_DOWNSAMPLED_01(uv2));
					float depthAvg = ((depth1 + depth2 + depth3) * 0.33333);
					float weight1 = abs(depth1 - depthAvg);
					float weight2 = abs(depth2 - depthAvg);
					float weight3 = abs(depth3 - depthAvg);
					float minWeight = 1.0 / max(1.0, min(weight1, min(weight2, weight3)));
					float match1 = (weight1 * minWeight) < _BlurDepthMin;
					float match2 = (weight2 * minWeight) < _BlurDepthMin;
					float match3 = (weight3 * minWeight) < _BlurDepthMin;
					float count = match1 + match2 + match3;

					// 2+ depth matches, try alpha
					UNITY_BRANCH
					if (count > 1.1)
					{
						fixed4 col2 = WM_SAMPLE_FULL_SCREEN_TEXTURE(_MainTex, uv1);
						fixed4 col3 = WM_SAMPLE_FULL_SCREEN_TEXTURE(_MainTex, uv2);
						depthAvg = ((col.a + col2.a + col3.a) * 0.33333);
						weight1 = abs(col.a - depthAvg);
						weight2 = abs(col2.a - depthAvg);
						weight3 = abs(col3.a - depthAvg);
						match1 = (weight1 * minWeight) < _BlurAlphaMin;
						match2 = (weight2 * minWeight) < _BlurAlphaMin;
						match3 = (weight3 * minWeight) < _BlurAlphaMin;
						count = match1 + match2 + match3;
						col *= match1;
						col += (match2 * col2);
						col += (match3 * col3);
						col /= count;
					} // else stick with original pixel
				}
				else
				{
					// 17 tap approximation with 4 texture lookups
					float2 uv1 = float2(i.uv.x + i.offsets.x, i.uv.y - i.offsets.w);
					float2 uv2 = float2(i.uv.x - i.offsets.y, i.uv.y - i.offsets.z);
					float2 uv3 = float2(i.uv.x + i.offsets.y, i.uv.y + i.offsets.z);
					float2 uv4 = float2(i.uv.x - i.offsets.x, i.uv.y + i.offsets.w);
					float depth2 = (WM_SAMPLE_DEPTH_DOWNSAMPLED_01(uv1));
					float depth3 = (WM_SAMPLE_DEPTH_DOWNSAMPLED_01(uv2));
					float depth4 = (WM_SAMPLE_DEPTH_DOWNSAMPLED_01(uv3));
					float depth5 = (WM_SAMPLE_DEPTH_DOWNSAMPLED_01(uv4));
					float depthAvg = ((depth1 + depth2 + depth3 + depth4 + depth5) * 0.2);
					float weight1 = abs(depth1 - depthAvg);
					float weight2 = abs(depth2 - depthAvg);
					float weight3 = abs(depth3 - depthAvg);
					float weight4 = abs(depth4 - depthAvg);
					float weight5 = abs(depth5 - depthAvg);
					float minWeight = 1.0 / max(1.0, min(weight1, min(weight2, min(weight3, min(weight4, weight5)))));
					float match1 = (weight1 * minWeight) < _BlurDepthMin;
					float match2 = (weight2 * minWeight) < _BlurDepthMin;
					float match3 = (weight3 * minWeight) < _BlurDepthMin;
					float match4 = (weight4 * minWeight) < _BlurDepthMin;
					float match5 = (weight5 * minWeight) < _BlurDepthMin;
					float count = match1 + match2 + match3 + match4 + match5;

					// 2+ depth matches, try alpha
					UNITY_BRANCH
					if (count > 1.1)
					{
						fixed4 col2 = WM_SAMPLE_FULL_SCREEN_TEXTURE(_MainTex, uv1);
						fixed4 col3 = WM_SAMPLE_FULL_SCREEN_TEXTURE(_MainTex, uv2);
						fixed4 col4 = WM_SAMPLE_FULL_SCREEN_TEXTURE(_MainTex, uv3);
						fixed4 col5 = WM_SAMPLE_FULL_SCREEN_TEXTURE(_MainTex, uv4);
						depthAvg = ((col.a + col2.a + col3.a + col4.a + col5.a) * 0.2);
						weight1 = abs(col.a - depthAvg);
						weight2 = abs(col2.a - depthAvg);
						weight3 = abs(col3.a - depthAvg);
						weight4 = abs(col4.a - depthAvg);
						weight5 = abs(col5.a - depthAvg);
						match1 = (weight1 * minWeight) < _BlurAlphaMin;
						match2 = (weight2 * minWeight) < _BlurAlphaMin;
						match3 = (weight3 * minWeight) < _BlurAlphaMin;
						match4 = (weight4 * minWeight) < _BlurAlphaMin;
						match5 = (weight5 * minWeight) < _BlurAlphaMin;
						count = match1 + match2 + match3 + match4 + match5;
						col *= match1;
						col += (match2 * col2);
						col += (match3 * col3);
						col += (match4 * col4);
						col += (match5 * col5);
						col /= count;
					} // else stick with original pixel
				}
				col *= multiplier;
				if (col.a > 0.0)
				{
					ApplyDither(col.rgb, i.uv.xy, _BlurDitherLevel);
				}
				return col;
            }

            ENDCG
		}
	}
}
