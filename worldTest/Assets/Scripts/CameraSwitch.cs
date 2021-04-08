using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitch : MonoBehaviour
{
    public GameObject thirdPersonCameraObject;
    public GameObject firstPersonCameraObject;
    public Transform thirdPersonCameraTransform;
    public Transform firstPersonCameraTransform;
    public float coolDownTimer = .5f;
    public int cameraMode = 0;
    public bool firstSwitch = true;
   
    void Start()
    {
        thirdPersonCameraObject.SetActive(true);
        firstPersonCameraObject.SetActive(false);
     
    }
    void Update()
    {  
        float distance = Vector3.Distance(firstPersonCameraTransform.transform.position, thirdPersonCameraTransform.transform.position);

        Debug.DrawLine(firstPersonCameraTransform.transform.position, thirdPersonCameraTransform.transform.position, Color.green);

        Debug.Log(distance);

        coolDownTimer -= Time.deltaTime; 

        if(coolDownTimer <= 0)
        {
            coolDownTimer = 0f;
        
            if(coolDownTimer == 0f && Input.GetKeyDown(KeyCode.LeftShift))

            if(firstSwitch && coolDownTimer == 0)
            {
                firstSwitch = !firstSwitch;
                thirdPersonCameraObject.SetActive(false);
                firstPersonCameraObject.SetActive(true);
                coolDownTimer = .5f;
            }
            else
            {
                if(cameraMode == 1)
                {
                    cameraMode = 0;
                    thirdPersonCameraObject.SetActive(false);
                    firstPersonCameraObject.SetActive(true);
                }
                else
                {
                    cameraMode = 1;
                    thirdPersonCameraObject.SetActive(true);
                    firstPersonCameraObject.SetActive(false);
                }
            
                coolDownTimer = .5f;
            }
        
        }
    
     
    }
}

