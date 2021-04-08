﻿//
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

#ifndef __WEATHER_MAKER_SKY_BOX_SHADER__
#define __WEATHER_MAKER_SKY_BOX_SHADER__

#include "WeatherMakerLightShaderInclude.cginc"
#include "WeatherMakerAuroraShaderInclude.cginc"
#include "WeatherMakerSkyShaderInclude.cginc"
#include "WeatherMakerAtmosphereShaderInclude.cginc"

inline fixed4 SkyTexturedColor(fixed4 skyColor, fixed3 nightColor, fixed2 uv)
{
	fixed4 dayColor = tex2D(_DayTex, uv) * _WeatherMakerDayMultiplier;
	fixed4 dawnDuskColor = tex2D(_DawnDuskTex, uv);
	fixed4 dawnDuskColor2 = dawnDuskColor * _WeatherMakerDawnDuskMultiplier;
	dayColor += dawnDuskColor2;

	// hide night texture wherever dawn/dusk is opaque, reduce if clouds
	nightColor *= (1.0 - dawnDuskColor.a);

	// blend texture on top of sky
	fixed4 result = ((dayColor * dayColor.a) + (skyColor * (1.0 - dayColor.a)));

	// blend previous result on top of night
	return ((result * result.a) + (fixed4(nightColor, 1.0) * (1.0 - result.a)));
}

inline fixed4 SkyNonTexturedColor(fixed4 skyColor, fixed3 nightColor)
{
	return skyColor + fixed4(nightColor, 0.0);
}

fixed4 ComputeSkySphereColor(wm_volumetric_data i)
{
	//return tex2D(_NightTex, i.uv);

	fixed4 result = fixed4Zero;

	// uncomment next line to mirror sky along y = 0
	// i.rayDir.y = abs(i.rayDir.y);

	float3 origRay = normalize(i.rayDir.xyz);
	float3 rayDir = i.rayDir;

	// if Unity style sky, mirror sky, the ground looks bad
	//rayDir.y = lerp(rayDir.y, abs(rayDir.y), (WM_ENABLE_PROCEDURAL_TEXTURED_SKY || WM_ENABLE_PROCEDURAL_SKY));

	rayDir.y = (rayDir.y + 10.0 + _WeatherMakerSkyYOffset2D);
	rayDir = normalize(rayDir);
	fixed4 skyColor;
	fixed3 nightColor;
	fixed sunMoon;

	// use depth buffer and camera frustum to get an exact world position and take that distance from the camera
	float2 screenUV = i.projPos.xy / max(0.0001, i.projPos.w);
	float rawDepth = WM_SAMPLE_DEPTH(screenUV);
	float depth01 = Linear01Depth(rawDepth);
	float depth = LinearEyeDepth(rawDepth);
	float worldDistance01 = min(1.0, (depth / abs(normalize(i.viewPos).z)) * _ProjectionParams.w);
	float rayYFade = min(1.0, 2.0 * (1.0 - abs(origRay.y)));
	float sunSkyFadeLerp = lerp(1.0, sunSkyFade, _WeatherMakerSkyGroundColor.a);
	float depthFade = saturate(1.5 * ((depth01 >= 0.99999) + (((worldDistance01 - _WeatherMakerSkyFade.x) * _WeatherMakerSkyFade.y) * sunSkyFadeLerp * rayYFade)));

	//return fixed4(depth01.xxx, 1.0);
	//return fixed4(worldDistance01.rrr, 1.0);
	//return fixed4(rayDir, 1.0);

	UNITY_BRANCH
	if (depthFade > 0.00001)
	{
		UNITY_BRANCH
		if (WM_ENABLE_PROCEDURAL_TEXTURED_SKY || WM_ENABLE_PROCEDURAL_SKY || WM_ENABLE_PROCEDURAL_SKY_ATMOSPHERE || WM_ENABLE_PROCEDURAL_TEXTURED_SKY_ATMOSPHERE)
		{
			fixed4 skyColor;
			fixed groundAlpha = (_WeatherMakerSkyGroundColor.a * (origRay.y < 0.0));

			UNITY_BRANCH
			if (WM_ENABLE_PROCEDURAL_TEXTURED_SKY || WM_ENABLE_PROCEDURAL_SKY)
			{
				procedural_sky_info sky = CalculateScatteringCoefficients(_WeatherMakerSunDirectionUp, _WeatherMakerSunColor.rgb, 1.0, rayDir);
				procedural_sky_info sky2 = CalculateScatteringColor(_WeatherMakerSunDirectionUp, _WeatherMakerSunColor.rgb, _WeatherMakerSunVar1.x, rayDir, sky.inScatter, sky.outScatter);
				skyColor.rgb = sky2.skyColor.rgb;
				skyColor.rgb *= skyTintColor;
				skyColor.a = min(1.0, _NightDuskMultiplier * max(skyColor.r, max(skyColor.g, skyColor.b)));
			}
			else
			{
				skyColor.rgb = ComputeAtmosphericScatteringSkyColor(rayDir);
				skyColor.rgb *= skyTintColor;
				skyColor.a = min(1.0, _NightDuskMultiplier * max(skyColor.r, max(skyColor.g, skyColor.b)));
			}

			skyColor.rgb = lerp(skyColor.rgb, _WeatherMakerSkyGroundColor.rgb, groundAlpha);
			nightColor = lerp(GetNightColor(rayDir, i.uv, skyColor.a), _WeatherMakerSkyGroundColor.rgb, groundAlpha);

			UNITY_BRANCH
			if (WM_ENABLE_PROCEDURAL_TEXTURED_SKY)
			{
				result = SkyTexturedColor(skyColor, nightColor, i.uv);
			}
			else
			{
				result = SkyNonTexturedColor(skyColor, nightColor);
			}
		}
		else // WM_ENABLE_TEXTURED_SKY
		{
			nightColor = GetNightColor(rayDir, i.uv, 0.0);
			fixed4 dayColor = tex2D(_DayTex, i.uv) * _WeatherMakerDayMultiplier;
			fixed4 dawnDuskColor = (tex2D(_DawnDuskTex, i.uv) * _WeatherMakerDawnDuskMultiplier);
			result = (dayColor + dawnDuskColor + fixed4(nightColor, 0.0));
		}
		ApplyDither(result.rgb, origRay, _WeatherMakerSkyDitherLevel);
		result.rgb += _WeatherMakerSkyAddColor;
		result.a = depthFade;
	}
	return result;
}

#endif // __WEATHER_MAKER_SKY_BOX_SHADER__