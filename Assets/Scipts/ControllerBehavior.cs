using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ControllerBehavior : MonoBehaviour
{

    private InputDevice rightHandDevice;
    private InputDevice hmdDevice;
    private bool spawnedAnObject = false;
    [SerializeField] private GameObject editPlane;
    [SerializeField] private GameObject editPoint;
    [SerializeField] private GameObject rightController;

    private Vector3 lastControllerPosition = Vector3.zero;

    enum transformMode
    {
        none = 0,
        position = 1,
        rotation = 2,
        scale = 3
    }

    transformMode currentMode = transformMode.none;

    // Start is called before the first frame update
    void Start()
    {
        List<InputDevice> inputDevices = new List<InputDevice>();
        InputDeviceCharacteristics rightControllerCharacteristics = InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
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
        else if(triggerButtonValue == true)
        {
            EditObject();
        }
        else if(triggerButtonValue == false)
        {
            lastControllerPosition = Vector3.zero;
        }

    }

    void SpawnObject()
    {
        Vector3 origin = rightController.transform.position;
        Vector3 direction = rightController.transform.forward;

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
        Vector3 origin = rightController.transform.position;
        Vector3 direction = rightController.transform.forward;

        RaycastHit hit;
        currentMode = transformMode.position;
        if(Physics.Raycast(origin, direction, out hit))
        {
            if(hit.transform.parent.tag == "edit_plain" && currentMode == transformMode.position)
            {
                if(lastControllerPosition != Vector3.zero)
                {
                    Vector3 moveVector = origin - lastControllerPosition;
                    hit.transform.parent.transform.position += moveVector;
                }
                lastControllerPosition = origin;

            }
            else if(hit.transform.tag == "floor")
            {
                hit.transform.GetComponent<Renderer>().material.color = Color.red;
            }
        }
    }
}
