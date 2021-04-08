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
    /// Cloud layer profile, allows configuring a flat layer of clouds
    /// </summary>
    [CreateAssetMenu(fileName = "WeatherMakerCloudLayerProfile", menuName = "WeatherMaker/Cloud Layer Profile", order = 50)]
    public class WeatherMakerCloudLayerProfileScript : ScriptableObject
    {
        /// <summary>Texture for cloud noise - use single channel texture only.</summary>
        [Header("Clouds - noise")]
        [Tooltip("Texture for cloud noise - use single channel texture only.")]
        public Texture2D CloudNoise;

        /*
        /// <summary>Cloud sample count, layer 1</summary>
        [Tooltip("Cloud sample count, layer 1")]
        [Range(1, 100)]
        public int CloudSampleCount = 1;

        /// <summary>Cloud sample step multiplier, up to 4 octaves.</summary>
        [SingleLine("Cloud sample step multiplier, up to 4 octaves.")]
        public Vector4 CloudSampleStepMultiplier = new Vector4(50.0f, 50.0f, 50.0f, 50.0f);

        /// <summary>Cloud sample dither magic, controls appearance of clouds through ray march</summary>
        [SingleLine("Cloud sample dither magic, controls appearance of clouds through ray march")]
        public Vector4 CloudSampleDitherMagic;

        /// <summary>Cloud sample dither intensit</summary>
        [SingleLine("Cloud sample dither intensit")]
        public Vector4 CloudSampleDitherIntensity;
        */

        /// <summary>Cloud noise scale, up to 4 octaves, set to 0 to not process that octave.</summary>
        [SingleLine("Cloud noise scale, up to 4 octaves, set to 0 to not process that octave.")]
        public Vector4 CloudNoiseScale = new Vector4(0.0002f, 0.0f, 0.0f, 0.0f);

        /// <summary>Cloud noise multiplier, up to 4 octaves. Should add up to about 1.</summary>
        [SingleLine("Cloud noise multiplier, up to 4 octaves. Should add up to about 1.")]
        public Vector4 CloudNoiseMultiplier = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);

        /// <summary>Cloud noise velocity.</summary>
        [Tooltip("Cloud noise velocity.")]
        public Vector3 CloudNoiseVelocity;

        /// <summary>Cloud noise rotation in degrees.</summary>
        [MinMaxSlider(-360.0f, 360.0f, "Cloud noise rotation in degrees.")]
        public RangeOfFloats CloudNoiseRotation;

        /*
        /// <summary>Texture for masking cloud noise, makes clouds visible in only certain parts of the sky.</summary>
        [Tooltip("Texture for masking cloud noise, makes clouds visible in only certain parts of the sky.")]
        public Texture2D CloudNoiseMask;

        /// <summary>Cloud noise mask scale.</summary>
        [Tooltip("Cloud noise mask scale.")]
        [Range(0.000001f, 1.0f)]
        public float CloudNoiseMaskScale = 0.0001f;

        /// <summary>Offset for cloud noise mask.</summary>
        [Tooltip("Offset for cloud noise mask.")]
        public Vector2 CloudNoiseMaskOffset;

        /// <summary>Cloud noise mask velocity.</summary>
        [Tooltip("Cloud noise mask velocity.")]
        public Vector3 CloudNoiseMaskVelocity;

        /// <summary>Cloud noise mask rotation in degrees.</summary>
        [MinMaxSlider(-360.0f, 360.0f, "Cloud noise mask rotation in degrees.")]
        public RangeOfFloats CloudNoiseMaskRotation;
        */

        /// <summary>Cloud color, for lighting.</summary>
        [Header("Clouds - appearance")]
        [Tooltip("Cloud color, for lighting.")]
        public Color CloudColor = Color.white;

        /// <summary>Cloud emission color, always emits this color regardless of lighting.</summary>
        [Tooltip("Cloud emission color, always emits this color regardless of lighting.")]
        public Color CloudEmissionColor = Color.clear;

        /// <summary>Cloud gradient color, where center of gradient is sun at horizon, right is up.</summary>
        [Tooltip("Cloud gradient color, where center of gradient is sun at horizon, right is up.")]
        public Gradient CloudGradientColor;

        /// <summary>Cloud ambient light multiplier.</summary>
        [Tooltip("Cloud ambient light multiplier.")]
        [Range(0.0f, 10.0f)]
        public float CloudAmbientMultiplier = 1.0f;

        /// <summary>Cloud Scatter light multiplier</summary>
        [Tooltip("Cloud Scatter light multiplier")]
        [Range(0.0f, 10.0f)]
        public float CloudScatterMultiplier = 1.0f;

        /// <summary>Cloud height - affects how fast clouds move as player moves and affects scale.</summary>
        [Tooltip("Cloud height - affects how fast clouds move as player moves and affects scale.")]
        [Range(2000.0f, 200000.0f)]
        public float CloudHeight = 5000;

        /// <summary>Cloud cover, controls how many clouds / how thick the clouds are.</summary>
        [Tooltip("Cloud cover, controls how many clouds / how thick the clouds are.")]
        [Range(0.0f, 1.0f)]
        public float CloudCover = 0.0f;

        /// <summary>Cloud density, controls how opaque the clouds are and how much the cloud block directional light.</summary>
        [Tooltip("Cloud density, controls how opaque the clouds are and how much the cloud block directional light.")]
        [Range(0.0f, 1.0f)]
        public float CloudDensity = 0.0f;

        /// <summary>Cloud light absorption. As this approaches 0, more light is absorbed.</summary>
        [Tooltip("Cloud light absorption. As this approaches 0, more light is absorbed.")]
        [Range(0.01f, 1.0f)]
        public float CloudLightAbsorption = 0.13f;

        /// <summary>Cloud sharpness, controls how distinct the clouds are.</summary>
        [Tooltip("Cloud sharpness, controls how distinct the clouds are.")]
        [Range(0.0f, 1.0f)]
        public float CloudSharpness = 0.015f;

        /// <summary>Bring clouds down at the horizon at the cost of stretching over the top.</summary>
        [Tooltip("Bring clouds down at the horizon at the cost of stretching over the top.")]
        [Range(0.0f, 0.5f)]
        public float CloudRayOffset = 0.2f;
    }
}
