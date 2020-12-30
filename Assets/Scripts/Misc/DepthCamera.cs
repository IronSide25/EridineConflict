using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthCamera : MonoBehaviour
{
    Transform mainCamera;
    Vector3 mainCameraLastPos;

    void Start()
    {
        mainCamera = Camera.main.transform;
        mainCameraLastPos = mainCamera.position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 translation = mainCamera.position - mainCameraLastPos;
        transform. position += translation / 10;
        mainCameraLastPos = mainCamera.position;
        transform.rotation = mainCamera.rotation;
    }
}
