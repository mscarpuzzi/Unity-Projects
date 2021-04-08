using UnityEngine;
using System.Collections;

public class CameraOrbit : MonoBehaviour
{
    public GameObject thirdPersonCameraObject;
    public GameObject firstPersonCameraObject;
    public Transform thirdPersonCameraTransform;
    public Transform firstPersonCameraTransform;
    public Transform centerObject;
     public float distance = 6f;
     public float zoomOutLen = 5.5f;
     public float smooth = 1.2f;

    public Vector2 orbitYMinMax = new Vector2(-15.5f, 85);

    float orbitX;
    float orbitY;
    
    public float mouseSensitivity = 4f;
    public float orbitDampeningTime = .12f;
    Vector3 orbitSmoothVelocity;
    Vector3 currentOrbit;
    public bool zoomOut = false;
    


    void LateUpdate()
    {
        if(thirdPersonCameraObject.activeSelf == true)
        {
         orbitX += Input.GetAxis("Mouse X") * mouseSensitivity;
         orbitY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
         orbitY = Mathf.Clamp(orbitY, orbitYMinMax.x, orbitYMinMax.y);

          currentOrbit = Vector3.SmoothDamp(currentOrbit, new Vector3(orbitY ,orbitX), ref orbitSmoothVelocity, orbitDampeningTime);
         
          thirdPersonCameraObject.transform.eulerAngles = currentOrbit;
          
          thirdPersonCameraObject.transform.position = centerObject.transform.position - thirdPersonCameraObject.transform.forward * distance;
        

        
    









     }
        
    }
}