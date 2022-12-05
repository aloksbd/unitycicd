using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using Newtonsoft.Json;

using System.Threading.Tasks;
public class UploadCreatorAssets
{
    private static readonly HttpClient _httpClient = new HttpClient();
    public static async Task<string> UploadCreatorSubmissionAssets(CreatorUploadRequest uploadRequest)
    {
        // process the saved fbx files for video generation and creator submissions.
        string token = await TokenFetch.GetAccessToken();
         StreamContent fileStreamContent = null;
         fileStreamContent = new StreamContent(File.OpenRead(uploadRequest.filePath));
        if (uploadRequest.creatorAssetType == ObjectName.CREATOR_ASSET_TYPE_FBX)
        {
            fileStreamContent.Headers.Add("Content-Type", "application/octet-stream");
        }
        else
        {
            fileStreamContent.Headers.Add("Content-Type", "video/mp4");
        }
        string response = "";
        using (var multipartFormContent = new MultipartFormDataContent())
        {
            try
            {
                multipartFormContent.Add(fileStreamContent, name: "files", fileName: "myCreation");
                var result = await _httpClient.PostAsync(WHConstants.API_URL + "/creator-submissions/plot/video/" + uploadRequest.creatorSubmissionId, multipartFormContent);
                result.EnsureSuccessStatusCode();
                response = await result.Content.ReadAsStringAsync();
                return response;
            }
            catch (Exception e)
            {
                Trace.Exception(e);
            }
            return response;
        }
    }
}

public struct CreatorUploadRequest
{
    public string creatorSubmissionId;
    public string creatorAssetType;
    public string filePath;
}