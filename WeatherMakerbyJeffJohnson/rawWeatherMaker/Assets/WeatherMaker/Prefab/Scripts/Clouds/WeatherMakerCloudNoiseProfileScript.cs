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
    /// Cloud noise type
    /// </summary>
    public enum WeatherMakerCloudNoiseType
    {
        /// <summary>
        /// Perlin noise
        /// </summary>
        Perlin = 10,

        /// <summary>
        /// Simplex noise
        /// </summary>
        Simplex = 20,

        /// <summary>
        /// Worley noise
        /// </summary>
        Worley = 50,

        /// <summary>
        /// Perlin/worley noise
        /// </summary>
        PerlinWorley = 100,

        /// <summary>
        /// Simplex/worley noise
        /// </summary>
        SimplexWorley = 110
    }

    /// <summary>
    /// Cloud noise parameters
    /// </summary>
    [System.Serializable]
    public class WeatherMakerCloudNoiseParameters
    {
        /// <summary>Octaves</summary>
        [Range(1, 6)]
        [Tooltip("Octaves")]
        public int Octaves = 6;

        /// <summary>Period</summary>
        [Range(0.1f, 64.0f)]
        [Tooltip("Period")]
        public float Period = 6.0f;

        /// <summary>Brightness</summary>
        [Range(-1.0f, 3.0f)]
        [Tooltip("Brightness")]
        public float Brightness = 0.0f;

        /// <summary>Multiplier</summary>
        [Range(0.0f, 8.0f)]
        [Tooltip("Multiplier")]
        public float Multiplier = 1.0f;

        /// <summary>Power</summary>
        [Range(0.0f, 64.0f)]
        [Tooltip("Power")]
        public float Power = 1.0f;

        /// <summary>Whether to invert the noise</summary>
        [Tooltip("Whether to invert the noise")]
        public bool Invert;

        /// <summary>
        /// Get shader Params1
        /// </summary>
        /// <returns>Vector4</returns>
        public Vector4 GetParams1()
        {
            return new Vector4(Octaves, Period, Brightness, Invert ? 1.0f : 0.0f);
        }

        /// <summary>
        /// Get shader Params2
        /// </summary>
        /// <returns>Vector4</returns>
        public Vector4 GetParams2()
        {
            return new Vector4(Multiplier, Power, 0.0f, 0.0f);
        }
    }

    /// <summary>
    /// Cloud noise profile
    /// </summary>
    [CreateAssetMenu(fileName = "WeatherMakerCloudNoiseProfile_", menuName = "WeatherMaker/Cloud Noise Profile", order = 180)]
    public class WeatherMakerCloudNoiseProfileScript : ScriptableObject
    {
        /// <summary>Noise type</summary>
        [Tooltip("Noise type")]
        public WeatherMakerCloudNoiseType NoiseType = WeatherMakerCloudNoiseType.Perlin;

        /// <summary>Perlin noise parameters</summary>
        [Tooltip("Perlin noise parameters")]
        public WeatherMakerCloudNoiseParameters PerlinParameters;

        /// <summary>Worley noise parameters</summary>
        [Tooltip("Worley noise parameters")]
        public WeatherMakerCloudNoiseParameters WorleyParameters = new WeatherMakerCloudNoiseParameters { Invert = true };

        /// <summary>
        /// Apply profile to material
        /// </summary>
        /// <param name="material">Material</param>
        public void ApplyToMaterial(Material material)
        {
            material.SetInt(WMS._CloudNoiseType, (int)NoiseType);
            material.SetVector(WMS._CloudNoisePerlinParams1, PerlinParameters.GetParams1());
            material.SetVector(WMS._CloudNoisePerlinParams2, PerlinParameters.GetParams2());
            material.SetVector(WMS._CloudNoiseWorleyParams1, WorleyParameters.GetParams1());
            material.SetVector(WMS._CloudNoiseWorleyParams2, WorleyParameters.GetParams2());
        }
    }
}
