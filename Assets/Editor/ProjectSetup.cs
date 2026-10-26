using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace StellarIdle.Editor
{
    /// <summary>
    /// One-click project configuration (also runs automatically the first time the project is opened):
    /// main scene + build settings + Android player settings ready for Google Play.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string CompanyName = "YourCompany";
        const string ProductName = "Stellar Idle";
        const string AndroidPackage = "com.yourcompany.stellaridle";

        static ProjectSetup()
        {
            if (Application.isBatchMode || File.Exists(ScenePath)) return;
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && !File.Exists(ScenePath)) Run();
            };
        }

        [MenuItem("Stellar Idle/Setup Project")]
        public static void Run()
        {
            CreateMainScene();
            ConfigurePlayer();
            if (BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android)
                && EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }
            Debug.Log("[Stellar Idle] Project configured. Press Play to test.");
        }

        static void CreateMainScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                var cam = Camera.main;
                if (cam != null)
                {
                    cam.orthographic = true;
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color32(10, 12, 32, 255);
                }
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.runInBackground = false;

            // Google Play requires 64-bit builds; IL2CPP is required for ARM64.
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackage);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            AssetDatabase.SaveAssets();
        }
    }
}
