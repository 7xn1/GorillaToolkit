using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using Valve.Newtonsoft.Json;

namespace GorillaToolkit.Core;

// Credits to iidk's MediaManager class from GorillaMedia [Simplified it down, thanks for letting me use this ii!].
public class MediaManager : MonoBehaviour {
    public static string Title { get; private set; } = "Unknown";
    public static string Artist { get; private set; } = "Unknown";
    public static bool ValidData;

    private static string? QuickSongPath { get; set; }
    public static MediaManager? Instance { get; private set; }

    // Credits to Graze for the 'VirtualKeyCodes', etc.
    
    private enum VirtualKeyCodes : uint {
        NextTrack = 0xB0,
        PreviousTrack = 0xB1,
        PlayPause = 0xB3,
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    private static extern void keybd_event(uint bVk, uint bScan, uint dwFlags, uint dwExtraInfo);
    
    private static float _updateDataLatency;

    public void Awake() {
        Instance = this;
        QuickSongPath = Path.Combine(Path.GetTempPath(), "QuickSong.exe");

        if (File.Exists(QuickSongPath))
            File.Delete(QuickSongPath);

        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GorillaToolkit.Resources.QuickSong.exe");
        using FileStream fs = new FileStream(QuickSongPath, FileMode.Create, FileAccess.Write);
        stream!.CopyTo(fs);
    }

    public void Update() {
        if (!(Time.time > _updateDataLatency)) return;
        
        _updateDataLatency = Time.time + 3.50f;
        StartCoroutine(UpdateDataCoroutine());
    }

    private static async Task UpdateDataAsync() {
        if (QuickSongPath == null) return;

        var psi = new ProcessStartInfo {
            FileName = QuickSongPath,
            Arguments = "-all",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var proc = new Process();
        proc.StartInfo = psi;
        proc.Start();
        var output = await proc.StandardOutput.ReadToEndAsync();
        await Task.Run(() => proc.WaitForExit());

        ValidData = false;
        Title = "Unknown";
        Artist = "Unknown";

        try {
            Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(output);
            Title = (string)data["Title"];
            Artist = (string)data["Artist"];
            ValidData = true;
        }
        catch {
            // ignored
        }
    }

    IEnumerator UpdateDataCoroutine(float delay = 0f) {
        yield return new WaitForSeconds(delay);
        
        var task = UpdateDataAsync();
        
        while (!task.IsCompleted)
            yield return null;
    }

    private static void SendKey(VirtualKeyCodes virtualKeyCode) 
        => keybd_event((uint)virtualKeyCode, 0, 0, 0);

    public void PreviousTrack() {
        Instance!.StartCoroutine(UpdateDataCoroutine(0.1f));
        SendKey(VirtualKeyCodes.PreviousTrack);
    }

    public void PauseTrack() => SendKey(VirtualKeyCodes.PlayPause);

    public void SkipTrack() {
        Instance!.StartCoroutine(UpdateDataCoroutine(0.1f));
        SendKey(VirtualKeyCodes.NextTrack);
    }
}