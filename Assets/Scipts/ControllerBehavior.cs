using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

public class ControllerBehavior : MonoBehaviour
{
    private InputDevice rightHandDevice;
    private InputDevice leftHandDevice;
    private InputDevice hmdDevice;
    private bool spawnedAnObject = false;
    private bool changedTransformMode = false;
    [SerializeField] private GameObject editPlane;
    [SerializeField] private GameObject editPoint;
    [SerializeField] private GameObject rightController;
    [SerializeField] private GameObject UIPlane;

    private GameObject editObject = null;
    private Vector3 lastControllerPosition = Vector3.zero;

    GameObject sourcePoint = null;
    GameObject destinationPoint = null;

    private LineRenderer lineRenderer;
    private float pointCooldown = 0.8f;
    private float canPlacePoint = -1.0f;

    enum transformMode
    {
        position = 0,
        rotation = 1,
    }

    transformMode currentMode = transformMode.position;

    // Start is called before the first frame update
    void Start()
    {
        List<InputDevice> inputDevices = new List<InputDevice>();
        InputDeviceCharacteristics rightControllerCharacteristics = InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
        InputDeviceCharacteristics leftControllerCharacteristics = InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;
        InputDeviceCharacteristics headMountedDisplayCharacteristics = InputDeviceCharacteristics.HeadMounted;

        InputDevices.GetDevicesWithCharacteristics(rightControllerCharacteristics, inputDevices);

        if (inputDevices.Count > 0)
        {
            rightHandDevice = inputDevices[0];
        }
        InputDevices.GetDevicesWithCharacteristics(headMountedDisplayCharacteristics, inputDevices);
        if (inputDevices.Count > 0)
        {
            hmdDevice = inputDevices[0];
        }

        InputDevices.GetDevicesWithCharacteristics(leftControllerCharacteristics, inputDevices);
        if (inputDevices.Count > 0)
        {
            leftHandDevice = inputDevices[0];
        }
    }

    // Update is called once per frame
    void Update()
    {
        rightHandDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButtonValue);
        rightHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerButtonValue);
        rightHandDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryButtonValue);

        if (primaryButtonValue == true && !spawnedAnObject) 
        {
            SpawnObject();
        }
        else if (primaryButtonValue == false)
        {
            spawnedAnObject = false;
        }
        if (triggerButtonValue == true)
        {
            EditObject();
        }
        else if (triggerButtonValue == false)
        {
            lastControllerPosition = Vector3.zero;
            changedTransformMode = false;
            editObject = null;
        }

        if (secondaryButtonValue == true)
        {
            unselectPoint();
        }

        var floor = GameObject.FindGameObjectWithTag("floor");

        switch (currentMode)
        {
            case transformMode.position:
                UIPlane.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.Text>().text = "Position Mode";
                UIPlane.transform.GetChild(0).GetComponent<Renderer>().material.color = Color.blue;
                break;
            case transformMode.rotation:
                UIPlane.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<UnityEngine.UI.Text>().text = "Rotation Mode";
                UIPlane.transform.GetChild(0).GetComponent<Renderer>().material.color = Color.red;
                break;
            default:
                floor.GetComponent<Renderer>().material.color = Color.black;
                break;
        }
    }

    void SpawnObject()
    {
        Vector3 origin = rightController.transform.position;
        Vector3 direction = rightController.transform.forward;
        rightHandDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out var rotationValue);

        RaycastHit hit;

        if (Physics.Raycast(origin, direction, out hit))
        {
            if (hit.transform.tag == "floor")
            {
                spawnedAnObject = true;
                hmdDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 targetPos);

                Vector3 relativePos = targetPos - hit.point;
                relativePos.y = 0;
                Quaternion rotation = Quaternion.LookRotation(relativePos, Vector3.up);
                Instantiate(editPlane, hit.point, rotation);
            }
            else if (hit.transform.parent.tag == "edit_plain")
            {
                spawnedAnObject = true;
                var point = Instantiate(editPoint, hit.point, Quaternion.identity);
                point.transform.parent = hit.transform.parent.transform;
            }
        }
    }

    void EditObject()
    {
        leftHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 primaryAxisValue);

        ChangeEditMode(primaryAxisValue.x);

        Vector3 origin = rightController.transform.position;
        Vector3 direction = rightController.transform.forward;
        rightHandDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion deviceRotation);

        RaycastHit hit;
        if(editObject != null)
        {
            if (currentMode == transformMode.position)
            {
                if (lastControllerPosition != Vector3.zero)
                {
                    Vector3 moveVector = origin - lastControllerPosition;
                    editObject.transform.position += moveVector * 1.25f;
                }
                lastControllerPosition = origin;
            }
            else if (currentMode == transformMode.rotation)
            {
                editObject.transform.rotation = rightController.transform.rotation;
            }
        }
        else if (Physics.Raycast(origin, direction, out hit) && hit.transform.parent.tag == "edit_point")
        {
            if (sourcePoint == null && destinationPoint == null && Time.time > canPlacePoint)   
            {
                sourcePoint = hit.transform.gameObject;
                sourcePoint.GetComponent<Renderer>().material.color = Color.red;
            }
            else if (sourcePoint != null && destinationPoint == null && hit.transform.gameObject != sourcePoint)
            {
                destinationPoint = hit.transform.gameObject;
                destinationPoint.GetComponent<Renderer>().material.color = Color.green;
                drawLine();
            }
        }
        else if(Physics.Raycast(origin, direction, out hit) && hit.transform.parent.tag == "edit_plain")
        {
            editObject = hit.transform.parent.transform.gameObject;
        }
        
    }

    void ChangeEditMode(float axisValue)
    {
        if (axisValue > 0 && !changedTransformMode)
        {
            lastControllerPosition = Vector3.zero;
            changedTransformMode = true;
            switch (currentMode)
            {
                case transformMode.position:
                    currentMode = transformMode.rotation;
                    break;
                case transformMode.rotation:
                    currentMode = transformMode.position;
                    break;
            }
        }
        else if (axisValue < 0 && !changedTransformMode)
        {
            lastControllerPosition = Vector3.zero;
            changedTransformMode = true;
            switch (currentMode)
            {
                case transformMode.position:
                    currentMode = transformMode.rotation;
                    break;
                case transformMode.rotation:
                    currentMode = transformMode.position;
                    break;
            }
        }
        else if (axisValue == 0)
        {
            changedTransformMode = false;
        }
    }

    void drawLine()
    {
        //lineRenderer = new GameObject("Line").AddComponent<LineRenderer>();
        //lineRenderer.startColor = Color.black;
        //lineRenderer.endColor = Color.black;
        //lineRenderer.startWidth = 0.05f;
        //lineRenderer.endWidth = 0.05f;
        //lineRenderer.positionCount = 2;
        //lineRenderer.useWorldSpace = true;

        LineRenderer lineObj = new GameObject("Line").AddComponent<LineRenderer>();
        lineObj.startColor = Color.black;
        lineObj.endColor = Color.black;
        lineObj.startWidth = 0.05f;
        lineObj.endWidth = 0.05f;
        lineObj.positionCount = 2;
        lineObj.useWorldSpace = false;

        lineObj.SetPosition(0, new Vector3(sourcePoint.transform.position.x, sourcePoint.transform.position.y, sourcePoint.transform.position.z));
        lineObj.SetPosition(1, new Vector3(destinationPoint.transform.position.x, destinationPoint.transform.position.y, destinationPoint.transform.position.z));

        lineObj.transform.parent = sourcePoint.transform;

        sourcePoint.GetComponent<Renderer>().material.color = Color.white;
        destinationPoint.GetComponent<Renderer>().material.color = Color.white;

        sourcePoint = null;
        destinationPoint = null;

        canPlacePoint = Time.time + pointCooldown;
    }

    void unselectPoint()
    {
        sourcePoint.GetComponent<Renderer>().material.color = Color.white;
        sourcePoint = null;
    }
}