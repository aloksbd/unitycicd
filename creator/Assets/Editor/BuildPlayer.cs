using UnityEditor;
namespace Editor {          
     class MyEditorScript
     {
          static void PerformBuild ()
          {
//                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "CREATOR");
               BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
               buildPlayerOptions.scenes = new[] { "Assets/Scenes/Main.unity" };
               buildPlayerOptions.locationPathName = "build/Earth9-Creator.exe";
               buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
               buildPlayerOptions.options = BuildOptions.None;
               BuildPipeline.BuildPlayer(buildPlayerOptions);
          }
     }
}
