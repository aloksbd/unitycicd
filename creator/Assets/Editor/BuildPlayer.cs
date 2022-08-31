using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace Earth9Builder {
    class BuildScript
    {
        static void BuildCreator () {
            Build("CREATOR", "Earth9-Creator");
        }

        static void BuildPlay () {
            Build("PLAY", "Earth9");
        }

        static void Build (String scriptingDefineSymbol, String runner)
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, scriptingDefineSymbol+";FBXSDK_RUNTIME");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new[] { "Assets/Scenes/Version.unity","Assets/Scenes/WelcomeScreen.unity","Assets/Scenes/Main.unity" };

            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.StandaloneWindows64: {
                    buildPlayerOptions.locationPathName = "build/windows/"+runner+"/"+runner+".exe";
                    buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
                    break;
                }
                case BuildTarget.StandaloneOSX:
                    buildPlayerOptions.locationPathName = "build/mac/"+runner+"/"+runner+".app";
                    buildPlayerOptions.target = BuildTarget.StandaloneOSX;
                    break;
            }
                
            
            buildPlayerOptions.options = BuildOptions.None;
            BuildPipeline.BuildPlayer (buildPlayerOptions);
        }
    }
    
}