using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform player;

    // This sets the default, but Inspector overrides it
    public Vector3 offset = new Vector3(0, 15, -10);

    void Start()
    {
        // FORCE the offset to the correct value when the game starts
        // This ignores whatever is typed in the Inspector
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