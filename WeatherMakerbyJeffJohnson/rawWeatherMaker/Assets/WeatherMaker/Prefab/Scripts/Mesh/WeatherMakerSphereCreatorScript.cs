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
    /// Sphere creator
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class WeatherMakerSphereCreatorScript : MonoBehaviour
    {

#if UNITY_EDITOR

        /// <summary>Resolution of sphere. The higher the more triangles.</summary>
        [Header("Generation of sphere")]
        [Range(2, 6)]
        [Tooltip("Resolution of sphere. The higher the more triangles.")]
        public int Resolution = 6;

        [UnityEngine.HideInInspector]
        [UnityEngine.SerializeField]
        private int lastResolution = -1;

        /// <summary>UV mode for sphere generation</summary>
        [Tooltip("UV mode for sphere generation")]
        public UVMode UVMode = UVMode.Sphere;

        [UnityEngine.HideInInspector]
        [UnityEngine.SerializeField]
        private UVMode lastUVMode = (UVMode)int.MaxValue;

        private void DestroyMesh()
        {
            if (MeshFilter.sharedMesh != null)
            {
                GameObject.DestroyImmediate(MeshFilter.sharedMesh, false);
                MeshFilter.sharedMesh = null;
            }
        }

#endif

        /// <summary>
        /// Awake
        /// </summary>
        protected virtual void Awake()
        {
            MeshFilter = GetComponent<MeshFilter>();
            MeshRenderer = GetComponent<MeshRenderer>();
        }

        /// <summary>
        /// Start
        /// </summary>
        protected virtual void Start()
        {

        }

        /// <summary>
        /// Update
        /// </summary>
        protected virtual void Update()
        {

#if UNITY_EDITOR

            if (Resolution != lastResolution)
            {
                lastResolution = Resolution;
                DestroyMesh();
            }
            if (UVMode != lastUVMode)
            {
                lastUVMode = UVMode;
                DestroyMesh();
            }
            Mesh mesh = MeshFilter.sharedMesh;
            if (mesh == null)
            {
                MeshFilter.sharedMesh = WeatherMakerSphereCreator.Create(gameObject.name, Resolution, UVMode);
            }

#endif

        }

        /// <summary>
        /// LateUpdate
        /// </summary>
        protected virtual void LateUpdate()
        {

        }

        /// <summary>
        /// OnWillRenderObject
        /// </summary>
        protected virtual void OnWillRenderObject()
        {

        }

        /// <summary>
        /// OnDestroy
        /// </summary>
        protected virtual void OnDestroy()
        {
        }

        /// <summary>
        /// MeshFilter
        /// </summary>
        public MeshFilter MeshFilter { get; private set; }

        /// <summary>
        /// MeshRenderer
        /// </summary>
        public MeshRenderer MeshRenderer { get; private set; }

        /// <summary>
        /// Material
        /// </summary>
        public Material Material { get { return (MeshRenderer == null ? null : MeshRenderer.sharedMaterial); } }
    }
}