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

    private Vector3 lastControllerPosition = Vector3.zero;
    private Quaternion initialControllerRotation = Quaternion.identity;
    private Quaternion initialObjectRotation = Quaternion.identity;

    GameObject sourcePoint = null; //public Transform origin;
    GameObject destinationPoint = null; //public Transform destination;

    //draw line

    [SerializeField] private GameObject lineRenderObject;
    private LineRenderer line;


    enum transformMode
    {
        position = 0,
        rotation = 1,
        scale = 2
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
        else if(primaryButtonValue == false)
        {
            spawnedAnObject = false;
        }
        if(triggerButtonValue == true)
        {
            EditObject();
        }
        else if(triggerButtonValue == false)
        {
            lastControllerPosition = Vector3.zero;
            changedTransformMode = false;
        }


        if (secondaryButtonValue == true)
        {
            unselectPoint();
        }

    }

    void SpawnObject()
    {
        Vector3 origin = rightController.transform.position;
        Vector3 direction = rightController.transform.forward;
        rightHandDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out var rotationValue);
        

        RaycastHit hit;
        if(Physics.Raycast(origin, direction, out hit))
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
            else if(hit.transform.parent.tag == "edit_plain")
            {
                spawnedAnObject = true;
                var point = Instantiate(editPoint, hit.point, Quaternion.identity);
                point.transform.parent = hit.transform.parent.transform;
            }
            
        }
    }


    void drawLine(GameObject sourcePoint, GameObject destinationPoint)
    {
        line = lineRenderObject.GetComponent<LineRenderer>();
        line.SetPosition(0, sourcePoint.transform.position);
        line.SetPosition(1, destinationPoint.transform.position);
        line.SetWidth(.45f, .45f);
    }

    void EditObject()
    {
        leftHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 primaryAxisValue);

        ChangeEditMode(primaryAxisValue.x);

        Vector3 origin = rightController.transform.position;
        Vector3 direction = rightController.transform.forward;
        rightHandDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion deviceRotation);


        RaycastHit hit;

        if (sourcePoint != null && Physics.Raycast(origin, direction, out hit) && hit.transform.parent.tag == "edit_point")
        {
            destinationPoint = hit.transform.gameObject;
            if (destinationPoint == sourcePoint)
            {
                destinationPoint = null;
                return;
            }
            destinationPoint.GetComponent<Renderer>().material.color = Color.green;
            drawLine(sourcePoint, destinationPoint);

        }
        else if (Physics.Raycast(origin, direction, out hit) && hit.transform.parent.tag == "edit_point")
        {
            sourcePoint = hit.transform.gameObject;
            sourcePoint.GetComponent<Renderer>().material.color = Color.red;
        }

        if (Physics.Raycast(origin, direction, out hit) && hit.transform.parent.tag == "edit_plain")
        {
            if(currentMode == transformMode.position)
            {
                if(lastControllerPosition != Vector3.zero)
                {
                    Vector3 moveVector = origin - lastControllerPosition;
                    hit.transform.parent.transform.position += moveVector;
                }
                lastControllerPosition = origin;

            }
            else if(currentMode == transformMode.rotation)
            {
                if(initialControllerRotation == Quaternion.identity)
                {
                    initialControllerRotation = deviceRotation;
                    initialObjectRotation = hit.transform.parent.transform.rotation;
                }
                Quaternion controllerAngularDifference = initialControllerRotation * deviceRotation;
                hit.transform.parent.transform.rotation = controllerAngularDifference * initialObjectRotation;
            }
        }
    }

    void unselectPoint()
    {
        sourcePoint.GetComponent<Renderer>().material.color = Color.white;
        sourcePoint = null;
    }

    void ChangeEditMode(float axisValue)
    {   
        if(axisValue > 0 && !changedTransformMode)
        {
            changedTransformMode = true;
            switch (currentMode)
            {
                case transformMode.position:
                    currentMode = transformMode.rotation;
                    break;
                case transformMode.rotation:
                    currentMode = transformMode.scale;
                    break;
                case transformMode.scale:
                    currentMode = transformMode.position;
                    break;
            }
        }
        else if(axisValue < 0 && !changedTransformMode)
        {
            switch (currentMode)
            {
                case transformMode.position:
                    currentMode = transformMode.scale;
                    break;
                case transformMode.rotation:
                    currentMode = transformMode.position;
                    break;
                case transformMode.scale:
                    currentMode = transformMode.rotation;
                    break;
            }
        }
        else if(axisValue == 0)
        {
            changedTransformMode = false;
        }

        var floor = GameObject.FindGameObjectWithTag("floor");
        switch (currentMode)
        {
            case transformMode.position:
                floor.GetComponent<Renderer>().material.color = Color.blue;
                break;
            case transformMode.rotation:
                floor.GetComponent<Renderer>().material.color = Color.red;
                break;
            case transformMode.scale:
                floor.GetComponent<Renderer>().material.color = Color.green;
                break;
        }
    }
}
