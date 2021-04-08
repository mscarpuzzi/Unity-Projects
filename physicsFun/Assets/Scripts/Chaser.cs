using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chaser : MonoBehaviour
{
    Rigidbody myRigidbody; 
    public Transform target;
    public float speed = 9;
    public Vector3 velocity;
    void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 displacementFromTarget = target.position - transform.position;
        Vector3 dirToTarget = displacementFromTarget.normalized;
        velocity = speed * dirToTarget;
        float  distanceToTarget = displacementFromTarget.magnitude;
        if(distanceToTarget > 2f){
        myRigidbody.position += velocity * Time.deltaTime;

        }
    }
    void OnTrigger(Collider triggerCollider){

        if(triggerCollider.tag == "Box"){
           
            print("hey");
            

        }


    }


    


}
