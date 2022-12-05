using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

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
        public List<Buildings.Asset> assets;
        public bool isLive;
        public string version;
    }


    private static readonly HttpClient _httpClient = new HttpClient();
    public static async void SubmitCreatorChanges()
    {
        string submissionId = await AddCreatorSubmission();
        CreatorUploadRequest request = new CreatorUploadRequest();
        request.creatorSubmissionId = submissionId;
        request.creatorAssetType = ObjectName.CREATOR_ASSET_TYPE_FBX;
        // todo: make dynamic path according to building id.
        request.filePath = @"C:\Users\" + System.Windows.Forms.SystemInformation.UserName.ToString() + @"\Documents\earth9\eeb52773-318c-4a4b-a16b-c5ff0bb72623\eeb52773-318c-4a4b-a16b-c5ff0bb72623\myCreation.fbx";
        await UploadCreatorAssets.UploadCreatorSubmissionAssets(request);
    }

    public static async Task<string> AddCreatorSubmission()
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
        string buildingId = "53ca1211-e6cb-44d9-88e1-f329d89bbe78";
        if (DeeplinkHandler.Instance.isDeeplinkCalled)
        {
            buildingId = DeeplinkHandler.BuildData.building_id != null ? DeeplinkHandler.BuildData.building_id : "53ca1211-e6cb-44d9-88e1-f329d89bbe78";
        }
        dto.labelType = "GOOD_CREATOR_SUBMISSION";
        dto.buildingId = buildingId;
        string payload = JsonConvert.SerializeObject(dto);
        HttpContent c = new StringContent(payload, Encoding.UTF8, "application/json");
        try
        {
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

}