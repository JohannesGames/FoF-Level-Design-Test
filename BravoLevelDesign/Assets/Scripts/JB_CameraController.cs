using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JB_CameraController : MonoBehaviour
{

    public Transform head;
    private ControlPC pc;
    private Vector3 nextPosition;

    void Start()
    {
        pc = GetComponentInParent<ControlPC>();
        nextPosition.x = pc.transform.position.x;
        nextPosition.z = pc.transform.position.z;
        nextPosition.y = head.position.y;
        transform.position = nextPosition;
    }


    void Update()
    {
        nextPosition = pc.transform.position;
        nextPosition.y = head.position.y;
        transform.position = nextPosition;
    }
}
