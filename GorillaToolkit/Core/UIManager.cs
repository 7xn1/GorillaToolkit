using System.Collections;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Valve.VR;
using Vector3 = UnityEngine.Vector3;
// ReSharper disable Unity.PerformanceCriticalCodeInvocation

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
    private Transform? colorT;
    
    private TextMeshProUGUI? leftPercentage;
    private TextMeshProUGUI? rightPercentage;
    
    private TextMeshProUGUI? time;
    private TextMeshProUGUI? date;
    private TextMeshProUGUI? code;

    private TextMeshProUGUI? songName;
    private TextMeshProUGUI? songArtist;

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
        
        baseT = canvas.transform.Find("Base");
        mediaT = canvas.transform.Find("Media");
        controllerT = canvas.transform.Find("Controllers");
        colorT = canvas.transform.Find("ColorChanger");
        
        time = baseT.transform.Find("Time").GetComponent<TextMeshProUGUI>();
        date = baseT.transform.Find("Date").GetComponent<TextMeshProUGUI>();
        code = baseT.transform.Find("Code").GetComponent<TextMeshProUGUI>();
        
        songName = mediaT.transform.Find("Name").GetComponent<TextMeshProUGUI>();
        songArtist = mediaT.transform.Find("Artist").GetComponent<TextMeshProUGUI>();

        leftPercentage = controllerT.transform.Find("LeftHand/Percentage").GetComponent<TextMeshProUGUI>();
        rightPercentage = controllerT.transform.Find("RightHand/Percentage").GetComponent<TextMeshProUGUI>();

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

    private void LateUpdate() {
        if (!init) return;
        
        transform.position = GetLeftHandPosition();
        transform.position += 
            transform.forward * 0.15f +
            transform.up * 0.12f;
        
        Quaternion targetRotation = Quaternion.LookRotation(GorillaTagger.Instance.headCollider.transform.position - transform.position);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetRotation, 
            0.10f
        );
        
        bool input = SteamVR_Actions.gorillaTag_LeftJoystickClick.state;
        if (input && !wasOpen) {
            open = !open;
            StartCoroutine(open ? OpenUI() : CloseUI());
        }
        wasOpen = input;

        if (open) {
            Runtime();
            UpdatePlayPause();
        }
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
        
        GameObject? rightHand = controllerT?.Find("RightHand").gameObject;
        GameObject? leftHand = controllerT?.Find("LeftHand").gameObject;
        
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
        
        colliders.Find("Color")
                .AddComponent<ButtonManager>().Click = 
                ChangeColor;
    }

    // region because I HATE looking at the color methods.
    
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
        SetColor(controllerT?.Find("LeftHand").GetComponent<Image>(), color);
        SetColor(controllerT?.Find("RightHand").GetComponent<Image>(), color);
        
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

        while (Time.time - startTime < 0.10f) {
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
}