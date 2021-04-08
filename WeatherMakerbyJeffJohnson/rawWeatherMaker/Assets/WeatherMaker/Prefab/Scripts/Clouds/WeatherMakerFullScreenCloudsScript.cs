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

#if UNITY_2018_3_OR_NEWER

#define ENABLE_CLOUD_PROBE

#endif

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace DigitalRuby.WeatherMaker
{
    /// <summary>
    /// Full screen cloud script and renderer
    /// </summary>
    [ExecuteInEditMode]
    public class WeatherMakerFullScreenCloudsScript : MonoBehaviour
    {
        /// <summary>Cloud profile</summary>
        [Header("Full Screen Clouds - profile")]
        [Tooltip("Cloud profile")]
        [SerializeField]
        private WeatherMakerCloudProfileScript _CloudProfile;
        private WeatherMakerCloudProfileScript currentRenderCloudProfile;

        /// <summary>Whether to auto-clone the cloud profile when it changes. Set to false to directly edit an original cloud profile. Be careful if this is false - you can accidently overwrite changes to your cloud profile.</summary>
        [Tooltip("Whether to auto-clone the cloud profile when it changes. Set to false to directly edit an original cloud profile. Be careful if this is false - " +
            "you can accidently overwrite changes to your cloud profile.")]
        public bool AutoCloneCloudProfileOnChange = true;

        /// <summary>Aurora borealis profile</summary>
        [Tooltip("Aurora borealis profile")]
        [FormerlySerializedAs("Aurora")]
        public WeatherMakerAuroraProfileScript AuroraProfile;

        /// <summary>Aurora animation in seconds when changing aurora profile</summary>
        [Tooltip("Aurora animation in seconds when changing aurora profile")]
        [Range(0.0f, 120.0f)]
        public float AuroraAnimationDuration = 10.0f;

        /// <summary>Down sample scale.</summary>
        [Header("Full Screen Clouds - rendering")]
        [Tooltip("Down sample scale.")]
        public WeatherMakerDownsampleScale DownSampleScale = WeatherMakerDownsampleScale.HalfResolution;

        /// <summary>Downsample scale for cloud post process (dir light rays, etc.)</summary>
        [Tooltip("Downsample scale for cloud post process (dir light rays, etc.)")]
        public WeatherMakerDownsampleScale DownSampleScalePostProcess = WeatherMakerDownsampleScale.QuarterResolution;

        /// <summary>Cloud rendering material.</summary>
        [Tooltip("Cloud rendering material.")]
        public Material Material;

        /// <summary>Material to blit the full screen clouds.</summary>
        [Tooltip("Material to blit the full screen clouds.")]
        public Material FullScreenMaterial;

        /// <summary>Blur Material.</summary>
        [Tooltip("Blur Material.")]
        public Material BlurMaterial;

        /// <summary>Bilateral Blur Material</summary>
        [Tooltip("Bilateral Blur Material")]
        public Material BilateralBlurMaterial;

        /// <summary>Material for temporal reprojection</summary>
        [Tooltip("Material for temporal reprojection")]
        public Material TemporalReprojectionMaterial;

        /// <summary>Material for cloud shadows</summary>
        [Tooltip("Material for cloud shadows")]
        public Material ShadowMaterial;

        /// <summary>Blur Shader Type.</summary>
        [Tooltip("Blur Shader Type.")]
        public BlurShaderType BlurShader;

        /// <summary>Temporal reprojection size - allows rendering a portion of this effect over a number of frames to spread cost out over time. This can introduce rendering artifacts so be on the lookout for that.</summary>
        [Tooltip("Temporal reprojection size - allows rendering a portion of this effect over a number of frames to spread cost out over time. " +
            "This can introduce rendering artifacts so be on the lookout for that.")]
        public WeatherMakerTemporalReprojectionSize TemporalReprojection = WeatherMakerTemporalReprojectionSize.TwoByTwo;

        /// <summary>Render Queue</summary>
        [Tooltip("Render Queue")]
        public CameraEvent RenderQueue = CameraEvent.BeforeImageEffectsOpaque;

        /// <summary>Whether to render clouds in reflection cameras.</summary>
        [Tooltip("Whether to render clouds in reflection cameras.")]
        public bool AllowReflections = true;

        /// <summary>Weather map material (volumetric clouds)</summary>
        [Header("Full Screen Clouds - weather map")]
        [Tooltip("Weather map material (volumetric clouds)")]
        public Material WeatherMapMaterial;
        private readonly WeatherMakerMaterialCopy weatherMapMaterialCopy = new WeatherMakerMaterialCopy();

        /// <summary>Override the weather map no matter what the cloud profile is specifying. R = cloud coverage, G = reserved, B = cloud type (0 = stratus, 1 = cumulus), A = reserved.</summary>
        [Tooltip("Override the weather map no matter what the cloud profile is specifying. R = cloud coverage, G = reserved, B = cloud type (0 = stratus, 1 = cumulus), A = reserved.")]
        public Texture2D WeatherMapOverride;

        /// <summary>Override the weather map mask no matter what the cloud profile is specifying. Alpha channel of 0 blocks clouds completely, 1 shows clouds completely.</summary>
        [Tooltip("Override the weather map mask no matter what the cloud profile is specifying. Alpha channel of 0 blocks clouds completely, 1 shows clouds completely.")]
        public Texture2D WeatherMapMaskOverride;

        /// <summary>Whether to regenerate a weather map seed when the script is enabled. Set this to false if you don't want the weather map to change when the script is re-enabled.</summary>
        [Tooltip("Whether to regenerate a weather map seed when the script is enabled. Set this to false if you don't want the weather map to change when the script is re-enabled.")]
        public bool WeatherMapRegenerateSeedOnEnable = true;

        /// <summary>Additional offset to cloud ray to bring clouds up or down.</summary>
        [Header("Full Screen Clouds - Other")]
        [Tooltip("Additional offset to cloud ray to bring clouds up or down.")]
        [Range(-1.0f, 1.0f)]
        public float CloudRayOffset = 0.0f;

        /// <summary>Whether to auto-set the temporal reprojection material blend mode to sharp with fallback to blur if lightning is showing. Set to false to not auto-set the blend mode and leave as is.</summary>
        [Tooltip("Whether to auto-set the temporal reprojection material blend mode to sharp with fallback to blur if lightning is showing. Set to false to " +
            "not auto-set the blend mode and leave as is.")]
        public bool AutoSetTemporalReprojectionBlendMode = true;

        /// <summary>
        /// Real-time cloud noise profile, extremely slow do not use normally
        /// </summary>
        [Header("Full Screen Clouds - Realtime Noise (Debug Only, Very Slow)")]
        public WeatherMakerCloudNoiseProfileGroupScript CloudRealtimeNoiseProfile;

        /// <summary>
        /// Allow rendering to command buffer before the clouds render
        /// </summary>
        public readonly List<System.Action<WeatherMakerCommandBuffer>> BeforeCloudsRenderHooks = new List<System.Action<WeatherMakerCommandBuffer>>();

        private static float currentWeatherMapSeed = 0.0f;

        private bool animatingAurora;
        private WeatherMakerShaderPropertiesScript shaderProps;

        /// <summary>
        /// Cloud weather map
        /// </summary>
        public RenderTexture WeatherMapRenderTexture { get; private set; }

        /// <summary>
        /// Cloud shadow texture
        /// </summary>
        public RenderTexture CloudShadowRenderTexture { get; private set; }

        private WeatherMakerCloudProfileScript lastProfile;
        private WeatherMakerCloudProfileScript lastProfileOriginal;

        /// <summary>
        /// Get the current cloud profile
        /// </summary>
        public WeatherMakerCloudProfileScript CloudProfile
        {
            get { return _CloudProfile; }
            set { ShowCloudsAnimated(value, 0.0f, 5.0f); }
        }

        private WeatherMakerFullScreenEffect effect;
        private System.Action<WeatherMakerCommandBuffer> updateShaderPropertiesAction;
        private static WeatherMakerCloudProfileScript emptyProfile;
        private Collider cloudCollider;

        private readonly List<CameraValues> cameraValues = new List<CameraValues>();

        private class CameraValues
        {
            public Camera Camera;
            public float FieldOfView;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct CloudProbe
        {
            public Vector4 Source;
            public Vector4 Target;
        }

        /// <summary>
        /// Result of a cloud probe operation
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CloudProbeResult
        {
            /// <summary>
            /// Cloud density at the source (0 - 1)
            /// </summary>
            public float DensitySource;

            /// <summary>
            /// Cloud density at the target (0 - 1)
            /// </summary>
            public float DensityTarget;

            /// <summary>
            /// Cloud density ray average (64 samples, 0 - 1)
            /// </summary>
            public float DensityRayAverage;

            /// <summary>
            /// Cloud density ray sum (64 samples, 0 - 64)
            /// </summary>
            public float DensityRaySum;
        }

#if ENABLE_CLOUD_PROBE

        private static readonly List<CloudProbeComputeRequest> probeRequestPool = new List<CloudProbeComputeRequest>();
        private static readonly List<ComputeBuffer> computeBufferPool = new List<ComputeBuffer>();
        private readonly List<CloudProbeRequest> tempRequests = new List<CloudProbeRequest>();

        private class CloudProbeComputeRequest : IDisposable
        {
            public Camera Camera;
            public List<CloudProbeRequest> UserRequests = new List<CloudProbeRequest>();
            public List<CloudProbe> SamplesList = new List<CloudProbe>();
            public ComputeShader Shader;
            public ComputeBuffer Result;
            public AsyncGPUReadbackRequest Request;
            public void Reset()
            {
                UserRequests.Clear();
                SamplesList.Clear();
            }
            public void Dispose()
            {
                if (Result != null)
                {
                    if (!computeBufferPool.Contains(Result))
                    {
                        computeBufferPool.Add(Result);
                    }
                    Result = null;
                }
                if (!probeRequestPool.Contains(this))
                {
                    probeRequestPool.Add(this);
                }
            }
        }

        private class CloudProbeRequest
        {
            public Camera Camera;
            public Transform Source;
            public Transform Target;

            public override int GetHashCode()
            {
                return Camera.GetHashCode() + Source.GetHashCode() + Target.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (base.Equals(obj))
                {
                    return true;
                }
                else
                {
                    CloudProbeRequest other = obj as CloudProbeRequest;
                    if (other == null)
                    {
                        return false;
                    }
                    return (Camera == other.Camera && Source == other.Source && Target == other.Target);
                }
            }
        }

        private const float oneOver64 = 0.015625f;
        private ComputeShader cloudProbeShader;
        private readonly List<CloudProbeComputeRequest> cloudProbeComputeRequests = new List<CloudProbeComputeRequest>();
        private int cloudProbeShaderKernel;
        private readonly List<CloudProbeRequest> cloudProbeUserRequests = new List<CloudProbeRequest>();
        private readonly List<KeyValuePair<CloudProbeRequest, CloudProbeResult>> cloudProbeResults = new List<KeyValuePair<CloudProbeRequest, CloudProbeResult>>();

#endif

        private CommandBuffer weatherMapCommandBuffer;
        private WeatherMakerTemporalReprojectionState.TemporalReprojectionBlendMode currentTemporalReprojectionBlendMode;
        private System.Action<WeatherMakerCommandBuffer> preSetupCommandBufferAction;

        private void GenerateWeatherMap(Camera camera)
        {
            if (currentRenderCloudProfile == null || camera == null)
            {
                return;
            }

            weatherMapMaterialCopy.Update(WeatherMapMaterial);

            if (weatherMapMaterialCopy.Copy == null)
            {
                Debug.LogError("Must set weather map material on full screen cloud script");
                return;
            }

            if (ShadowMaterial == null)
            {
                Debug.LogError("Must set shadow material on full screen cloud script");
                return;
            }

            CreateWeatherMapTextures(camera);

#if ENABLE_CLOUD_PROBE

            currentRenderCloudProfile.UpdateWeatherMap(weatherMapMaterialCopy, camera, cloudProbeShader, WeatherMapRenderTexture, currentWeatherMapSeed);

#else

            currentRenderCloudProfile.UpdateWeatherMap(weatherMapMaterialCopy, camera, null, WeatherMapRenderTexture, currentWeatherMapSeed);

#endif

            if (weatherMapCommandBuffer == null)
            {
                weatherMapCommandBuffer = new CommandBuffer { name = "WeatherMapAndCloudShadows" };
            }
            camera.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, weatherMapCommandBuffer);
            camera.RemoveCommandBuffer(CameraEvent.BeforeReflections, weatherMapCommandBuffer);
            weatherMapCommandBuffer.Clear();
            if (WeatherMapRenderTexture != null)
            {
                if (WeatherMapOverride != null)
                {
                    weatherMapCommandBuffer.Blit(WeatherMapOverride, WeatherMapRenderTexture);
                }
                else if (currentRenderCloudProfile.WeatherMapRenderTextureOverride == null)
                {
                    Texture mask = (WeatherMapMaskOverride == null ? currentRenderCloudProfile.WeatherMapRenderTextureMask : WeatherMapMaskOverride);
                    weatherMapCommandBuffer.Blit(mask, WeatherMapRenderTexture, weatherMapMaterialCopy, 0);
                }
                else
                {
                    weatherMapCommandBuffer.Blit(currentRenderCloudProfile.WeatherMapRenderTextureOverride, WeatherMapRenderTexture);
                }
                weatherMapCommandBuffer.SetGlobalTexture(WMS._WeatherMakerWeatherMapTexture, WeatherMapRenderTexture);
                RenderTextureDescriptor desc = new RenderTextureDescriptor(CloudShadowRenderTexture.width, CloudShadowRenderTexture.height, RenderTextureFormat.RHalf, 0) { sRGB = false };
                weatherMapCommandBuffer.GetTemporaryRT(WMS._MainTex2, desc, FilterMode.Bilinear);
                weatherMapCommandBuffer.Blit(null, WMS._MainTex2, ShadowMaterial, 1);
                weatherMapCommandBuffer.Blit(null, CloudShadowRenderTexture, ShadowMaterial, 2);
                weatherMapCommandBuffer.ReleaseTemporaryRT(WMS._MainTex2);
                if (camera.actualRenderingPath == RenderingPath.DeferredShading)
                {
                    camera.AddCommandBuffer(CameraEvent.BeforeReflections, weatherMapCommandBuffer);
                }
                else
                {
                    camera.AddCommandBuffer(CameraEvent.AfterDepthTexture, weatherMapCommandBuffer);
                }
            }
        }

        private void UpdateShaderProperties(WeatherMakerCommandBuffer b)
        {
            if (currentRenderCloudProfile == null || WeatherMakerScript.Instance == null || WeatherMakerScript.Instance.PerformanceProfile == null)
            {
                return;
            }

            if (AuroraProfile != null)
            {
                AuroraProfile.UpdateShaderVariables();
            }

            WeatherMakerCloudVolumetricProfileScript vol = currentRenderCloudProfile.CloudLayerVolumetric1;
            int sampleCount = WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudDirLightRaySampleCount;
            SetShaderCloudParameters(b.Material, b.Camera);

            // check if shafts are disabled
            if (sampleCount <= 0 ||
                vol.CloudCover.LastValue <= 0.001f ||
                vol.CloudDirLightRayBrightness <= 0.001f ||
                b.CameraType != WeatherMakerCameraType.Normal ||
                vol.CloudDirLightRayTintColor.a <= 0.0f)
            {
                b.Material.SetInt(WMS._WeatherMakerFogSunShaftMode, 0);
                return;
            }

            // check each dir light to see if it can do shafts
            bool atLeastOneLightHasShafts = false;
            foreach (WeatherMakerCelestialObjectScript obj in WeatherMakerLightManagerScript.Instance.Suns.Union(WeatherMakerLightManagerScript.Instance.Moons))
            {
                if (obj != null && obj.OrbitTypeIsPerspective && obj.LightIsOn && obj.ViewportPosition.z > 0.0f && obj.ShaftMultiplier > 0.0f)
                {
                    atLeastOneLightHasShafts = true;
                    break;
                }
            }

            if (atLeastOneLightHasShafts)
            {
                // sun shafts are visible
                Vector4 dm = WeatherMakerCloudVolumetricProfileScript.CloudDirLightRayDitherMagic;
                float dither = WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudDirLightRayDither;
                b.Material.SetInt(WMS._WeatherMakerFogSunShaftMode, 1);
                bool gamma = (QualitySettings.activeColorSpace == ColorSpace.Gamma);
                float brightness = vol.CloudDirLightRayBrightness * (gamma ? 1.0f : 0.33f);
                b.Material.SetVector(WMS._WeatherMakerFogSunShaftsParam1, new Vector4(vol.CloudDirLightRaySpread / (float)sampleCount, (float)sampleCount, brightness, 1.0f / (float)sampleCount));
                b.Material.SetVector(WMS._WeatherMakerFogSunShaftsParam2, new Vector4(vol.CloudDirLightRayStepMultiplier, vol.CloudDirLightRayDecay, dither, 0.0f));
                b.Material.SetVector(WMS._WeatherMakerFogSunShaftsTintColor, new Vector4(vol.CloudDirLightRayTintColor.r * vol.CloudDirLightRayTintColor.a, vol.CloudDirLightRayTintColor.g * vol.CloudDirLightRayTintColor.a,
                    vol.CloudDirLightRayTintColor.b * vol.CloudDirLightRayTintColor.a, vol.CloudDirLightRayTintColor.a));
                b.Material.SetVector(WMS._WeatherMakerFogSunShaftsDitherMagic, new Vector4(dm.x * Screen.width, dm.y * Screen.height,
                    dm.z * Screen.width, dm.w * Screen.height));
                b.Material.SetFloat(WMS._WeatherMakerFogSunShaftsBackgroundIntensity, 0.0f);
                b.Material.SetColor(WMS._WeatherMakerFogSunShaftsBackgroundTintColor, Color.clear);
            }
            else
            {
                b.Material.SetInt(WMS._WeatherMakerFogSunShaftMode, 0);
            }
        }

        private void DeleteAndTransitionRenderProfile(WeatherMakerCloudProfileScript newProfile)
        {
            lastProfileOriginal = newProfile;
            if (newProfile != null)
            {
                if (newProfile.name.IndexOf("(Clone)") < 0 && AutoCloneCloudProfileOnChange && Application.isPlaying)
                {
                    newProfile = newProfile.Clone();
                }
            }
            if (currentRenderCloudProfile != null)
            {
                if (newProfile != null)
                {
                    currentRenderCloudProfile.CopyStateTo(newProfile);
                }
                if (currentRenderCloudProfile != emptyProfile)
                {
                    if (currentRenderCloudProfile.CloudLayer1.name.IndexOf("(Clone)") >= 0)
                    {
                        DestroyImmediate(currentRenderCloudProfile.CloudLayer1);
                    }
                    if (currentRenderCloudProfile.CloudLayer2.name.IndexOf("(Clone)") >= 0)
                    {
                        DestroyImmediate(currentRenderCloudProfile.CloudLayer2);
                    }
                    if (currentRenderCloudProfile.CloudLayer3.name.IndexOf("(Clone)") >= 0)
                    {
                        DestroyImmediate(currentRenderCloudProfile.CloudLayer3);
                    }
                    if (currentRenderCloudProfile.CloudLayer4.name.IndexOf("(Clone)") >= 0)
                    {
                        DestroyImmediate(currentRenderCloudProfile.CloudLayer4);
                    }
                    if (currentRenderCloudProfile.CloudLayerVolumetric1.name.IndexOf("(Clone)") >= 0)
                    {
                        DestroyImmediate(currentRenderCloudProfile.CloudLayerVolumetric1);
                    }
                    if (currentRenderCloudProfile.name.IndexOf("(Clone)") >= 0)
                    {
                        DestroyImmediate(currentRenderCloudProfile);
                    }
                }
            }
            currentRenderCloudProfile = _CloudProfile = lastProfile = newProfile;
        }

        private void EnsureProfile()
        {
            if (WeatherMakerScript.Instance == null)
            {
                return;
            }

            if (emptyProfile == null)
            {
                emptyProfile = WeatherMakerScript.Instance.LoadResource<WeatherMakerCloudProfileScript>("WeatherMakerCloudProfile_None");
                if (Application.isPlaying)
                {
                    emptyProfile = emptyProfile.Clone();
                }
            }
            if (_CloudProfile == null)
            {
                _CloudProfile = lastProfileOriginal = emptyProfile;
            }
            else if (AutoCloneCloudProfileOnChange && _CloudProfile.name.IndexOf("(Clone)", StringComparison.OrdinalIgnoreCase) < 0 && Application.isPlaying)
            {
                _CloudProfile = _CloudProfile.Clone();
            }
            currentRenderCloudProfile = _CloudProfile;
            if (effect == null)
            {
                lastProfile = currentRenderCloudProfile = _CloudProfile;
                effect = new WeatherMakerFullScreenEffect
                {
                    CommandBufferName = "WeatherMakerFullScreenCloudsScript",
                    RenderQueue = RenderQueue
                };
            }
        }

        private void Update()
        {
            if (WeatherMakerScript.Instance == null)
            {
                return;
            }

            // cleanup camera values
            for (int i = cameraValues.Count - 1; i >= 0; i--)
            {
                if (cameraValues[i].Camera == null)
                {
                    cameraValues.RemoveAt(i);
                }
            }

            if (AuroraProfile == null)
            {
                AuroraProfile = WeatherMakerScript.Instance.LoadResource<WeatherMakerAuroraProfileScript>("WeatherMakerAuroraProfile_None");
            }

            if (CloudProfile != null)
            {
                if (WeatherMakerSkySphereScript.Instance != null)
                {
                    CloudProfile.AtmosphereProfile = WeatherMakerSkySphereScript.Instance.SkySphereProfile.AtmosphereProfile;
                }
                if (CloudProfile.AuroraProfile != AuroraProfile)
                {
                    WeatherMakerAuroraProfileScript oldProfile = CloudProfile.AuroraProfile;
                    WeatherMakerAuroraProfileScript newProfile = AuroraProfile;
                    CloudProfile.AuroraProfile = newProfile;
                    animatingAurora = true;
                    newProfile.AnimateFrom(oldProfile, AuroraAnimationDuration, "WeatherMakerFullScreenCloudsScriptAurora", () => animatingAurora = false);
                }
                CloudGlobalShadow = CloudProfile.CloudGlobalShadow;
                if (AutoSetTemporalReprojectionBlendMode)
                {
                    bool valueChanged = false;
                    foreach (CameraValues value in cameraValues)
                    {
                        if (value.FieldOfView != value.Camera.fieldOfView)
                        {
                            value.FieldOfView = value.Camera.fieldOfView;
                            valueChanged = true;
                        }
                    }
                    if (LightningBolt.lightningBoltCount == 0 && !valueChanged)
                    {
                        currentTemporalReprojectionBlendMode = WeatherMakerTemporalReprojectionState.TemporalReprojectionBlendMode.Sharp;
                    }
                    else
                    {
                        currentTemporalReprojectionBlendMode = WeatherMakerTemporalReprojectionState.TemporalReprojectionBlendMode.Blur;
                    }
                }
            }
            if (AuroraProfile != null && !animatingAurora)
            {
                AuroraProfile.UpdateAnimationProperties();
            }

#if ENABLE_CLOUD_PROBE

            CleanupCloudProbes();

#endif

            // debug only
            if (CloudRealtimeNoiseProfile != null)
            {
                CloudRealtimeNoiseProfile.SetGlobalShader();
            }
        }

        private void LateUpdate()
        {
            // ensure cover is 0 in case no one else sets it
            Shader.SetGlobalFloat(WMS._CloudCoverVolumetric, 0.0f);
            Shader.SetGlobalFloatArray(WMS._CloudCover, emptyFloatArray);

            if (WeatherMakerScript.Instance == null || WeatherMakerScript.Instance.PerformanceProfile == null)
            {
                return;
            }

            DownSampleScale = WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudDownsampleScale;
            DownSampleScalePostProcess = WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudDownsampleScalePostProcess;
            TemporalReprojection = WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudTemporalReprojectionSize;
            if (!WeatherMakerScript.Instance.PerformanceProfile.EnableVolumetricClouds)
            {
                // don't use temporal reprojection for flat clouds
                TemporalReprojection = WeatherMakerTemporalReprojectionSize.None;
            }

            if (CloudProfile != lastProfile)
            {
                if (CloudProfile != null)
                {
                    CloudProfile.EnsureNonNullLayers();
                }
                DeleteAndTransitionRenderProfile(CloudProfile);
            }

            updateShaderPropertiesAction = (updateShaderPropertiesAction ?? UpdateShaderProperties);
            if (currentRenderCloudProfile != null)
            {
                currentRenderCloudProfile.Update();
                effect.SetupEffect(Material, FullScreenMaterial, BlurMaterial, BlurShader, DownSampleScale, WeatherMakerDownsampleScale.Disabled,
                    (WeatherMakerScript.Instance.PerformanceProfile.EnableVolumetricClouds ? DownSampleScalePostProcess : WeatherMakerDownsampleScale.Disabled),
                    TemporalReprojectionMaterial, TemporalReprojection, updateShaderPropertiesAction, CloudProfile.CloudsEnabled, currentTemporalReprojectionBlendMode,
                    preSetupCommandBufferAction);
            }
        }

        private void CreateWeatherMapTextures(Camera camera)
        {
            if (WeatherMakerScript.Instance == null)
            {
                return;
            }

            if (WeatherMapRenderTexture == null || WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudWeatherMapSize != WeatherMapRenderTexture.width)
            {
                WeatherMapRenderTexture = WeatherMakerFullScreenEffect.DestroyRenderTexture(WeatherMapRenderTexture);
                int size = WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudWeatherMapSize;
                WeatherMapRenderTexture = new RenderTexture(size, size, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                WeatherMapRenderTexture.name = "WeatherMakerWeatherMapRenderTexture";
                WeatherMapRenderTexture.wrapMode = TextureWrapMode.Repeat;
                WeatherMapRenderTexture.filterMode = FilterMode.Bilinear;
                WeatherMapRenderTexture.autoGenerateMips = true;
                WeatherMapRenderTexture.antiAliasing = 1;
                WeatherMapRenderTexture.anisoLevel = 1;
                WeatherMapRenderTexture.useMipMap = true;
                WeatherMapRenderTexture.Create();
                if (currentWeatherMapSeed == 0.0f || WeatherMapRegenerateSeedOnEnable)
                {
                    currentWeatherMapSeed = UnityEngine.Random.Range(0.001f, 128.0f);
                }
            }

            if (CloudShadowRenderTexture == null || WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudShadowTextureSize != CloudShadowRenderTexture.width)
            {
                CloudShadowRenderTexture = WeatherMakerFullScreenEffect.DestroyRenderTexture(CloudShadowRenderTexture);
                int size = WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudShadowTextureSize;
                CloudShadowRenderTexture = new RenderTexture(size, size, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
                CloudShadowRenderTexture.name = "WeatherMakerCloudShadowTexture";
                CloudShadowRenderTexture.wrapMode = TextureWrapMode.Repeat;
                CloudShadowRenderTexture.filterMode = FilterMode.Bilinear;
                CloudShadowRenderTexture.autoGenerateMips = true;
                CloudShadowRenderTexture.antiAliasing = 1;
                CloudShadowRenderTexture.anisoLevel = 1;
                CloudShadowRenderTexture.useMipMap = true;
                CloudShadowRenderTexture.Create();
            }

            Shader.SetGlobalTexture(WMS._WeatherMakerCloudShadowTexture, CloudShadowRenderTexture);
        }

        private void OnEnable()
        {
            if (TemporalReprojectionMaterial == null || WeatherMakerScript.Instance == null)
            {
                return;
            }

#if ENABLE_CLOUD_PROBE

            handleCloudProbeResultCallback = HandleAsyncGpuCloudProbe;
            if (cloudProbeShader == null && SystemInfo.supportsComputeShaders && SystemInfo.supportsAsyncGPUReadback)
            {
                cloudProbeShader = WeatherMakerScript.Instance.LoadResource<ComputeShader>("WeatherMakerCloudProbeShader"); 
                if (cloudProbeShader != null)
                {
                    cloudProbeShaderKernel = cloudProbeShader.FindKernel("CSMain");
                }
            }

#endif

            preSetupCommandBufferAction = PreSetupCommandBuffer;
            shaderProps = new WeatherMakerShaderPropertiesScript();
            WeatherMakerScript.EnsureInstance(this, ref instance);
            EnsureProfile();
            if (WeatherMakerCommandBufferManagerScript.Instance != null)
            {
                WeatherMakerCommandBufferManagerScript.Instance.RegisterPreCull(CameraPreCull, this);
                WeatherMakerCommandBufferManagerScript.Instance.RegisterPreRender(CameraPreRender, this);
                WeatherMakerCommandBufferManagerScript.Instance.RegisterPostRender(CameraPostRender, this);
            }
            cloudCollider = GetComponent<Collider>();
            Shader.SetGlobalFloat(WMS._WeatherMakerAuroraSeed, UnityEngine.Random.Range(-65536.0f, 65536.0f));
            currentTemporalReprojectionBlendMode = (WeatherMakerTemporalReprojectionState.TemporalReprojectionBlendMode)TemporalReprojectionMaterial.GetInt(WMS._TemporalReprojection_BlendMode);
            if (AuroraProfile == null)
            {
                AuroraProfile = WeatherMakerScript.Instance.LoadResource<WeatherMakerAuroraProfileScript>("WeatherMakerAuroraProfile_None");
            }
        }

        private void OnDisable()
        {
            if (effect != null)
            {
                effect.Dispose();
            }
            WeatherMapRenderTexture = WeatherMakerFullScreenEffect.DestroyRenderTexture(WeatherMapRenderTexture);
            CloudShadowRenderTexture = WeatherMakerFullScreenEffect.DestroyRenderTexture(CloudShadowRenderTexture);
            weatherMapMaterialCopy.Dispose();

#if ENABLE_CLOUD_PROBE

            DisposeCloudProbes();

#endif

            if (weatherMapCommandBuffer != null)
            {
                weatherMapCommandBuffer.Clear();
                weatherMapCommandBuffer.Release();
                weatherMapCommandBuffer = null;
            }
        }

        private void OnDestroy()
        {
            if (effect != null)
            {
                effect.Dispose();
            }
            DeleteAndTransitionRenderProfile(null);
            if (WeatherMakerCommandBufferManagerScript.Instance != null)
            {
                WeatherMakerCommandBufferManagerScript.Instance.UnregisterPreCull(this);
                WeatherMakerCommandBufferManagerScript.Instance.UnregisterPreRender(this);
                WeatherMakerCommandBufferManagerScript.Instance.UnregisterPostRender(this);
            }
            WeatherMakerScript.ReleaseInstance(ref instance);
        }

        private static readonly float[] emptyFloatArray = new float[4] { 0.0f, 0.0f, 0.0f, 0.0f };
        internal void SetShaderCloudParameters(Material material, Camera camera)
        {
            if (WeatherMakerLightManagerScript.Instance != null && currentRenderCloudProfile != null)
            {

#if ENABLE_CLOUD_PROBE

                currentRenderCloudProfile.SetShaderCloudParameters(material, cloudProbeShader, camera, WeatherMapRenderTexture);
                DispatchCloudProbes(camera);

#else

                currentRenderCloudProfile.SetShaderCloudParameters(material, null, camera, WeatherMapRenderTexture);

#endif

                shaderProps.Update(material);
                WeatherMakerLightManagerScript.Instance.UpdateShaderVariables(null, shaderProps, cloudCollider);
            }
        }

        private bool DisallowCamera(Camera camera)
        {
            bool allowReflections;
            if (WeatherMakerScript.Instance == null || WeatherMakerScript.Instance.PerformanceProfile == null)
            {
                allowReflections = AllowReflections;
            }
            else
            {
                allowReflections = WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudAllowReflections;
            }
            return (effect == null || camera.orthographic || WeatherMakerScript.ShouldIgnoreCamera(this, camera, !allowReflections));
        }

        private void RenderCelestialObject(WeatherMakerCelestialObjectScript obj, CommandBuffer cmd)
        {
            if (obj.MeshFilter == null || obj.Renderer == null || obj.Renderer.sharedMaterial == null)
            {
                return;
            }

            cmd.DrawMesh(obj.MeshFilter.sharedMesh, Matrix4x4.TRS(obj.transform.position, obj.transform.rotation, obj.transform.lossyScale), obj.Renderer.sharedMaterial);
        }

        private void PreSetupCommandBuffer(WeatherMakerCommandBuffer cmdBuffer)
        {
            if (WeatherMakerScript.Instance == null || WeatherMakerScript.Instance.PerformanceProfile == null ||
                WeatherMakerLightManagerScript.Instance == null || WeatherMakerCommandBufferManagerScript.Instance == null)
            {
                return;
            }

            // TODO: Atmospheric scattering not supported in VR yet
            if (WeatherMakerScript.Instance.PerformanceProfile.AtmosphereQuality != WeatherMakerAtmosphereQuality.Disabled && SystemInfo.supportsComputeShaders &&
                WeatherMakerLightManagerScript.Instance.SunPerspective != null && WeatherMakerLightManagerScript.Instance.SunPerspective.LightIsOn &&
                !cmdBuffer.Camera.stereoEnabled)
            {
                RenderTextureFormat defaultFormat = (cmdBuffer.Camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
                bool lightShafts = (BilateralBlurMaterial != null && WeatherMakerScript.Instance.PerformanceProfile.AtmosphericLightShaftSampleCount > 0);
                if (lightShafts)
                {
                    cmdBuffer.CommandBuffer.SetGlobalFloat(WMS._WeatherMakerAtmosphereLightShaftEnable, 1.0f);
                    cmdBuffer.CommandBuffer.GetTemporaryRT(WMS._MainTex3, WeatherMakerFullScreenEffect.GetRenderTextureDescriptor(2, 0, 1, RenderTextureFormat.RHalf, 0, cmdBuffer.Camera), FilterMode.Bilinear);
                    cmdBuffer.CommandBuffer.GetTemporaryRT(WMS._MainTex2, WeatherMakerFullScreenEffect.GetRenderTextureDescriptor(1, 0, 1, RenderTextureFormat.RHalf, 0, cmdBuffer.Camera), FilterMode.Bilinear);
                    cmdBuffer.CommandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, WMS._MainTex3, cmdBuffer.Material, 5);
                    cmdBuffer.CommandBuffer.SetGlobalFloat(WMS._SrcBlendMode, (float)BlendMode.One);
                    cmdBuffer.CommandBuffer.SetGlobalFloat(WMS._DstBlendMode, (float)BlendMode.Zero);

                    // pre-requisite of bilateral blur
                    cmdBuffer.CommandBuffer.SetGlobalTexture(WMS._WeatherMakerTemporaryDepthTexture, WeatherMakerCommandBufferManagerScript.Instance.HalfDepthBuffer);
                    WeatherMakerFullScreenEffect.AttachBilateralBlur(cmdBuffer.CommandBuffer, BilateralBlurMaterial, WMS._MainTex3, WMS._MainTex2, RenderTextureFormat.RHalf, WeatherMakerDownsampleScale.HalfResolution, cmdBuffer.Camera);
                    cmdBuffer.CommandBuffer.SetGlobalTexture(WMS._WeatherMakerAtmosphereLightShaftTexture, WMS._MainTex2);
                }
                else
                {
                    cmdBuffer.CommandBuffer.SetGlobalFloat(WMS._WeatherMakerAtmosphereLightShaftEnable, 0.0f);
                }
                cmdBuffer.CommandBuffer.GetTemporaryRT(WMS._MainTex5, WeatherMakerFullScreenEffect.GetRenderTextureDescriptor(1, 0, 1, defaultFormat, 0, cmdBuffer.Camera), FilterMode.Bilinear);

                // save background for blending atmospheric scattering
                cmdBuffer.CommandBuffer.SetGlobalTexture(WMS._MainTex2, WeatherMakerFullScreenEffect.CameraTargetIdentifier());

                // render atmospheric scattering into temporary texture
                cmdBuffer.CommandBuffer.Blit(WeatherMakerFullScreenEffect.CameraTargetIdentifier(), WMS._MainTex5, cmdBuffer.Material, 4);

                // re-draw temporary texture on to frame buffer
                cmdBuffer.CommandBuffer.Blit(WMS._MainTex5, WeatherMakerFullScreenEffect.CameraTargetIdentifier());
                cmdBuffer.CommandBuffer.ReleaseTemporaryRT(WMS._MainTex5);

                if (lightShafts)
                {
                    cmdBuffer.CommandBuffer.ReleaseTemporaryRT(WMS._MainTex3);
                    cmdBuffer.CommandBuffer.ReleaseTemporaryRT(WMS._MainTex2);
                }
            }

            // draw sky
            if ((cmdBuffer.Camera.clearFlags != CameraClearFlags.Skybox || RenderSettings.skybox == null) && WeatherMakerSkySphereScript.Instance != null)
            {
                MeshFilter filter = WeatherMakerSkySphereScript.Instance.MeshFilter;
                Renderer renderer = WeatherMakerSkySphereScript.Instance.MeshRenderer;
                Transform obj = WeatherMakerSkySphereScript.Instance.transform;
                cmdBuffer.CommandBuffer.DrawMesh(filter.sharedMesh, Matrix4x4.TRS(cmdBuffer.Camera.transform.position, obj.transform.rotation, new Vector3(100.0f, 100.0f, 100.0f)), renderer.sharedMaterial);
            }

            // draw moons or other celestial objects
            RenderCelestialObject(WeatherMakerLightManagerScript.Instance.SunPerspective, cmdBuffer.CommandBuffer);
            foreach (WeatherMakerCelestialObjectScript obj in WeatherMakerLightManagerScript.Instance.Moons)
            {
                RenderCelestialObject(obj, cmdBuffer.CommandBuffer);
            }

            foreach (System.Action<WeatherMakerCommandBuffer> action in BeforeCloudsRenderHooks)
            {
                action.Invoke(cmdBuffer);
            }
        }

        private void CameraPreCull(Camera camera)
        {
            if (DisallowCamera(camera))
            {
                return;
            }

            // setup weather map and positions for this camera
            if (WeatherMakerScript.GetCameraType(camera) == WeatherMakerCameraType.Normal && WeatherMakerCommandBufferManagerScript.CameraStack < 2)
            {
                GenerateWeatherMap(camera);
            }

            effect.PreCullCamera(camera);
        }

#if ENABLE_CLOUD_PROBE

        private void DisposeCloudProbes()
        {
            // lock for pool access
            lock (cloudProbeComputeRequests)
            {
                foreach (CloudProbeComputeRequest request in cloudProbeComputeRequests)
                {
                    request.Dispose();
                }
                cloudProbeComputeRequests.Clear();
            }

            cloudProbeUserRequests.Clear();
            cloudProbeResults.Clear();
            foreach (ComputeBuffer buffer in computeBufferPool)
            {
                buffer.Dispose();
            }
            computeBufferPool.Clear();
        }

        private System.Action<AsyncGPUReadbackRequest> handleCloudProbeResultCallback;

        private void HandleAsyncGpuCloudProbe(AsyncGPUReadbackRequest request)
        {
            CloudProbeComputeRequest requestToHandle = null;

            // lock for pool access
            lock (cloudProbeComputeRequests)
            {
                foreach (CloudProbeComputeRequest request2 in cloudProbeComputeRequests)
                {
                    if (request2.Request.Equals(request))
                    {
                        requestToHandle = request2;
                        break;
                    }
                }
            }

            if (requestToHandle != null)
            {
                HandleCloudProbeResult(requestToHandle);
            }
        }

        private void HandleCloudProbeResult(CloudProbeComputeRequest request)
        {
            Unity.Collections.NativeArray<CloudProbe> samples = request.Request.GetData<CloudProbe>();
            for (int userRequestIndex = 0; userRequestIndex < request.UserRequests.Count; userRequestIndex++)
            {
                int existingResultIndex = -1;
                for (int resultIndex = 0; resultIndex < cloudProbeResults.Count; resultIndex++)
                {
                    if (cloudProbeResults[resultIndex].Key.Camera == request.Camera)
                    {
                        existingResultIndex = resultIndex;
                        break;
                    }
                }
                CloudProbeResult result = new CloudProbeResult
                {
                    DensitySource = samples[userRequestIndex].Source.x,
                    DensityTarget = samples[userRequestIndex].Source.y,
                    DensityRaySum = samples[userRequestIndex].Source.z,
                    DensityRayAverage = samples[userRequestIndex].Source.z * oneOver64
                };
                if (existingResultIndex == -1)
                {
                    cloudProbeResults.Add(new KeyValuePair<CloudProbeRequest, CloudProbeResult>(request.UserRequests[userRequestIndex], result));
                }
                else
                {
                    cloudProbeResults[existingResultIndex] = new KeyValuePair<CloudProbeRequest, CloudProbeResult>(request.UserRequests[userRequestIndex], result);
                }
            }

            // lock for pool access
            lock (cloudProbeComputeRequests)
            {
                request.Dispose();
                cloudProbeComputeRequests.Remove(request);
            }
        }

        private void CleanupCloudProbes()
        {
            cloudProbeUserRequests.Clear();
            for (int i = cloudProbeResults.Count - 1; i >= 0; i--)
            {
                if (cloudProbeResults[i].Key == null || cloudProbeResults[i].Key.Camera == null || cloudProbeResults[i].Key.Source == null || !cloudProbeResults[i].Key.Source.gameObject.activeInHierarchy)
                {
                    cloudProbeResults.RemoveAt(i);
                }
            }
        }

        private void DispatchCloudProbes(Camera camera)
        {
            if (cloudProbeShader != null && cloudProbeUserRequests.Count != 0)
            {
                CloudProbeComputeRequest existingRequest;

                // lock for pool access
                lock (cloudProbeComputeRequests)
                {
                    // if we have a pending request, don't stack requests, just wait for it to be done
                    existingRequest = cloudProbeComputeRequests.FirstOrDefault(r => r.Camera == camera);
                    if (existingRequest != null)
                    {
                        return;
                    }
                }

                tempRequests.Clear();
                tempRequests.AddRange(cloudProbeUserRequests.Where(r => r.Camera == camera));
                if (tempRequests.Count == 0)
                {
                    return;
                }

                if (probeRequestPool.Count == 0)
                {
                    existingRequest = new CloudProbeComputeRequest();
                }
                else
                {
                    // lock for pool access
                    lock (cloudProbeComputeRequests)
                    {
                        existingRequest = probeRequestPool[probeRequestPool.Count - 1];
                        probeRequestPool.RemoveAt(probeRequestPool.Count - 1);
                    }
                    existingRequest.Reset();
                }

                ComputeBuffer computeBuffer = null;

                // lock for pool access
                lock (cloudProbeComputeRequests)
                {
                    for (int i = computeBufferPool.Count - 1; i >= 0; i--)
                    {
                        if (computeBufferPool[i].count == tempRequests.Count)
                        {
                            computeBuffer = computeBufferPool[i];
                            computeBufferPool.RemoveAt(i);
                            break;
                        }
                    }
                }
                if (computeBuffer == null)
                {
                    computeBuffer = new ComputeBuffer(tempRequests.Count, 32);
                }
                existingRequest.Camera = camera;
                existingRequest.UserRequests.AddRange(tempRequests);
                existingRequest.Shader = cloudProbeShader;
                existingRequest.Result = computeBuffer;
                for (int i = 0; i < tempRequests.Count; i++)
                {
                    existingRequest.SamplesList.Add(new CloudProbe
                    {
                        Source = (tempRequests[i].Source == null ? Vector4.zero : (Vector4)tempRequests[i].Source.position),
                        Target = (tempRequests[i].Target == null ? Vector4.zero : (Vector4)tempRequests[i].Target.position)
                    });
                }
                existingRequest.Result.SetData(existingRequest.SamplesList);

                // lock for pool access
                lock (cloudProbeComputeRequests)
                {
                    existingRequest.Shader.SetBuffer(cloudProbeShaderKernel, "probe", existingRequest.Result);
                    existingRequest.Shader.Dispatch(cloudProbeShaderKernel, existingRequest.SamplesList.Count, 1, 1);
                    cloudProbeComputeRequests.Add(existingRequest);
                    existingRequest.Request = AsyncGPUReadback.Request(existingRequest.Result, handleCloudProbeResultCallback);
                }
            }
        }

#endif

        private void CameraPreRender(Camera camera)
        {
            if (DisallowCamera(camera))
            {
                return;
            }

            effect.PreRenderCamera(camera);
        }

        private void CameraPostRender(Camera camera)
        {
            if (DisallowCamera(camera))
            {
                return;
            }

            effect.PostRenderCamera(camera);
            UpdateCameraValues(camera);
        }

        private void UpdateCameraValues(Camera camera)
        {
            // add new cameras
            foreach (CameraValues value in cameraValues)
            {
                if (value.Camera == camera)
                {
                    return;
                }
            }
            cameraValues.Add(new CameraValues { Camera = camera, FieldOfView = camera.fieldOfView });
        }

        /// <summary>
        /// Get cloud probe results
        /// </summary>
        /// <param name="camera">The camera to get cloud density with</param>
        /// <param name="source">The source transform to get cloud density with</param>
        /// <param name="dest">The destination transform to get cloud density with, set to null or source if sampling just a single point</param>
        /// <returns>Cloud probe result</returns>
        public CloudProbeResult GetCloudProbe(Camera camera, Transform source, Transform dest)
        {

#if ENABLE_CLOUD_PROBE

            if (cloudProbeShader != null)
            {
                dest = (dest == null ? source : dest);
                foreach (var kv in cloudProbeResults)
                {
                    if (kv.Key.Camera == camera && kv.Key.Source == source && kv.Key.Target == dest)
                    {
                        return kv.Value;
                    }
                }
            }

#endif

            return new CloudProbeResult();
        }

        /// <summary>
        /// Request a cloud probe for a transform, must be called every frame as these are cleared every frame. Call from camera pre cull event.
        /// </summary>
        /// <param name="camera">Current camera</param>
        /// <param name="source">Transform to request</param>
        /// <param name="target">Target or null for just the point of the transform. If target is set, a ray is cast out and a cloud density sample accumulated.</param>
        /// <returns>True if added, false if failure or there are already pending requests for this camera</returns>
        public bool RequestCloudProbe(Camera camera, Transform source, Transform target)
        {

#if ENABLE_CLOUD_PROBE

            if (source == null || cloudProbeShader == null || WeatherMakerScript.GetCameraType(camera) != WeatherMakerCameraType.Normal || !enabled || !gameObject.activeInHierarchy)
            {
                return false;
            }

            // if we have a pending request for this camera, source and target, do not add new request
            target = (target == null ? source : target);
            if (cloudProbeUserRequests.FirstOrDefault(u => u.Camera == camera && u.Source == source && u.Target == target) != null)
            {
                return false;
            }

            cloudProbeUserRequests.Add(new CloudProbeRequest { Camera = camera, Source = source, Target = target });
            return true;

#else

            return false;

#endif

        }

        /// <summary>
        /// Show clouds animated. Animates cover, density, sharpness, light absorption and color. To ensure smooth animations, all noise textures on all layers in both profiles should match.
        /// </summary>
        /// <param name="duration">Transition duration in seconds</param>
        /// <param name="profileName">Cloud profile name</param>
        /// <param name="tweenKey">Tween key</param>
        public void ShowCloudsAnimated(float duration, string profileName, string tweenKey = null)
        {
            if (WeatherMakerScript.Instance != null)
            {
                ShowCloudsAnimated(WeatherMakerScript.Instance.LoadResource<WeatherMakerCloudProfileScript>(profileName), 0.0f, duration, string.Empty);
            }
        }

        /// <summary>
        /// Show clouds animated. Animates cover, density, sharpness, light absorption and color. To ensure smooth animations, all noise textures on all layers in both profiles should match.
        /// </summary>
        /// <param name="newProfile">Cloud profile, or pass null to hide clouds</param>
        /// <param name="transitionDelay">Delay before transition</param>
        /// <param name="transitionDuration">Transition duration in seconds</param>
        /// <param name="tweenKey">Tween key</param>
        public void ShowCloudsAnimated(WeatherMakerCloudProfileScript newProfile, float transitionDelay, float transitionDuration, string tweenKey = null)
        {
            if (!isActiveAndEnabled || currentRenderCloudProfile == null || WeatherMakerScript.Instance == null || WeatherMakerScript.Instance.PerformanceProfile == null ||
                WeatherMakerLightManagerScript.Instance == null || WeatherMakerLightManagerScript.Instance.SunPerspective == null)
            {
                Debug.LogError("Full screen cloud script must be enabled to show clouds");
                return;
            }

            WeatherMakerCelestialObjectScript sun = WeatherMakerLightManagerScript.Instance.SunPerspective;
            tweenKey = (tweenKey ?? string.Empty);
            WeatherMakerCloudProfileScript oldProfile = currentRenderCloudProfile;
            // set to empty profile if null passed in
            if (newProfile == null)
            {
                newProfile = emptyProfile;
            }
            newProfile.EnsureNonNullLayers();

            // dynamic start and end properties
            float endCover1, endCover2, endCover3, endCover4;
            float endDensity1, endDensity2, endDensity3, endDensity4;
            float startSharpness1, startSharpness2, startSharpness3, startSharpness4;
            float endSharpness1, endSharpness2, endSharpness3, endSharpness4;
            float startLightAbsorption1, startLightAbsorption2, startLightAbsorption3, startLightAbsorption4;
            float endLightAbsorption1, endLightAbsorption2, endLightAbsorption3, endLightAbsorption4;
            float startRayOffset1, startRayOffset2, startRayOffset3, startRayOffset4;
            float endRayOffset1, endRayOffset2, endRayOffset3, endRayOffset4;
            Vector4 startScale1, startScale2, startScale3, startScale4;
            Vector4 endScale1, endScale2, endScale3, endScale4;
            Vector4 startMultiplier1, startMultiplier2, startMultiplier3, startMultiplier4;
            Vector4 endMultiplier1, endMultiplier2, endMultiplier3, endMultiplier4;
            //float startMaskScale1, startMaskScale2, startMaskScale3, startMaskScale4;
            //float endMaskScale1, endMaskScale2, endMaskScale3, endMaskScale4;
            float startHeight1, startHeight2, startHeight3, startHeight4;
            float endHeight1, endHeight2, endHeight3, endHeight4;
            WeatherMakerCloudVolumetricProfileScript oldVol = oldProfile.CloudLayerVolumetric1;
            WeatherMakerCloudVolumetricProfileScript newVol = newProfile.CloudLayerVolumetric1;
            Color oldVolColor = oldVol.CloudColor;
            Color oldVolEmissionColor = oldVol.CloudEmissionColor;
            float oldVolCover = oldVol.CloudCover.LastValue;
            float oldVolCoverSecondary = oldVol.CloudCoverSecondary.LastValue;
            float oldVolDensity = oldVol.CloudDensity.LastValue;
            float oldVolNoiseHeightPower = oldVol.CloudHeightNoisePowerVolumetric.LastValue;
            float oldVolHeight = oldProfile.CloudHeight.LastValue;
            float oldVolHeightTop = oldProfile.CloudHeightTop.LastValue;
            float oldVolPlanetRadius = oldProfile.CloudPlanetRadius;
            float oldVolRayOffset = oldVol.CloudRayOffset;
            float oldVolMinRayY = oldVol.CloudMinRayY;
            float oldVolCloudType = oldVol.CloudType.LastValue;
            float oldVolCloudTypeSecondary = oldVol.CloudTypeSecondary.LastValue;
            float oldVolCoverageScale = oldProfile.WeatherMapCloudCoverageScale.LastValue;
            float oldVolCoverageAdder = oldProfile.WeatherMapCloudCoverageAdder.LastValue;
            //Vector3 oldVolCoverageOffset = oldProfile.cloudCoverageOffset;
            float oldVolCoveragePower = oldProfile.WeatherMapCloudCoveragePower.LastValue;
            float oldVolCoverageRotation = oldProfile.WeatherMapCloudCoverageRotation.LastValue;
            float oldVolTypeAdder = oldProfile.WeatherMapCloudTypeAdder.LastValue;
            //Vector3 oldVolTypeOffset = oldProfile.cloudTypeOffset;
            float oldVolTypePower = oldProfile.WeatherMapCloudTypePower.LastValue;
            float oldVolTypeScale = oldProfile.WeatherMapCloudTypeScale.LastValue;
            float oldVolTypeRotation = oldProfile.WeatherMapCloudTypeRotation.LastValue;
            Vector4 oldVolStratusVector = WeatherMakerCloudVolumetricProfileScript.CloudHeightGradientToVector4(oldProfile.CloudLayerVolumetric1.CloudGradientStratus);
            Vector4 oldVolStratoCumulusVector = WeatherMakerCloudVolumetricProfileScript.CloudHeightGradientToVector4(oldProfile.CloudLayerVolumetric1.CloudGradientStratoCumulus);
            Vector4 oldVolCumulusVector = WeatherMakerCloudVolumetricProfileScript.CloudHeightGradientToVector4(oldProfile.CloudLayerVolumetric1.CloudGradientCumulus);
            Vector4 oldVolShapeAnimationVelocity = oldVol.CloudShapeAnimationVelocity;
            Vector4 oldVolDetailAnimationVelocity = oldVol.CloudDetailAnimationVelocity;
            Vector4 oldVolHPhase = oldVol.CloudHenyeyGreensteinPhase;
            float oldVolDirLightMultiplier = oldVol.CloudDirLightMultiplier;
            float oldVolPointSpotLightMultiplier = oldVol.CloudPointSpotLightMultiplier;
            float oldVolAmbientGroundIntensity = oldVol.CloudAmbientGroundIntensity;
            float oldVolAmbientSkyIntensity = oldVol.CloudAmbientSkyIntensity;
            float oldVolSkyIntensity = oldVol.CloudSkyIntensity;
            float oldVolAmbientGroundHeightMultiplier = oldVol.CloudAmbientGroundHeightMultiplier;
            float oldVolAmbientSkyHeightMultiplier = oldVol.CloudAmbientSkyHeightMultiplier;
            float oldVolLightAbsorption = oldVol.CloudLightAbsorption;
            float oldVolDirLightIndirectMultiplier = oldVol.CloudDirLightIndirectMultiplier;
            float oldVolShapeNoiseMin = oldVol.CloudShapeNoiseMin.LastValue;
            float oldVolShapeNoiseMax = oldVol.CloudShapeNoiseMax.LastValue;
            float oldVolPowderMultiplier = oldVol.CloudPowderMultiplier.LastValue;
            float oldVolBottomFade = oldVol.CloudBottomFade.LastValue;
            float oldVolOpticalDistanceMultiplier = oldVol.CloudOpticalDistanceMultiplier;
            float oldVolHorizonFadeMultiplier = oldVol.CloudHorizonFadeMultiplier;
            Vector4 oldVolNoiseScale = oldVol.CloudNoiseScale;
            float oldVolNoiseScalar = oldVol.CloudNoiseScalar.LastValue;
            float oldVolNoiseDetailPower = oldVol.CloudNoiseDetailPower.LastValue;
            Color oldVolDirLightGradientColor = oldVol.CloudDirLightGradientColorColor;
            float oldVolDirLightRayBrightness = oldVol.CloudDirLightRayBrightness;
            float oldVolDirLightRayDecay = oldVol.CloudDirLightRayDecay;
            float oldVolDirLightRaySpread = oldVol.CloudDirLightRaySpread;
            float oldVolDirLightRayStepMultiplier = oldVol.CloudDirLightRayStepMultiplier;
            float oldVolMinRaymarchMultiplier = oldVol.CloudMinRayMarchMultiplier;
            Color oldVolDirLightRayTintColor = oldVol.CloudDirLightRayTintColor;
            Vector3 oldVolWeatherMapScale = oldProfile.WeatherMapScale;
            float oldVolCloudLightDither = oldProfile.CloudLayerVolumetric1.CloudLightDitherLevel;

            Color newVolColor = newVol.CloudColor;
            Color newVolEmissionColor = newVol.CloudEmissionColor;
            float newVolCover = newVol.CloudCover.Random();
            float newVolCoverSecondary = newVol.CloudCoverSecondary.Random();
            float newVolDensity = newVol.CloudDensity.Random();
            float newVolNoiseHeightPower = newVol.CloudHeightNoisePowerVolumetric.Random();
            float newVolHeight = newProfile.CloudHeight.Random();
            float newVolHeightTop = newProfile.CloudHeightTop.Random();
            float newVolPlanetRadius = newProfile.CloudPlanetRadius;
            float newVolRayOffset = newVol.CloudRayOffset;
            float newVolMinRayY = newVol.CloudMinRayY;
            float newVolCloudType = newVol.CloudType.Random();
            float newVolCloudTypeSecondary = newVol.CloudTypeSecondary.Random();
            float newVolCoverageAdder = newProfile.WeatherMapCloudCoverageAdder.Random();
            //Vector3 newVolCoverageOffset = Vector3.zero;
            float newVolCoveragePower = newProfile.WeatherMapCloudCoveragePower.Random();
            float newVolCoverageScale = newProfile.WeatherMapCloudCoverageScale.Random();
            float newVolCoverageRotation = newProfile.WeatherMapCloudCoverageRotation.Random();
            float newVolTypeAdder = newProfile.WeatherMapCloudTypeAdder.Random();
            //Vector3 newVolTypeOffset = Vector3.zero;
            float newVolTypePower = newProfile.WeatherMapCloudTypePower.Random();
            float newVolTypeRotation = newProfile.WeatherMapCloudTypeRotation.Random();
            float newVolTypeScale = newProfile.WeatherMapCloudTypeScale.Random();
            Vector4 newVolStratusVector = WeatherMakerCloudVolumetricProfileScript.CloudHeightGradientToVector4(newProfile.CloudLayerVolumetric1.CloudGradientStratus);
            Vector4 newVolStratoCumulusVector = WeatherMakerCloudVolumetricProfileScript.CloudHeightGradientToVector4(newProfile.CloudLayerVolumetric1.CloudGradientStratoCumulus);
            Vector4 newVolCumulusVector = WeatherMakerCloudVolumetricProfileScript.CloudHeightGradientToVector4(newProfile.CloudLayerVolumetric1.CloudGradientCumulus);
            Vector4 newVolShapeAnimationVelocity = newProfile.CloudLayerVolumetric1.CloudShapeAnimationVelocity;
            Vector4 newVolDetailAnimationVelocity = newProfile.CloudLayerVolumetric1.CloudDetailAnimationVelocity;
            Vector4 newVolHPhase = newVol.CloudHenyeyGreensteinPhase;
            float newVolDirLightMultiplier = newVol.CloudDirLightMultiplier;
            float newVolPointSpotLightMultiplier = newVol.CloudPointSpotLightMultiplier;
            float newVolAmbientGroundIntensity = newVol.CloudAmbientGroundIntensity;
            float newVolAmbientSkyIntensity = newVol.CloudAmbientSkyIntensity;
            float newVolSkyIntensity = newVol.CloudSkyIntensity;
            float newVolAmbientGroundHeightMultiplier = newVol.CloudAmbientGroundHeightMultiplier;
            float newVolAmbientSkyHeightMultiplier = newVol.CloudAmbientSkyHeightMultiplier;
            float newVolLightAbsorption = newVol.CloudLightAbsorption;
            float newVolDirLightIndirectMultiplier = newVol.CloudDirLightIndirectMultiplier;
            float newVolShapeNoiseMin = newVol.CloudShapeNoiseMin.Random();
            float newVolShapeNoiseMax = newVol.CloudShapeNoiseMax.Random();
            float newVoPowderMultiplier = newVol.CloudPowderMultiplier.Random();
            float newVolBottomFade = newVol.CloudBottomFade.Random();
            float newVolOpticalDistanceMultiplier = newVol.CloudOpticalDistanceMultiplier;
            float newVolHorizonFadeMultiplier = newVol.CloudHorizonFadeMultiplier;
            Vector4 newVolNoiseScale = newVol.CloudNoiseScale;
            float newVolNoiseScalar = newVol.CloudNoiseScalar.Random();
            float newVolNoiseDetailPower = newVol.CloudNoiseDetailPower.Random();
            Color newVolDirLightGradientColor = sun.GetGradientColor(newVol.CloudDirLightGradientColor);
            float newVolDirLightRayBrightness = newVol.CloudDirLightRayBrightness;
            float newVolDirLightRayDecay = newVol.CloudDirLightRayDecay;
            float newVolDirLightRaySpread = newVol.CloudDirLightRaySpread;
            float newVolDirLightRayStepMultiplier = newVol.CloudDirLightRayStepMultiplier;
            float newVolMinRaymarchMultiplier = newVol.CloudMinRayMarchMultiplier;
            Color newVolDirLightRayTintColor = newVol.CloudDirLightRayTintColor;
            Vector3 newVolWeatherMapScale = newProfile.WeatherMapScale;
            float newVolCloudLightDither = newProfile.CloudLayerVolumetric1.CloudLightDitherLevel;

            // apply end cover and density
            endCover1 = newProfile.CloudLayer1.CloudCover;
            endCover2 = newProfile.CloudLayer2.CloudCover;
            endCover3 = newProfile.CloudLayer3.CloudCover;
            endCover4 = newProfile.CloudLayer4.CloudCover;
            endDensity1 = newProfile.CloudLayer1.CloudDensity;
            endDensity2 = newProfile.CloudLayer2.CloudDensity;
            endDensity3 = newProfile.CloudLayer3.CloudDensity;
            endDensity4 = newProfile.CloudLayer4.CloudDensity;

            float startCover1 = oldProfile.CloudLayer1.CloudCover;
            float startCover2 = oldProfile.CloudLayer2.CloudCover;
            float startCover3 = oldProfile.CloudLayer3.CloudCover;
            float startCover4 = oldProfile.CloudLayer4.CloudCover;
            float startDensity1 = oldProfile.CloudLayer1.CloudDensity;
            float startDensity2 = oldProfile.CloudLayer2.CloudDensity;
            float startDensity3 = oldProfile.CloudLayer3.CloudDensity;
            float startDensity4 = oldProfile.CloudLayer4.CloudDensity;
            Color startColor1 = oldProfile.CloudLayer1.CloudColor;
            Color startColor2 = oldProfile.CloudLayer2.CloudColor;
            Color startColor3 = oldProfile.CloudLayer3.CloudColor;
            Color startColor4 = oldProfile.CloudLayer4.CloudColor;
            Color startEmissionColor1 = oldProfile.CloudLayer1.CloudEmissionColor;
            Color startEmissionColor2 = oldProfile.CloudLayer2.CloudEmissionColor;
            Color startEmissionColor3 = oldProfile.CloudLayer3.CloudEmissionColor;
            Color startEmissionColor4 = oldProfile.CloudLayer4.CloudEmissionColor;
            float startAmbientMultiplier1 = oldProfile.CloudLayer1.CloudAmbientMultiplier;
            float startAmbientMultiplier2 = oldProfile.CloudLayer2.CloudAmbientMultiplier;
            float startAmbientMultiplier3 = oldProfile.CloudLayer3.CloudAmbientMultiplier;
            float startAmbientMultiplier4 = oldProfile.CloudLayer4.CloudAmbientMultiplier;
            float startScatterMultiplier1 = oldProfile.CloudLayer1.CloudScatterMultiplier;
            float startScatterMultiplier2 = oldProfile.CloudLayer2.CloudScatterMultiplier;
            float startScatterMultiplier3 = oldProfile.CloudLayer3.CloudScatterMultiplier;
            float startScatterMultiplier4 = oldProfile.CloudLayer4.CloudScatterMultiplier;
            Vector4 startVelocity1 = oldProfile.CloudLayer1.CloudNoiseVelocity;
            Vector4 startVelocity2 = oldProfile.CloudLayer2.CloudNoiseVelocity;
            Vector4 startVelocity3 = oldProfile.CloudLayer3.CloudNoiseVelocity;
            Vector4 startVelocity4 = oldProfile.CloudLayer4.CloudNoiseVelocity;
            //Vector4 startMaskVelocity1 = oldProfile.CloudLayer1.CloudNoiseMaskVelocity;
            //Vector4 startMaskVelocity2 = oldProfile.CloudLayer2.CloudNoiseMaskVelocity;
            //Vector4 startMaskVelocity3 = oldProfile.CloudLayer3.CloudNoiseMaskVelocity;
            //Vector4 startMaskVelocity4 = oldProfile.CloudLayer4.CloudNoiseMaskVelocity;
            Vector4 startRotation = new Vector4(oldProfile.CloudLayer1.CloudNoiseRotation.LastValue, oldProfile.CloudLayer2.CloudNoiseRotation.LastValue, oldProfile.CloudLayer3.CloudNoiseRotation.LastValue, oldProfile.CloudLayer4.CloudNoiseRotation.LastValue);
            //Vector4 startMaskRotation = new Vector4(oldProfile.CloudLayer1.CloudNoiseMaskRotation.LastValue, oldProfile.CloudLayer2.CloudNoiseMaskRotation.LastValue, oldProfile.CloudLayer3.CloudNoiseMaskRotation.LastValue, oldProfile.CloudLayer4.CloudNoiseMaskRotation.LastValue);
            float startDirectionalLightIntensityMultiplier = oldProfile.DirectionalLightIntensityMultiplier;
            float startDirectionalLightScatterMultiplier = oldProfile.DirectionalLightScatterMultiplier;

            Color endColor1 = newProfile.CloudLayer1.CloudColor;
            Color endColor2 = newProfile.CloudLayer2.CloudColor;
            Color endColor3 = newProfile.CloudLayer3.CloudColor;
            Color endColor4 = newProfile.CloudLayer4.CloudColor;
            Color endEmissionColor1 = newProfile.CloudLayer1.CloudEmissionColor;
            Color endEmissionColor2 = newProfile.CloudLayer2.CloudEmissionColor;
            Color endEmissionColor3 = newProfile.CloudLayer3.CloudEmissionColor;
            Color endEmissionColor4 = newProfile.CloudLayer4.CloudEmissionColor;
            float endAmbientMultiplier1 = newProfile.CloudLayer1.CloudAmbientMultiplier;
            float endAmbientMultiplier2 = newProfile.CloudLayer2.CloudAmbientMultiplier;
            float endAmbientMultiplier3 = newProfile.CloudLayer3.CloudAmbientMultiplier;
            float endAmbientMultiplier4 = newProfile.CloudLayer4.CloudAmbientMultiplier;
            float endScatterMultiplier1 = newProfile.CloudLayer1.CloudScatterMultiplier;
            float endScatterMultiplier2 = newProfile.CloudLayer2.CloudScatterMultiplier;
            float endScatterMultiplier3 = newProfile.CloudLayer3.CloudScatterMultiplier;
            float endScatterMultiplier4 = newProfile.CloudLayer4.CloudScatterMultiplier;
            Vector3 endVelocity1 = newProfile.CloudLayer1.CloudNoiseVelocity;
            Vector3 endVelocity2 = newProfile.CloudLayer2.CloudNoiseVelocity;
            Vector3 endVelocity3 = newProfile.CloudLayer3.CloudNoiseVelocity;
            Vector3 endVelocity4 = newProfile.CloudLayer4.CloudNoiseVelocity;
            //Vector3 endMaskVelocity1 = newProfile.CloudLayer1.CloudNoiseMaskVelocity;
            //Vector3 endMaskVelocity2 = newProfile.CloudLayer2.CloudNoiseMaskVelocity;
            //Vector3 endMaskVelocity3 = newProfile.CloudLayer3.CloudNoiseMaskVelocity;
            //Vector3 endMaskVelocity4 = newProfile.CloudLayer4.CloudNoiseMaskVelocity;
            Vector4 endRotation = new Vector4(newProfile.CloudLayer1.CloudNoiseRotation.Random(), newProfile.CloudLayer2.CloudNoiseRotation.Random(), newProfile.CloudLayer3.CloudNoiseRotation.Random(), newProfile.CloudLayer4.CloudNoiseRotation.Random());
            //Vector4 endMaskRotation = new Vector4(newProfile.CloudLayer1.CloudNoiseMaskRotation.Random(), newProfile.CloudLayer2.CloudNoiseMaskRotation.Random(), newProfile.CloudLayer3.CloudNoiseMaskRotation.Random(), newProfile.CloudLayer4.CloudNoiseMaskRotation.Random());
            float endDirectionalLightIntensityMultiplier = newProfile.DirectionalLightIntensityMultiplier;
            float endDirectionalLightScatterMultiplier = newProfile.DirectionalLightScatterMultiplier;

            if (startCover1 == 0.0f && startCover2 == 0.0f && startCover3 == 0.0f && startCover4 == 0.0f)
            {
                // transition from no clouds to clouds, start at new profile values
                startSharpness1 = endSharpness1 = newProfile.CloudLayer1.CloudSharpness;
                startSharpness2 = endSharpness2 = newProfile.CloudLayer2.CloudSharpness;
                startSharpness3 = endSharpness3 = newProfile.CloudLayer3.CloudSharpness;
                startSharpness4 = endSharpness4 = newProfile.CloudLayer4.CloudSharpness;
                startLightAbsorption1 = endLightAbsorption1 = newProfile.CloudLayer1.CloudLightAbsorption;
                startLightAbsorption2 = endLightAbsorption2 = newProfile.CloudLayer2.CloudLightAbsorption;
                startLightAbsorption3 = endLightAbsorption3 = newProfile.CloudLayer3.CloudLightAbsorption;
                startLightAbsorption4 = endLightAbsorption4 = newProfile.CloudLayer4.CloudLightAbsorption;
                startScale1 = endScale1 = newProfile.CloudLayer1.CloudNoiseScale;
                startScale2 = endScale2 = newProfile.CloudLayer2.CloudNoiseScale;
                startScale3 = endScale3 = newProfile.CloudLayer3.CloudNoiseScale;
                startScale4 = endScale4 = newProfile.CloudLayer4.CloudNoiseScale;
                startMultiplier1 = endMultiplier1 = newProfile.CloudLayer1.CloudNoiseMultiplier;
                startMultiplier2 = endMultiplier2 = newProfile.CloudLayer2.CloudNoiseMultiplier;
                startMultiplier3 = endMultiplier3 = newProfile.CloudLayer3.CloudNoiseMultiplier;
                startMultiplier4 = endMultiplier4 = newProfile.CloudLayer4.CloudNoiseMultiplier;
                //startMaskScale1 = endMaskScale1 = newProfile.CloudLayer1.CloudNoiseMaskScale;
                //startMaskScale2 = endMaskScale2 = newProfile.CloudLayer2.CloudNoiseMaskScale;
                //startMaskScale3 = endMaskScale3 = newProfile.CloudLayer3.CloudNoiseMaskScale;
                //startMaskScale4 = endMaskScale4 = newProfile.CloudLayer4.CloudNoiseMaskScale;
                startHeight1 = endHeight1 = newProfile.CloudLayer1.CloudHeight;
                startHeight2 = endHeight2 = newProfile.CloudLayer2.CloudHeight;
                startHeight3 = endHeight3 = newProfile.CloudLayer3.CloudHeight;
                startHeight4 = endHeight4 = newProfile.CloudLayer4.CloudHeight;
                _CloudProfile = newProfile;
            }
            else if (endCover1 == 0.0f && endCover2 == 0.0f && endCover3 == 0.0f && endCover4 == 0.0f)
            {
                // transition from clouds to no clouds, start at old profile values
                startSharpness1 = endSharpness1 = oldProfile.CloudLayer1.CloudSharpness;
                startSharpness2 = endSharpness2 = oldProfile.CloudLayer2.CloudSharpness;
                startSharpness3 = endSharpness3 = oldProfile.CloudLayer3.CloudSharpness;
                startSharpness4 = endSharpness4 = oldProfile.CloudLayer4.CloudSharpness;
                startLightAbsorption1 = endLightAbsorption1 = oldProfile.CloudLayer1.CloudLightAbsorption;
                startLightAbsorption2 = endLightAbsorption2 = oldProfile.CloudLayer2.CloudLightAbsorption;
                startLightAbsorption3 = endLightAbsorption3 = oldProfile.CloudLayer3.CloudLightAbsorption;
                startLightAbsorption4 = endLightAbsorption4 = oldProfile.CloudLayer4.CloudLightAbsorption;
                startScale1 = endScale1 = oldProfile.CloudLayer1.CloudNoiseScale;
                startScale2 = endScale2 = oldProfile.CloudLayer2.CloudNoiseScale;
                startScale3 = endScale3 = oldProfile.CloudLayer3.CloudNoiseScale;
                startScale4 = endScale4 = oldProfile.CloudLayer4.CloudNoiseScale;
                startMultiplier1 = endMultiplier1 = oldProfile.CloudLayer1.CloudNoiseMultiplier;
                startMultiplier2 = endMultiplier2 = oldProfile.CloudLayer2.CloudNoiseMultiplier;
                startMultiplier3 = endMultiplier3 = oldProfile.CloudLayer3.CloudNoiseMultiplier;
                startMultiplier4 = endMultiplier4 = oldProfile.CloudLayer4.CloudNoiseMultiplier;
                //startMaskScale1 = endMaskScale1 = oldProfile.CloudLayer1.CloudNoiseMaskScale;
                //startMaskScale2 = endMaskScale2 = oldProfile.CloudLayer2.CloudNoiseMaskScale;
                //startMaskScale3 = endMaskScale3 = oldProfile.CloudLayer3.CloudNoiseMaskScale;
                //startMaskScale4 = endMaskScale4 = oldProfile.CloudLayer4.CloudNoiseMaskScale;
                startHeight1 = endHeight1 = oldProfile.CloudLayer1.CloudHeight;
                startHeight2 = endHeight2 = oldProfile.CloudLayer2.CloudHeight;
                startHeight3 = endHeight3 = oldProfile.CloudLayer3.CloudHeight;
                startHeight4 = endHeight4 = oldProfile.CloudLayer4.CloudHeight;

                // transition with old profile, the new profile is empty and can't be used for transition
                _CloudProfile = oldProfile;
            }
            else
            {
                // regular transition from one clouds to another, transition normally
                startScale1 = oldProfile.CloudLayer1.CloudNoiseScale;
                startScale2 = oldProfile.CloudLayer2.CloudNoiseScale;
                startScale3 = oldProfile.CloudLayer3.CloudNoiseScale;
                startScale4 = oldProfile.CloudLayer4.CloudNoiseScale;
                endScale1 = newProfile.CloudLayer1.CloudNoiseScale;
                endScale2 = newProfile.CloudLayer2.CloudNoiseScale;
                endScale3 = newProfile.CloudLayer3.CloudNoiseScale;
                endScale4 = newProfile.CloudLayer4.CloudNoiseScale;
                startMultiplier1 = oldProfile.CloudLayer1.CloudNoiseMultiplier;
                startMultiplier2 = oldProfile.CloudLayer2.CloudNoiseMultiplier;
                startMultiplier3 = oldProfile.CloudLayer3.CloudNoiseMultiplier;
                startMultiplier4 = oldProfile.CloudLayer4.CloudNoiseMultiplier;
                endMultiplier1 = newProfile.CloudLayer1.CloudNoiseMultiplier;
                endMultiplier2 = newProfile.CloudLayer2.CloudNoiseMultiplier;
                endMultiplier3 = newProfile.CloudLayer3.CloudNoiseMultiplier;
                endMultiplier4 = newProfile.CloudLayer4.CloudNoiseMultiplier;
                //startMaskScale1 = oldProfile.CloudLayer1.CloudNoiseMaskScale;
                //startMaskScale2 = oldProfile.CloudLayer2.CloudNoiseMaskScale;
                //startMaskScale3 = oldProfile.CloudLayer3.CloudNoiseMaskScale;
                //startMaskScale4 = oldProfile.CloudLayer4.CloudNoiseMaskScale;
                //endMaskScale1 = newProfile.CloudLayer1.CloudNoiseMaskScale;
                //endMaskScale2 = newProfile.CloudLayer2.CloudNoiseMaskScale;
                //endMaskScale3 = newProfile.CloudLayer3.CloudNoiseMaskScale;
                //endMaskScale4 = newProfile.CloudLayer4.CloudNoiseMaskScale;
                startSharpness1 = oldProfile.CloudLayer1.CloudSharpness;
                startSharpness2 = oldProfile.CloudLayer2.CloudSharpness;
                startSharpness3 = oldProfile.CloudLayer3.CloudSharpness;
                startSharpness4 = oldProfile.CloudLayer4.CloudSharpness;
                endSharpness1 = newProfile.CloudLayer1.CloudSharpness;
                endSharpness2 = newProfile.CloudLayer2.CloudSharpness;
                endSharpness3 = newProfile.CloudLayer3.CloudSharpness;
                endSharpness4 = newProfile.CloudLayer4.CloudSharpness;
                startLightAbsorption1 = oldProfile.CloudLayer1.CloudLightAbsorption;
                startLightAbsorption2 = oldProfile.CloudLayer2.CloudLightAbsorption;
                startLightAbsorption3 = oldProfile.CloudLayer3.CloudLightAbsorption;
                startLightAbsorption4 = oldProfile.CloudLayer4.CloudLightAbsorption;
                endLightAbsorption1 = newProfile.CloudLayer1.CloudLightAbsorption;
                endLightAbsorption2 = newProfile.CloudLayer2.CloudLightAbsorption;
                endLightAbsorption3 = newProfile.CloudLayer3.CloudLightAbsorption;
                endLightAbsorption4 = newProfile.CloudLayer4.CloudLightAbsorption;
                startHeight1 = oldProfile.CloudLayer1.CloudHeight;
                startHeight2 = oldProfile.CloudLayer2.CloudHeight;
                startHeight3 = oldProfile.CloudLayer3.CloudHeight;
                startHeight4 = oldProfile.CloudLayer4.CloudHeight;
                endHeight1 = newProfile.CloudLayer1.CloudHeight;
                endHeight2 = newProfile.CloudLayer2.CloudHeight;
                endHeight3 = newProfile.CloudLayer3.CloudHeight;
                endHeight4 = newProfile.CloudLayer4.CloudHeight;

                // use new profile for transition
                _CloudProfile = newProfile;
            }
            startRayOffset1 = oldProfile.CloudLayer1.CloudRayOffset;
            startRayOffset2 = oldProfile.CloudLayer2.CloudRayOffset;
            startRayOffset3 = oldProfile.CloudLayer3.CloudRayOffset;
            startRayOffset4 = oldProfile.CloudLayer4.CloudRayOffset;
            endRayOffset1 = newProfile.CloudLayer1.CloudRayOffset;
            endRayOffset2 = newProfile.CloudLayer2.CloudRayOffset;
            endRayOffset3 = newProfile.CloudLayer3.CloudRayOffset;
            endRayOffset4 = newProfile.CloudLayer4.CloudRayOffset;

            DeleteAndTransitionRenderProfile(newProfile);

            // create temp object for animation, we don't want to modify variables in the actual assets during animation
            if (Application.isPlaying && _CloudProfile.name.IndexOf("(clone)", StringComparison.OrdinalIgnoreCase) < 0)
            {
                _CloudProfile = _CloudProfile.Clone();
            }
            lastProfile = currentRenderCloudProfile = _CloudProfile;

            if (!WeatherMakerScript.Instance.PerformanceProfile.EnableVolumetricClouds)
            {
                // turn off volumetric clouds if not allowed
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudCover.LastValue = oldVolCover = newVolCover = 0.0f;
            }

            if (newVolCover > 0.0f)
            {
                if ((currentRenderCloudProfile.CloudLayerVolumetric1.FlatLayerMask & WeatherMakerVolumetricCloudsFlatLayerMask.One) != WeatherMakerVolumetricCloudsFlatLayerMask.One)
                {
                    endCover1 = currentRenderCloudProfile.CloudLayer1.CloudCover = 0.0f;
                }
                if ((currentRenderCloudProfile.CloudLayerVolumetric1.FlatLayerMask & WeatherMakerVolumetricCloudsFlatLayerMask.Two) != WeatherMakerVolumetricCloudsFlatLayerMask.Two)
                {
                    endCover2 = currentRenderCloudProfile.CloudLayer2.CloudCover = 0.0f;
                }
                if ((currentRenderCloudProfile.CloudLayerVolumetric1.FlatLayerMask & WeatherMakerVolumetricCloudsFlatLayerMask.Three) != WeatherMakerVolumetricCloudsFlatLayerMask.Three)
                {
                    endCover3 = currentRenderCloudProfile.CloudLayer3.CloudCover = 0.0f;
                }
                if ((currentRenderCloudProfile.CloudLayerVolumetric1.FlatLayerMask & WeatherMakerVolumetricCloudsFlatLayerMask.Four) != WeatherMakerVolumetricCloudsFlatLayerMask.Four)
                {
                    endCover4 = currentRenderCloudProfile.CloudLayer4.CloudCover = 0.0f;
                }
            }

            currentRenderCloudProfile.AtmosphereProfile = (currentRenderCloudProfile.AtmosphereProfile ?? oldProfile.AtmosphereProfile); // copy aurora profile straight over if new clouds have no aurora profile
            currentRenderCloudProfile.AuroraProfile = (currentRenderCloudProfile.AuroraProfile ?? oldProfile.AuroraProfile); // copy aurora profile straight over if new clouds have no aurora profile
            currentRenderCloudProfile.isAnimating = true;

            // animate animatable properties
            FloatTween tween = TweenFactory.Tween("WeatherMakerClouds_" + GetInstanceID() + tweenKey, 0.0f, 1.0f, transitionDuration, TweenScaleFunctions.QuadraticEaseInOut, (ITween<float> c) =>
            {
                float progress = c.CurrentValue;
                currentRenderCloudProfile.CloudLayer1.CloudNoiseScale = Vector4.Lerp(startScale1, endScale1, progress);
                currentRenderCloudProfile.CloudLayer2.CloudNoiseScale = Vector4.Lerp(startScale2, endScale2, progress);
                currentRenderCloudProfile.CloudLayer3.CloudNoiseScale = Vector4.Lerp(startScale3, endScale3, progress);
                currentRenderCloudProfile.CloudLayer4.CloudNoiseScale = Vector4.Lerp(startScale4, endScale4, progress);
                currentRenderCloudProfile.CloudLayer1.CloudNoiseMultiplier = Vector4.Lerp(startMultiplier1, endMultiplier1, progress);
                currentRenderCloudProfile.CloudLayer2.CloudNoiseMultiplier = Vector4.Lerp(startMultiplier2, endMultiplier2, progress);
                currentRenderCloudProfile.CloudLayer3.CloudNoiseMultiplier = Vector4.Lerp(startMultiplier3, endMultiplier3, progress);
                currentRenderCloudProfile.CloudLayer4.CloudNoiseMultiplier = Vector4.Lerp(startMultiplier4, endMultiplier4, progress);
                currentRenderCloudProfile.CloudLayer1.CloudNoiseRotation.LastValue = Mathf.Lerp(startRotation.x, endRotation.x, progress);
                currentRenderCloudProfile.CloudLayer2.CloudNoiseRotation.LastValue = Mathf.Lerp(startRotation.y, endRotation.y, progress);
                currentRenderCloudProfile.CloudLayer3.CloudNoiseRotation.LastValue = Mathf.Lerp(startRotation.z, endRotation.z, progress);
                currentRenderCloudProfile.CloudLayer4.CloudNoiseRotation.LastValue = Mathf.Lerp(startRotation.w, endRotation.w, progress);
                currentRenderCloudProfile.CloudLayer1.CloudNoiseVelocity = Vector3.Lerp(startVelocity1, endVelocity1, progress);
                currentRenderCloudProfile.CloudLayer2.CloudNoiseVelocity = Vector3.Lerp(startVelocity2, endVelocity2, progress);
                currentRenderCloudProfile.CloudLayer3.CloudNoiseVelocity = Vector3.Lerp(startVelocity3, endVelocity3, progress);
                currentRenderCloudProfile.CloudLayer4.CloudNoiseVelocity = Vector3.Lerp(startVelocity4, endVelocity4, progress);
                //currentRenderCloudProfile.CloudLayer1.CloudNoiseMaskScale = Mathf.Lerp(startMaskScale1, endMaskScale1, progress);
                //currentRenderCloudProfile.CloudLayer2.CloudNoiseMaskScale = Mathf.Lerp(startMaskScale2, endMaskScale2, progress);
                //currentRenderCloudProfile.CloudLayer3.CloudNoiseMaskScale = Mathf.Lerp(startMaskScale3, endMaskScale3, progress);
                //currentRenderCloudProfile.CloudLayer4.CloudNoiseMaskScale = Mathf.Lerp(startMaskScale4, endMaskScale4, progress);
                //currentRenderCloudProfile.CloudLayer1.CloudNoiseMaskRotation.LastValue = Mathf.Lerp(startMaskRotation.x, endMaskRotation.x, progress);
                //currentRenderCloudProfile.CloudLayer2.CloudNoiseMaskRotation.LastValue = Mathf.Lerp(startMaskRotation.y, endMaskRotation.y, progress);
                //currentRenderCloudProfile.CloudLayer3.CloudNoiseMaskRotation.LastValue = Mathf.Lerp(startMaskRotation.z, endMaskRotation.z, progress);
                //currentRenderCloudProfile.CloudLayer4.CloudNoiseMaskRotation.LastValue = Mathf.Lerp(startMaskRotation.w, endMaskRotation.w, progress);
                //currentRenderCloudProfile.CloudLayer1.CloudNoiseMaskVelocity = Vector3.Lerp(startMaskVelocity1, endMaskVelocity1, progress);
                //currentRenderCloudProfile.CloudLayer2.CloudNoiseMaskVelocity = Vector3.Lerp(startMaskVelocity2, endMaskVelocity2, progress);
                //currentRenderCloudProfile.CloudLayer3.CloudNoiseMaskVelocity = Vector3.Lerp(startMaskVelocity3, endMaskVelocity3, progress);
                //currentRenderCloudProfile.CloudLayer4.CloudNoiseMaskVelocity = Vector3.Lerp(startMaskVelocity4, endMaskVelocity4, progress);
                currentRenderCloudProfile.CloudLayer1.CloudCover = Mathf.Lerp(startCover1, endCover1, progress);
                currentRenderCloudProfile.CloudLayer2.CloudCover = Mathf.Lerp(startCover2, endCover2, progress);
                currentRenderCloudProfile.CloudLayer3.CloudCover = Mathf.Lerp(startCover3, endCover3, progress);
                currentRenderCloudProfile.CloudLayer4.CloudCover = Mathf.Lerp(startCover4, endCover4, progress);
                currentRenderCloudProfile.CloudLayer1.CloudDensity = Mathf.Lerp(startDensity1, endDensity1, progress);
                currentRenderCloudProfile.CloudLayer2.CloudDensity = Mathf.Lerp(startDensity2, endDensity2, progress);
                currentRenderCloudProfile.CloudLayer3.CloudDensity = Mathf.Lerp(startDensity3, endDensity3, progress);
                currentRenderCloudProfile.CloudLayer4.CloudDensity = Mathf.Lerp(startDensity4, endDensity4, progress);
                currentRenderCloudProfile.CloudLayer1.CloudSharpness = Mathf.Lerp(startSharpness1, endSharpness1, progress);
                currentRenderCloudProfile.CloudLayer2.CloudSharpness = Mathf.Lerp(startSharpness2, endSharpness2, progress);
                currentRenderCloudProfile.CloudLayer3.CloudSharpness = Mathf.Lerp(startSharpness3, endSharpness3, progress);
                currentRenderCloudProfile.CloudLayer4.CloudSharpness = Mathf.Lerp(startSharpness4, endSharpness4, progress);
                currentRenderCloudProfile.CloudLayer1.CloudLightAbsorption = Mathf.Lerp(startLightAbsorption1, endLightAbsorption1, progress);
                currentRenderCloudProfile.CloudLayer2.CloudLightAbsorption = Mathf.Lerp(startLightAbsorption2, endLightAbsorption2, progress);
                currentRenderCloudProfile.CloudLayer3.CloudLightAbsorption = Mathf.Lerp(startLightAbsorption3, endLightAbsorption3, progress);
                currentRenderCloudProfile.CloudLayer4.CloudLightAbsorption = Mathf.Lerp(startLightAbsorption4, endLightAbsorption4, progress);
                currentRenderCloudProfile.CloudLayer1.CloudColor = Color.Lerp(startColor1, endColor1, progress);
                currentRenderCloudProfile.CloudLayer2.CloudColor = Color.Lerp(startColor2, endColor2, progress);
                currentRenderCloudProfile.CloudLayer3.CloudColor = Color.Lerp(startColor3, endColor3, progress);
                currentRenderCloudProfile.CloudLayer4.CloudColor = Color.Lerp(startColor4, endColor4, progress);
                currentRenderCloudProfile.CloudLayer1.CloudEmissionColor = Color.Lerp(startEmissionColor1, endEmissionColor1, progress);
                currentRenderCloudProfile.CloudLayer2.CloudEmissionColor = Color.Lerp(startEmissionColor2, endEmissionColor2, progress);
                currentRenderCloudProfile.CloudLayer3.CloudEmissionColor = Color.Lerp(startEmissionColor3, endEmissionColor3, progress);
                currentRenderCloudProfile.CloudLayer4.CloudEmissionColor = Color.Lerp(startEmissionColor4, endEmissionColor4, progress);
                currentRenderCloudProfile.CloudLayer1.CloudAmbientMultiplier = Mathf.Lerp(startAmbientMultiplier1, endAmbientMultiplier1, progress);
                currentRenderCloudProfile.CloudLayer2.CloudAmbientMultiplier = Mathf.Lerp(startAmbientMultiplier2, endAmbientMultiplier2, progress);
                currentRenderCloudProfile.CloudLayer3.CloudAmbientMultiplier = Mathf.Lerp(startAmbientMultiplier3, endAmbientMultiplier3, progress);
                currentRenderCloudProfile.CloudLayer4.CloudAmbientMultiplier = Mathf.Lerp(startAmbientMultiplier4, endAmbientMultiplier4, progress);
                currentRenderCloudProfile.CloudLayer1.CloudScatterMultiplier = Mathf.Lerp(startScatterMultiplier1, endScatterMultiplier1, progress);
                currentRenderCloudProfile.CloudLayer2.CloudScatterMultiplier = Mathf.Lerp(startScatterMultiplier2, endScatterMultiplier2, progress);
                currentRenderCloudProfile.CloudLayer3.CloudScatterMultiplier = Mathf.Lerp(startScatterMultiplier3, endScatterMultiplier3, progress);
                currentRenderCloudProfile.CloudLayer4.CloudScatterMultiplier = Mathf.Lerp(startScatterMultiplier4, endScatterMultiplier4, progress);
                currentRenderCloudProfile.CloudLayer1.CloudHeight = Mathf.Lerp(startHeight1, endHeight1, progress);
                currentRenderCloudProfile.CloudLayer2.CloudHeight = Mathf.Lerp(startHeight2, endHeight2, progress);
                currentRenderCloudProfile.CloudLayer3.CloudHeight = Mathf.Lerp(startHeight3, endHeight3, progress);
                currentRenderCloudProfile.CloudLayer4.CloudHeight = Mathf.Lerp(startHeight4, endHeight4, progress);
                currentRenderCloudProfile.CloudLayer1.CloudRayOffset = Mathf.Lerp(startRayOffset1, endRayOffset1, progress);
                currentRenderCloudProfile.CloudLayer2.CloudRayOffset = Mathf.Lerp(startRayOffset2, endRayOffset2, progress);
                currentRenderCloudProfile.CloudLayer3.CloudRayOffset = Mathf.Lerp(startRayOffset3, endRayOffset3, progress);
                currentRenderCloudProfile.CloudLayer4.CloudRayOffset = Mathf.Lerp(startRayOffset4, endRayOffset4, progress);

                currentRenderCloudProfile.CloudLayerVolumetric1.CloudColor = Color.Lerp(oldVolColor, newVolColor, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudCover.LastValue = Mathf.Lerp(oldVolCover, newVolCover, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudCoverSecondary.LastValue = Mathf.Lerp(oldVolCoverSecondary, newVolCoverSecondary, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudDensity.LastValue = Mathf.Lerp(oldVolDensity, newVolDensity, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudHeightNoisePowerVolumetric.LastValue = Mathf.Lerp(oldVolNoiseHeightPower, newVolNoiseHeightPower, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudEmissionColor = Color.Lerp(oldVolEmissionColor, newVolEmissionColor, progress);
                currentRenderCloudProfile.CloudHeight.LastValue = Mathf.Lerp(oldVolHeight, newVolHeight, progress);
                currentRenderCloudProfile.CloudHeightTop.LastValue = Mathf.Lerp(oldVolHeightTop, newVolHeightTop, progress);
                currentRenderCloudProfile.CloudPlanetRadius = Mathf.Lerp(oldVolPlanetRadius, newVolPlanetRadius, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudHenyeyGreensteinPhase = Vector4.Lerp(oldVolHPhase, newVolHPhase, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudDirLightMultiplier = Mathf.Lerp(oldVolDirLightMultiplier, newVolDirLightMultiplier, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudLightDitherLevel = Mathf.Lerp(oldVolCloudLightDither, newVolCloudLightDither, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudPointSpotLightMultiplier = Mathf.Lerp(oldVolPointSpotLightMultiplier, newVolPointSpotLightMultiplier, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudAmbientGroundIntensity = Mathf.Lerp(oldVolAmbientGroundIntensity, newVolAmbientGroundIntensity, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudAmbientSkyIntensity = Mathf.Lerp(oldVolAmbientSkyIntensity, newVolAmbientSkyIntensity, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudSkyIntensity = Mathf.Lerp(oldVolSkyIntensity, newVolSkyIntensity, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudAmbientGroundHeightMultiplier = Mathf.Lerp(oldVolAmbientGroundHeightMultiplier, newVolAmbientGroundHeightMultiplier, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudAmbientSkyHeightMultiplier = Mathf.Lerp(oldVolAmbientSkyHeightMultiplier, newVolAmbientSkyHeightMultiplier, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudLightAbsorption = Mathf.Lerp(oldVolLightAbsorption, newVolLightAbsorption, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudDirLightIndirectMultiplier = Mathf.Lerp(oldVolDirLightIndirectMultiplier, newVolDirLightIndirectMultiplier, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudShapeNoiseMin.LastValue = Mathf.Lerp(oldVolShapeNoiseMin, newVolShapeNoiseMin, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudShapeNoiseMax.LastValue = Mathf.Lerp(oldVolShapeNoiseMax, newVolShapeNoiseMax, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudPowderMultiplier.LastValue = Mathf.Lerp(oldVolPowderMultiplier, newVoPowderMultiplier, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudBottomFade.LastValue = Mathf.Lerp(oldVolBottomFade, newVolBottomFade, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudOpticalDistanceMultiplier = Mathf.Lerp(oldVolOpticalDistanceMultiplier, newVolOpticalDistanceMultiplier, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudHorizonFadeMultiplier = Mathf.Lerp(oldVolHorizonFadeMultiplier, newVolHorizonFadeMultiplier, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudShapeAnimationVelocity = Vector4.Lerp(oldVolShapeAnimationVelocity, newVolShapeAnimationVelocity, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudDetailAnimationVelocity = Vector4.Lerp(oldVolDetailAnimationVelocity, newVolDetailAnimationVelocity, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudNoiseScale = Vector4.Lerp(oldVolNoiseScale, newVolNoiseScale, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudDirLightGradientColorColor = Color.Lerp(oldVolDirLightGradientColor, newVolDirLightGradientColor, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudNoiseScalar.LastValue = Mathf.Lerp(oldVolNoiseScalar, newVolNoiseScalar, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudNoiseDetailPower.LastValue = Mathf.Lerp(oldVolNoiseDetailPower, newVolNoiseDetailPower, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudRayOffset = Mathf.Lerp(oldVolRayOffset, newVolRayOffset, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudMinRayY = Mathf.Lerp(oldVolMinRayY, newVolMinRayY, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudMinRayMarchMultiplier = Mathf.Lerp(oldVolMinRaymarchMultiplier, newVolMinRaymarchMultiplier, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudType.LastValue = Mathf.Lerp(oldVolCloudType, newVolCloudType, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudTypeSecondary.LastValue = Mathf.Lerp(oldVolCloudTypeSecondary, newVolCloudTypeSecondary, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudDirLightRayBrightness = Mathf.Lerp(oldVolDirLightRayBrightness, newVolDirLightRayBrightness, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudDirLightRayDecay = Mathf.Lerp(oldVolDirLightRayDecay, newVolDirLightRayDecay, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudDirLightRaySpread = Mathf.Lerp(oldVolDirLightRaySpread, newVolDirLightRaySpread, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudDirLightRayStepMultiplier = Mathf.Lerp(oldVolDirLightRayStepMultiplier, newVolDirLightRayStepMultiplier, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudDirLightRayTintColor = Color.Lerp(oldVolDirLightRayTintColor, newVolDirLightRayTintColor, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudGradientStratusVector = Vector4.Lerp(oldVolStratusVector, newVolStratusVector, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudGradientStratoCumulusVector = Vector4.Lerp(oldVolStratoCumulusVector, newVolStratoCumulusVector, progress);
                currentRenderCloudProfile.CloudLayerVolumetric1.CloudGradientCumulusVector = Vector4.Lerp(oldVolCumulusVector, newVolCumulusVector, progress);

                currentRenderCloudProfile.WeatherMapCloudCoverageAdder.LastValue = Mathf.Lerp(oldVolCoverageAdder, newVolCoverageAdder, progress);
                //currentRenderCloudProfile.cloudCoverageOffset = Vector3.Lerp(oldVolCoverageOffset, newVolCoverageOffset, progress);
                currentRenderCloudProfile.WeatherMapCloudCoveragePower.LastValue = Mathf.Lerp(oldVolCoveragePower, newVolCoveragePower, progress);
                currentRenderCloudProfile.WeatherMapCloudCoverageRotation.LastValue = Mathf.Lerp(oldVolCoverageRotation, newVolCoverageRotation, progress);
                currentRenderCloudProfile.WeatherMapCloudCoverageScale.LastValue = Mathf.Lerp(oldVolCoverageScale, newVolCoverageScale, progress);
                currentRenderCloudProfile.WeatherMapCloudTypeAdder.LastValue = Mathf.Lerp(oldVolTypeAdder, newVolTypeAdder, progress);
                //currentRenderCloudProfile.cloudTypeOffset = Vector3.Lerp(oldVolTypeOffset, newVolTypeOffset, progress);
                currentRenderCloudProfile.WeatherMapCloudTypePower.LastValue = Mathf.Lerp(oldVolTypePower, newVolTypePower, progress);
                currentRenderCloudProfile.WeatherMapCloudTypeRotation.LastValue = Mathf.Lerp(oldVolTypeRotation, newVolTypeRotation, progress);
                currentRenderCloudProfile.WeatherMapCloudTypeScale.LastValue = Mathf.Lerp(oldVolTypeScale, newVolTypeScale, progress);
                currentRenderCloudProfile.WeatherMapScale = Vector3.Lerp(oldVolWeatherMapScale, newVolWeatherMapScale, progress);

                currentRenderCloudProfile.DirectionalLightIntensityMultiplier = Mathf.Lerp(startDirectionalLightIntensityMultiplier, endDirectionalLightIntensityMultiplier, progress);
                currentRenderCloudProfile.DirectionalLightScatterMultiplier = Mathf.Lerp(startDirectionalLightScatterMultiplier, endDirectionalLightScatterMultiplier, progress);

                currentRenderCloudProfile.CloudLayerVolumetric1.lerpProgress = progress;
            }, (ITween<float> c) =>
            {
                currentRenderCloudProfile.isAnimating = false;
            });
            tween.Delay = transitionDelay;
        }

        internal void ForceReload()
        {
            ShowCloudsAnimated(lastProfileOriginal, 0.0f, 0.01f);
        }

        /// <summary>
        /// Hide clouds animated, all layers
        /// </summary>
        /// <param name="duration">Transition duration in seconds</param>
        public void HideCloudsAnimated(float duration)
        {
            ShowCloudsAnimated((WeatherMakerCloudProfileScript)null, 0.0f, duration);
        }

#if ENABLE_CLOUD_PROBE

        public static bool CloudProbeEnabled
        {
            get { return SystemInfo.supportsComputeShaders && SystemInfo.supportsAsyncGPUReadback; }
        }

#else

        /// <summary>
        /// Whether cloud probes are enabled
        /// </summary>
        public static bool CloudProbeEnabled { get { return false; } }

#endif

        /// <summary>
        /// Get the current global shadow from clouds
        /// </summary>
        public float CloudGlobalShadow { get; private set; }

        private static WeatherMakerFullScreenCloudsScript instance;
        /// <summary>
        /// Shared instance of full screen clouds script
        /// </summary>
        public static WeatherMakerFullScreenCloudsScript Instance
        {
            get { return WeatherMakerScript.FindOrCreateInstance(ref instance); }
        }
    }
}
