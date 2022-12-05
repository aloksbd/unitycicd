using UnityEngine;
using UnityEditor;

public class FullPackageExporter
{
    [MenuItem("Assets/Export Package With Settings")]
    static void Export ()
    {
        TFeedback.FeedbackEvent(EventCategory.UX, EventAction.Click, "Export_Package_With_Settings");

        AssetDatabase.ExportPackage
        (
            "Assets",
            "Full Project.unitypackage",
            ExportPackageOptions.Interactive | ExportPackageOptions.Recurse | ExportPackageOptions.IncludeLibraryAssets | ExportPackageOptions.IncludeDependencies
        );
    }
}

