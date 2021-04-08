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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalRuby.WeatherMaker
{
    /// <summary>
    /// Volumetric cloud profile
    /// </summary>
    [CreateAssetMenu(fileName = "WeatherMakerCloudLayerVolumetricProfile", menuName = "WeatherMaker/Cloud Layer Volumetric Profile", order = 41)]
    public class WeatherMakerCloudVolumetricProfileScript : ScriptableObject
    {
        /// <summary>Texture for cloud noise shape (perlin, worley) - RGBA</summary>
        [Header("Clouds - noise")]
        [Tooltip("Texture for cloud noise shape (perlin, worley) - RGBA")]
        public Texture3D CloudNoiseShape;

        /// <summary>Texture for cloud noise detail (worley) - A</summary>
        [Tooltip("Texture for cloud noise detail (worley) - A")]
        public Texture3D CloudNoiseDetail;

        /// <summary>Texture for cloud noise curl (turbulence) - RGB (XYZ)</summary>
        [Tooltip("Texture for cloud noise curl (turbulence) - RGB (XYZ)")]
        public Texture2D CloudNoiseCurl;

        /// <summary>Cloud noise scale (shape, detail, curl, curl intensity)</summary>
        [Tooltip("Cloud noise scale (shape, detail, curl, curl intensity)")]
        public Vector4 CloudNoiseScale = new Vector4(0.65f, 1.3f, 0.4f, 0.2f);

        /// <summary>Cloud noise scalar, x = multiplier, y = adder, zw = reserved.</summary>
        [MinMaxSlider(0.01f, 4.0f, "Cloud noise scalar, x = multiplier, y = adder, zw = reserved.")]
        public RangeOfFloats CloudNoiseScalar = new RangeOfFloats(0.95f, 1.06f);

        /// <summary>Cloud noise detail power, controls how much the detail noise affects the clouds.</summary>
        [MinMaxSlider(0.01f, 1.0f, "Cloud noise detail power, controls how much the detail noise affects the clouds.")]
        public RangeOfFloats CloudNoiseDetailPower = new RangeOfFloats(0.35f, 0.42f);

        /// <summary>Cloud noise height power, controls how uniform noise is. Lower values produce more uniform noise at lower heights.</summary>
        [MinMaxSlider(0.0f, 1000.0f, "Cloud noise height power, controls how uniform noise is. Lower values produce more uniform noise at lower heights.")]
        public RangeOfFloats CloudHeightNoisePowerVolumetric = new RangeOfFloats(100.0f, 100.0f);

        /// <summary>Max optical depth multiplier, determines horizon fade and other sky blending effects</summary>
        [Header("Clouds - appearance")]
        [Tooltip("Max optical depth multiplier, determines horizon fade and other sky blending effects")]
        [Range(1.0f, 100.0f)]
        public float CloudOpticalDistanceMultiplier = 10.0f;

        /// <summary>Fades clouds at horizon/larger optical depths</summary>
        [Tooltip("Fades clouds at horizon/larger optical depths")]
        [Range(0.0f, 10.0f)]
        public float CloudHorizonFadeMultiplier = 1.0f;

        /// <summary>Offset the ray y direction from the horizon.</summary>
        [Tooltip("Offset the ray y direction from the horizon.")]
        [Range(-1.0f, 1.0f)]
        public float CloudRayOffset = 0.01f;

        /// <summary>Cloud max ray y value, ray y below this is culled.</summary>
        [Tooltip("Cloud max ray y value, ray y below this is culled.")]
        [Range(-1.0f, 1.0f)]
        public float CloudMinRayY = -1.0f;

        /// <summary>Min cloud ray multiplier</summary>
        [Tooltip("Min cloud ray multiplier")]
        [Range(1.0f, 10.0f)]
        public float CloudMinRayMarchMultiplier = 1.0f;

        /// <summary>Cloud ray march paramers. X: min step, Y: max step, Z: min to max step lerp (0-1)</summary>
        [Tooltip("Cloud ray march paramers. X: min step, Y: max step, Z: min to max step lerp (0-1)")]
        public Vector3 CloudRayMarchParamers = new Vector3(512.0f, 512.0f, 0.0f);

        /// <summary>Cloud shape animation/turbulence.</summary>
        [Header("Cloud animation/turbulence")]
        [Tooltip("Cloud shape animation/turbulence.")]
        public Vector3 CloudShapeAnimationVelocity = new Vector3(0.0f, -2.0f, 0.0f);

        /// <summary>Cloud detail animation/turbulence.</summary>
        [Tooltip("Cloud detail animation/turbulence.")]
        public Vector3 CloudDetailAnimationVelocity = new Vector3(0.0f, -1.3f, 0.0f);

        /// <summary>Cloud color.</summary>
        [Header("Clouds - colors")]
        [Tooltip("Cloud color.")]
        public Color CloudColor = Color.white;

        /// <summary>Cloud emission color, always emits this color regardless of lighting.</summary>
        [Tooltip("Cloud emission color, always emits this color regardless of lighting.")]
        public Color CloudEmissionColor = Color.clear;

        /// <summary>Cloud dir light gradient color, where center of gradient is sun at horizon, right is 'noon'.</summary>
        [Tooltip("Cloud dir light gradient color, where center of gradient is sun at horizon, right is 'noon'.")]
        public Gradient CloudDirLightGradientColor = new Gradient();
        internal Color CloudDirLightGradientColorColor;

        /// <summary>Cloud dir light multiplier</summary>
        [Header("Clouds - lights")]
        [Tooltip("Cloud dir light multiplier")]
        [Range(0.0f, 10.0f)]
        public float CloudDirLightMultiplier = 5.0f;

        /// <summary>Cloud light dither level, helps with night clouds banding</summary>
        [Tooltip("Cloud light dither level, helps with night clouds banding")]
        [Range(0.0f, 1.0f)]
        public float CloudLightDitherLevel = 0.0008f;

        /// <summary>Point/spot light multiplier</summary>
        [Tooltip("Point/spot light multiplier")]
        [Range(0.0f, 10.0f)]
        public float CloudPointSpotLightMultiplier = 1.0f;

        /// <summary>How much clouds absorb light, affects shadows in the clouds</summary>
        [Tooltip("How much clouds absorb light, affects shadows in the clouds")]
        [Range(0.0f, 64.0f)]
        public float CloudLightAbsorption = 4.0f;

        /// <summary>Henyey Greenstein Phase/Silver lining (x = forward, y = back, z = forward multiplier, w = back multiplier).</summary>
        [Tooltip("Henyey Greenstein Phase/Silver lining (x = forward, y = back, z = forward multiplier, w = back multiplier).")]
        public Vector4 CloudHenyeyGreensteinPhase = new Vector4(0.7f, -0.4f, 0.2f, 1.0f);

        /// <summary>Indirect directional light multiplier (indirect scattering)</summary>
        [Tooltip("Indirect directional light multiplier (indirect scattering)")]
        [Range(0.0f, 10.0f)]
        public float CloudDirLightIndirectMultiplier = 1.0f;

        /// <summary>Ambient ground intensity</summary>
        [Header("Clouds - ambient light")]
        [Tooltip("Ambient ground intensity")]
        [Range(0.0f, 100.0f)]
        public float CloudAmbientGroundIntensity = 6.0f;

        /// <summary>Ambient sky intensity, this is how much the ambient sky color from the day night cycle influences the cloud color</summary>
        [Tooltip("Ambient sky intensity, this is how much the ambient sky color from the day night cycle influences the cloud color")]
        [Range(0.0f, 100.0f)]
        public float CloudAmbientSkyIntensity = 16.0f;

        /// <summary>Sky background intensity, this is how much the actual sky pixel colors influence the cloud color</summary>
        [Tooltip("Sky background intensity, this is how much the actual sky pixel colors influence the cloud color")]
        [Range(0.0f, 100.0f)]
        public float CloudSkyIntensity = 1.0f;

        /// <summary>Increases ambient ground light towards higher cloud heights</summary>
        [Tooltip("Increases ambient ground light towards higher cloud heights")]
        [Range(0.0f, 1.0f)]
        public float CloudAmbientGroundHeightMultiplier = 1.0f;

        /// <summary>Increases ambient sky light towards lower cloud heights</summary>
        [Tooltip("Increases ambient sky light towards lower cloud heights")]
        [Range(0.0f, 1.0f)]
        public float CloudAmbientSkyHeightMultiplier = 1.0f;

        /// <summary>Stratus cloud gradient, controls cloud density over height (4 control points)</summary>
        [Header("Clouds - shape")]
        [Tooltip("Stratus cloud gradient, controls cloud density over height (4 control points)")]
        public Gradient CloudGradientStratus;
        internal Vector4 CloudGradientStratusVector;

        /// <summary>Stratocumulus cloud gradient, controls cloud density over height (4 control points)</summary>
        [Tooltip("Stratocumulus cloud gradient, controls cloud density over height (4 control points)")]
        public Gradient CloudGradientStratoCumulus;
        internal Vector4 CloudGradientStratoCumulusVector;

        /// <summary>Cumulus cloud gradient, controls cloud density over height (4 control points)</summary>
        [Tooltip("Cumulus cloud gradient, controls cloud density over height (4 control points)")]
        public Gradient CloudGradientCumulus;
        internal Vector4 CloudGradientCumulusVector;

        /// <summary>Cloud min noise smoothing value range</summary>
        [MinMaxSlider(0.0f, 1.0f, "Cloud min noise smoothing value range")]
        public RangeOfFloats CloudShapeNoiseMin = new RangeOfFloats(0.14f, 0.16f);

        /// <summary>Cloud max noise smoothing value range</summary>
        [MinMaxSlider(0.0f, 1.0f, "Cloud max noise smoothing value range")]
        public RangeOfFloats CloudShapeNoiseMax = new RangeOfFloats(0.35f, 0.37f);

        /// <summary>Cloud powder multiplier / dark edge multiplier, brightens up bumps/billows in higher clouds</summary>
        [MinMaxSlider(0.0f, 10.0f, "Cloud powder multiplier / dark edge multiplier, brightens up bumps/billows in higher clouds")]
        public RangeOfFloats CloudPowderMultiplier = new RangeOfFloats(3.7f, 4.3f);

        /// <summary>Fades bottom of clouds as value approaches 1</summary>
        [MinMaxSlider(0.0f, 1.0f, "Fades bottom of clouds as value approaches 1")]
        public RangeOfFloats CloudBottomFade = new RangeOfFloats(0.28f, 0.32f);

        /// <summary>Cloud cover, controls how many clouds / how thick the clouds are.</summary>
        [Header("Clouds - cover")]
        [MinMaxSlider(0.0f, 1.0f, "Cloud cover, controls how many clouds / how thick the clouds are.")]
        public RangeOfFloats CloudCover = new RangeOfFloats(0.35f, 0.4f);

        /// <summary>Secondary / connected cloud cover, this controls how much the weather map cover texture is used.</summary>
        [MinMaxSlider(0.0f, 1.0f, "Secondary / connected cloud cover, this controls how much the weather map cover texture is used.")]
        public RangeOfFloats CloudCoverSecondary = new RangeOfFloats(0.0f, 0.0f);

        /// <summary>Cloud type - 0 is lowest flattest type of cloud, 1 is largest and puffiest (cummulus)</summary>
        [MinMaxSlider(0.0f, 1.0f, "Cloud type - 0 is lowest flattest type of cloud, 1 is largest and puffiest (cummulus)")]
        public RangeOfFloats CloudType = new RangeOfFloats(0.4f, 0.6f);

        /// <summary>Secondary cloud type, this controls how much the weather map type texture is used.</summary>
        [MinMaxSlider(0.0f, 1.0f, "Secondary cloud type, this controls how much the weather map type texture is used.")]
        public RangeOfFloats CloudTypeSecondary = new RangeOfFloats(0.0f, 0.0f);

        /// <summary>Cloud density, controls how well formed the clouds are.</summary>
        [MinMaxSlider(0.0f, 1.0f, "Cloud density, controls how well formed the clouds are.")]
        public RangeOfFloats CloudDensity = new RangeOfFloats(0.95f, 1.0f);

        /// <summary>Allowed flat layers</summary>
        [Header("Clouds - flat layer allowance")]
        [WeatherMaker.EnumFlag("Allowed flat layers")]
        public WeatherMakerVolumetricCloudsFlatLayerMask FlatLayerMask = WeatherMakerVolumetricCloudsFlatLayerMask.Four;

        /// <summary>Dir light ray spread (0 - 1).</summary>
        [Header("Clouds - dir light rays")]
        [Tooltip("Dir light ray spread (0 - 1).")]
        [Range(0.0f, 1.0f)]
        public float CloudDirLightRaySpread = 0.65f;

        /// <summary>Increases the dir light ray brightness</summary>
        [Tooltip("Increases the dir light ray brightness")]
        [Range(0.0f, 10.0f)]
        public float CloudDirLightRayBrightness = 0.075f;

        /// <summary>Combined with each dir light ray march, this determines how much light is accumulated each step.</summary>
        [Tooltip("Combined with each dir light ray march, this determines how much light is accumulated each step.")]
        [Range(0.0f, 1000.0f)]
        public float CloudDirLightRayStepMultiplier = 21.0f;

        /// <summary>Determines light fall-off from start of dir light ray. Set to 1 for no fall-off.</summary>
        [Tooltip("Determines light fall-off from start of dir light ray. Set to 1 for no fall-off.")]
        [Range(0.5f, 1.0f)]
        public float CloudDirLightRayDecay = 0.97f;

        /// <summary>Dir light ray tint color. Alpha value determines tint intensity.</summary>
        [Tooltip("Dir light ray tint color. Alpha value determines tint intensity.")]
        public Color CloudDirLightRayTintColor = Color.white;

        /// <summary>
        /// Magic dither values for cloud rays
        /// </summary>
        public static readonly Vector4 CloudDirLightRayDitherMagic = new Vector4(2.34325f, 5.235345f, 1024.0f, 1024.0f);

        /// <summary>
        /// Progress for all internal lerp variables
        /// </summary>
        internal float lerpProgress;

        /// <summary>
        /// Convert gradient times to Vector4
        /// </summary>
        /// <param name="gradient">Gradient</param>
        /// <returns>Vector4</returns>
        public static Vector4 CloudHeightGradientToVector4(Gradient gradient)
        {
            GradientColorKey[] colorKeys = gradient.colorKeys;
            int keyCount = colorKeys.Length;
            Vector4 vec;
            if (keyCount > 0)
            {
                vec.x = colorKeys[0].time;
                if (keyCount > 1)
                {
                    vec.y = colorKeys[1].time;
                    if (keyCount > 2)
                    {
                        vec.z = colorKeys[2].time;
                        if (keyCount > 3)
                        {
                            vec.w = colorKeys[3].time;
                        }
                        else
                        {
                            vec.w = vec.z;
                        }
                    }
                    else
                    {
                        vec.z = vec.w = vec.y;
                    }
                }
                else
                {
                    vec.y = vec.z = vec.w = vec.x;
                }
            }
            else
            {
                vec.x = vec.y = vec.z = vec.w = 0.0f;
            }
            return vec;
        }
    }

    /// <summary>
    /// Flay layer mask that are allowed with the volumetric clouds
    /// </summary>
    [System.Flags]
    public enum WeatherMakerVolumetricCloudsFlatLayerMask
    {
        /// <summary>
        /// Layer one allowed
        /// </summary>
        One = 1,

        /// <summary>
        /// Layer two allowed
        /// </summary>
        Two = 2,

        /// <summary>
        /// Layer three allowed
        /// </summary>
        Three = 4,

        /// <summary>
        /// Layer four allowed
        /// </summary>
        Four = 8
    }
}
