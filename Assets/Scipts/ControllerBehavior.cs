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

    private GameObject editObject = null;
    private Vector3 lastControllerPosition = Vector3.zero;
    private Quaternion initialControllerRotation = Quaternion.identity;
    private Quaternion initialObjectRotation = Quaternion.identity;

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
            editObject = null;
            initialControllerRotation = Quaternion.identity;
            initialObjectRotation = Quaternion.identity;
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
                    editObject.transform.position += moveVector;
                }
                lastControllerPosition = origin;

            }
            else if (currentMode == transformMode.rotation)
            {
                if (initialControllerRotation == Quaternion.identity)
                {
                    initialControllerRotation = rightController.transform.rotation;
                    initialObjectRotation = editObject.transform.rotation;
                }
                Quaternion controllerAngularDifference = Quaternion.Inverse(rightController.transform.rotation) * initialControllerRotation;
                editObject.transform.parent.transform.rotation = controllerAngularDifference * initialObjectRotation;
            }
            else if(currentMode == transformMode.scale)
            {
                if (lastControllerPosition != Vector3.zero)
                {
                    //Vector3 moveVector = origin - lastControllerPosition;
                    //if(Vector3.Dot(direction, editObject.transform.forward) > 0)
                    //{

                    //}
                    //var distance = moveVector.magnitude;
                    //foreach (Transform child in editObject.transform)
                    //{
                    //    if (child.tag == "plane")
                    //        child.localScale.x += distance;
                    //}
                }
                lastControllerPosition = origin;
            }
        }
        else if(Physics.Raycast(origin, direction, out hit) && hit.transform.parent.tag == "edit_plain")
        {
            editObject = hit.transform.parent.transform.gameObject;
        }
    }

    void ChangeEditMode(float axisValue)
    {   
        if(axisValue > 0 && !changedTransformMode)
        {
            lastControllerPosition = Vector3.zero;
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
            lastControllerPosition = Vector3.zero;
            changedTransformMode = true;
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
