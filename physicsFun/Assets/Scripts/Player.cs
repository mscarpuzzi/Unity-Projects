using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed = 10;
    Rigidbody myRigidbody;
    Vector3 velocity;

    void Start ()
    {
        myRigidbody = GetComponent<Rigidbody>();


    }
    // Update is called once per frame
    void Update()
    {
        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        Vector3 direction = input.normalized;
        velocity = speed * direction;
        Vector3 move = velocity * Time.deltaTime;
    }
     void FixedUpdate()
    {
            myRigidbody.position += velocity * Time.fixedDeltaTime;
    }

}
