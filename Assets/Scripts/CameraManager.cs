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
    float cameraHeight;
    Camera cam;

    private void Start()
    {
        cameraHeight = transform.position.y;
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if(!Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetMouseButtonDown(2))
                lastPosition = Input.mousePosition;
            if (Input.GetMouseButton(2))
            {
                Vector3 delta = lastPosition - Input.mousePosition;
                transform.Translate(delta * Time.deltaTime * dragSpeed);
                lastPosition = Input.mousePosition;
            }
            Vector3 point = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cam.nearClipPlane));
            transform.Translate((point - transform.position).normalized * Input.GetAxis("Mouse ScrollWheel") * scrollSpeed * Time.deltaTime, Space.World);            

            if (Input.GetMouseButton(1) /*&& !Input.GetKey(KeyCode.LeftControl)*/)
            {
                Vector3 rot = cam.transform.rotation.eulerAngles;
                transform.Rotate(0f, Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime, 0f, Space.World);
                transform.Rotate(-Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime, 0f, 0f, Space.Self);
            }
            /*if (Input.GetMouseButtonDown(0))
                Cursor.lockState = CursorLockMode.Locked;*/
        }
    }
}
