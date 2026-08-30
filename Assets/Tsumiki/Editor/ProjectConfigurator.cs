#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Linq;

namespace Tsumiki.Editor
{
    public static class ProjectConfigurator
    {
        [MenuItem("つみき/プロジェクトを設定")]
        public static void Configure()
        {
            PlayerSettings.productName = "つみき なんこ？";
            PlayerSettings.companyName = "Hayashi Minoru";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.hayashiminoru.tsumikinanko");
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.iOS, ApiCompatibilityLevel.NET_Standard);

            Directory.CreateDirectory("Assets/Tsumiki/Scenes");
            const string scenePath = "Assets/Tsumiki/Scenes/Main.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("つみき なんこ？ project settings configured.");
        }

        public static void BuildIos()
        {
            Configure();
            Directory.CreateDirectory("Builds/iOS");
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = "Builds/iOS",
                target = BuildTarget.iOS,
                options = BuildOptions.Development
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new BuildFailedException($"iOS build failed: {report.summary.result}");
        }
    }
}
#endif
