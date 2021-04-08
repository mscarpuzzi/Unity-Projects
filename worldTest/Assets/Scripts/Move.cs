using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
   public float speed = 10f;
   Vector3 velocity;

    // Update is called once per frame
    void LateUpdate()
    {
       Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
       Vector3 direction = input.normalized;
        velocity = speed * input.normalized;
        Vector3 move = velocity * Time.deltaTime;
        transform.Translate(move);
    }
}
