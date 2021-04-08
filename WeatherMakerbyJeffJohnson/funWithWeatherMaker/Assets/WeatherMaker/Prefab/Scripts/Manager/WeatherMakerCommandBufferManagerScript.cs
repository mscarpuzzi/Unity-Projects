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

// #define ENABLE_FORWARD_OPAQUE_CAPTURE

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DigitalRuby.WeatherMaker
{
    /// <summary>
    /// Weather maker camera types
    /// </summary>
    public enum WeatherMakerCameraType
    {
        /// <summary>
        /// Normal
        /// </summary>
        Normal,

        /// <summary>
        /// Reflection (water, mirror, etc.)
        /// </summary>
        Reflection,

        /// <summary>
        /// Cube map (reflection probe, etc.)
        /// </summary>
        CubeMap,

        /// <summary>
        /// Pre-render or other camera, internal use, should generally be ignored
        /// </summary>
        Other
    }

    /// <summary>
    /// Represents a command buffer
    /// </summary>
    public class WeatherMakerCommandBuffer
    {
        /// <summary>
        /// Camera the command buffer is attached to
        /// </summary>
        public Camera Camera;

        /// <summary>
        /// Render queue for the command buffer
        /// </summary>
        public CameraEvent RenderQueue;

        /// <summary>
        /// The command buffer
        /// </summary>
        public CommandBuffer CommandBuffer;

        /// <summary>
        /// A copy of the original material to render with, will be destroyed when command buffer is removed
        /// </summary>
        public Material Material;

        /// <summary>
        /// Reprojection state or null if none
        /// </summary>
        public WeatherMakerTemporalReprojectionState ReprojectionState;

        /// <summary>
        /// Whether the command buffer is a reflection
        /// </summary>
        public WeatherMakerCameraType CameraType { get; set; }
    }

    /// <summary>
    /// Command buffer manager
    /// </summary>
    [ExecuteInEditMode]
    public class WeatherMakerCommandBufferManagerScript : MonoBehaviour
    {
        /// <summary>Material to downsample the depth buffer</summary>
        [Tooltip("Material to downsample the depth buffer")]
        public Material DownsampleDepthMaterial;

        /// <summary>The max distance a camera x,y or z value can go from the origin before it's position is reset to 0. Either set OriginOffsetAutoAdjustAll to true or hook into the OriginOffsetChanged event to adjust your game objects when this happens.</summary>
        [Tooltip("The max distance a camera x,y or z value can go from the origin before it's position is reset to 0. Either set OriginOffsetAutoAdjustAll to true or hook into the OriginOffsetChanged event to adjust your game objects when this happens.")]
        public float OriginOffsetDistance;

        /// <summary>If true, all root game objects will have their transform adjusted by the origin offset. If false, it is up to you to adjust the game object transform positions using the OriginOffsetChanged event.</summary>
        [Tooltip("If true, all root game objects will have their transform adjusted by the origin offset. If false, it is up to you to adjust the game object transform positions using the OriginOffsetChanged event.")]
        public bool OriginOffsetAutoAdjustAll;

        /// <summary>Origin offset change event</summary>
        [Tooltip("Origin offset change event")]
        public WeatherMakerOriginOffsetEvent OriginOffsetChanged;

        /// <summary>
        /// Set this in OnWillRenderObject for the current reflection Vector
        /// </summary>
        public static Vector3? CurrentReflectionPlane;
        private static readonly List<GameObject> rootObjects = new List<GameObject>();
        private readonly List<WeatherMakerCommandBuffer> commandBuffers = new List<WeatherMakerCommandBuffer>();
        private readonly List<KeyValuePair<System.Action<Camera>, MonoBehaviour>> preCullEvents = new List<KeyValuePair<System.Action<Camera>, MonoBehaviour>>();
        private readonly List<KeyValuePair<System.Action<Camera>, MonoBehaviour>> preRenderEvents = new List<KeyValuePair<System.Action<Camera>, MonoBehaviour>>();
        private readonly List<KeyValuePair<System.Action<Camera>, MonoBehaviour>> postRenderEvents = new List<KeyValuePair<System.Action<Camera>, MonoBehaviour>>();
        private readonly List<Camera> cameraStack = new List<Camera>();
        private readonly List<WeatherMakerCameraProperties> cameraProps = new List<WeatherMakerCameraProperties>();

        private const string depthCommandBufferName = "WeatherMakerDepthDownsample";
        private CommandBuffer depthCommandBuffer;
        private float commandBufferCheckTimer;

#if ENABLE_FORWARD_OPAQUE_CAPTURE

        private const string afterForwardOpaqueCommandBufferName = "WeatherMakerAfterForwardOpaque";
        private CommandBuffer afterForwardOpaqueCommandBuffer;
        public RenderTexture AfterOpaqueBuffer { get; private set; }

#endif

#if COPY_FULL_DEPTH_TEXTURE

        private RenderTexture depthBuffer;

#endif

        /// <summary>
        /// Half depth texture
        /// </summary>
        public RenderTexture HalfDepthBuffer { get; private set; }

        /// <summary>
        /// Quarter depth texture
        /// </summary>
        public RenderTexture QuarterDepthBuffer { get; private set; }

        /// <summary>
        /// Eighth depth texture
        /// </summary>
        public RenderTexture EighthDepthBuffer { get; private set; }

        /// <summary>
        /// Sixteenth depth texture
        /// </summary>
        public RenderTexture SixteenthDepthBuffer { get; private set; }

#if COPY_FULL_DEPTH_TEXTURE

        public RenderTargetIdentifier DepthBufferId { get; private set; }

#endif

        /// <summary>
        /// Current camera stack count
        /// </summary>
        public static int CameraStack { get { return (Instance == null ? 0 : Instance.cameraStack.Count); } }

        /// <summary>
        /// Current base camera, any cameras rendered recursively go into a stack
        /// </summary>
        public static Camera BaseCamera { get { return (Instance == null || Instance.cameraStack.Count == 0 ? null : Instance.cameraStack[0]); } }

        internal readonly Matrix4x4[] view = new Matrix4x4[2];
        internal readonly Matrix4x4[] inverseView = new Matrix4x4[2];
        internal readonly Matrix4x4[] inverseProj = new Matrix4x4[2];
        internal readonly Vector4[] cameraFrustumCorners = new Vector4[8];
        private readonly Vector3[] cameraFrustumCornersTemp = new Vector3[4];

#if UNITY_EDITOR

        private int repaintCount = 10; // HACK to force command buffers to render in game view

#endif

        private void UpdateDeferredShadingKeyword(Camera camera)
        {
            if (camera.actualRenderingPath == RenderingPath.DeferredShading)
            {
                Shader.SetGlobalInt(WMS._WeatherMakerDeferredShading, 1);
            }
            else
            {
                Shader.SetGlobalInt(WMS._WeatherMakerDeferredShading, 0);
            }
        }

        private void CalculateMatrixes(Camera camera, Camera baseCamera)
        {
            if (camera.stereoEnabled)
            {
                view[0] = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
                view[1] = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);

                // see https://github.com/chriscummings100/worldspaceposteffect/blob/master/Assets/WorldSpacePostEffect/WorldSpacePostEffect.cs
                inverseView[0] = view[0].inverse;
                inverseView[1] = view[1].inverse;

                // only use base camera projection
                inverseProj[0] = baseCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left).inverse;
                inverseProj[1] = baseCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right).inverse;
            }
            else
            {
                view[0] = view[1] = camera.worldToCameraMatrix;

                inverseView[0] = inverseView[1] = view[0].inverse;

                // only use base camera projection
                inverseProj[0] = inverseProj[1] = baseCamera.projectionMatrix.inverse;
            }
        }

        private void CalculateFrustumCorners(Camera camera)
        {
            float farClipPlane = Mathf.Min(100000.0f, camera.farClipPlane);
            camera.CalculateFrustumCorners(camera.rect, farClipPlane, (camera.stereoEnabled ? Camera.MonoOrStereoscopicEye.Left : Camera.MonoOrStereoscopicEye.Mono), cameraFrustumCornersTemp);
            cameraFrustumCornersTemp[0].z = -cameraFrustumCornersTemp[0].z;
            cameraFrustumCornersTemp[1].z = -cameraFrustumCornersTemp[1].z;
            cameraFrustumCornersTemp[2].z = -cameraFrustumCornersTemp[2].z;
            cameraFrustumCornersTemp[3].z = -cameraFrustumCornersTemp[3].z;
            Matrix4x4 leftViewToWorld = (camera.stereoEnabled ? camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left).inverse : camera.cameraToWorldMatrix);
            cameraFrustumCorners[0] = leftViewToWorld * cameraFrustumCornersTemp[0]; // bottom left
            cameraFrustumCorners[1] = leftViewToWorld * cameraFrustumCornersTemp[1]; // top left
            cameraFrustumCorners[2] = leftViewToWorld * cameraFrustumCornersTemp[2]; // top right
            cameraFrustumCorners[3] = leftViewToWorld * cameraFrustumCornersTemp[3]; // bottom right
            camera.CalculateFrustumCorners(camera.rect, farClipPlane, Camera.MonoOrStereoscopicEye.Right, cameraFrustumCornersTemp);
            cameraFrustumCornersTemp[0].z = -cameraFrustumCornersTemp[0].z;
            cameraFrustumCornersTemp[1].z = -cameraFrustumCornersTemp[1].z;
            cameraFrustumCornersTemp[2].z = -cameraFrustumCornersTemp[2].z;
            cameraFrustumCornersTemp[3].z = -cameraFrustumCornersTemp[3].z;
            Matrix4x4 rightViewToWorld = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right).inverse;
            cameraFrustumCorners[4] = rightViewToWorld * cameraFrustumCornersTemp[0];
            cameraFrustumCorners[5] = rightViewToWorld * cameraFrustumCornersTemp[1];
            cameraFrustumCorners[6] = rightViewToWorld * cameraFrustumCornersTemp[2];
            cameraFrustumCorners[7] = rightViewToWorld * cameraFrustumCornersTemp[3];
        }

        private void SetupCommandBufferForCamera(Camera camera)
        {

#if !UNITY_SERVER

            if (camera == null)
            {
                return;
            }

            WeatherMakerCameraProperties props = GetCameraProperties(camera);
            WeatherMakerCameraType cameraType = WeatherMakerScript.GetCameraType(camera);
            Camera baseCamera = WeatherMakerCommandBufferManagerScript.BaseCamera;
            UpdateDeferredShadingKeyword(camera);
            CalculateMatrixes(camera, baseCamera);
            CalculateFrustumCorners(camera);
            Shader.SetGlobalMatrixArray(WMS._WeatherMakerInverseView, inverseView);
            Shader.SetGlobalMatrixArray(WMS._WeatherMakerInverseProj, inverseProj);
            Shader.SetGlobalMatrixArray(WMS._WeatherMakerView, view);
            Shader.SetGlobalVectorArray(WMS._WeatherMakerCameraFrustumCorners, cameraFrustumCorners);
            Shader.SetGlobalVector(WMS._WeatherMakerCameraOriginOffset, props.OriginOffsetCumulative);
            Shader.SetGlobalVector(WMS._WeatherMakerCameraPosComputeShader, camera.transform.position);
            if (cameraType == WeatherMakerCameraType.CubeMap)
            {
                Shader.SetGlobalFloat(WMS._WeatherMakerCameraRenderMode, 2.0f);
            }
            else if (cameraType == WeatherMakerCameraType.Reflection)
            {
                Shader.SetGlobalFloat(WMS._WeatherMakerCameraRenderMode, 1.0f);
            }
            else
            {
                Shader.SetGlobalFloat(WMS._WeatherMakerCameraRenderMode, 0.0f);
            }

#endif

        }

        private void CleanupCommandBuffer(WeatherMakerCommandBuffer commandBuffer)
        {
            if (commandBuffer == null)
            {
                return;
            }
            else if (commandBuffer.Material != null && commandBuffer.Material.name.IndexOf("(clone)", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                GameObject.DestroyImmediate(commandBuffer.Material);
            }
            if (commandBuffer.Camera != null)
            {
                commandBuffer.Camera.RemoveCommandBuffer(commandBuffer.RenderQueue, commandBuffer.CommandBuffer);
                commandBuffer.Camera = null;
            }
            if (commandBuffer.CommandBuffer != null)
            {
                CommandBuffer toDispose = commandBuffer.CommandBuffer;
                commandBuffer.CommandBuffer = null;
                toDispose.Dispose();
            }
        }

        private void CleanupCameras()
        {
            // remove destroyed camera command buffers
            for (int i = commandBuffers.Count - 1; i >= 0; i--)
            {
                if (commandBuffers[i].Camera == null)
                {
                    CleanupCommandBuffer(commandBuffers[i]);
                    commandBuffers.RemoveAt(i);
                }
            }
            for (int i = cameraProps.Count - 1; i >= 0; i--)
            {
                if (cameraProps[i].Camera == null)
                {
                    cameraProps.RemoveAt(i);
                }
            }
        }

        private void RemoveAllCommandBuffers()
        {
            for (int i = commandBuffers.Count - 1; i >= 0; i--)
            {
                CleanupCommandBuffer(commandBuffers[i]);
            }
            commandBuffers.Clear();
        }

        private void Update()
        {

#if !UNITY_SERVER

            UpdateDepthDownsampler();

#endif

        }

        private void LateUpdate()
        {
            CleanupCameras();
        }

        private void OnEnable()
        {

#if !UNITY_SERVER

            // use pre-render to give all other pre-cull scripts a chance to set properties, state, etc.

#if UNITY_URP

            RenderPipelineManager.beginCameraRendering += CameraBeginRendering;
            RenderPipelineManager.endCameraRendering += CameraEndRendering;
            RenderPipelineManager.beginFrameRendering += CameraBeginFrameRendering;
            RenderPipelineManager.endFrameRendering += CameraEndFrameRendering;

#else

            Camera.onPreCull += CameraPreCull;
            Camera.onPreRender += CameraPreRender;
            Camera.onPostRender += CameraPostRender;

#endif

            CleanupDepthTextures();

#if UNITY_EDITOR

            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

            if (OriginOffsetDistance > 0.0f && Application.isPlaying)
            {
                UnityEngine.Debug.Log("You have set a non-zero OriginOffsetDistance on WeatherMakerCommandBufferManagerScript, please ensure you have setup everything properly");
            }

#endif

#endif

        }

        private void OnDisable()
        {
            // use pre-render to give all other pre-cull scripts a chance to set properties, state, etc.

#if !UNITY_SERVER

#if UNITY_URP

            RenderPipelineManager.beginCameraRendering -= CameraBeginRendering;
            RenderPipelineManager.endCameraRendering -= CameraEndRendering;
            RenderPipelineManager.beginFrameRendering -= CameraBeginFrameRendering;
            RenderPipelineManager.endFrameRendering -= CameraEndFrameRendering;

#else

            Camera.onPreCull -= CameraPreCull;
            Camera.onPreRender -= CameraPreRender;
            Camera.onPostRender -= CameraPostRender;

#endif

            CleanupDepthTextures();

#if ENABLE_FORWARD_OPAQUE_CAPTURE

            if (AfterOpaqueBuffer != null)
            {
                AfterOpaqueBuffer.Release();
                Destroy(AfterOpaqueBuffer);
                AfterOpaqueBuffer = null;
            }
            AfterOpaqueBuffer = WeatherMakerFullScreenEffect.DestroyRenderTexture(AfterOpaqueBuffer);

#endif

#endif

        }

#if UNITY_URP

        private ScriptableRenderContext currentLWRPContext;

        private void CameraBeginRendering(ScriptableRenderContext context, Camera camera)
        {
            CameraPreCull(camera);
            CameraPreRender(camera);
        }

        private void CameraEndRendering(ScriptableRenderContext context, Camera camera)
        {
            CameraPostRender(camera);
        }

        private void CameraBeginFrameRendering(ScriptableRenderContext ctx, Camera[] cameras)
        {
            currentLWRPContext = ctx;
        }

        private void CameraEndFrameRendering(ScriptableRenderContext ctx, Camera[] cameras)
        {

        }

#endif

        private bool ListHasScript(List<KeyValuePair<System.Action<Camera>, MonoBehaviour>> list, MonoBehaviour script)
        {
            foreach (KeyValuePair<System.Action<Camera>, MonoBehaviour> item in list)
            {
                if (item.Value == script)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Register for pre cull events. Call from OnEnable.
        /// </summary>
        /// <param name="action">Action</param>
        /// <param name="script">Script</param>
        public void RegisterPreCull(System.Action<Camera> action, MonoBehaviour script)
        {

#if !UNITY_SERVER

            if (script != null && !ListHasScript(preCullEvents, script))
            {
                preCullEvents.Add(new KeyValuePair<System.Action<Camera>, MonoBehaviour>(action, script));
            }

#endif

        }

        /// <summary>
        /// Unregister pre cull events. Call from OnDestroy.
        /// </summary>
        /// <param name="script">Script</param>
        public void UnregisterPreCull(MonoBehaviour script)
        {

#if !UNITY_SERVER

            if (script != null)
            {
                for (int i = preCullEvents.Count - 1; i >= 0; i--)
                {
                    if (preCullEvents[i].Value == script)
                    {
                        preCullEvents.RemoveAt(i);
                    }
                }
            }

#endif

        }

        /// <summary>
        /// Register pre render events. Call from OnEnable.
        /// </summary>
        /// <param name="action">Action</param>
        /// <param name="script">Script</param>
        /// <param name="highPriority">High priority go to front of the list, low to the back</param>
        public void RegisterPreRender(System.Action<Camera> action, MonoBehaviour script, bool highPriority = false)
        {

#if !UNITY_SERVER

            if (script != null && !ListHasScript(preRenderEvents, script))
            {
                if (highPriority)
                {
                    preRenderEvents.Add(new KeyValuePair<System.Action<Camera>, MonoBehaviour>(action, script));
                }
                else
                {
                    preRenderEvents.Insert(0, new KeyValuePair<System.Action<Camera>, MonoBehaviour>(action, script));
                }
            }

#endif

        }

        /// <summary>
        /// Unregister pre render events. Call from OnDestroy.
        /// </summary>
        /// <param name="script">Script</param>
        public void UnregisterPreRender(MonoBehaviour script)
        {

#if !UNITY_SERVER

            if (script != null)
            {
                for (int i = preRenderEvents.Count - 1; i >= 0; i--)
                {
                    if (preRenderEvents[i].Value == script)
                    {
                        preRenderEvents.RemoveAt(i);
                    }
                }
            }

#endif

        }

        /// <summary>
        /// Register post render events. Call from OnEnable.
        /// </summary>
        /// <param name="action">Action</param>
        /// <param name="script">Script</param>
        public void RegisterPostRender(System.Action<Camera> action, MonoBehaviour script)
        {

#if !UNITY_SERVER

            if (script != null && !ListHasScript(postRenderEvents, script))
            {
                postRenderEvents.Add(new KeyValuePair<System.Action<Camera>, MonoBehaviour>(action, script));
            }

#endif

        }

        /// <summary>
        /// Unregister post render events. Call from OnDestroy.
        /// </summary>
        /// <param name="script">Script</param>
        public void UnregisterPostRender(MonoBehaviour script)
        {

#if !UNITY_SERVER

            if (script != null)
            {
                for (int i = postRenderEvents.Count - 1; i >= 0; i--)
                {
                    if (postRenderEvents[i].Value == script)
                    {
                        postRenderEvents.RemoveAt(i);
                    }
                }
            }

#endif

        }

        /// <summary>
        /// Render a camera, handles LWRP, etc.
        /// </summary>
        /// <param name="camera">Camera to render</param>
        public void RenderCamera(Camera camera)
        {

#if !UNITY_SERVER

#if UNITY_URP

            // for LWRP, these events are not invoked in RenderSingleCamera
            CameraPreCull(camera);
            CameraPreRender(camera);

#if UNITY_2019_3_OR_NEWER

            UnityEngine.Rendering.Universal.UniversalRenderPipeline.RenderSingleCamera(currentLWRPContext, camera);

#else

            UnityEngine.Rendering.LWRP.LightweightRenderPipeline.RenderSingleCamera(currentLWRPContext, camera);

#endif

            CameraPostRender(camera);

#else

            camera.Render();

#endif

#endif

        }

        /// <summary>
        /// Add a command buffer to keep track of during rendering
        /// </summary>
        /// <param name="cmdBuffer">Command buffer</param>
        public void AddCommandBuffer(WeatherMakerCommandBuffer cmdBuffer)
        {

#if !UNITY_SERVER

            if (!commandBuffers.Contains(cmdBuffer))
            {
                commandBuffers.Add(cmdBuffer);
            }

#endif

        }

        /// <summary>
        /// Remove a command buffer from rendering
        /// </summary>
        /// <param name="cmdBuffer">Command buffer</param>
        public void RemoveCommandBuffer(WeatherMakerCommandBuffer cmdBuffer)
        {

#if !UNITY_SERVER

            commandBuffers.Remove(cmdBuffer);

#endif

        }

        /// <summary>
        /// Get weather maker camera properties
        /// </summary>
        /// <param name="camera">Camera</param>
        /// <returns>Properties or null if camera is null</returns>
        public WeatherMakerCameraProperties GetCameraProperties(Camera camera)
        {
            if (camera != null)
            {
                foreach (WeatherMakerCameraProperties props in cameraProps)
                {
                    if (props.Camera == camera)
                    {
                        return props;
                    }
                }
                WeatherMakerCameraProperties newProps = new WeatherMakerCameraProperties(camera);
                cameraProps.Add(newProps);
                return newProps;
            }
            return null;
        }

        /// <summary>
        /// Update global shader values for a compute shader
        /// </summary>
        /// <param name="props">Shader properties to update</param>
        /// <param name="camera">Camera</param>
        public void UpdateShaderPropertiesForCamera(WeatherMakerShaderPropertiesScript props, Camera camera)
        {

#if !UNITY_SERVER

            if (props == null || camera == null)
            {
                return;
            }
            WeatherMakerCameraProperties cameraProps = GetCameraProperties(camera);
            props.SetVector(WMS._WeatherMakerCameraOriginOffset, cameraProps.OriginOffsetCumulative);
            props.SetVector(WMS._WeatherMakerCameraPosComputeShader, camera.transform.position);

#endif

        }

        /// <summary>
        /// Update global shader values for a compute shader
        /// </summary>
        /// <param name="shader">Compute shader</param>
        /// <param name="camera">Camera</param>
        public void UpdateShaderPropertiesForCamera(ComputeShader shader, Camera camera)
        {

#if !UNITY_SERVER

            if (shader == null || camera == null)
            {
                return;
            }
            WeatherMakerCameraProperties cameraProps = GetCameraProperties(camera);
            shader.SetVector(WMS._WeatherMakerCameraOriginOffset, cameraProps.OriginOffsetCumulative);
            shader.SetVector(WMS._WeatherMakerCameraPosComputeShader, camera.transform.position);

#endif

        }

        /// <summary>
        /// Reset the origin offset
        /// </summary>
        public void ResetOriginOffset()
        {

#if !UNITY_SERVER

            foreach (WeatherMakerCameraProperties props in cameraProps)
            {
                props.OriginOffsetCumulative = props.OriginOffsetCurrent = Vector3.zero;
            }

#endif

        }

        private void CreateAfterForwardOpaqueCommandBuffer(Camera camera)
        {

#if ENABLE_FORWARD_OPAQUE_CAPTURE

            if (afterForwardOpaqueCommandBuffer != null)
            {
                camera.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, afterForwardOpaqueCommandBuffer);
                camera.RemoveCommandBuffer(CameraEvent.AfterSkybox, afterForwardOpaqueCommandBuffer);
            }
            if (afterForwardOpaqueCommandBuffer == null || (AfterOpaqueBuffer != null && (AfterOpaqueBuffer.width != camera.pixelWidth || AfterOpaqueBuffer.height != camera.pixelHeight)))
            {
                if (AfterOpaqueBuffer != null)
                {
                    AfterOpaqueBuffer.Clear();
                    AfterOpaqueBuffer.Release();
                    Destroy(AfterOpaqueBuffer);
                    AfterOpaqueBuffer = WeatherMakerFullScreenEffect.CreateRenderTexture(WeatherMakerFullScreenEffect.GetRenderTextureDescriptor(1, 1, 1, RenderTextureFormat.DefaultHDR, 0, camera));
                }
                afterForwardOpaqueCommandBuffer = new CommandBuffer { name = afterForwardOpaqueCommandBufferName + Time.unscaledDeltaTime };
                afterForwardOpaqueCommandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, AfterOpaqueBuffer);
                afterForwardOpaqueCommandBuffer.SetGlobalTexture(WMS._CameraOpaqueTexture, AfterOpaqueBuffer);
            }
            if (camera.clearFlags == CameraClearFlags.Skybox)
            {
                camera.AddCommandBuffer(CameraEvent.AfterSkybox, afterForwardOpaqueCommandBuffer);
            }
            else
            {
                camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, afterForwardOpaqueCommandBuffer);
            }

#endif

        }

        private void CreateAndAddDepthCommandBuffer(Camera camera)
        {
            if (HalfDepthBuffer == null)
            {
                return;
            }
            else if (camera.depthTextureMode == DepthTextureMode.None)
            {
                camera.depthTextureMode = DepthTextureMode.Depth;
            }

            bool deferred =

#if UNITY_URP

            // TODO: Revisit with LWRP deferred
            false;

#else

            (camera.actualRenderingPath == RenderingPath.DeferredLighting || camera.actualRenderingPath == RenderingPath.DeferredShading);

#endif

            depthCommandBuffer = (depthCommandBuffer == null ? new CommandBuffer { name = "WeatherMakerDownsampleDepth" } : depthCommandBuffer);
            camera.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, depthCommandBuffer);
            camera.RemoveCommandBuffer(CameraEvent.BeforeReflections, depthCommandBuffer);
            depthCommandBuffer.Clear();
            if (deferred && UnityEngine.XR.XRDevice.isPresent)
            {
                // bug in VR, deferred depth texture not set
                depthCommandBuffer.SetGlobalTexture(WMS._CameraDepthTexture, BuiltinRenderTextureType.ResolvedDepth);
            }

#if COPY_FULL_DEPTH_TEXTURE

            depthCommandBuffer.SetGlobalFloat(WMS._DownsampleDepthScale, 1.0f);
            depthCommandBuffer.Blit(HalfDepthBufferId, DepthBufferId, DownsampleDepthMaterial, 0);

#endif

            depthCommandBuffer.SetGlobalFloat(WMS._DownsampleDepthScale, 2.0f);
            depthCommandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, HalfDepthBuffer, DownsampleDepthMaterial, 1);
            depthCommandBuffer.SetGlobalFloat(WMS._DownsampleDepthScale, 4.0f);
            depthCommandBuffer.Blit(HalfDepthBuffer, QuarterDepthBuffer, DownsampleDepthMaterial, 2);
            depthCommandBuffer.SetGlobalFloat(WMS._DownsampleDepthScale, 8.0f);
            depthCommandBuffer.Blit(QuarterDepthBuffer, EighthDepthBuffer, DownsampleDepthMaterial, 3);
            depthCommandBuffer.SetGlobalFloat(WMS._DownsampleDepthScale, 16.0f);
            depthCommandBuffer.Blit(EighthDepthBuffer, SixteenthDepthBuffer, DownsampleDepthMaterial, 4);
            if (deferred)
            {
                camera.AddCommandBuffer(CameraEvent.BeforeReflections, depthCommandBuffer);
            }
            else
            {
                camera.AddCommandBuffer(CameraEvent.AfterDepthTexture, depthCommandBuffer);
            }
        }

        private RenderTargetIdentifier UpdateDepthDownsampler(ref RenderTexture tex, int scale)
        {
            RenderTextureDescriptor desc = WeatherMakerFullScreenEffect.GetRenderTextureDescriptor(scale, 0, 1, RenderTextureFormat.RFloat);
            if (tex == null || tex.width != desc.width || tex.height != desc.height)
            {
                WeatherMakerFullScreenEffect.DestroyRenderTexture(ref tex);
                tex = WeatherMakerFullScreenEffect.CreateRenderTexture(desc, FilterMode.Point, TextureWrapMode.Clamp);
                tex.name = "WeatherMakerDepthTexture_" + scale;
            }
            return tex;
        }

        private void UpdateDepthDownsampler()
        {
            if (DownsampleDepthMaterial != null)
            {

#if COPY_FULL_DEPTH_TEXTURE

                DepthBufferId = UpdateDepthDownsampler(ref depthBuffer, DepthBufferId, 1);

#endif

                RenderTexture tmp = HalfDepthBuffer;
                UpdateDepthDownsampler(ref tmp, 2);
                HalfDepthBuffer = tmp;
                tmp = QuarterDepthBuffer;
                UpdateDepthDownsampler(ref tmp, 4);
                QuarterDepthBuffer = tmp;
                tmp = EighthDepthBuffer;
                UpdateDepthDownsampler(ref tmp, 8);
                EighthDepthBuffer = tmp;
                tmp = SixteenthDepthBuffer;
                UpdateDepthDownsampler(ref tmp, 16);
                SixteenthDepthBuffer = tmp;

#if COPY_FULL_DEPTH_TEXTURE

                Shader.SetGlobalTexture(WMS._CameraDepthTextureOne, depthBuffer);

#endif

                Shader.SetGlobalTexture(WMS._CameraDepthTextureHalf, HalfDepthBuffer);
                Shader.SetGlobalTexture(WMS._CameraDepthTextureQuarter, QuarterDepthBuffer);
                Shader.SetGlobalTexture(WMS._CameraDepthTextureEighth, EighthDepthBuffer);
                Shader.SetGlobalTexture(WMS._CameraDepthTextureSixteenth, SixteenthDepthBuffer);
            }
        }

        private void CleanupDepthTextures()
        {
            HalfDepthBuffer = WeatherMakerFullScreenEffect.DestroyRenderTexture(HalfDepthBuffer);
            QuarterDepthBuffer = WeatherMakerFullScreenEffect.DestroyRenderTexture(QuarterDepthBuffer);
            EighthDepthBuffer = WeatherMakerFullScreenEffect.DestroyRenderTexture(EighthDepthBuffer);
            SixteenthDepthBuffer = WeatherMakerFullScreenEffect.DestroyRenderTexture(SixteenthDepthBuffer);
            WeatherMakerFullScreenEffect.ReleaseCommandBuffer(ref depthCommandBuffer);
        }

        private void AttachDepthCommandBuffer(Camera camera)
        {
            if (DownsampleDepthMaterial != null && !WeatherMakerScript.ShouldIgnoreCamera(this, camera, false))
            {
                CreateAndAddDepthCommandBuffer(camera);
            }
        }

        private void RemoveEmptyCommandBuffers(Camera camera)
        {
            if ((commandBufferCheckTimer += Time.deltaTime) >= 1.0f)
            {
                commandBufferCheckTimer = 0.0f;
                foreach (CameraEvent evt in System.Enum.GetValues(typeof(CameraEvent)))
                {
                    CommandBuffer[] cmdBuffers = camera.GetCommandBuffers(evt);
                    foreach (CommandBuffer cmdBuffer in cmdBuffers)
                    {
                        if (cmdBuffer.sizeInBytes == 0)
                        {
                            camera.RemoveCommandBuffer(evt, cmdBuffer);
                        }
                    }
                }
            }
        }

        private void CheckOriginOffset(Camera camera)
        {
            if (!Application.isPlaying || camera.cameraType != CameraType.Game ||
                WeatherMakerScript.GetCameraType(camera) != WeatherMakerCameraType.Normal || OriginOffsetDistance <= 0.0f || WeatherMakerScript.Instance == null)
            {
                return;
            }

            // check for a camera too far away from origin, when this happens adjust the camera close to the origin and
            //  move the root transform that contains the camera by the same offset
            WeatherMakerCameraProperties props = GetCameraProperties(camera);
            Vector3 pos = camera.transform.position;
            Vector3 originOffset = Vector3.zero;
            if (Mathf.Abs(pos.x) > OriginOffsetDistance || Mathf.Abs(pos.y) > OriginOffsetDistance || Mathf.Abs(pos.z) > OriginOffsetDistance)
            {
                originOffset -= pos;
            }
            if (originOffset != Vector3.zero)
            {
                // temporal reprojections don't do well with a large jump in camera position, I think it is something with the change in view or projection
                // matrix, more research and debugging is needed, for now we force the next frame to do a full re-render
                foreach (WeatherMakerCommandBuffer buf in commandBuffers)
                {
                    if (buf.ReprojectionState != null)
                    {
                        buf.ReprojectionState.NeedsFirstFrameHandling = true;
                    }
                }
                props.OriginOffsetCumulative += originOffset;
                props.OriginOffsetCurrent = originOffset;
                OriginOffsetChanged.Invoke(props);
                if (OriginOffsetAutoAdjustAll)
                {
                    for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                    {
                        UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).GetRootGameObjects(rootObjects);
                        foreach (GameObject obj in rootObjects)
                        {
                            Transform t = obj.transform;
                            if (t.transform != WeatherMakerScript.Instance.transform && t.GetComponentInChildren<Canvas>() == null)
                            {
                                t.position += originOffset;
                            }
                        }
                        rootObjects.Clear();
                    }
                }
                UnityEngine.Debug.LogFormat("Adjusted origin offset, current: {0}, cumulative: {1}.", props.OriginOffsetCurrent, props.OriginOffsetCumulative);
            }
        }

        private void InvokeEvents(Camera camera, List<KeyValuePair<System.Action<Camera>, MonoBehaviour>> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Value == null)
                {
                    list.RemoveAt(i);
                }
                else if (list[i].Value.enabled)
                {
                    list[i].Key(camera);
                }
            }
        }

        private void CameraPreCull(Camera camera)
        {
            // avoid infinite loop
            if (camera == null || cameraStack.Contains(camera) || WeatherMakerScript.ShouldIgnoreCamera(this, camera, false))
            {
                return;
            }
            else if (WeatherMakerScript.Instance.AllowCameras != null && !WeatherMakerScript.Instance.AllowCameras.Contains(camera))
            {
                // add to the allow camera list if not already in it
                WeatherMakerScript.Instance.AllowCameras.Add(camera);
            }


            RemoveEmptyCommandBuffers(camera);
            cameraStack.Add(camera);
            CheckOriginOffset(camera);
            InvokeEvents(camera, preCullEvents);
        }

        private void CameraPreRender(Camera camera)
        {
            if (camera == null)
            {
                return;
            }
            else if (cameraStack.Contains(camera))
            {
                SetupCommandBufferForCamera(camera);
                AttachDepthCommandBuffer(camera);
                CreateAfterForwardOpaqueCommandBuffer(camera);
                InvokeEvents(camera, preRenderEvents);
            }

#if UNITY_EDITOR

            if (!Application.isPlaying && repaintCount > 0)
            {
                repaintCount--;
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }

#endif

        }

        private void CameraPostRender(Camera camera)
        {
            if (camera != null && cameraStack.Contains(camera))
            {
                cameraStack.Remove(camera);
                InvokeEvents(camera, postRenderEvents);
            }
        }

        private static WeatherMakerCommandBufferManagerScript instance;
        /// <summary>
        /// Shared instance of weather maker manager script
        /// </summary>
        public static WeatherMakerCommandBufferManagerScript Instance
        {
            get { return WeatherMakerScript.FindOrCreateInstance<WeatherMakerCommandBufferManagerScript>(ref instance, true); }
        }
    }

    /// <summary>
    /// Weather Maker camera properties
    /// </summary>
    public class WeatherMakerCameraProperties
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="camera">Camera</param>
        public WeatherMakerCameraProperties(Camera camera)
        {
            Camera = camera;
        }

        /// <summary>
        /// Camera
        /// </summary>
        public Camera Camera { get; private set; }

        /// <summary>
        /// Cumulative origin offset, use this value in shaders where the camera position needs to have a smooth transition between origin offsets
        /// </summary>
        public Vector3 OriginOffsetCumulative { get; set; }

        /// <summary>
        /// The most recent origin offset, use this value to position game objects when the origin offset changes
        /// </summary>
        public Vector3 OriginOffsetCurrent { get; set; }
    }

    /// <summary>
    /// WeatherMaker origin offset event
    /// </summary>
    [System.Serializable]
    public class WeatherMakerOriginOffsetEvent : UnityEngine.Events.UnityEvent<WeatherMakerCameraProperties> { }
}