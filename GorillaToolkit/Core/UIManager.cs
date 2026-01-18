using System.Collections;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Valve.VR;
using Vector3 = UnityEngine.Vector3;
// ReSharper disable Unity.PerformanceCriticalCodeInvocation
// ReSharper disable LocalVariableHidesMember

// ReSharper disable InconsistentNaming

namespace GorillaToolkit.Core;

public class UIManager : MonoBehaviour {
    private bool init;
    
    private bool open;
    private bool wasOpen;

    private bool settingsOpen;

    public bool leftHand = true;
    
    public static UIManager? Instance;
    
    private Canvas? canvas;
    private Transform? colliders;

    // Main Transforms
    
    private Transform? baseT;
    private Transform? mediaT;
    private Transform? settingsT;
    private Transform? headsetT;
    private Transform? buttonsT;
    
    // Headset Information Transforms
    
    private Transform? batteryT;
    private Transform? controllerT;

    // Settings Transforms
    
    private Transform? handT;
    private Transform? colorT;

    // TMP
    
    private TextMeshProUGUI? headsetPercentage;
    private TextMeshProUGUI? leftPercentage;
    private TextMeshProUGUI? rightPercentage;
    
    private TextMeshProUGUI? time;
    private TextMeshProUGUI? date;
    private TextMeshProUGUI? code;
    private TextMeshProUGUI? fps;

    private TextMeshProUGUI? songName;
    private TextMeshProUGUI? songArtist;

    // Texture2D
    
    public Texture2D? pauseTexture;
    public Texture2D? playTexture;
    
    // Prolly going to redo color system at some point, just made it so you can swap for now :)
    
    private readonly Color32[] colors = [
        new(255, 0, 0, 255),
        new(255, 105, 0, 255),
        new(230, 255, 0, 255),
        new(0, 255, 10, 255), 
        new(0, 140, 255, 255),
        new(160, 0, 255, 255), 
        new(255, 0, 230, 255)
    ];
    private int colorIndex = 0;
    
    private void Awake() {
        Instance = this;
        
        transform.localScale = new Vector3(0.20f, 0.20f, 0.20f);
        
        canvas = transform.Find("Canvas").GetComponent<Canvas>();
        colliders = transform.Find("Colliders");
        
        // Main Transform Set
        
        baseT = canvas.transform.Find("Base");
        mediaT = canvas.transform.Find("Media");
        settingsT = canvas.transform.Find("Settings");
        headsetT = canvas.transform.Find("Headset");
        buttonsT = canvas.transform.Find("Buttons");
        
        // Headset Information Transform Set
        
        batteryT = headsetT.transform.Find("Battery");
        controllerT = headsetT.transform.Find("Controllers");
        
        // Settings Transform Set
        
        handT = settingsT.transform.Find("SwapHand");
        colorT = settingsT.transform.Find("ChangeColor");
        
        // TMP Set
        
        time = baseT.transform.Find("Time").GetComponent<TextMeshProUGUI>();
        date = baseT.transform.Find("Date").GetComponent<TextMeshProUGUI>();
        code = baseT.transform.Find("Code").GetComponent<TextMeshProUGUI>();
        fps = baseT.transform.Find("FPS").GetComponent<TextMeshProUGUI>();
        
        songName = mediaT.transform.Find("Name").GetComponent<TextMeshProUGUI>();
        songArtist = mediaT.transform.Find("Artist").GetComponent<TextMeshProUGUI>();

        leftPercentage = controllerT.transform.Find("LeftHand/Percentage").GetComponent<TextMeshProUGUI>();
        rightPercentage = controllerT.transform.Find("RightHand/Percentage").GetComponent<TextMeshProUGUI>();
        
        headsetPercentage = batteryT.transform.Find("Percentage").GetComponent<TextMeshProUGUI>();

        // Texture2D Set
        
        pauseTexture = Plugin.Instance?.assetBundle?
            .LoadAsset<Texture2D>("Pause");
        playTexture = Plugin.Instance?.assetBundle?
            .LoadAsset<Texture2D>("Play");
        
        TriggerButtons();
        LoadColor();
        
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

    private Vector3 GetRightHandPosition() {
        Transform hand = GorillaTagger.Instance.rightHandTransform;
        return hand.position + hand.rotation * 
            GorillaLocomotion.GTPlayer.Instance.RightHand.handOffset;
    }
    
    private void LateUpdate() {
        if (!init) return;
        
        transform.position = leftHand ? 
            GetLeftHandPosition() : 
            GetRightHandPosition();
        transform.position += 
            transform.forward * 0.15f +
            transform.up * 0.12f;
        
        Quaternion targetRotation = Quaternion.LookRotation(GorillaTagger.Instance.headCollider.transform.position - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetRotation, 
            0.10f
        );
        
        bool input = SteamVR_Actions.gorillaTag_LeftJoystickClick.state ||
                     Keyboard.current.jKey.wasPressedThisFrame;
        if (input && !wasOpen) {
            open = !open;
            StartCoroutine(open ? OpenUI() : CloseUI());
        }
        wasOpen = input;
        
        // DevelopmentShit();
        
        if (!open) return;
        
        Runtime();
        UpdatePlayPause();
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    private void Runtime() {
        if (time != null) time.text = DateTime.Now.ToString("HH:mm");
        if (date != null) date.text = $"{DateTime.Now:dd/MM/yyyy} {DateTime.Now.DayOfWeek}";
        if (code != null) code.text = PhotonNetwork.InRoom
                ? $"Room Code: {PhotonNetwork.CurrentRoom.Name}"
                : "Room Code: Not in room";
        if (fps != null) fps.text = $"FPS: {Mathf.RoundToInt(1.00f / Time.smoothDeltaTime)}";

        if (songName != null) songName.text = MediaManager.Title;
        if (songArtist != null) songArtist.text = MediaManager.Artist;
        
        settingsT?.gameObject.SetActive(settingsOpen);
        colliders?.transform.Find("Settings")
            .gameObject.SetActive(settingsOpen);
        
        HeadsetInformation();
    }

    private void HeadsetInformation() {
        ControllerManager? instance = ControllerManager.Instance;
        if (instance == null) return;
        
        GameObject? rightHand = controllerT?.Find("RightHand").gameObject;
        GameObject? leftHand = controllerT?.Find("LeftHand").gameObject;
        GameObject? headset = headsetT?.Find("Battery").gameObject; 
        
        headset?.SetActive(instance.headsetBattery > 0.00f);
        leftHand?.SetActive(instance.leftControllerBattery > 0.00f);
        rightHand?.SetActive(instance.rightControllerBattery > 0.00f);

        int leftBattery = Mathf.RoundToInt(instance.leftControllerBattery * 100);
        int rightBattery = Mathf.RoundToInt(instance.rightControllerBattery * 100);
        
        if (leftPercentage != null) leftPercentage.text = $"{leftBattery}%";
        if (rightPercentage != null) rightPercentage.text = $"{rightBattery}%";
        
        if (headsetPercentage != null) headsetPercentage.text = $"{Mathf.RoundToInt(instance.headsetBattery * 100)}%";
    }
    
    private void TriggerButtons() {
        if (colliders == null) return;
        
        // Media Triggers
        
        colliders.Find("Media/Previous")
            .AddComponent<ButtonManager>().Click = 
            () => MediaManager.Instance!.PreviousTrack();
        
        colliders.Find("Media/PlayPause")
            .AddComponent<ButtonManager>().Click = 
            () => MediaManager.Instance!.PauseTrack();
         
        colliders.Find("Media/Skip")
            .AddComponent<ButtonManager>().Click = 
            () => MediaManager.Instance!.SkipTrack();
        
        // Settings Triggers
        
        colliders.Find("Settings/Color")
            .AddComponent<ButtonManager>().Click = 
            ChangeColor;
        
        colliders.Find("Settings/Hand")
            .AddComponent<ButtonManager>().Click =
            () => leftHand = !leftHand;
        
        // Button Triggers
        
        colliders.Find("Buttons/Settings")
            .AddComponent<ButtonManager>().Click =
            () => settingsOpen = !settingsOpen;
    }
    
    #region | Color Methods |

    private void SetColor(Image? img, Color color) {
        if (img != null) img.color = color;
    }

    private void ChangeColor() {
        colorIndex = (colorIndex + 1) % colors.Length;
        ApplyColor();
        PlayerPrefs.SetInt("GorillaToolkit_ColorIndex", colorIndex);
        PlayerPrefs.Save();
    }

    private void ApplyColor() {
        Color color = colorIndex == 6 
            ? new Color(0.1694782f, 0.1504984f, 0.3584906f)
            : colors[colorIndex];
        
        Color secondaryColor = colorIndex == 6 
            ? new Color(0.03906193f, 0.0252314f, 0.1981132f)
            : color;

        SetColor(baseT?.GetComponent<Image>(), color);
        SetColor(colorT?.GetComponent<Image>(), color);
        SetColor(batteryT?.GetComponent<Image>(), color);
        SetColor(controllerT?.Find("RightHand")?.GetComponent<Image>(), color);
        SetColor(controllerT?.Find("LeftHand")?.GetComponent<Image>(), color);
        SetColor(handT?.GetComponent<Image>(), color);
        
        SetColor(buttonsT?.transform.Find("Settings")?.GetComponent<Image>(), secondaryColor);
        SetColor(mediaT?.GetComponent<Image>(), secondaryColor);
    }

    private void LoadColor() {
        colorIndex = PlayerPrefs.GetInt("GorillaToolkit_ColorIndex", 0);
        ApplyColor();
    }

    #endregion 
    
    private IEnumerator OpenUI() {
        canvas!.gameObject.SetActive(true);
        colliders!.gameObject.SetActive(true);
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

        while (Time.time - startTime < 0.10f ) {
            transform.localScale = Vector3.Lerp(
                new Vector3(0.20f, 0.20f, 0.20f),
                Vector3.zero,
                (Time.time - startTime) / 0.10f
            );

            yield return null;
        }

        transform.localScale = Vector3.zero;
        canvas!.gameObject.SetActive(false);
        colliders!.gameObject.SetActive(false);
    }
    
    private void UpdatePlayPause() {
        Image? playPauseButton = mediaT?.transform.Find("PlayPause").GetComponent<Image>();
        if (playPauseButton && pauseTexture && playTexture) {
            bool playing = MediaManager.Paused;
        
            Sprite sprite = Sprite.Create(
                playing ? playTexture : pauseTexture,
                new Rect(0, 0, playing ? playTexture.width : pauseTexture.width, 
                    playing ? playTexture.height : pauseTexture.height),
                new Vector2(0.5f, 0.5f)
            );
        
            playPauseButton.sprite = sprite;
        }
    }

    // Ignore this method, for me testing without being inside of VR.
    
    private void DevelopmentShit() {
        if (Keyboard.current.tKey.wasPressedThisFrame) ChangeColor();
        if (Keyboard.current.yKey.wasPressedThisFrame) settingsOpen = !settingsOpen;
        if (Keyboard.current.uKey.wasPressedThisFrame) leftHand = !leftHand;
    }
}