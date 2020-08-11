using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    Vector3 lastPosition;
    public float dragSpeed = 1;
    public float scrollSpeed = 1;
    public float cameraHeightMin = -200;
    public float cameraHeightMax = 200;
    float cameraHeight;

    public float rotationSpeed = 5;
    public float panSpeed = 5;

    private void Start()
    {
        cameraHeight = transform.position.y;
    }


    void LateUpdate()
    {
        if (Input.GetMouseButtonDown(2))
            lastPosition = Input.mousePosition;
        if (Input.GetMouseButton(2))
        {
            Vector3 delta = lastPosition - Input.mousePosition;
            transform.Translate(delta * Time.deltaTime * dragSpeed);
            lastPosition = Input.mousePosition;
        }

        cameraHeight -= Input.GetAxis("Mouse ScrollWheel") * scrollSpeed;
        cameraHeight = Mathf.Clamp(cameraHeight, cameraHeightMin, cameraHeightMax);
        Vector3 cameraPos = transform.position;
        cameraPos.y = cameraHeight;
        transform.position = cameraPos;
        
        if (Input.GetMouseButton(1))
        {
            Vector3 rot = Camera.main.transform.rotation.eulerAngles;
            transform.Rotate(0f, Input.GetAxis("Mouse X") * panSpeed, 0f, Space.World);
            transform.Rotate(-Input.GetAxis("Mouse Y") * panSpeed, 0f, 0f, Space.Self);
        }
        /*if (Input.GetMouseButtonDown(0))
            Cursor.lockState = CursorLockMode.Locked;*/
    }

    

}
