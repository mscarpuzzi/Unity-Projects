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

#ifndef WEATHER_MAKER_TEMPORAL_REPROJECTION_SHADER_INCLUDE
#define WEATHER_MAKER_TEMPORAL_REPROJECTION_SHADER_INCLUDE

#define WEATHER_MAKER_IS_FULL_SCREEN_EFFECT

#include "WeatherMakerCoreShaderInclude.cginc"

// stupid error, dx11 is dumb
#pragma warning( disable : 4000 )

// reprojection frame
UNITY_DECLARE_SCREENSPACE_TEXTURE(_TemporalReprojection_SubFrame);
uniform float4 _TemporalReprojection_SubFrame_TexelSize;
static const float4 temporalReprojectionSubFrameBlurOffsets = float4(_TemporalReprojection_SubFrame_TexelSize.x * 0.5, _TemporalReprojection_SubFrame_TexelSize.x * 1.5,
	_TemporalReprojection_SubFrame_TexelSize.y * 0.5, _TemporalReprojection_SubFrame_TexelSize.y * 1.5);
static const float4 temporalReprojectionSubFrameBlurOffsets2 = float4(_TemporalReprojection_SubFrame_TexelSize.x * 1.5, _TemporalReprojection_SubFrame_TexelSize.x * 2.5,
	_TemporalReprojection_SubFrame_TexelSize.y * 1.5, _TemporalReprojection_SubFrame_TexelSize.y * 2.5);
static const float4 temporalReprojectionSubFrameBlurOffsets3 = float4(_TemporalReprojection_SubFrame_TexelSize.x * 2.5, _TemporalReprojection_SubFrame_TexelSize.x * 3.5,
	_TemporalReprojection_SubFrame_TexelSize.y * 2.5, _TemporalReprojection_SubFrame_TexelSize.y * 3.5);

// previous frame
UNITY_DECLARE_SCREENSPACE_TEXTURE(_TemporalReprojection_PrevFrame);
uniform float4 _TemporalReprojection_PrevFrame_TexelSize;
static const float4 temporalReprojectionPrevFrameBlurOffsets = float4(_TemporalReprojection_PrevFrame_TexelSize.x * 0.5, _TemporalReprojection_PrevFrame_TexelSize.x * 1.5,
	_TemporalReprojection_PrevFrame_TexelSize.y * 0.5, _TemporalReprojection_PrevFrame_TexelSize.y * 1.5);

// frame parameters
uniform float _TemporalReprojection_SubFrameNumber;
uniform float _TemporalReprojection_SubPixelSize;

// controls how similar neighbor pixels must be before the pixel is completely redrawn
uniform float _TemporalReprojection_SimilarityMax;

// matrixes, index with unity_StereoEyeIndex
uniform float4x4 _TemporalReprojection_ipivpvp[2];
uniform float4x4 _TemporalReprojection_PreviousViewProjection[2];
uniform float4x4 _TemporalReprojection_InverseProjectionView[2];
uniform float4x4 _TemporalReprojection_InverseProjection[2];
uniform float4x4 _TemporalReprojection_Projection[2];
uniform float4x4 _TemporalReprojection_InverseView[2];
uniform float4x4 _TemporalReprojection_View[2];
uniform float4x4 _TemporalReprojection_PreviousView[2];

struct v2fTemporalReprojectionDefault
{
	float4 position : SV_POSITION;
	float2 uv : TEXCOORD0;
	WM_BASE_VERTEX_TO_FRAG
};

#if !defined(WEATHER_MAKER_TEMPORAL_REPROJECTION_FRAGMENT_TYPE)

#define WEATHER_MAKER_TEMPORAL_REPROJECTION_FRAGMENT_TYPE v2fTemporalReprojectionDefault

WEATHER_MAKER_TEMPORAL_REPROJECTION_FRAGMENT_TYPE temporal_reprojection_vert_default(appdata_base v)
{
	WM_INSTANCE_VERT(v, WEATHER_MAKER_TEMPORAL_REPROJECTION_FRAGMENT_TYPE, o);
	o.position = UnityObjectToClipPos(v.vertex);
	o.uv = AdjustFullScreenUV(v.texcoord);
	return o;
}

#endif

#if !defined(WEATHER_MAKER_TEMPORAL_REPROJECTION_BLEND_FUNC)

#define WEATHER_MAKER_TEMPORAL_REPROJECTION_BLEND_FUNC temporal_reprojection_blend_default

inline fixed4 temporal_reprojection_blend_default(fixed4 prev, fixed4 cur, fixed4 diff, float4 uv, WEATHER_MAKER_TEMPORAL_REPROJECTION_FRAGMENT_TYPE i)
{
	return prev;
}

#endif

#if !defined(WEATHER_MAKER_TEMPORAL_REPROJECTION_SAMPLE_PREV)

#define WEATHER_MAKER_TEMPORAL_REPROJECTION_SAMPLE_PREV(uv, samp) WM_SAMPLE_FULL_SCREEN_TEXTURE(samp, uv.zw)

#endif

// optional...

// fragment type, must provide uv field (float2 or float4)
// WEATHER_MAKER_TEMPORAL_REPROJECTION_FRAGMENT_TYPE

// func(prevPixel, curPixel, uv) returns difference (0-1), where 0 is identical, 1 is very different, uv = screen xy, reproj xy
// return x = difference, yzw user defined
// fixed4 compareFunc(fixed4 prev, fixed4 cur, float4 uv) { return 0; }
// WEATHER_MAKER_TEMPORAL_REPROJECTION_COMPARE_FUNC

// returns color, this is the expensive function we try to minimize as much as possible, handles instancing for you
// fixed4 fragFunc(WEATHER_MAKER_TEMPORAL_REPROJECTION_FRAGMENT_TYPE i) { return fixed4(...); }
// WEATHER_MAKER_TEMPORAL_REPROJECTION_FRAGMENT_FUNC : func(WEATHER_MAKER_TEMPORAL_REPROJECTION_FRAGMENT_TYPE i) 

// handle when an off screen pixel comes on screen, this is tricky - we have to prevent artifacts but it's also
//  not ideal to simply call WEATHER_MAKER_TEMPORAL_REPROJECTION_FRAGMENT_FUNC especially for fast rotating camera
//  uv = screen xy, reproj xy
// fixed4 offScreenFunc(fixed4 prev, fixed4 cur, float4 uv, WEATHER_MAKER_TEMPORAL_REPROJECTION_FRAGMENT_TYPE i) { return cur; }
// WEATHER_MAKER_TEMPORAL_REPROJECTION_OFF_SCREEN_FUNC

// returns color, uv = screen xy, reproj xy
// fixed4 blendFunc(fixed4 prev, fixed4 cur, fixed4 diff, float4 uv, WEATHER_MAKER_TEMPORAL_REPROJECTION_FRAGMENT_TYPE i) { return prev; }
// WEATHER_MAKER_TEMPORAL_REPROJECTION_BLEND_FUNC

// sample the previous frame, defaults to the temporal previous pixel
// fixed4 samplePrevFunc(float4 uv, sampler2D tex) { return tex2D(tex, uv.zw); }
// WEATHER_MAKER_TEMPORAL_REPROJECTION_SAMPLE_PREV(uv, samp)

// define to show overdraw, to a float4, half4 or fixed4
// WEATHER_MAKER_TEMPORAL_REPROJECTION_SHOW_OVERDRAW

fixed4 temporal_reprojection_fragment_custom(WEATHER_MAKER_TEMPORAL_REPROJECTION_FRAGMENT_TYPE i) : SV_TARGET
{
	WM_INSTANCE_FRAG(i);

#if defined(WEATHER_MAKER_TEMPORAL_REPROJECTION_FRAGMENT_FUNC)

#if defined(SHADER_API_GLES3)

	return WEATHER_MAKER_TEMPORAL_REPROJECTION_FRAGMENT_FUNC(i);

#endif

	UNITY_BRANCH
	if (weatherMakerTemporalReprojectionEnabled == 2)
	{

#endif

		float2 pixelCoord = floor(i.uv * _TemporalReprojection_PrevFrame_TexelSize.zw);
		float x = fmod(pixelCoord.x, _TemporalReprojection_SubPixelSize);
		float y = fmod(pixelCoord.y, _TemporalReprojection_SubPixelSize);
		float frame = floor((y * _TemporalReprojection_SubPixelSize) + x);

		// map to pixel pos
		float2 currentFramePixelCoord = floor(i.uv.xy * _TemporalReprojection_SubFrame_TexelSize.zw) + 0.5; // get in the center of the pixel, avoid bilinear filtering

		// map back to uv
		float2 currentFrameUV = currentFramePixelCoord * _TemporalReprojection_SubFrame_TexelSize.xy;

		UNITY_BRANCH
		if (frame == _TemporalReprojection_SubFrameNumber)
		{
			// sample the current temporal reprojection frame
			return WM_SAMPLE_FULL_SCREEN_TEXTURE(_TemporalReprojection_SubFrame, currentFrameUV);
		}
		else
		{
			float2 uv = i.uv.xy;

#if defined(UNITY_SINGLE_PASS_STEREO)

			// for double wide texture, get uv in 0-1 range first
			uv.x -= (0.5 * unity_StereoEyeIndex);
			uv.x *= 2.0;

#endif

			float4 reproj;

			// code derived from https://github.com/playdeadgames/temporal
			UNITY_BRANCH
			if (unity_OrthoParams.w == 1.0)
			{
				// orthographic mode, position never changes
				reproj = float4(uv, 0.0, 0.0);
			}
			else
			{
				// multiply by previous frame matrix (inverse projection, inverse view to model position and then previous view and then again projection
				//  this gets us to the uv from the previous frame

				// map uv (0 - 1) to projection (-1 - 1)
				float4 prevPos = float4(uv.xy * 2.0 - 1.0, 1.0, 1.0);
				reproj = mul(_TemporalReprojection_ipivpvp[unity_StereoEyeIndex], prevPos);

				//prevPos = mul(_TemporalReprojection_InverseProjectionView[unity_StereoEyeIndex], prevPos);
				//reproj.xyz = mul((float3x3)_TemporalReprojection_PreviousViewProjection[unity_StereoEyeIndex], prevPos.xyz);

				//prevPos = mul(_TemporalReprojection_InverseProjection[unity_StereoEyeIndex], prevPos);
				//prevPos = mul(_TemporalReprojection_InverseView[unity_StereoEyeIndex], prevPos);
				//prevPos = mul(_TemporalReprojection_PreviousView[unity_StereoEyeIndex], prevPos);
				//reproj = mul(_TemporalReprojection_Projection[unity_StereoEyeIndex], prevPos);

				// perspective divide and then turn back into uv from projection (-1, 1) to (0 - 1)
				reproj.xyz = ((reproj.xyz / reproj.w) + 1.0) * 0.5;
			}

			// if the previous frame uv was on the screen...
			UNITY_BRANCH
			if (reproj.y >= 0.0 && reproj.y <= 1.0 && reproj.x >= 0.0 && reproj.x <= 1.0 && reproj.z >= 0.0)
			{

#if defined(UNITY_SINGLE_PASS_STEREO)

				// put back to double wide texture uv coord
				reproj.x *= 0.5;
				reproj.x += (0.5 * unity_StereoEyeIndex);

#endif

				// use previous frame
				fixed4 prev = WEATHER_MAKER_TEMPORAL_REPROJECTION_SAMPLE_PREV(float4(i.uv.xy, reproj.xy), _TemporalReprojection_PrevFrame);

#if defined(WEATHER_MAKER_TEMPORAL_REPROJECTION_BLEND_FUNC)

				UNITY_BRANCH
				if (_TemporalReprojection_BlendMode == 0)
				{
					// custom blend func not needed in blur mode
					return prev;
				}
				else if (prev.a > 0.001)
				{
					// sample the sub frame
					fixed4 cur = WM_SAMPLE_FULL_SCREEN_TEXTURE(_TemporalReprojection_SubFrame, currentFrameUV);

					fixed4 diff;
					diff.y = min(1.0, max(cur.r, max(cur.g, cur.b))) * cur.a;
					diff.z = min(1.0, max(prev.r, max(prev.g, prev.b))) * prev.a;
					//diff.y = Luminance(cur.rgb) * cur.a;
					//diff.z = Luminance(prev.rgb) * prev.a;
					diff.x = abs(diff.y - diff.z);
					diff.w = abs(cur.a - prev.a);

					// smoothly blend the two pixels in some manner appropriate for the type of shader
					// the default is to just return the previous frame pixel, but this can cause bleeding
					// so the shader might have more knowledge on how to better blend the pixel, like the
					// fog shader can quickly compute the fog depth and apply that to the pixel alpha
					return WEATHER_MAKER_TEMPORAL_REPROJECTION_BLEND_FUNC(prev, cur, diff, float4(i.uv.xy, reproj.xy), i);
				}
				else
				{
					// return fixed4(1, 0, 0, 1); // debug
					// else pixel was fully blocked by depth buffer last frame, force redraw this frame
				}

#else

				return prev;

#endif

			}
			else
			{
				fixed4 cur = WM_SAMPLE_FULL_SCREEN_TEXTURE(_TemporalReprojection_SubFrame, i.uv.xy);

#if defined(WEATHER_MAKER_TEMPORAL_REPROJECTION_OFF_SCREEN_FUNC)

				if (_TemporalReprojection_BlendMode == 1)
				{
					// grab the previous and current pixel and handle off screen projection
					// generally we don't want to re-render the entire off screen pixel as this is expensive
					// so we try to blur, fudge it whatever we can do to make it look good enough
					fixed4 prev = WM_SAMPLE_FULL_SCREEN_TEXTURE(_TemporalReprojection_PrevFrame, i.uv.xy);
					cur = WEATHER_MAKER_TEMPORAL_REPROJECTION_OFF_SCREEN_FUNC(prev, cur, float4(i.uv.xy, reproj.xy), i);
				}

#endif

				return cur;
			}
		}

#if defined(WEATHER_MAKER_TEMPORAL_REPROJECTION_FRAGMENT_FUNC)

	}

#endif

#if defined(WEATHER_MAKER_TEMPORAL_REPROJECTION_FRAGMENT_FUNC)

	// we have to redraw the pixel if it is different enough that it will look bad on screen

#if defined(WEATHER_MAKER_TEMPORAL_REPROJECTION_SHOW_OVERDRAW)

	if (weatherMakerTemporalReprojectionEnabled == 2)
	{
		return WEATHER_MAKER_TEMPORAL_REPROJECTION_SHOW_OVERDRAW; // test overdraw performance
	}

#endif

	return WEATHER_MAKER_TEMPORAL_REPROJECTION_FRAGMENT_FUNC(i);

#endif

	return fixed4(1.0, 0.0, 1.0, 1.0); // error
}

#endif
