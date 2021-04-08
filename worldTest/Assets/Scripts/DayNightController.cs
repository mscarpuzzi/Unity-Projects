using UnityEngine;
using System.Collections;
 
public class DayNightController : MonoBehaviour {
 
    public Light sun;
    
    public float secondsInFullDay = 120f;
      
    [HideInInspector]
    public float timeMultiplier = 1.0f;
     [Range(0,1)]
    public float currentTimeOfDay = 0.0f;
    
    
    void FixedUpdate() {
        
        UpdateSun();
 
        currentTimeOfDay += (Time.deltaTime / secondsInFullDay) * timeMultiplier;
 
        if (currentTimeOfDay >= 1.0f) {
            currentTimeOfDay = 0.0f;
        }
    }
    
    void UpdateSun() {


        sun.transform.localRotation = Quaternion.Euler((currentTimeOfDay * 360f) - 90, ((currentTimeOfDay * 360f)-50), 90);

    }


}