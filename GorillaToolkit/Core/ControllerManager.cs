using UnityEngine;
using UnityEngine.XR;
using Valve.VR;
// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable InconsistentNaming

namespace GorillaToolkit.Core;

public class ControllerManager : MonoBehaviour {
    public static ControllerManager? Instance;
    
    public float leftControllerBattery;
    public float rightControllerBattery;
    
    private void Awake()
        => Instance = this;
    
    private void Update() {
        if (XRSettings.isDeviceActive) {
            leftControllerBattery = GetBattery(ETrackedControllerRole.LeftHand);
            rightControllerBattery = GetBattery(ETrackedControllerRole.RightHand);   
        } else {
            leftControllerBattery = 0.00f;
            rightControllerBattery = 0.00f;
        }
    }
    
    // This took me so long to figure out :[
    private float GetBattery(ETrackedControllerRole role) {
        uint deviceIndex = OpenVR.System.GetTrackedDeviceIndexForControllerRole(role);
        
        if (deviceIndex == OpenVR.k_unTrackedDeviceIndexInvalid)
            return 0f;
        
        var error = ETrackedPropertyError.TrackedProp_Success;
        
        float level = OpenVR.System.GetFloatTrackedDeviceProperty(
            deviceIndex,
            ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float,
            ref error
        );
        
        return level;
    }
}