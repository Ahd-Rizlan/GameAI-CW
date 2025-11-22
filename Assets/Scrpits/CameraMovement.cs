using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using static UnityEngine.GraphicsBuffer;

public class CameraMovement : MonoBehaviour
{
    public Transform player;
    public Vector3 offset;
    
    // Start is called before the first frame update
    void LateUpdate()
    {
        transform.position = player.position + offset;
        transform.LookAt(player); //

    }
}
