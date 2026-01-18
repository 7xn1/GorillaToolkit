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
        public static Plugin? Instance;

        public AssetBundle? assetBundle;
        
        private static AudioClip? ClickSound;
        private static AudioSource? AudioSource;
        public static void PlayHitSound() => AudioSource?.PlayOneShot(ClickSound);
        
        private void Start() {
            Instance = this;

            gameObject.AddComponent<MediaManager>();
            gameObject.AddComponent<ControllerManager>();

            GorillaTagger.OnPlayerSpawned(OnGameInitialized);
        }

        private void OnGameInitialized() {
            using Stream? stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("GorillaToolkit.Resources.kit");
            assetBundle = AssetBundle.LoadFromStream(stream);

            AudioSource = new GameObject("ToolkitSource").AddComponent<AudioSource>();
            AudioSource.spatialBlend = 0f; AudioSource.playOnAwake = false;
            ClickSound = assetBundle.LoadAsset<AudioClip>("click");
            
            GameObject toolKitUI = Instantiate(
                assetBundle.LoadAsset<GameObject>("UI")
            );
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
