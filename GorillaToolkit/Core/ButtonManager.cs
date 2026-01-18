using UnityEngine;
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace GorillaToolkit.Core;

public class ButtonManager : MonoBehaviour {
    private float _lastTime;
    public Action? Click;

    public void Awake() {
        GetComponent<BoxCollider>().isTrigger = true;
        GetComponent<BoxCollider>().providesContacts = true;
        
        gameObject.SetLayer(UnityLayer.GorillaInteractable);
    }

    public void OnTriggerEnter(Collider collider) {
        
        if (!enabled ||
            !(Time.realtimeSinceStartup > _lastTime) ||
            !collider.TryGetComponent(out GorillaTriggerColliderHandIndicator handIndicator) || 
            collider.name != (UIManager.Instance.leftHand ? "RightHandTriggerCollider" : "LeftHandTriggerCollider")) 
            return;
        
        _lastTime = Time.realtimeSinceStartup + 0.250f;

        GorillaTagger.Instance.StartVibration(
            handIndicator.isLeftHand,
            GorillaTagger.Instance.tapHapticStrength / 2f,
            GorillaTagger.Instance.tapHapticDuration
        );

        Plugin.PlayHitSound();
        Click?.Invoke();
    }
}