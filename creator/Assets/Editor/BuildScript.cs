using System;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using ImaginationOverflow.UniversalDeepLinking;

namespace Earth9Builder
{
    class BuildScript
    {
        static AppLinkingConfiguration deeplink_configuration = ImaginationOverflow.UniversalDeepLinking.Storage.ConfigurationStorage.Load();
        static void BuildCreator()
        {
            Build("CREATOR", "earth9-creator");
        }

        static void BuildAdmin()
        {
            Build("ADMIN", "earth9-admin");
        }

        static void BuildPlay()
        {
            Build("PLAY", "earth9");
        }

        static void Build(String scriptingDefineSymbol, String runner)
        {
            deeplink_configuration.DisplayName = runner;
            deeplink_configuration.DeepLinkingProtocols[0].Scheme = runner;
            ImaginationOverflow.UniversalDeepLinking.Storage.ConfigurationStorage.Save(deeplink_configuration);

            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, scriptingDefineSymbol + ";FBXSDK_RUNTIME");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new[] { "Assets/Scenes/Main.unity" };

            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.StandaloneWindows64:
                    {
                        buildPlayerOptions.locationPathName = "build/windows/" + runner + "/" + runner + ".exe";
                        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;

                        break;
                    }
                case BuildTarget.StandaloneOSX:
                    buildPlayerOptions.locationPathName = "build/mac/" + runner + "/" + runner + ".app";
                    buildPlayerOptions.target = BuildTarget.StandaloneOSX;
                    break;
            }
            buildPlayerOptions.options = BuildOptions.None;
            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }
    }

}
