using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LookingAround : MonoBehaviour
{
    public float sensitivity = 100f;
    public Transform body;
    float xRot = 0f;
    public bool MouseLook;
    // Update is called once per frame
    void FixedUpdate()
    {

        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime;

        xRot -= mouseY;

        xRot = Mathf.Clamp(xRot, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRot, 0f, 0f);

        body.Rotate(Vector3.up * mouseX);
      

    }
}
