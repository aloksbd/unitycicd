
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using System;
using UnityEngine.UI;
public class VersionChanger : MonoBehaviour
{
    private static int count;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V) && VersionDownloader.submissionFiles.Count > 0)
        {
            if (count >= VersionDownloader.submissionFiles.Count)
            {
                count = 0;
            }
            var item = VersionDownloader.submissionFiles.ElementAt(count);
            count++;
            if (SceneObject.GetPlayer(SceneObject.Mode.Creator) != null)
            {
                WHFbxImporter2D.ImportObjects(item.Key, new List<Vector3>());
            }
            else
            {
                WHFbxImporter.ImportObjects(item.Key);
            }
        }
    }
}