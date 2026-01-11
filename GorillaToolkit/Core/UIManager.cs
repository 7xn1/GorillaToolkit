using System.Collections;
using Photon.Pun;
using TMPro;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

// ReSharper disable InconsistentNaming

namespace GorillaToolkit.Core;

public class UIManager : MonoBehaviour {
    private bool init;
    
    private bool open;
    private bool wasOpen;
    
    public static UIManager? Instance;
    
    private Canvas? canvas;
    private Transform? colliders;

    private Transform? baseT;
    private Transform? mediaT;
    private Transform? controllerT;
    
    private TextMeshProUGUI? leftPercentage;
    private TextMeshProUGUI? rightPercentage;
    
    private TextMeshProUGUI? time;
    private TextMeshProUGUI? date;
    private TextMeshProUGUI? code;

    private TextMeshProUGUI? songName;
    private TextMeshProUGUI? songArtist;
    
    private void Awake() {
        Instance = this;
        
        transform.localEulerAngles = new Vector3(30f, -90f, 90.00f);
        transform.localScale = new Vector3(0.20f, 0.20f, 0.20f);
        
        canvas = transform.Find("Canvas").GetComponent<Canvas>();
        colliders = transform.Find("Colliders");
        
        baseT = canvas.transform.Find("Base");
        mediaT = canvas.transform.Find("Media");
        controllerT = canvas.transform.Find("Controllers");
        
        time = baseT.transform.Find("Time").GetComponent<TextMeshProUGUI>();
        date = baseT.transform.Find("Date").GetComponent<TextMeshProUGUI>();
        code = baseT.transform.Find("Code").GetComponent<TextMeshProUGUI>();
        
        songName = mediaT.transform.Find("Name").GetComponent<TextMeshProUGUI>();
        songArtist = mediaT.transform.Find("Artist").GetComponent<TextMeshProUGUI>();

        leftPercentage = controllerT.transform.Find("LeftHand/Percentage").GetComponent<TextMeshProUGUI>();
        rightPercentage = controllerT.transform.Find("RightHand/Percentage").GetComponent<TextMeshProUGUI>();

        TriggerButtons();
        
        canvas.gameObject.SetActive(false);
        colliders.gameObject.SetActive(false);
        open = false;
        
        init = true;
    }
    
    private Vector3 GetLeftHandPosition() {
        Transform hand = GorillaTagger.Instance.leftHandTransform;
        return hand.position + hand.rotation * 
            GorillaLocomotion.GTPlayer.Instance.LeftHand.handOffset;
    }

    private void LateUpdate() {
        if (!init) return;
        
        transform.position = GetLeftHandPosition();
        transform.position += 
            transform.forward * 0.15f +
            transform.up * 0.20f;
        transform.LookAt(GorillaTagger.Instance.headCollider.transform.position);

        bool input = ControllerInputPoller.instance.leftControllerSecondaryButton;
        if (input && !wasOpen) {
            open = !open;
            StartCoroutine(open ? OpenUI() : CloseUI());
        }
        wasOpen = input;
        
        if (open) Runtime();
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    private void Runtime() {
        if (time != null) time.text = DateTime.Now.ToString("HH:mm");
        if (date != null) date.text = $"{DateTime.Now:dd/MM/yyyy} {DateTime.Now.DayOfWeek}";
        if (code != null) code.text = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null
                ? $"Room Code: {PhotonNetwork.CurrentRoom.Name}"
                : "Room Code: Not in room";

        if (songName != null) songName.text = MediaManager.Title;
        if (songArtist != null) songArtist.text = MediaManager.Artist;

        ControllerManager? instance = ControllerManager.Instance;
        if (instance == null) return;
        
        var rightHand = controllerT?.Find("RightHand").gameObject;
        var leftHand = controllerT?.Find("LeftHand").gameObject;
        
        leftHand?.SetActive(instance.leftControllerBattery > 0.00f);
        rightHand?.SetActive(instance.rightControllerBattery > 0.00f);

        if (leftPercentage != null) leftPercentage.text = $"{Mathf.RoundToInt(instance.leftControllerBattery * 100)}%";
        if (rightPercentage != null) rightPercentage.text = $"{Mathf.RoundToInt(instance.rightControllerBattery * 100)}%";
    }

    private void TriggerButtons() {
        if (colliders == null) return;
        
        colliders.Find("Previous")
            .AddComponent<ButtonManager>().Click = 
            () => MediaManager.Instance!.PreviousTrack();
        
        colliders.Find("PlayPause")
            .AddComponent<ButtonManager>().Click = 
            () => MediaManager.Instance!.PauseTrack();
         
        colliders.Find("Skip")
            .AddComponent<ButtonManager>().Click = 
            () => MediaManager.Instance!.SkipTrack();
    }
    
    private IEnumerator OpenUI() {
        gameObject.SetActive(true);
        transform.localScale = Vector3.zero;

        float startTime = Time.time;

        while (Time.time - startTime < 0.10f) {
            transform.localScale = Vector3.Lerp(
                Vector3.zero,
                new Vector3(0.20f, 0.20f, 0.20f),
                (Time.time - startTime) / 0.10f
            );

            yield return null;
        }

        transform.localScale = new Vector3(0.20f, 0.20f, 0.20f);
    }

    private IEnumerator CloseUI() {
        transform.localScale = new Vector3(0.20f, 0.20f, 0.20f);
        float startTime = Time.time;

        while (Time.time - startTime < 0.10f) {
            transform.localScale = Vector3.Lerp(
                new Vector3(0.20f, 0.20f, 0.20f),
                Vector3.zero,
                (Time.time - startTime) / 0.10f
            );

            yield return null;
        }

        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }
}