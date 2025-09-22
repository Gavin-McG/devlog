using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

enum MouseState
{
    None,
    Waiting,
    Dragging
}


public class CameraManager : MonoBehaviour
{

    [SerializeField] Vector2 camBoundsX = new Vector2(-20, 20);
    [SerializeField] Vector2 camBoundsY = new Vector2(-20, 20);

    [SerializeField] Vector2 camSizeBounds = new Vector2(1, 20);
    [SerializeField] float dragThreshold = 5;

    [SerializeField] float smoothFactor = 0.95f;


    public static UnityEvent mouseClick = new UnityEvent();

    Camera cam;
    GameObject camObject;

    float camSize;
    float goalCamSize;

    MouseState state;
    Vector3 clickPos = Vector3.zero;

    private void Awake()
    {
        cam = Camera.main;
        camObject = cam.gameObject;
        camSize = cam.orthographicSize;
        goalCamSize = camSize;

        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = -1;
    }

    private void Update()
    {
        HandleClickInput();
        HandleScrollInput();
        SmoothZoom();
        BoundCamera();
    }


    void HandleClickInput()
    {
        switch (state)
        {
            case MouseState.None:
                NoneState();
                break;
            case MouseState.Waiting:
                WaitingState();
                break;
            case MouseState.Dragging:
                DraggingState();
                break;
        }
    }


    void NoneState()
    {
        if (Input.GetMouseButtonDown(0))
        {
            clickPos = Input.mousePosition;
            state = MouseState.Waiting;
        }
    }

    void WaitingState()
    {
        if (Input.GetMouseButtonUp(0))
        {
            mouseClick.Invoke();
            state = MouseState.None;
        }
        else if ((Input.mousePosition-clickPos).magnitude > dragThreshold)
        {
            state = MouseState.Dragging;
        } 
    }

    void DraggingState()
    {
        if (Input.GetMouseButtonUp(0))
        {
            state = MouseState.None;
        }

        //caluclate movement
        Vector3 change = clickPos - Input.mousePosition;
        change.x /= Screen.width;
        change.y /= Screen.height;
        change.x *= 2*camSize * cam.aspect;
        change.y *= 2*camSize;

        //apply movement
        camObject.transform.Translate(change);

        clickPos = Input.mousePosition;
    }



    //use the scroll input of mouse to zoom in/out
    void HandleScrollInput()
    {
        //calculate new size
        float newCamSize = goalCamSize - Input.mouseScrollDelta.y;
        goalCamSize = Mathf.Clamp(newCamSize, camSizeBounds.x, camSizeBounds.y);
    }


    void SmoothZoom()
    {
        //calculate new size
        float difference = goalCamSize - camSize;
        float newCamSize = camSize + difference * (1-Mathf.Pow(smoothFactor, Time.deltaTime));

        //move camera to zoom towards pointer
        float factor = newCamSize / camSize;
        Vector3 zoomPoint = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector3 offset = zoomPoint - camObject.transform.position;
        camObject.transform.position = zoomPoint - factor * offset;

        camSize = newCamSize;
        cam.orthographicSize = camSize;
    }


    //restrict camera to rectangular bounds
    void BoundCamera()
    {
        float x = Mathf.Clamp(camObject.transform.position.x, camBoundsX.x, camBoundsX.y);
        float y = Mathf.Clamp(camObject.transform.position.y, camBoundsY.x, camBoundsY.y);
        float z = camObject.transform.position.z;
        camObject.transform.position = new Vector3(x, y, z);
    }
}
