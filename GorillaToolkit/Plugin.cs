using System.Reflection;
using BepInEx;
using GorillaToolkit.Core;
using TMPro;
using UnityEngine;
// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable InconsistentNaming
// ReSharper disable ShaderLabShaderReferenceNotResolved

namespace GorillaToolkit { 
    [BepInPlugin(Constants.PluginGuid, Constants.PluginName, Constants.PluginVersion)]
    public class Plugin : BaseUnityPlugin {
        public static Plugin? Instance; // Just in case [I used it later on :)].

        public static AudioClip? ClickSound;
        private static AudioSource? audioSource;
        public static void PlaySound(AudioClip clip) => audioSource?.PlayOneShot(clip);
        
        private void Start() {
            Instance = this;
            
            gameObject.AddComponent<MediaManager>();
            gameObject.AddComponent<ControllerManager>();

            GorillaTagger.OnPlayerSpawned(OnGameInitialized);
        }

        private void OnGameInitialized() {
            using Stream? stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("GorillaToolkit.Resources.kit");
            AssetBundle assetBundle = AssetBundle.LoadFromStream(stream);

            audioSource = new GameObject("ToolkitSource").AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f; audioSource.playOnAwake = false;
            ClickSound = assetBundle.LoadAsset<AudioClip>("click");
            
            GameObject toolKitUI = Instantiate(assetBundle.LoadAsset<GameObject>("UI"));
            toolKitUI.AddComponent<UIManager>();
            FixShaders(toolKitUI);
        }   
        
        private void FixShaders(GameObject go) {
            foreach (Transform child in go.transform)
                FixShaders(child.gameObject);
        
            if (go.TryGetComponent(out TextMeshProUGUI tmp)) {
                tmp.fontMaterial = new Material(tmp.fontMaterial) { shader = 
                    Shader.Find("TextMeshPro/Mobile/Distance Field")
                };   
            }
        }
    }
}
