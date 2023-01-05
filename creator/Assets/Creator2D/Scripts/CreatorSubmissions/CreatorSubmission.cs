using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using TerrainEngine;
using System.IO;

public class CreatorSubmission : MonoBehaviour
{

    private const string MEDIA_TYPE_JSON = "application/json";

    [Serializable]
    public class CreatorSubmissionDto
    {
        public string gameModificationId;
        public string plotId;
        public string version;
        public int blocks;
        public string labelType; // this is for test purpose only ( good type and bad type of creator submission : done for ease of testing voting algorithm)
        public string buildingId;
    }

    // todo : make generic class that holds response pattern of api.
    [Serializable]
    public class CreatorSubmissionResponse
    {
        public Data data;
        public string message;
        public bool success;
    }

    [Serializable]
    public class Data
    {
        public string id;
        public List<OsmBuildings.Asset> assets;
        public bool isLive;
        public string version;
    }
    public class ActiveBuildRequest
    {
        public string buildingId { get; set; }
    }

    private static readonly HttpClient _httpClient = new HttpClient();
    public static async void SubmitCreatorChanges(bool isForUnsubmittedCreation = false)
    {
        string buildingId = isForUnsubmittedCreation ? GetUsersUnSubmittedBuildingId() : CreatorUIController.buildingID;
        CreatorUploadRequest request = new CreatorUploadRequest();
        request.creatorAssetType = ObjectName.CREATOR_ASSET_TYPE_FBX;
        string fbxPath = CacheFolderUtils.fbxFolder(buildingId);
        request.filePath = fbxPath + WHConstants.PATH_DIVIDER + "myCreation.fbx";
        string submissionId = await AddCreatorSubmission(buildingId, request.filePath);
        request.creatorSubmissionId = submissionId;
        await UploadCreatorAssets.UploadCreatorSubmissionAssets(request);
        DeleteLocalCreation(fbxPath);
        MonoBehaviour mono = GameObject.Find(ObjectName.BOOTSTRAP_OBJECT).GetComponent<MonoBehaviour>();
        mono.StartCoroutine(ShowCompletedMsg("Submitted sucessfully."));
    }

    static IEnumerator ShowCompletedMsg(string completedMsg)
    {
        var loadingUI = SceneObject.Find(SceneObject.Mode.Welcome, ObjectName.LOADING_UI);
        loadingUI.SetActive(false);
        LoadingUIController.ActiveMode = LoadingUIController.Mode.SavedOrSubmitted;
        LoadingUIController.labelTitle = completedMsg;
        loadingUI.SetActive(true);
        yield return new WaitForSeconds(2f);
        loadingUI.SetActive(false);
    }

    public static async Task<string> AddCreatorSubmission(string buildingId,string filePath)
    {
        string submissionId = "";
        if (_httpClient.DefaultRequestHeaders.Authorization == null)
        {
            string token = await TokenFetch.GetAccessToken();
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
        }
        var res = await _httpClient.GetAsync(WHConstants.API_URL + WHConstants.SUBMISSION_VERSION_ROUTE);

        CreatorSubmissionDto dto = new CreatorSubmissionDto();
        dto.gameModificationId = "eeb52773-318c-4a4b-a16b-c5ff0bb72623";
        dto.plotId = "d46d0d39-7388-43a1-a8ed-0c5d2787c163";
        dto.blocks = 2;
        // todo : flow to decide default building for user.
        dto.labelType = "GOOD_CREATOR_SUBMISSION";
        dto.buildingId = buildingId;
        string payload = JsonConvert.SerializeObject(dto);
        HttpContent c = new StringContent(payload, Encoding.UTF8, "application/json");
        try
        {
            if (!File.Exists(filePath))
            {
                await CreatorUIController.OnSaveBeforeSubmit();
            }
            LoadingUIController.ActiveMode = LoadingUIController.Mode.Submitting;
            var loadingUI = SceneObject.Find(SceneObject.Mode.Welcome, ObjectName.LOADING_UI);
            loadingUI.SetActive(true);
            var response = await _httpClient.PostAsync(WHConstants.API_URL + "/creator-submissions", c);
            response.EnsureSuccessStatusCode();
            string resp = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
            CreatorSubmissionResponse submissionResponse = JsonConvert.DeserializeObject<CreatorSubmissionResponse>(resp);
            submissionId = submissionResponse.data.id;
            return submissionId;
        }
        catch (Exception e)
        {
            Trace.Exception(e);
        }
        return submissionId;
    }

    public static async Task ActiveBuilds(string buildingId)
    {
        if (_httpClient.DefaultRequestHeaders.Authorization == null)
        {
            string token = await TokenFetch.GetAccessToken();
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
        }
        ActiveBuildRequest payload = new ActiveBuildRequest()
        {
            buildingId = buildingId
        };
        HttpContent c = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        try
        {
            await _httpClient.PostAsync(WHConstants.API_URL + "/active-builds/add", c);
        }
        catch (Exception e)
        {
            Trace.Exception(e);
        }
    }

    public static void DeleteLocalCreation(string filePath)
    {
        Directory.Delete(filePath, true);
    }

    public static void DeleteAllLocalCreation()
    {
        if (Directory.Exists(Application.persistentDataPath + "/UserCreation/" + WHConstants.USER))
        {
            DirectoryInfo dirs = new DirectoryInfo(Application.persistentDataPath + "/UserCreation/" + WHConstants.USER);
            foreach (DirectoryInfo dir in dirs.GetDirectories())
            {
                Directory.Delete(dir.FullName, true);
            }
        }
    }

    public static string GetUsersUnSubmittedBuildingId()
    {
        string[] dirs = Directory.GetDirectories(CacheFolderUtils.getUserDataFolder(), "*", SearchOption.TopDirectoryOnly);

        foreach (string dir in dirs)
        {
            if (File.Exists(dir + "\\myCreation.fbx"))
            {
                return dir.Substring(dir.LastIndexOf("\\") + 1);
            }
        }
        return null;
    }
}