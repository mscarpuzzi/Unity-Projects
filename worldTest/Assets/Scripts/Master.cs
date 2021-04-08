using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Master : MonoBehaviour
{
    public GameObject thirdPersonCameraObject;
    public GameObject firstPersonCameraObject;
    public Transform thirdPersonCameraTransform;
    public Transform firstPersonCameraTransform;
    public Transform centerObject;
    public float coolDownTimer = .5f;
    public int cameraMode = 0;
    public bool firstSwitch = true;
    public float distance = 2f;

    public Vector2 orbitYMinMax = new Vector2(5, 85);

    float orbitX;
    float orbitY;
    
    public float mouseSensitivity = 4f;
    public float orbitDampeningTime = .12f;
    Vector3 orbitSmoothVelocity;
    Vector3 currentOrbit;
    
   
    void Start()
    {
        thirdPersonCameraObject.SetActive(true);
        firstPersonCameraObject.SetActive(false);

     
    }
    void LateUpdate()
    {    
       if(thirdPersonCameraObject.activeSelf == true)
     {
         orbitX += Input.GetAxis("Mouse X") * mouseSensitivity;
         orbitY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
         orbitY = Mathf.Clamp(orbitY, orbitYMinMax.x, orbitYMinMax.y); 

        thirdPersonCameraObject.transform.position = centerObject.transform.position - thirdPersonCameraObject.transform.forward * distance;

        currentOrbit = Vector3.SmoothDamp(currentOrbit, new Vector3(orbitY ,orbitX), ref orbitSmoothVelocity, orbitDampeningTime);

        thirdPersonCameraObject.transform.eulerAngles = currentOrbit;

        if(Input.GetKeyDown(KeyCode.R))
        {  
            thirdPersonCameraObject.transform.poistion

            

           float farDistance = .75f * distance + distance;
        }

     }
        
    
     /*Debug.DrawLine(firstPersonCameraTransform.transform.position, thirdPersonCameraTransform.transform.position, Color.green);

     
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
    









         

        */ 
     
    }
 }

