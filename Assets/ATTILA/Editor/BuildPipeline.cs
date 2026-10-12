using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ATTILA.Editor
{
    /// <summary>
    /// One-command builds. WebGL is the cross-platform target: it opens on Windows, macOS, Linux, phones and
    /// tablets from a browser, which is why the game also carries touch controls.
    /// Batch: -executeMethod ATTILA.Editor.BuildPipeline.BuildWebGL (or BuildMac, or BuildAll).
    /// </summary>
    public static class BuildPipeline
    {
        private const string Output = "Builds";

        private static string[] Scenes => EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();

        [MenuItem("ATTILA/Build/WebGL")]
        public static void BuildWebGL()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            PlayerSettings.runInBackground = true;
            Run(BuildTarget.WebGL, BuildTargetGroup.WebGL, Path.Combine(Output, "WebGL"));
        }

        [MenuItem("ATTILA/Build/macOS")]
        public static void BuildMac() => Run(BuildTarget.StandaloneOSX, BuildTargetGroup.Standalone, Path.Combine(Output, "macOS", "ATTILA.app"));

        public static void BuildAll()
        {
            BuildMac();
            BuildWebGL();
        }

        private static void Run(BuildTarget target, BuildTargetGroup group, string path)
        {
            Configure();
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Output);
            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                target = target,
                targetGroup = group,
                locationPathName = path,
                options = BuildOptions.None
            };
            var report = UnityEditor.BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"ATTILA BUILD {target}: {summary.result}, {summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors.");
            if (Application.isBatchMode && summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        /// <summary>Settings every target shares: name, orientation and the resolutions the UI is drawn for.</summary>
        private static void Configure()
        {
            PlayerSettings.companyName = "ATTILA";
            PlayerSettings.productName = "ATTILA";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.WebGL, ApiCompatibilityLevel.NET_Standard);
        }
    }
}
