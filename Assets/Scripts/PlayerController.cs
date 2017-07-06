using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    /* Inspector Tunables */
    public float PlayerMovementVelocity;

    /* Private Data */
    Rigidbody rb;

    // Use this for initialization
    void Start () {
        rb = GetComponent<Rigidbody>();
    }
    
    // Update is called once per frame
    void Update () {
        ProcessMovement();
        ProcessAttacks();
    }

    /* TODO: Deal with user-invoked movement of the player character */
    void ProcessMovement ()
    {
        Vector3 desired_velocity = Vector3.zero;

        if (Input.GetKey(KeyCode.UpArrow))
            desired_velocity = new Vector3(0, 1, 0);
        else if (Input.GetKey(KeyCode.LeftArrow))
            desired_velocity = new Vector3(-1, 0, 0);
        else if (Input.GetKey(KeyCode.RightArrow))
            desired_velocity = Vector3.right;
        else if (Input.GetKey(KeyCode.DownArrow))
            desired_velocity = Vector3.down;

        rb.velocity = desired_velocity * PlayerMovementVelocity;

        /* NOTE:
         * A reminder to study and implement the grid-movement mechanic.
         * Also, consider using Rigidbodies (GetComponent<Rigidbody>().velocity)
         * to attain movement automatic collision-detection.
         * https://docs.unity3d.com/ScriptReference/Rigidbody.html
         * Also also, remember to attain framerate-independence via Time.deltaTime
         * https://docs.unity3d.com/ScriptReference/Time-deltaTime.html 
         */
    }

    /* TODO: Deal with user-invoked usage of weapons and items */
    void ProcessAttacks()
    {

    }
}
