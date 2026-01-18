using UnityEngine;
using Valve.VR;
// ReSharper disable SuggestVarOrType_BuiltInTypes
// ReSharper disable InconsistentNaming

namespace GorillaToolkit.Core;

public class ControllerManager : MonoBehaviour {
    public static ControllerManager? Instance;
    
    public float leftControllerBattery;
    public float rightControllerBattery;
    public float headsetBattery;
    
    private bool openVRInitialized;
    
    private void Awake() {
        Instance = this;
        
        try {
            openVRInitialized = OpenVR.System != null;
        } catch { openVRInitialized = false; }
    }
    
    private void Update() {
        if (!openVRInitialized) return;
        
        leftControllerBattery = GetControllerBattery(ETrackedControllerRole.LeftHand);
        rightControllerBattery = GetControllerBattery(ETrackedControllerRole.RightHand);
        headsetBattery = GetHeadsetBattery();
    }
    
    private float GetControllerBattery(ETrackedControllerRole role) {
        if (!openVRInitialized || OpenVR.System == null) return 0f;
        
        try {
            uint deviceIndex = OpenVR.System.GetTrackedDeviceIndexForControllerRole(role);
            
            if (deviceIndex == OpenVR.k_unTrackedDeviceIndexInvalid) return 0f;
            if (!OpenVR.System.IsTrackedDeviceConnected(deviceIndex)) return 0f;
            
            var error = ETrackedPropertyError.TrackedProp_Success;
            
            float level = OpenVR.System.GetFloatTrackedDeviceProperty(
                deviceIndex,
                ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float,
                ref error
            );
            
            if (error != ETrackedPropertyError.TrackedProp_Success) return 0f;
            
            return level;
        } catch {
            return 0f;
        }
    }

    private float GetHeadsetBattery() {
        if (!openVRInitialized || OpenVR.System == null) return 0f;
        
        try {
            uint hmd = OpenVR.k_unTrackedDeviceIndex_Hmd;
        
            if (!OpenVR.System.IsTrackedDeviceConnected(hmd)) return 0f;
        
            var error = ETrackedPropertyError.TrackedProp_Success;
            
            float level = OpenVR.System.GetFloatTrackedDeviceProperty(
                hmd,
                ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float,
                ref error
            );
            
            if (error != ETrackedPropertyError.TrackedProp_Success) return 0f;
        
            return level;
        } catch {
            return 0f;
        }
    }
}