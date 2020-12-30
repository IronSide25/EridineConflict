using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    Vector3 lastPosition;
    public float dragSpeed = 1;
    public float scrollSpeed = 1;
    public float rotationSpeed = 5;
    public float cameraHeightMin = -200;
    public float cameraHeightMax = 200;
    public float maxDistanceFromOrigin = 1000;
    Camera cam;
    private Vector3 lastCameraPos;
    private float lastCameraHeight;

    private void Start()
    {
        lastCameraHeight = transform.position.y;
        lastCameraPos = transform.position;
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if(!Input.GetKey(KeyCode.LeftControl))
        {
            float y = transform.position.y;
            if (Input.GetMouseButtonDown(2))
                lastPosition = Input.mousePosition;
            if (Input.GetMouseButton(2))
            {              
                Vector3 delta = lastPosition - Input.mousePosition;
                transform.Translate(delta * dragSpeed);          
                lastPosition = Input.mousePosition;
            }

            Vector3 point = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cam.nearClipPlane));
            transform.Translate((point - transform.position).normalized * Input.GetAxis("Mouse ScrollWheel") * scrollSpeed, Space.World);

            if (Vector2.Distance(Vector2.zero, new Vector2(transform.position.x, transform.position.z)) > maxDistanceFromOrigin)
            {
                //transform.position = lastCameraPos;
                transform.position = new Vector3(lastCameraPos.x, transform.position.y, lastCameraPos.z);
            }
            lastCameraPos = transform.position;

            if(transform.position.y > cameraHeightMax || transform.position.y < cameraHeightMin)
            {
                Vector3 pos = transform.position;
                pos.y = lastCameraHeight;
                transform.position = pos;
            }
            lastCameraHeight = transform.position.y;
           
            if (Input.GetMouseButton(1) /*&& !Input.GetKey(KeyCode.LeftControl)*/)
            {
                transform.Rotate(0f, Input.GetAxis("Mouse X") * rotationSpeed, 0f, Space.World);
                transform.Rotate(-Input.GetAxis("Mouse Y") * rotationSpeed, 0f, 0f, Space.Self);
            }
            /*if (Input.GetMouseButtonDown(0))
                Cursor.lockState = CursorLockMode.Locked;*/
        }
    }

    public void ResetCamera()
    {
        transform.position = new Vector3(30, 75, 65);
        transform.eulerAngles = new Vector3(50, 180, 0);
    }
}
