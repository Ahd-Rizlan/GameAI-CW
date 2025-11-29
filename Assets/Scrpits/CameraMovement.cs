using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    private Transform player;

    public Vector3 offset = new Vector3(0, 15, -10);

    void Start()
    {

        offset = new Vector3(0, 15, -10);
    }

    void LateUpdate()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                transform.position = player.position + offset;

            }
            return;
        }

        transform.position = player.position + offset;
        transform.LookAt(player);
    }
}