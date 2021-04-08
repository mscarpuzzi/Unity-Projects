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
    /// Types of clouds
    /// </summary>
    public enum WeatherMakerCloudType
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,

        /// <summary>
        /// Light
        /// </summary>
        Light = 10,

        /// <summary>
        /// LightScattered
        /// </summary>
        LightScattered = 15,

        /// <summary>
        /// LightMedium
        /// </summary>
        LightMedium = 20,

        /// <summary>
        /// LightMediumScattered
        /// </summary>
        LightMediumScattered = 25,

        /// <summary>
        /// Medium
        /// </summary>
        Medium = 30,

        /// <summary>
        /// MediumScattered
        /// </summary>
        MediumScattered = 35,

        /// <summary>
        /// MediumHeavy
        /// </summary>
        MediumHeavy = 40,

        /// <summary>
        /// MediumHeavyScattered
        /// </summary>
        MediumHeavyScattered = 45,

        /// <summary>
        /// HeavyDark
        /// </summary>
        HeavyDark = 50,

        /// <summary>
        /// HeavyScattered
        /// </summary>
        HeavyScattered = 55,

        /// <summary>
        /// HeavyBright
        /// </summary>
        HeavyBright = 60,

        /// <summary>
        /// Storm
        /// </summary>
        Storm = 70,

        /// <summary>
        /// Custom
        /// </summary>
        Custom = 250,

        /// <summary>
        /// Overcast
        /// </summary>
        Overcast = 48
    }

    /// <summary>
    /// Weather maker cloud profile script, contains flat and volumetric layers
    /// </summary>
    [CreateAssetMenu(fileName = "WeatherMakerCloudProfile", menuName = "WeatherMaker/Cloud Profile", order = 40)]
    [System.Serializable]
    public class WeatherMakerCloudProfileScript : ScriptableObject
    {
        /// <summary>The first, and lowest cloud layer, null for none</summary>
        [Header("Layers")]
        [Tooltip("The first, and lowest cloud layer, null for none")]
        public WeatherMakerCloudLayerProfileScript CloudLayer1;

        /// <summary>The second, and second lowest cloud layer, null for none</summary>
        [Tooltip("The second, and second lowest cloud layer, null for none")]
        public WeatherMakerCloudLayerProfileScript CloudLayer2;

        /// <summary>The third, and third lowest cloud layer, null for none</summary>
        [Tooltip("The third, and third lowest cloud layer, null for none")]
        public WeatherMakerCloudLayerProfileScript CloudLayer3;

        /// <summary>The fourth, and highest cloud layer, null for none</summary>
        [Tooltip("The fourth, and highest cloud layer, null for none")]
        public WeatherMakerCloudLayerProfileScript CloudLayer4;

        /// <summary>Allow a single layer of volumetric clouds. In the future, more volumetric layers might be supported</summary>
        [Tooltip("Allow a single layer of volumetric clouds. In the future, more volumetric layers might be supported")]
        public WeatherMakerCloudVolumetricProfileScript CloudLayerVolumetric1;

        /// <summary>How much to multiply directional light intensities by when clouds are showing. Ignored for volumetric clouds.</summary>
        [Header("Lighting - intensity")]
        [Tooltip("How much to multiply directional light intensities by when clouds are showing. Ignored for volumetric clouds.")]
        [Range(0.0f, 1.0f)]
        public float DirectionalLightIntensityMultiplier = 1.0f;

        /// <summary>How much the clouds reduce directional light scattering. Affects fog and other volumetric effects.</summary>
        [Tooltip("How much the clouds reduce directional light scattering. Affects fog and other volumetric effects.")]
        [Range(0.0f, 1.0f)]
        public float DirectionalLightScatterMultiplier = 1.0f;

        /// <summary>How much clouds affect directional light intensity, lower values ensure no reduction. Ignored for volumetric clouds.</summary>
        [Tooltip("How much clouds affect directional light intensity, lower values ensure no reduction. Ignored for volumetric clouds.")]
        [Range(0.0f, 3.0f)]
        public float CloudLightStrength = 1.0f;

        /// <summary>Set a custom weather map texture, bypassing the auto-generated weather map</summary>
        [Header("Weather map (volumetric only)")]
        [Tooltip("Set a custom weather map texture, bypassing the auto-generated weather map")]
        public Texture WeatherMapRenderTextureOverride;

        /// <summary>Set a custom weather map texture mask, this will mask out all areas of the weather map based on lower alpha values.</summary>
        [Tooltip("Set a custom weather map texture mask, this will mask out all areas of the weather map based on lower alpha values.")]
        public Texture WeatherMapRenderTextureMask;

        /// <summary>Velocity of weather map mask in uv coordinates (0 - 1)</summary>
        [Tooltip("Velocity of weather map mask in uv coordinates (0 - 1)")]
        public Vector2 WeatherMapRenderTextureMaskVelocity;

        /// <summary>Offset of weather map mask (0 - 1). Velocity is applied automatically but you can set manually as well.</summary>
        [Tooltip("Offset of weather map mask (0 - 1). Velocity is applied automatically but you can set manually as well.")]
        public Vector2 WeatherMapRenderTextureMaskOffset;

        /// <summary>Clamp for weather map mask offset to ensure that it does not go too far out of bounds.</summary>
        [Tooltip("Clamp for weather map mask offset to ensure that it does not go too far out of bounds.")]
        public Vector2 WeatherMapRenderTextureMaskOffsetClamp = new Vector2(-1.1f, 1.1f);

        /// <summary>Weather map scale, x,y = noise generation multiplier, z = world scale.</summary>
        [Tooltip("Weather map scale, x,y = noise generation multiplier, z = world scale.")]
        public Vector3 WeatherMapScale = new Vector3(1.0f, 1.0f, 0.00001f);

        /// <summary>Weather map cloud coverage velocity, xy units per second, z change per second</summary>
        [Tooltip("Weather map cloud coverage velocity, xy units per second, z change per second")]
        public Vector3 WeatherMapCloudCoverageVelocity = new Vector3(11.0f, 15.0f, 0.0f);

        /// <summary>Scale of cloud coverage. Higher values produce smaller clouds.</summary>
        [MinMaxSlider(0.01f, 100.0f, "Scale of cloud coverage. Higher values produce smaller clouds.")]
        public RangeOfFloats WeatherMapCloudCoverageScale = new RangeOfFloats(4.0f, 16.0f);

        /// <summary>Rotation of cloud coverage. Rotates coverage map around center of weather map.</summary>
        [MinMaxSlider(-360.0f, 360.0f, "Rotation of cloud coverage. Rotates coverage map around center of weather map.")]
        public RangeOfFloats WeatherMapCloudCoverageRotation;

        /// <summary>Cloud coverage adder. Higher values create more cloud coverage.</summary>
        [MinMaxSlider(-1.0f, 1.0f, "Cloud coverage adder. Higher values create more cloud coverage.")]
        public RangeOfFloats WeatherMapCloudCoverageAdder = new RangeOfFloats { Minimum = 0.0f, Maximum = 0.0f };

        /// <summary>Cloud coverage power. Higher values create more firm cloud coverage edges.</summary>
        [MinMaxSlider(0.0f, 16.0f, "Cloud coverage power. Higher values create more firm cloud coverage edges.")]
        public RangeOfFloats WeatherMapCloudCoveragePower = new RangeOfFloats { Minimum = 1.0f, Maximum = 1.0f };

        /// <summary>Weather map cloud type velocity, xy units per second, z change per second</summary>
        [Tooltip("Weather map cloud type velocity, xy units per second, z change per second")]
        public Vector3 WeatherMapCloudTypeVelocity = new Vector3(17.0f, 10.0f, 0.0f);

        /// <summary>Scale of cloud types. Higher values produce more jagged clouds.</summary>
        [MinMaxSlider(0.01f, 100.0f, "Scale of cloud types. Higher values produce more jagged clouds.")]
        public RangeOfFloats WeatherMapCloudTypeScale = new RangeOfFloats(2.0f, 8.0f);

        /// <summary>Rotation of cloud type. Rotates cloud type map around center of weather map.</summary>
        [MinMaxSlider(-360.0f, 360.0f, "Rotation of cloud type. Rotates cloud type map around center of weather map.")]
        public RangeOfFloats WeatherMapCloudTypeRotation;

        /// <summary>Cloud type adder. Higher values create more cloud type.</summary>
        [MinMaxSlider(-1.0f, 1.0f, "Cloud type adder. Higher values create more cloud type.")]
        public RangeOfFloats WeatherMapCloudTypeAdder = new RangeOfFloats { Minimum = 0.0f, Maximum = 0.0f };

        /// <summary>Cloud type power. Higher values create more firm cloud type edges.</summary>
        [MinMaxSlider(0.0f, 16.0f, "Cloud type power. Higher values create more firm cloud type edges.")]
        public RangeOfFloats WeatherMapCloudTypePower = new RangeOfFloats { Minimum = 1.0f, Maximum = 1.0f };

        /// <summary>Cloud height.</summary>
        [Header("Planet (volumetric only)")]
        [SingleLine("Cloud height.")]
        public RangeOfFloats CloudHeight = new RangeOfFloats { Minimum = 1500, Maximum = 2000 };

        /// <summary>Cloud height top - clouds extend from CloudHeight to this value.</summary>
        [SingleLine("Cloud height top - clouds extend from CloudHeight to this value.")]
        public RangeOfFloats CloudHeightTop = new RangeOfFloats { Minimum = 4000, Maximum = 5000 };

        /// <summary>Planet radius for sphere cloud mapping. 1200000.0 seems to work well.</summary>
        [Tooltip("Planet radius for sphere cloud mapping. 1200000.0 seems to work well.")]
        public float CloudPlanetRadius = 1200000.0f;

        private const float scaleReducer = 0.1f;

        /// <summary>
        /// Checks whether clouds are enabled
        /// </summary>
        public bool CloudsEnabled { get; private set; }

        /// <summary>
        /// Sum of cloud cover, max of 1
        /// </summary>
        public float CloudCoverTotal { get; private set; }

        /// <summary>
        /// Sum of cloud density, max of 1
        /// </summary>
        public float CloudDensityTotal { get; private set; }

        /// <summary>
        /// Sum of cloud light absorption, max of 1
        /// </summary>
        public float CloudLightAbsorptionTotal { get; private set; }

        /// <summary>
        /// Aurora profile
        /// </summary>
        public WeatherMakerAuroraProfileScript AuroraProfile { get; set; }

        /// <summary>
        /// Atmosphere profile
        /// </summary>
        public WeatherMakerAtmosphereProfileScript AtmosphereProfile { get; set; }

        /// <summary>
        /// Directional light block
        /// </summary>
        public float CloudDirectionalLightDirectBlock { get; private set; }

        private Vector3 cloudNoiseVelocityAccum1;
        private Vector3 cloudNoiseVelocityAccum2;
        private Vector3 cloudNoiseVelocityAccum3;
        private Vector3 cloudNoiseVelocityAccum4;

        private Vector3 velocityAccumCoverage;
        private Vector3 velocityAccumType;
        internal bool isAnimating;

        //unused currently
        //internal Vector3 cloudCoverageOffset;
        //internal Vector3 cloudTypeOffset;

        private readonly WeatherMakerShaderPropertiesScript shaderProps = new WeatherMakerShaderPropertiesScript();

        private void SetShaderVolumetricCloudShaderProperties(WeatherMakerShaderPropertiesScript props, Texture weatherMap, WeatherMakerCelestialObjectScript sun, Camera camera)
        {
            props.SetTexture(WMS._WeatherMakerWeatherMapTexture, weatherMap);
            props.SetVector(WMS._WeatherMakerWeatherMapScale, new Vector4(WeatherMapScale.x, WeatherMapScale.y, WeatherMapScale.z, 1.0f / WeatherMapScale.z));
            props.SetFloat(WMS._CloudCoverVolumetric, CloudLayerVolumetric1.CloudCover.LastValue);
            props.SetFloat(WMS._CloudCoverSecondaryVolumetric, CloudLayerVolumetric1.CloudCoverSecondary.LastValue);
            props.SetFloat(WMS._CloudTypeVolumetric, CloudLayerVolumetric1.CloudType.LastValue);
            props.SetFloat(WMS._CloudTypeSecondaryVolumetric, CloudLayerVolumetric1.CloudTypeSecondary.LastValue);
            props.SetFloat(WMS._CloudDensityVolumetric, CloudLayerVolumetric1.CloudDensity.LastValue);
            props.SetFloat(WMS._CloudHeightNoisePowerVolumetric, CloudLayerVolumetric1.CloudHeightNoisePowerVolumetric.LastValue);
            props.SetInt(WMS._CloudDirLightRaySampleCount, WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudDirLightRaySampleCount);
            props.SetFloat(WMS._CloudPlanetRadiusVolumetric, CloudPlanetRadius);
            props.SetFloat(WMS._CloudNoiseScalarVolumetric, CloudLayerVolumetric1.CloudNoiseScalar.LastValue * WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudNoiseMultiplier);
            props.SetTexture(WMS._CloudNoiseShapeVolumetric, CloudLayerVolumetric1.CloudNoiseShape);
            props.SetTexture(WMS._CloudNoiseDetailVolumetric, CloudLayerVolumetric1.CloudNoiseDetail);
            props.SetTexture(WMS._CloudNoiseCurlVolumetric, CloudLayerVolumetric1.CloudNoiseCurl);
            props.SetVector(WMS._CloudShapeAnimationVelocity, CloudLayerVolumetric1.CloudShapeAnimationVelocity);
            props.SetVector(WMS._CloudDetailAnimationVelocity, CloudLayerVolumetric1.CloudDetailAnimationVelocity);
            props.SetVector(WMS._CloudNoiseScaleVolumetric, CloudLayerVolumetric1.CloudNoiseScale);
            props.SetFloat(WMS._CloudNoiseDetailPowerVolumetric, CloudLayerVolumetric1.CloudNoiseDetailPower.LastValue);
            props.SetFloat(WMS._CloudShapeNoiseMinVolumetric, CloudLayerVolumetric1.CloudShapeNoiseMin.LastValue);
            props.SetFloat(WMS._CloudShapeNoiseMaxVolumetric, CloudLayerVolumetric1.CloudShapeNoiseMax.LastValue);
            props.SetFloat(WMS._CloudBottomFadeVolumetric, CloudLayerVolumetric1.CloudBottomFade.LastValue);
            props.SetFloat(WMS._WeatherMakerDirectionalLightScatterMultiplier, DirectionalLightScatterMultiplier);

            // lower cloud level sphere
            // assign global shader so shadow map can see them
            props.SetFloat(WMS._CloudStartVolumetric, CloudHeight.LastValue);
            props.SetFloat(WMS._CloudStartSquaredVolumetric, CloudHeight.LastValue * CloudHeight.LastValue);
            props.SetFloat(WMS._CloudPlanetStartVolumetric, CloudHeight.LastValue + CloudPlanetRadius);
            props.SetFloat(WMS._CloudPlanetStartSquaredVolumetric, Mathf.Pow(CloudHeight.LastValue + CloudPlanetRadius, 2.0f));

            // height of top minus bottom cloud layer
            float height = CloudHeightTop.LastValue - CloudHeight.LastValue;
            props.SetFloat(WMS._CloudHeightVolumetric, height);
            props.SetFloat(WMS._CloudHeightInverseVolumetric, 1.0f / height);
            height *= height;
            props.SetFloat(WMS._CloudHeightSquaredVolumetric, height);
            props.SetFloat(WMS._CloudHeightSquaredInverseVolumetric, 1.0f / height);

            // upper cloud level sphere
            props.SetFloat(WMS._CloudEndVolumetric, CloudHeightTop.LastValue);
            height = CloudHeightTop.LastValue * CloudHeightTop.LastValue;
            props.SetFloat(WMS._CloudEndSquaredVolumetric, height);
            props.SetFloat(WMS._CloudEndSquaredInverseVolumetric, 1.0f / height);
            props.SetFloat(WMS._CloudPlanetEndVolumetric, CloudHeightTop.LastValue + CloudPlanetRadius);
            props.SetFloat(WMS._CloudPlanetEndSquaredVolumetric, Mathf.Pow(CloudHeightTop.LastValue + CloudPlanetRadius, 2.0f));

            props.SetFloat(WMS._CloudPlanetRadiusNegativeVolumetric, -CloudPlanetRadius);
            props.SetFloat(WMS._CloudPlanetRadiusSquaredVolumetric, CloudPlanetRadius * CloudPlanetRadius);

            props.SetVector(WMS._CloudHenyeyGreensteinPhaseVolumetric, CloudLayerVolumetric1.CloudHenyeyGreensteinPhase);
            props.SetFloat(WMS._CloudRayOffsetVolumetric, CloudLayerVolumetric1.CloudRayOffset +
                (WeatherMakerFullScreenCloudsScript.Instance == null ? 0.0f : WeatherMakerFullScreenCloudsScript.Instance.CloudRayOffset));
            props.SetFloat(WMS._CloudMinRayYVolumetric, CloudLayerVolumetric1.CloudMinRayY);

            props.SetVector(WMS._CloudGradientStratus, CloudLayerVolumetric1.CloudGradientStratusVector);
            props.SetVector(WMS._CloudGradientStratoCumulus, CloudLayerVolumetric1.CloudGradientStratoCumulusVector);
            props.SetVector(WMS._CloudGradientCumulus, CloudLayerVolumetric1.CloudGradientCumulusVector);

            props.SetVector(WMS._CloudNoiseSampleCountVolumetric, WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudSampleCount.ToVector2());
            props.SetInt(WMS._CloudRaymarchSkipThreshold, WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudRaymarchSkipThreshold);
            props.SetFloat(WMS._CloudRaymarchMaybeInCloudStepMultiplier, WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudRaymarchMaybeInCloudStepMultiplier);
            props.SetFloat(WMS._CloudRaymarchInCloudStepMultiplier, WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudRaymarchInCloudStepMultiplier);
            props.SetFloat(WMS._CloudRaymarchSkipMultiplier, WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudRaymarchSkipMultiplier);
            props.SetInt(WMS._CloudRaymarchSkipMultiplierMaxCount, WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudRaymarchSkipMultiplierMaxCount);
            props.SetVector(WMS._CloudNoiseLodVolumetric, WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudLod.ToVector2());
            props.SetColor(WMS._CloudColorVolumetric, CloudLayerVolumetric1.CloudColor);
            props.SetColor(WMS._CloudDirColorVolumetric, CloudLayerVolumetric1.CloudDirLightGradientColorColor);
            props.SetColor(WMS._CloudEmissionColorVolumetric, CloudLayerVolumetric1.CloudEmissionColor);
            props.SetFloat(WMS._CloudDirLightMultiplierVolumetric, CloudLayerVolumetric1.CloudDirLightMultiplier);
            props.SetFloat(WMS._CloudLightDitherLevel, CloudLayerVolumetric1.CloudLightDitherLevel);
            props.SetFloat(WMS._CloudPointSpotLightMultiplierVolumetric, CloudLayerVolumetric1.CloudPointSpotLightMultiplier);
            props.SetFloat(WMS._CloudAmbientGroundIntensityVolumetric, CloudLayerVolumetric1.CloudAmbientGroundIntensity);
            props.SetFloat(WMS._CloudAmbientSkyIntensityVolumetric, CloudLayerVolumetric1.CloudAmbientSkyIntensity);
            float backgroundSkyStyle = (WeatherMakerSkySphereScript.Instance == null || WeatherMakerSkySphereScript.Instance.SkySphereProfile == null ||
                WeatherMakerSkySphereScript.Instance.SkySphereProfile.SkyMode == WeatherMakeSkyMode.ProceduralUnityStyle ||
                WeatherMakerSkySphereScript.Instance.SkySphereProfile.SkyMode == WeatherMakeSkyMode.ProceduralTexturedUnityStyle ? 0.0f : 1.0f);
            props.SetVector(WMS._CloudBackgroundSkyIntensityVolumetric, new Vector4(CloudLayerVolumetric1.CloudSkyIntensity, backgroundSkyStyle, 0.0f, 0.0f));
            props.SetFloat(WMS._CloudAmbientSkyHeightMultiplierVolumetric, CloudLayerVolumetric1.CloudAmbientSkyHeightMultiplier);
            props.SetFloat(WMS._CloudAmbientGroundHeightMultiplierVolumetric, CloudLayerVolumetric1.CloudAmbientGroundHeightMultiplier);
            props.SetFloat(WMS._CloudLightAbsorptionVolumetric, CloudLayerVolumetric1.CloudLightAbsorption);
            props.SetFloat(WMS._CloudDirLightIndirectMultiplierVolumetric, CloudLayerVolumetric1.CloudDirLightIndirectMultiplier);
            props.SetFloat(WMS._CloudPowderMultiplierVolumetric, CloudLayerVolumetric1.CloudPowderMultiplier.LastValue);
            props.SetFloat(WMS._CloudOpticalDistanceMultiplierVolumetric, CloudLayerVolumetric1.CloudOpticalDistanceMultiplier);
            props.SetFloat(WMS._CloudHorizonFadeMultiplierVolumetric, CloudLayerVolumetric1.CloudHorizonFadeMultiplier);
            props.SetFloat(WMS._CloudDirLightSampleCount, WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudDirLightSampleCount);
            props.SetFloat(WMS._CloudLightStepMultiplierVolumetric, WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudDirLightStepMultiplier);
            props.SetFloat(WMS._CloudMaxRayLengthMultiplierVolumetric, WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudMaxRayLengthMultiplier);
            props.SetFloat(WMS._CloudRayDitherVolumetric, WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudRayDither);
            props.SetFloat(WMS._CloudRaymarchMultiplierVolumetric, Mathf.Max(CloudLayerVolumetric1.CloudMinRayMarchMultiplier, WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudRaymarchMultiplier));
            props.SetVector(WMS._CloudRayMarchParameters, CloudLayerVolumetric1.CloudRayMarchParamers);

            // sample details for dir light ray march if max lod is small
            props.SetFloat(WMS._CloudDirLightLod, WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudDirLightLod);
            props.SetInt(WMS._CloudRaymarchSampleDetailsForDirLight, WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudDirLightSampleDetails ? 1 : 0);

            // flat clouds
            float cloudCover1 = 0.0f;
            float cloudCover2 = 0.0f;
            float cloudCover3 = 0.0f;
            float cloudCover4 = 0.0f;
            if (isAnimating || CloudLayerVolumetric1 == null || CloudLayerVolumetric1.CloudCover.Maximum == 0.0f || !WeatherMakerScript.Instance.PerformanceProfile.EnableVolumetricClouds ||
                (CloudLayerVolumetric1.FlatLayerMask & WeatherMakerVolumetricCloudsFlatLayerMask.One) == WeatherMakerVolumetricCloudsFlatLayerMask.One)
            {
                cloudCover1 = CloudLayer1.CloudCover;
            }
            if (isAnimating || CloudLayerVolumetric1 == null || CloudLayerVolumetric1.CloudCover.Maximum == 0.0f || !WeatherMakerScript.Instance.PerformanceProfile.EnableVolumetricClouds ||
                (CloudLayerVolumetric1.FlatLayerMask & WeatherMakerVolumetricCloudsFlatLayerMask.Two) == WeatherMakerVolumetricCloudsFlatLayerMask.Two)
            {
                cloudCover2 = CloudLayer2.CloudCover;
            }
            if (isAnimating || CloudLayerVolumetric1 == null || CloudLayerVolumetric1.CloudCover.Maximum == 0.0f || !WeatherMakerScript.Instance.PerformanceProfile.EnableVolumetricClouds ||
                (CloudLayerVolumetric1.FlatLayerMask & WeatherMakerVolumetricCloudsFlatLayerMask.Three) == WeatherMakerVolumetricCloudsFlatLayerMask.Three)
            {
                cloudCover3 = CloudLayer3.CloudCover;
            }
            if (isAnimating || CloudLayerVolumetric1 == null || CloudLayerVolumetric1.CloudCover.Maximum == 0.0f || !WeatherMakerScript.Instance.PerformanceProfile.EnableVolumetricClouds ||
                (CloudLayerVolumetric1.FlatLayerMask & WeatherMakerVolumetricCloudsFlatLayerMask.Four) == WeatherMakerVolumetricCloudsFlatLayerMask.Four)
            {
                cloudCover4 = CloudLayer4.CloudCover;
            }
            props.SetTexture(WMS._CloudNoise1, CloudLayer1.CloudNoise ?? Texture2D.blackTexture);
            props.SetTexture(WMS._CloudNoise2, CloudLayer2.CloudNoise ?? Texture2D.blackTexture);
            props.SetTexture(WMS._CloudNoise3, CloudLayer3.CloudNoise ?? Texture2D.blackTexture);
            props.SetTexture(WMS._CloudNoise4, CloudLayer4.CloudNoise ?? Texture2D.blackTexture);
            props.SetVectorArray(WMS._CloudNoiseScale, CloudLayer1.CloudNoiseScale * scaleReducer, CloudLayer2.CloudNoiseScale * scaleReducer, CloudLayer3.CloudNoiseScale * scaleReducer, CloudLayer4.CloudNoiseScale * scaleReducer);
            props.SetVectorArray(WMS._CloudNoiseMultiplier, CloudLayer1.CloudNoiseMultiplier, CloudLayer2.CloudNoiseMultiplier, CloudLayer3.CloudNoiseMultiplier, CloudLayer4.CloudNoiseMultiplier);
            props.SetVectorArray(WMS._CloudNoiseVelocity, cloudNoiseVelocityAccum1, cloudNoiseVelocityAccum2, cloudNoiseVelocityAccum3, cloudNoiseVelocityAccum4);
            props.SetFloatArrayRotation(WMS._CloudNoiseRotation, CloudLayer1.CloudNoiseRotation.LastValue, CloudLayer2.CloudNoiseRotation.LastValue, CloudLayer3.CloudNoiseRotation.LastValue, CloudLayer4.CloudNoiseRotation.LastValue);
            props.SetFloatArray(WMS._CloudHeight, CloudLayer1.CloudHeight, CloudLayer2.CloudHeight, CloudLayer3.CloudHeight, CloudLayer4.CloudHeight);
            props.SetFloatArray(WMS._CloudCover, cloudCover1, cloudCover2, cloudCover3, cloudCover4);
            props.SetFloatArray(WMS._CloudDensity, CloudLayer1.CloudDensity, CloudLayer2.CloudDensity, CloudLayer3.CloudDensity, CloudLayer4.CloudDensity);
            props.SetFloatArray(WMS._CloudSharpness, CloudLayer1.CloudSharpness, CloudLayer2.CloudSharpness, CloudLayer3.CloudSharpness, CloudLayer4.CloudSharpness);
            props.SetFloatArray(WMS._CloudRayOffset, CloudLayer1.CloudRayOffset, CloudLayer2.CloudRayOffset, CloudLayer3.CloudRayOffset, CloudLayer4.CloudRayOffset);
            props.SetColorArray(WMS._CloudColor,
                CloudLayer1.CloudColor * sun.GetGradientColor(CloudLayer1.CloudGradientColor),
                CloudLayer2.CloudColor * sun.GetGradientColor(CloudLayer2.CloudGradientColor),
                CloudLayer3.CloudColor * sun.GetGradientColor(CloudLayer3.CloudGradientColor),
                CloudLayer4.CloudColor * sun.GetGradientColor(CloudLayer4.CloudGradientColor));
            props.SetColorArray(WMS._CloudEmissionColor,
                CloudLayer1.CloudEmissionColor,
                CloudLayer2.CloudEmissionColor,
                CloudLayer3.CloudEmissionColor,
                CloudLayer4.CloudEmissionColor);
            props.SetFloatArray(WMS._CloudAmbientMultiplier,
                CloudLayer1.CloudAmbientMultiplier,
                CloudLayer2.CloudAmbientMultiplier,
                CloudLayer3.CloudAmbientMultiplier,
                CloudLayer4.CloudAmbientMultiplier);
            props.SetFloatArray(WMS._CloudScatterMultiplier,
                CloudLayer1.CloudScatterMultiplier,
                CloudLayer2.CloudScatterMultiplier,
                CloudLayer3.CloudScatterMultiplier,
                CloudLayer4.CloudScatterMultiplier);
            /*
            if (CloudLayer1.CloudNoiseMask != null || CloudLayer2.CloudNoiseMask != null || CloudLayer3.CloudNoiseMask != null || CloudLayer4.CloudNoiseMask != null)
            {
                cloudMaterial.SetTexture(WMS._CloudNoiseMask1, CloudLayer1.CloudNoiseMask ?? Texture2D.whiteTexture);
                cloudMaterial.SetTexture(WMS._CloudNoiseMask2, CloudLayer2.CloudNoiseMask ?? Texture2D.whiteTexture);
                cloudMaterial.SetTexture(WMS._CloudNoiseMask3, CloudLayer3.CloudNoiseMask ?? Texture2D.whiteTexture);
                cloudMaterial.SetTexture(WMS._CloudNoiseMask4, CloudLayer4.CloudNoiseMask ?? Texture2D.whiteTexture);
                WeatherMakerShaderIds.SetVectorArray(cloudMaterial, "_CloudNoiseMaskOffset",
                    CloudLayer1.CloudNoiseMaskOffset,
                    CloudLayer2.CloudNoiseMaskOffset,
                    CloudLayer3.CloudNoiseMaskOffset,
                    CloudLayer4.CloudNoiseMaskOffset);
                WeatherMakerShaderIds.SetVectorArray(cloudMaterial, "_CloudNoiseMaskVelocity", cloudNoiseMaskVelocityAccum1, cloudNoiseMaskVelocityAccum2, cloudNoiseMaskVelocityAccum3, cloudNoiseMaskVelocityAccum4);
                WeatherMakerShaderIds.SetFloatArray(cloudMaterial, "_CloudNoiseMaskScale",
                    (CloudLayer1.CloudNoiseMask == null ? 0.0f : CloudLayer1.CloudNoiseMaskScale * scaleReducer),
                    (CloudLayer2.CloudNoiseMask == null ? 0.0f : CloudLayer2.CloudNoiseMaskScale * scaleReducer),
                    (CloudLayer3.CloudNoiseMask == null ? 0.0f : CloudLayer3.CloudNoiseMaskScale * scaleReducer),
                    (CloudLayer4.CloudNoiseMask == null ? 0.0f : CloudLayer4.CloudNoiseMaskScale * scaleReducer));
                WeatherMakerShaderIds.SetFloatArrayRotation(cloudMaterial, "_CloudNoiseMaskRotation",
                    CloudLayer1.CloudNoiseMaskRotation.LastValue,
                    CloudLayer2.CloudNoiseMaskRotation.LastValue,
                    CloudLayer3.CloudNoiseMaskRotation.LastValue,
                    CloudLayer4.CloudNoiseMaskRotation.LastValue);
            }
            */
            props.SetFloatArray(WMS._CloudLightAbsorption,
                CloudLayer1.CloudLightAbsorption,
                CloudLayer2.CloudLightAbsorption,
                CloudLayer3.CloudLightAbsorption,
                CloudLayer4.CloudLightAbsorption);

            if (WeatherMakerCommandBufferManagerScript.Instance != null)
            {
                WeatherMakerCommandBufferManagerScript.Instance.UpdateShaderPropertiesForCamera(props, camera);
            }
        }

        /// <summary>
        /// Set shader cloud values
        /// </summary>
        /// <param name="cloudMaterial">Cloud material</param>
        /// <param name="cloudProbe">Cloud probe shader</param>
        /// <param name="camera">Camera</param>
        /// <param name="weatherMap">Weather map texture</param>
        public void SetShaderCloudParameters(Material cloudMaterial, ComputeShader cloudProbe, Camera camera, Texture weatherMap)
        {
            if (WeatherMakerScript.Instance == null ||
                WeatherMakerScript.Instance.PerformanceProfile == null ||
                WeatherMakerDayNightCycleManagerScript.Instance == null ||
                WeatherMakerLightManagerScript.Instance == null)
            {
                return;
            }

            WeatherMakerCelestialObjectScript sun = (camera == null || !camera.orthographic ? WeatherMakerLightManagerScript.Instance.SunPerspective : WeatherMakerLightManagerScript.Instance.SunOrthographic);
            if (sun == null)
            {
                return;
            }

            if (!isAnimating)
            {
                CloudLayerVolumetric1.CloudDirLightGradientColorColor = sun.GetGradientColor(CloudLayerVolumetric1.CloudDirLightGradientColor);
                CloudLayerVolumetric1.CloudGradientStratusVector = WeatherMakerCloudVolumetricProfileScript.CloudHeightGradientToVector4(CloudLayerVolumetric1.CloudGradientStratus);
                CloudLayerVolumetric1.CloudGradientStratoCumulusVector = WeatherMakerCloudVolumetricProfileScript.CloudHeightGradientToVector4(CloudLayerVolumetric1.CloudGradientStratoCumulus);
                CloudLayerVolumetric1.CloudGradientCumulusVector = WeatherMakerCloudVolumetricProfileScript.CloudHeightGradientToVector4(CloudLayerVolumetric1.CloudGradientCumulus);
            }

            // update global shader values
            shaderProps.Update(null);
            SetShaderVolumetricCloudShaderProperties(shaderProps, weatherMap, sun, camera);

            // update compute shader values
            if (cloudProbe != null)
            {
                shaderProps.Update(cloudProbe);
                SetShaderVolumetricCloudShaderProperties(shaderProps, weatherMap, sun, camera);
            }
            
            Shader.SetGlobalInt(WMS._WeatherMakerCloudVolumetricShadowSampleCount, WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudShadowSampleCount);

            if (CloudsEnabled)
            {
                float cover = Mathf.Min(1.0f, CloudCoverTotal * (1.5f - CloudLightAbsorptionTotal));
                float sunIntensityMultiplier = Mathf.Clamp(1.0f - (cover * CloudLightStrength), 0.2f, 1.0f);
                float sunIntensityMultiplierWithoutLightStrength = Mathf.Clamp(1.0f - (cover * cover * 0.85f), 0.2f, 1.0f);
                float cloudShadowReducer = sunIntensityMultiplierWithoutLightStrength;
                float dirLightMultiplier = sunIntensityMultiplier * Mathf.Lerp(1.0f, DirectionalLightIntensityMultiplier, cover);
                Shader.SetGlobalFloat(WMS._WeatherMakerCloudGlobalShadow2, cloudShadowReducer);
                CloudGlobalShadow = cloudShadowReducer = Mathf.Min(cloudShadowReducer, Shader.GetGlobalFloat(WMS._WeatherMakerCloudGlobalShadow));
                CloudDirectionalLightDirectBlock = dirLightMultiplier;

                // if we have shadows turned on, use screen space shadows
                if (QualitySettings.shadows != ShadowQuality.Disable && WeatherMakerLightManagerScript.ScreenSpaceShadowMode != UnityEngine.Rendering.BuiltinShaderMode.Disabled &&
                    WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudShadowDownsampleScale != WeatherMakerDownsampleScale.Disabled &&
                    WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudShadowSampleCount > 0)
                {
                    // do not reduce light intensity or shadows, screen space shadows are being used
                    WeatherMakerLightManagerScript.Instance.DirectionalLightIntensityMultipliers.Remove("WeatherMakerFullScreenCloudsScript");
                    Shader.SetGlobalFloat(WMS._WeatherMakerDirLightMultiplier, 1.0f);
                    Shader.SetGlobalFloat(WMS._WeatherMakerCloudGlobalShadow, 1.0f);
                }
                else
                {
                    // save dir light multiplier so flat clouds can adjust to the dimmed light
                    Shader.SetGlobalFloat(WMS._WeatherMakerDirLightMultiplier, 1.0f / Mathf.Max(0.0001f, dirLightMultiplier));
                    Shader.SetGlobalFloat(WMS._WeatherMakerCloudGlobalShadow, cloudShadowReducer);

                    // brighten up on orthographic, looks better
                    if (WeatherMakerScript.Instance.MainCamera != null && WeatherMakerScript.Instance.MainCamera.orthographic)
                    {
                        sunIntensityMultiplier = Mathf.Min(1.0f, sunIntensityMultiplier * 2.0f);
                    }

                    // we rely on sun intensity reduction to fake shadows
                    WeatherMakerLightManagerScript.Instance.DirectionalLightIntensityMultipliers["WeatherMakerFullScreenCloudsScript"] = dirLightMultiplier;
                }
            }
            else
            {
                WeatherMakerLightManagerScript.Instance.DirectionalLightIntensityMultipliers.Remove("WeatherMakerFullScreenCloudsScript");
                WeatherMakerLightManagerScript.Instance.DirectionalLightShadowIntensityMultipliers.Remove("WeatherMakerFullScreenCloudsScript");
            }
        }

        private void LoadDefaultLayerIfNeeded(ref WeatherMakerCloudLayerProfileScript script)
        {
            if (script == null && WeatherMakerScript.Instance != null)
            {
                script = WeatherMakerScript.Instance.LoadResource<WeatherMakerCloudLayerProfileScript>("WeatherMakerCloudLayerProfile_None");
            }
        }

        private void LoadDefaultLayerIfNeeded(ref WeatherMakerCloudVolumetricProfileScript script)
        {
            if (script == null && WeatherMakerScript.Instance != null)
            {
                script = WeatherMakerScript.Instance.LoadResource<WeatherMakerCloudVolumetricProfileScript>("WeatherMakerCloudLayerProfileVolumetric_None");
            }
        }

        private void UpdateWeatherMap(WeatherMakerShaderPropertiesScript props, Texture weatherMap, float weatherMapSeed, Camera camera)
        {
            props.SetTexture(WMS._WeatherMakerWeatherMapTexture, weatherMap);
            props.SetVector(WMS._WeatherMakerWeatherMapScale, new Vector4(WeatherMapScale.x, WeatherMapScale.y, WeatherMapScale.z, 1.0f / WeatherMapScale.z));
            props.SetFloat(WMS._CloudCoverVolumetric, CloudLayerVolumetric1.CloudCover.LastValue);
            props.SetFloat(WMS._CloudCoverSecondaryVolumetric, CloudLayerVolumetric1.CloudCoverSecondary.LastValue);
            props.SetFloat(WMS._CloudDensityVolumetric, CloudLayerVolumetric1.CloudDensity.LastValue);
            props.SetFloat(WMS._CloudTypeVolumetric, CloudLayerVolumetric1.CloudType.LastValue);
            props.SetFloat(WMS._CloudTypeSecondaryVolumetric, CloudLayerVolumetric1.CloudTypeSecondary.LastValue);
            props.SetVector(WMS._CloudCoverageVelocity, velocityAccumCoverage);
            props.SetVector(WMS._CloudTypeVelocity, velocityAccumType);
            props.SetFloat(WMS._CloudCoverageFrequency, WeatherMapCloudCoverageScale.LastValue);
            props.SetFloat(WMS._CloudTypeFrequency, WeatherMapCloudTypeScale.LastValue);
            float r = WeatherMapCloudCoverageRotation.LastValue * Mathf.Deg2Rad;
            props.SetVector(WMS._CloudCoverageRotation, new Vector2(Mathf.Sin(r), Mathf.Cos(r)));
            r = WeatherMapCloudTypeRotation.LastValue * Mathf.Deg2Rad;
            props.SetVector(WMS._CloudTypeRotation, new Vector2(Mathf.Sin(r), Mathf.Cos(r)));
            props.SetFloat(WMS._CloudCoverageAdder, WeatherMapCloudCoverageAdder.LastValue);
            //weatherMapMaterial.SetVector(WMS._CloudCoverageOffset, cloudCoverageOffset);
            props.SetFloat(WMS._CloudCoveragePower, WeatherMapCloudCoveragePower.LastValue);
            props.SetFloat(WMS._CloudTypeAdder, WeatherMapCloudTypeAdder.LastValue);
            //weatherMapMaterial.SetVector(WMS._CloudTypeOffset, cloudTypeOffset);
            props.SetFloat(WMS._CloudTypePower, WeatherMapCloudTypePower.LastValue);
            props.SetVector(WMS._MaskOffset, WeatherMapRenderTextureMaskOffset);
            props.SetFloat(WMS._WeatherMakerWeatherMapSeed, weatherMapSeed);
            if (WeatherMakerCommandBufferManagerScript.Instance != null)
            {
                WeatherMakerCommandBufferManagerScript.Instance.UpdateShaderPropertiesForCamera(props, camera);
            }
        }

        /// <summary>
        /// Update the weather map
        /// </summary>
        /// <param name="weatherMapMaterial">Weather map material</param>
        /// <param name="camera">Camera</param>
        /// <param name="cloudProbeShader">Cloud probe shader</param>
        /// <param name="weatherMap">Weather map render texture</param>
        /// <param name="weatherMapSeed">Weather map seed</param>
        public void UpdateWeatherMap(Material weatherMapMaterial, Camera camera, ComputeShader cloudProbeShader, RenderTexture weatherMap, float weatherMapSeed)
        {
            if (CloudLayerVolumetric1 == null)
            {
                return;
            }
            if (camera == null)
            {
                camera = Camera.main;
                if (camera == null)
                {
                    return;
                }
            }

            shaderProps.Update(null);
            UpdateWeatherMap(shaderProps, weatherMap, weatherMapSeed, camera);
            shaderProps.Update(weatherMapMaterial);
            UpdateWeatherMap(shaderProps, weatherMap, weatherMapSeed, camera);
            if (cloudProbeShader != null)
            {
                shaderProps.Update(cloudProbeShader);
                UpdateWeatherMap(shaderProps, weatherMap, weatherMapSeed, camera);
            }
        }

        /// <summary>
        /// Ensure all layers have profiles
        /// </summary>
        public void EnsureNonNullLayers()
        {
            LoadDefaultLayerIfNeeded(ref CloudLayer1);
            LoadDefaultLayerIfNeeded(ref CloudLayer2);
            LoadDefaultLayerIfNeeded(ref CloudLayer3);
            LoadDefaultLayerIfNeeded(ref CloudLayer4);
            LoadDefaultLayerIfNeeded(ref CloudLayerVolumetric1);
        }

        /// <summary>
        /// Deep clone the entire profile
        /// </summary>
        /// <returns></returns>
        public WeatherMakerCloudProfileScript Clone()
        {
            WeatherMakerCloudProfileScript clone = ScriptableObject.Instantiate(this);
            clone.EnsureNonNullLayers();
            clone.CloudLayer1 = ScriptableObject.Instantiate(clone.CloudLayer1);
            clone.CloudLayer2 = ScriptableObject.Instantiate(clone.CloudLayer2);
            clone.CloudLayer3 = ScriptableObject.Instantiate(clone.CloudLayer3);
            clone.CloudLayer4 = ScriptableObject.Instantiate(clone.CloudLayer4);
            clone.CloudLayerVolumetric1 = ScriptableObject.Instantiate(clone.CloudLayerVolumetric1);
            CopyStateTo(clone);
            return clone;
        }

        /// <summary>
        /// Update
        /// </summary>
        public void Update()
        {
            EnsureNonNullLayers();
            CloudsEnabled =
            (
                (CloudLayerVolumetric1.CloudColor.a > 0.0f && CloudLayerVolumetric1.CloudCover.LastValue > 0.001f) ||
                (CloudLayer1.CloudNoise != null && CloudLayer1.CloudColor.a > 0.0f && CloudLayer1.CloudCover > 0.0f) ||
                (CloudLayer2.CloudNoise != null && CloudLayer2.CloudColor.a > 0.0f && CloudLayer2.CloudCover > 0.0f) ||
                (CloudLayer3.CloudNoise != null && CloudLayer3.CloudColor.a > 0.0f && CloudLayer3.CloudCover > 0.0f) ||
                (CloudLayer4.CloudNoise != null && CloudLayer4.CloudColor.a > 0.0f && CloudLayer4.CloudCover > 0.0f) ||
                (AuroraProfile != null && AuroraProfile.AuroraEnabled)
            );
            bool enableVol = WeatherMakerScript.Instance.PerformanceProfile.EnableVolumetricClouds;
            float volCover = (enableVol ? CloudLayerVolumetric1.CloudCover.LastValue : 0.0f);
            float volCoverDensity = (enableVol ? (CloudLayerVolumetric1.CloudCover.LastValue * CloudLayerVolumetric1.CloudDensity.LastValue) : 0.0f);
            float volAbsorption = (enableVol ? Mathf.Min(1.0f, (Mathf.Clamp(1.0f - (CloudLayerVolumetric1.CloudCover.LastValue * CloudLayerVolumetric1.CloudDensity.LastValue), 0.0f, 1.0f))) : 0.0f);
            CloudCoverTotal = Mathf.Min(1.0f, (CloudLayer1.CloudCover + CloudLayer2.CloudCover + CloudLayer3.CloudCover + CloudLayer4.CloudCover + volCover));
            CloudDensityTotal = Mathf.Min(1.0f,
                volCoverDensity +
                (CloudLayer1.CloudCover * CloudLayer1.CloudDensity) +
                (CloudLayer2.CloudCover * CloudLayer2.CloudDensity) +
                (CloudLayer3.CloudCover * CloudLayer1.CloudDensity) +
                (CloudLayer4.CloudCover * CloudLayer4.CloudDensity));
            CloudLightAbsorptionTotal = volAbsorption +
                (CloudLayer1.CloudCover * CloudLayer1.CloudLightAbsorption) +
                (CloudLayer2.CloudCover * CloudLayer2.CloudLightAbsorption) +
                (CloudLayer3.CloudCover * CloudLayer3.CloudLightAbsorption) +
                (CloudLayer4.CloudCover * CloudLayer4.CloudLightAbsorption);
            float flatVelocityMultiplier = Time.deltaTime * 0.005f;
            //cloudNoiseMaskVelocityAccum1 += (CloudLayer1.CloudNoiseMaskVelocity * velMult);
            //cloudNoiseMaskVelocityAccum2 += (CloudLayer2.CloudNoiseMaskVelocity * velMult);
            //cloudNoiseMaskVelocityAccum3 += (CloudLayer3.CloudNoiseMaskVelocity * velMult);
            //cloudNoiseMaskVelocityAccum4 += (CloudLayer4.CloudNoiseMaskVelocity * velMult);
            cloudNoiseVelocityAccum1 += (CloudLayer1.CloudNoiseVelocity * flatVelocityMultiplier);
            cloudNoiseVelocityAccum2 += (CloudLayer2.CloudNoiseVelocity * flatVelocityMultiplier);
            cloudNoiseVelocityAccum3 += (CloudLayer3.CloudNoiseVelocity * flatVelocityMultiplier);
            cloudNoiseVelocityAccum4 += (CloudLayer4.CloudNoiseVelocity * flatVelocityMultiplier);
            float velocityScale = Time.deltaTime * 10.0f * WeatherMapScale.z;
            velocityAccumCoverage += (WeatherMapCloudCoverageVelocity * velocityScale);
            velocityAccumType += (WeatherMapCloudTypeVelocity * velocityScale);
            WeatherMapRenderTextureMaskOffset += (WeatherMapRenderTextureMaskVelocity * Time.deltaTime);

            // ensure mask offset does not go to far out of bounds
            WeatherMapRenderTextureMaskOffset.x = Mathf.Clamp(WeatherMapRenderTextureMaskOffset.x, WeatherMapRenderTextureMaskOffsetClamp.x, WeatherMapRenderTextureMaskOffsetClamp.y);
            WeatherMapRenderTextureMaskOffset.y = Mathf.Clamp(WeatherMapRenderTextureMaskOffset.y, WeatherMapRenderTextureMaskOffsetClamp.x, WeatherMapRenderTextureMaskOffsetClamp.y);
        }

        /// <summary>
        /// Copy profile state to another profile
        /// </summary>
        /// <param name="other">Other profile</param>
        public void CopyStateTo(WeatherMakerCloudProfileScript other)
        {
            other.velocityAccumCoverage = velocityAccumCoverage;
            other.velocityAccumType = velocityAccumType;
            other.cloudNoiseVelocityAccum1 = this.cloudNoiseVelocityAccum1;
            other.cloudNoiseVelocityAccum2 = this.cloudNoiseVelocityAccum2;
            other.cloudNoiseVelocityAccum3 = this.cloudNoiseVelocityAccum3;
            other.cloudNoiseVelocityAccum4 = this.cloudNoiseVelocityAccum4;
            other.CloudCoverTotal = this.CloudCoverTotal;
            other.CloudDensityTotal = this.CloudDensityTotal;
            other.CloudLightAbsorptionTotal = this.CloudLightAbsorptionTotal;
            other.CloudsEnabled = this.CloudsEnabled;
            other.WeatherMapRenderTextureMaskVelocity = this.WeatherMapRenderTextureMaskVelocity;
            other.WeatherMapRenderTextureMaskOffset = this.WeatherMapRenderTextureMaskOffset;
        }

        /// <summary>
        /// Cloud global shadow value
        /// </summary>
        public float CloudGlobalShadow { get; private set; }
    }
}
