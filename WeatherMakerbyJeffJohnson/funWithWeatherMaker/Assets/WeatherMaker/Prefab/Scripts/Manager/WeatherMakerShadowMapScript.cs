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
using UnityEngine.Rendering;

namespace DigitalRuby.WeatherMaker
{
    /// <summary>
    /// Shadow map generator script, add to a dir light
    /// </summary>
    [RequireComponent(typeof(Light))]
    [ExecuteInEditMode]
    public class WeatherMakerShadowMapScript : MonoBehaviour
    {
        /// <summary>The texture name for shaders to access the cascaded shadow map, null/empty for none.</summary>
        [Tooltip("The texture name for shaders to access the cascaded shadow map, null/empty for none.")]
        public string ShaderTextureName = "_WeatherMakerShadowMapTexture";

        /// <summary>Optional material to add cloud shadows to the shadow map, null for no cloud shadows.</summary>
        [Tooltip("Optional material to add cloud shadows to the shadow map, null for no cloud shadows.")]
        public Material CloudShadowMaterial;

        /// <summary>Gaussian blur material.</summary>
        [Tooltip("Gaussian blur material.")]
        public Material BlurMaterial;

        private Light _light;
        private CommandBuffer _commandBufferDepthShadows;
        private CommandBuffer _commandBufferScreenSpaceShadows1;
        private CommandBuffer _commandBufferScreenSpaceShadows2;

        private RenderTexture tempShadowBuffer;

        private void RemoveCommandBuffer(LightEvent evt, ref CommandBuffer commandBuffer, bool nullOut = true)
        {

#if !UNITY_URP

            if (_light != null && commandBuffer != null)
            {
                // putting these in try/catch as Unity 2018.3 throws weird errors
                try
                {
                    _light.RemoveCommandBuffer(evt, commandBuffer);
                    if (nullOut)
                    {
                        commandBuffer.Clear();
                        commandBuffer.Release();
                    }
                }
                catch
                {
                    // eat exceptions
                }
                if (nullOut)
                {
                    commandBuffer = null;
                }
            }

#endif

        }

        private void CleanupCommandBuffers()
        {
            RemoveCommandBuffer(LightEvent.AfterShadowMap, ref _commandBufferDepthShadows);
            RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, ref _commandBufferScreenSpaceShadows1);
            RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, ref _commandBufferScreenSpaceShadows2);
        }

        private void AddShadowMapCommandBuffer()
        {

#if !UNITY_URP

            if (_light != null && !string.IsNullOrEmpty(ShaderTextureName))
            {
                if (_commandBufferDepthShadows == null)
                {
                    _commandBufferDepthShadows = new CommandBuffer { name = "WeatherMakerShadowMapDepthShadowScript_" + gameObject.name };
                }
                _commandBufferDepthShadows.Clear();
                _light.RemoveCommandBuffer(LightEvent.AfterShadowMap, _commandBufferDepthShadows);
                _commandBufferDepthShadows.SetGlobalTexture(ShaderTextureName, BuiltinRenderTextureType.CurrentActive);
                _light.AddCommandBuffer(LightEvent.AfterShadowMap, _commandBufferDepthShadows);
            }

#endif

        }

        private void AddScreenSpaceShadowsCommandBuffer(Camera camera)
        {
            if (CloudShadowMaterial != null && _light != null && _light.type == LightType.Directional &&
                _light.shadows != LightShadows.None && WeatherMakerLightManagerScript.Instance != null &&
                WeatherMakerLightManagerScript.ScreenSpaceShadowMode != BuiltinShaderMode.Disabled &&
                (WeatherMakerScript.Instance == null || WeatherMakerScript.Instance.PerformanceProfile == null ||
                (WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudShadowDownsampleScale != WeatherMakerDownsampleScale.Disabled &&
                WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudShadowSampleCount > 0)))
            {
                if (_commandBufferScreenSpaceShadows1 == null)
                {
                    // copy the screen space shadow texture for re-use later
                    _commandBufferScreenSpaceShadows1 = new CommandBuffer { name = "WeatherMakerShadowMapScreensSpaceShadowScriptBlur_" + gameObject.name };
                }
                if (_commandBufferScreenSpaceShadows2 == null)
                {
                    // copy the screen space shadow texture for re-use later
                    _commandBufferScreenSpaceShadows2 = new CommandBuffer { name = "WeatherMakerShadowMapScreensSpaceShadowScriptBlit_" + gameObject.name };
                }

                _commandBufferScreenSpaceShadows1.Clear();
                _commandBufferScreenSpaceShadows1.SetGlobalFloat(WMS._BlendOp, (float)BlendOp.Add);
                _commandBufferScreenSpaceShadows1.SetGlobalFloat(WMS._SrcBlendMode, (float)BlendMode.One);
                _commandBufferScreenSpaceShadows1.SetGlobalFloat(WMS._DstBlendMode, (float)BlendMode.Zero);

#if UNITY_URP

                _commandBufferScreenSpaceShadows1.SetGlobalTexture(WMS._MainTex5, WMS._ScreenSpaceShadowmapTexture);
                _commandBufferScreenSpaceShadows1.Blit(WMS._ScreenSpaceShadowmapTexture, tempShadowBuffer, CloudShadowMaterial, 0);
                camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBufferScreenSpaceShadows1);

#else

                _commandBufferScreenSpaceShadows1.SetGlobalTexture(WMS._MainTex5, BuiltinRenderTextureType.CurrentActive);
                _commandBufferScreenSpaceShadows1.Blit(BuiltinRenderTextureType.CurrentActive, tempShadowBuffer, CloudShadowMaterial, 0);
                _light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, _commandBufferScreenSpaceShadows1);

#endif

                // screen space shadow mask does not use concept of stereo, so turn it off
                _commandBufferScreenSpaceShadows2.Clear();
                _commandBufferScreenSpaceShadows2.SetGlobalFloat(WMS._SrcBlendMode, (float)BlendMode.One);
                _commandBufferScreenSpaceShadows2.SetGlobalFloat(WMS._DstBlendMode, (float)BlendMode.One);
                _commandBufferScreenSpaceShadows2.SetGlobalFloat(WMS._WeatherMakerAdjustFullScreenUVStereoDisable, 1.0f);

#if UNITY_URP

                _commandBufferScreenSpaceShadows2.Blit(tempShadowBuffer, WMS._ScreenSpaceShadowmapTexture, BlurMaterial, 0);
                _commandBufferScreenSpaceShadows2.SetGlobalTexture(WeatherMakerLightManagerScript.Instance.ScreenSpaceShadowsRenderTextureName, WMS._ScreenSpaceShadowmapTexture);

#else

                _commandBufferScreenSpaceShadows2.Blit(tempShadowBuffer, BuiltinRenderTextureType.CurrentActive, BlurMaterial, 0);
                _commandBufferScreenSpaceShadows2.SetGlobalTexture(WeatherMakerLightManagerScript.Instance.ScreenSpaceShadowsRenderTextureName, BuiltinRenderTextureType.CurrentActive);

#endif

                // must be set back to 0 after the blit
                _commandBufferScreenSpaceShadows2.SetGlobalFloat(WMS._WeatherMakerAdjustFullScreenUVStereoDisable, 0.0f);

#if UNITY_URP

                _commandBufferScreenSpaceShadows2.SetRenderTarget(WeatherMakerFullScreenEffect.CameraTargetIdentifier());
                camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBufferScreenSpaceShadows2);

#else

                _light.AddCommandBuffer(LightEvent.AfterScreenspaceMask, _commandBufferScreenSpaceShadows2);

#endif

            }
            else
            {
                CleanupCommandBuffers();
            }
        }

        private void CreateCommandBuffers()
        {
            CleanupCommandBuffers();
            AddShadowMapCommandBuffer();
        }

        private void OnEnable()
        {
            _light = GetComponent<Light>();
            CreateCommandBuffers();
            if (WeatherMakerCommandBufferManagerScript.Instance != null)
            {
                WeatherMakerCommandBufferManagerScript.Instance.RegisterPreCull(CameraPreCull, this);
                WeatherMakerCommandBufferManagerScript.Instance.RegisterPostRender(CameraPostRender, this);
            }
        }

        private void OnDisable()
        {
            CleanupCommandBuffers();
        }

        private void OnDestroy()
        {
            if (WeatherMakerCommandBufferManagerScript.Instance != null)
            {
                WeatherMakerCommandBufferManagerScript.Instance.UnregisterPreCull(this);
                WeatherMakerCommandBufferManagerScript.Instance.UnregisterPostRender(this);
            }
        }

        private void CameraPreCull(Camera camera)
        {
            if (WeatherMakerScript.Instance == null || WeatherMakerScript.Instance.PerformanceProfile == null)
            {
                return;
            }

            WeatherMakerCameraType cameraType = WeatherMakerScript.GetCameraType(camera);
            if ((WeatherMakerCommandBufferManagerScript.CameraStack == 1 && cameraType == WeatherMakerCameraType.Normal) ||
                (cameraType == WeatherMakerCameraType.Reflection && WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudReflectionShadows))
            {
                // render cloud shadows at half scale for large screens
                int scale = (int)WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudShadowDownsampleScale;
                tempShadowBuffer = RenderTexture.GetTemporary(WeatherMakerFullScreenEffect.GetRenderTextureDescriptor(scale, 0, 1, RenderTextureFormat.ARGB32, 0, camera));
                tempShadowBuffer.wrapMode = TextureWrapMode.Clamp;
                tempShadowBuffer.filterMode = FilterMode.Bilinear;
                AddScreenSpaceShadowsCommandBuffer(camera);

                // ensure that the any shader using cloud shadows knows the correct cloud shadow parameters
                if (CloudShadowMaterial != null)
                {
                    Shader.SetGlobalFloat(WMS._CloudShadowMapAdder, CloudShadowMaterial.GetFloat(WMS._CloudShadowMapAdder));
                    Shader.SetGlobalFloat(WMS._CloudShadowMapMultiplier, CloudShadowMaterial.GetFloat(WMS._CloudShadowMapMultiplier));
                    Shader.SetGlobalFloat(WMS._CloudShadowMapPower, CloudShadowMaterial.GetFloat(WMS._CloudShadowMapPower));
                    Shader.SetGlobalFloat(WMS._WeatherMakerCloudVolumetricShadowDither, CloudShadowMaterial.GetFloat(WMS._WeatherMakerCloudVolumetricShadowDither));
                    Shader.SetGlobalTexture(WMS._WeatherMakerCloudShadowDetailTexture, CloudShadowMaterial.GetTexture(WMS._WeatherMakerCloudShadowDetailTexture));
                    Shader.SetGlobalFloat(WMS._WeatherMakerCloudShadowDetailScale, CloudShadowMaterial.GetFloat(WMS._WeatherMakerCloudShadowDetailScale));
                    Shader.SetGlobalFloat(WMS._WeatherMakerCloudShadowDetailIntensity, CloudShadowMaterial.GetFloat(WMS._WeatherMakerCloudShadowDetailIntensity));
                    Shader.SetGlobalFloat(WMS._WeatherMakerCloudShadowDetailFalloff, CloudShadowMaterial.GetFloat(WMS._WeatherMakerCloudShadowDetailFalloff));
                    Shader.SetGlobalFloat(WMS._WeatherMakerCloudShadowDistanceFade, CloudShadowMaterial.GetFloat(WMS._WeatherMakerCloudShadowDistanceFade));
                }
            }
        }

        private void CameraPostRender(Camera camera)
        {
            if (WeatherMakerScript.Instance == null || WeatherMakerScript.Instance.PerformanceProfile == null)
            {
                return;
            }

            WeatherMakerCameraType cameraType = WeatherMakerScript.GetCameraType(camera);
            if ((WeatherMakerCommandBufferManagerScript.CameraStack == 0 && cameraType == WeatherMakerCameraType.Normal) ||
                (cameraType == WeatherMakerCameraType.Reflection && WeatherMakerScript.Instance.PerformanceProfile.VolumetricCloudReflectionShadows))
            {
                RenderTexture.ReleaseTemporary(tempShadowBuffer);

#if UNITY_URP

                if (_commandBufferDepthShadows != null)
                {
                    camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBufferDepthShadows);
                }
                if (_commandBufferScreenSpaceShadows1 != null)
                {
                    camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBufferScreenSpaceShadows1);
                }
                if (_commandBufferScreenSpaceShadows2 != null)
                {
                    camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, _commandBufferScreenSpaceShadows2);
                }

#else

                RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, ref _commandBufferScreenSpaceShadows1, false);
                RemoveCommandBuffer(LightEvent.AfterScreenspaceMask, ref _commandBufferScreenSpaceShadows2, false);

#endif

            }
        }
    }
}