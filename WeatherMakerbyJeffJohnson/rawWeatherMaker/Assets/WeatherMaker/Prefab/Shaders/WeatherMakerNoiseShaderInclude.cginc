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

#ifndef _WEATHER_MAKER_SIMPLE_NOISE_SHADER_INCLUDE_
#define _WEATHER_MAKER_SIMPLE_NOISE_SHADER_INCLUDE_

//1/7
#define K 0.142857142857
//3/7
#define Ko 0.428571428571

// 1 / 289
#define ONE_OVER_289 0.00346020761245674740484429065744f

inline float mod(float x, float y) { return x - y * floor(x / y); }
inline float2 mod(float2 x, float y) { return x - y * floor(x / y); }
inline float3 mod(float3 x, float y) { return x - y * floor(x / y); }
inline float4 mod(float4 x, float y) { return x - y * floor(x / y); }

inline float mod289(float x) {	return x - floor(x * ONE_OVER_289) * 289.0; }
inline float2 mod289(float2 x) { return x - floor(x * ONE_OVER_289) * 289.0; }
inline float3 mod289(float3 x) { return x - floor(x * ONE_OVER_289) * 289.0; }
inline float4 mod289(float4 x) { return x - floor(x * ONE_OVER_289) * 289.0; }

inline float permute(float x) { return mod289(x*x*34.0 + x); }
inline float2 permute(float2 x) { return mod289(x*x*34.0 + x); }
inline float3 permute(float3 x) { return mod289(x*x*34.0 + x); }
inline float4 permute(float4 x) { return mod289(x*x*34.0 + x); }

inline float perm289(float x) { return mod289(((x * 34.0) + 1.0) * x); }
inline float2 perm289(float2 x) { return mod289(((x * 34.0) + 1.0) * x); }
inline float3 perm289(float3 x) { return mod289(((x * 34.0) + 1.0) * x); }
inline float4 perm289(float4 x) { return mod289(((x * 34.0) + 1.0) * x); }

inline float fade(float t) { return t * t*t*(t*(t*6.0 - 15.0) + 10.0); }
inline float2 fade(float2 t) { return t * t*t*(t*(t*6.0 - 15.0) + 10.0); }
inline float3 fade(float3 t) { return t * t*t*(t*(t*6.0 - 15.0) + 10.0); }
inline float4 fade(float4 t) { return t * t*t*(t*(t*6.0 - 15.0) + 10.0); }

#define permutation permute

inline float4 taylorInvSqrt(float4 r) { return (float4)1.79284291400159 - r * 0.85373472095314; }

float genericNoise3D(float3 p)
{
	float3 a = floor(p);
	float3 d = p - a;
	d = d * d * (3.0 - 2.0 * d);

	float4 b = a.xxyy + float4(0.0, 1.0, 0.0, 1.0);
	float4 k1 = perm289(b.xyxy);
	float4 k2 = perm289(k1.xyxy + b.zzww);

	float4 c = k2 + a.zzzz;
	float4 k3 = perm289(c);
	float4 k4 = perm289(c + 1.0);

	float4 o1 = frac(k3 * (1.0 / 41.0));
	float4 o2 = frac(k4 * (1.0 / 41.0));

	float4 o3 = o2 * d.z + o1 * (1.0 - d.z);
	float2 o4 = o3.yw * d.x + o3.xz * (1.0 - d.x);

	return o4.y * d.y + o4.x * (1.0 - d.y);
}

float simplexNoiseFast3D(float3 x)
{

#define simplex_hash(n) (frac(sin(n) * 43758.5453))

	// The noise function returns a value in the range 0 to 1

	float3 p = floor(x);
	float3 f = frac(x);

	f = f * f * (3.0 - 2.0 * f);
	float n = p.x + p.y * 57.0 + 113.0 * p.z;

	return lerp
	(
		lerp
		(
			lerp(simplex_hash(n + 0.0), simplex_hash(n + 1.0), f.x),
			lerp(simplex_hash(n + 57.0), simplex_hash(n + 58.0), f.x),
			f.y
		),
		lerp
		(
			lerp(simplex_hash(n + 113.0), simplex_hash(n + 114.0), f.x),
			lerp(simplex_hash(n + 170.0), simplex_hash(n + 171.0), f.x),
			f.y
		),
		f.z
	);

#undef simplex_hash

}

#endif // _WEATHER_MAKER_NOISE_SHADER_INCLUDE_
