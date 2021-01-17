using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthCamera : MonoBehaviour
{
    private Transform mainCamera;
    private Vector3 mainCameraLastPos;
    public float movementCoef = 10;

    void Start()
    {
        mainCamera = Camera.main.transform;
        mainCameraLastPos = mainCamera.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 translation = mainCamera.position - mainCameraLastPos;
        transform. position += translation / movementCoef;
        mainCameraLastPos = mainCamera.position;
        transform.rotation = mainCamera.rotation;
    }
}
