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

        static void BuildAdmin()
        {
            deeplink_configuration.DisplayName = "earth9-admin";
            deeplink_configuration.DeepLinkingProtocols[0].Scheme = "earth9-admin";
            ImaginationOverflow.UniversalDeepLinking.Storage.ConfigurationStorage.Save(deeplink_configuration);

            string[] scriptingDefineSymbols = { "ADMIN", "ENV_PROD" };
            Build(scriptingDefineSymbols, "earth9-admin");
        }

        static void BuildPlay()
        {
            deeplink_configuration.DisplayName = "earth9-launcher";
            deeplink_configuration.DeepLinkingProtocols[0].Scheme = "earth9-launcher";
            ImaginationOverflow.UniversalDeepLinking.Storage.ConfigurationStorage.Save(deeplink_configuration);

            string[] scriptingDefineSymbols = { "PLAY", "ENV_PROD" };
            Build(scriptingDefineSymbols, "earth9");
        }

        static void BuildTestingAdmin()
        {
            deeplink_configuration.DisplayName = "earth9-admin-testing";
            deeplink_configuration.DeepLinkingProtocols[0].Scheme = "earth9-admin-testing";
            ImaginationOverflow.UniversalDeepLinking.Storage.ConfigurationStorage.Save(deeplink_configuration);
            string[] scriptingDefineSymbols = { "ADMIN", "ENV_TESTING" };
            Build(scriptingDefineSymbols, "earth9-admin-testing");
        }

        static void BuildTestingPlay()
        {
            deeplink_configuration.DisplayName = "earth9-launcher-testing";
            deeplink_configuration.DeepLinkingProtocols[0].Scheme = "earth9-launcher-testing";
            ImaginationOverflow.UniversalDeepLinking.Storage.ConfigurationStorage.Save(deeplink_configuration);

            string[] scriptingDefineSymbols = { "PLAY", "ENV_TESTING" };
            Build(scriptingDefineSymbols, "earth9-testing");
        }

        static void Build(String[] scriptingDefineSymbols, String runner)
        {
            PlayerSettings.forceSingleInstance = true;
            string scriptingDefineSymbol = "";
            foreach (var item in scriptingDefineSymbols)
            {
                if (scriptingDefineSymbol.Length > 0)
                    scriptingDefineSymbol = scriptingDefineSymbol + ";";
                scriptingDefineSymbol = scriptingDefineSymbol + item;
            }
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
