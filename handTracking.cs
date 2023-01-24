using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.MagicLeap;
using UnityEngine.SceneManagement;


using HandGestures = UnityEngine.XR.MagicLeap.InputSubsystem.Extensions.DeviceFeatureUsages.HandGesture;
using GestureClassification = UnityEngine.XR.MagicLeap.InputSubsystem.Extensions.MLGestureClassification;

public class handTracking : MonoBehaviour
{


    [SerializeField] GameObject shelf;
    [SerializeField] GameObject item;

    private InputDevice leftHandDevice;
    private InputDevice rightHandDevice;

    private bool preRenderHandUpdate = true;

    bool isHolding = false;

    int handUsed = -1; //left is 0, right is 1

    // Start is called before the first frame update
    void Start()
    {
        if (!MLPermissions.CheckPermission(MLPermission.HandTracking).IsOk)
        {
            Debug.LogError($"You must include the {MLPermission.HandTracking} permission in the AndroidManifest.xml to run this example.");
            enabled = false;
            return;
        }

        GestureClassification.StartTracking();
        InputSubsystem.Extensions.MLHandTracking.StartTracking();
        InputSubsystem.Extensions.MLHandTracking.SetPreRenderHandUpdate(preRenderHandUpdate);
    }

    // Update is called once per frame
    void Update()
    {


        if (!leftHandDevice.isValid || !rightHandDevice.isValid)
        {
            List<InputDevice> foundDevices = new List<InputDevice>();
            InputDevices.GetDevices(foundDevices);

            foreach (InputDevice device in foundDevices)
            {
                if (device.name == GestureClassification.LeftGestureInputDeviceName)
                {
                    leftHandDevice = device;
                    continue;
                }

                if (device.name == GestureClassification.RightGestureInputDeviceName)
                {
                    rightHandDevice = device;
                    continue;
                }


            }
            return;
        }


        if (leftHandDevice.isValid)
        {
            leftHandDevice.TryGetFeatureValue(HandGestures.GestureTransformPosition, out Vector3 leftPos);
            leftHandDevice.TryGetFeatureValue(HandGestures.GestureTransformRotation, out Quaternion leftRot);
            GestureClassification.TryGetHandPosture(leftHandDevice, out GestureClassification.PostureType leftPosture);



            updateHandling(leftPos, leftPosture, 0);
        }

        if (rightHandDevice.isValid)
        {
            rightHandDevice.TryGetFeatureValue(HandGestures.GestureTransformPosition, out Vector3 rightPos);
            rightHandDevice.TryGetFeatureValue(HandGestures.GestureTransformRotation, out Quaternion rightRot);
            GestureClassification.TryGetHandPosture(rightHandDevice, out GestureClassification.PostureType rightPosture);


            updateHandling(rightPos, rightPosture, 1);
        }


    }



    void updateHandling(Vector3 loc, GestureClassification.PostureType gesture, int handBeingUsed)
    {
        if(gesture == GestureClassification.PostureType.Pinch)
        {
            if (isHolding && handBeingUsed == handUsed) //If we are already holding object with pinch then keep moving object
            {
                Debug.Log("Moving Object");
                MoveObject(loc, item);
                return;
            }

            else //We are pinching but not holding object, check if we can hold object
            {
                if (PointInOABB(loc, item.GetComponent<BoxCollider>())){ //If we are able to, pick up object
                    Debug.Log("Picking up");
                    PickUp(handBeingUsed);
                    return;
                }
            }
        }
        else if (gesture == GestureClassification.PostureType.Open && handBeingUsed == handUsed) //We are not pinching and therefore are not/should not be holding object
        {
            if (isHolding)
            {
                LetGo();
                return;
            }
        }

    }

    void PickUp(int handBeingUsed)
    {
        handUsed = handBeingUsed;
        if (!isHolding)
        {
            isHolding = true;
        }
    }

    void MoveObject(Vector3 itemCenter,GameObject item)
    {
        if(isHolding)
        {
            item.transform.position = itemCenter; //Move item based on hand location
        }
    }

    void LetGo()
    {
        if (isHolding)
        {
            Debug.Log("Not holding anymore");
            isHolding = false;
            handUsed = -1;
            checkCollision(item.transform.position, shelf.GetComponent<BoxCollider>());

        }
    }



    void checkCollision(Vector3 point, BoxCollider box)
    {
        if (!isHolding)
        {
           if (PointInOABB(point, box))
            {
                Debug.Log("Congratulations");
                Reset();

            }
        }
    }

    bool PointInOABB(Vector3 point, BoxCollider box)
    {
        point = box.transform.InverseTransformPoint(point) - box.center;

        float halfX = (box.size.x * 0.5f);
        float halfY = (box.size.y * 0.5f);
        float halfZ = (box.size.z * 0.5f);
        if (point.x < halfX && point.x > -halfX &&
           point.y < halfY && point.y > -halfY &&
           point.z < halfZ && point.z > -halfZ)
            return true;
        else
            return false;
    }

    void Reset()
    {

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex == SceneManager.sceneCountInBuildSettings)
        {
            nextSceneIndex = 0;
        }

        SceneManager.LoadScene(nextSceneIndex);
    }



}
