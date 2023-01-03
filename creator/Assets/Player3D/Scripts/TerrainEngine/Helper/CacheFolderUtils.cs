using UnityEngine;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Random = UnityEngine.Random;
using System.Threading;

namespace TerrainEngine
{
    public class CacheFolderUtils
    {

        private static string _cacheFolder = System.IO.Path.GetTempPath() + "Earth9_GIS/";
        private static string _userCreationFolder = Application.persistentDataPath + "/UserCreation/";
        private static string _userDataFolder = _userCreationFolder + WHConstants.USER;
        private static string _fbxFolder;
        public static string cacheFolder
        {
            get
            {
                if (!Directory.Exists(_cacheFolder)) Directory.CreateDirectory(_cacheFolder);
                return _cacheFolder;
            }
        }

        private static string userDataFolder
        {
            get
            {
                if (String.IsNullOrEmpty(_userDataFolder))
                    _userDataFolder = _userCreationFolder;
                if (!Directory.Exists(_userDataFolder)) Directory.CreateDirectory(_userDataFolder);
                return _userDataFolder;
            }
        }
        private static string userCreationFolder
        {
            get
            {
                if (!Directory.Exists(_userCreationFolder)) Directory.CreateDirectory(_userCreationFolder);
                return _userCreationFolder;
            }
        }


        public static string fbxFolder(string buildingId)
        {
            _fbxFolder = Path.Combine(userDataFolder, buildingId);
            if (!Directory.Exists(_fbxFolder)) Directory.CreateDirectory(_fbxFolder);
            return _fbxFolder;
        }

        public static string getUserDataFolder()
        {
            return _userDataFolder;
        }
    }

}