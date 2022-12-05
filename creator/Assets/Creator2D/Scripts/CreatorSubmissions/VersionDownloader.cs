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
public class VersionDownloader
{
    private static string user = System.Windows.Forms.SystemInformation.UserName.ToString();

    [Serializable]
    public struct SubmissionDetail
    {
        public string version;
        public bool isLive;

    }
    public static Dictionary<string, SubmissionDetail> submissionFiles = new Dictionary<string, SubmissionDetail>();
    private static readonly HttpClient _httpClient = new HttpClient();

    public async static void PrepareData()
    {
        CreateDirs();
        if (_httpClient.DefaultRequestHeaders.Authorization == null)
        {
            string token = await TokenFetch.GetAccessToken();
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
        }
        var res = await _httpClient.GetAsync(WHConstants.API_URL + WHConstants.SUBMISSION_VERSION_ROUTE);
        string submissionResponse = await res.Content.ReadAsStringAsync();
        List<CreatorSubmission.Data> submissions = JsonConvert.DeserializeObject<List<CreatorSubmission.Data>>(submissionResponse);
        // download the fbx of creations in local
        for (int i = 0; i < submissions.Count; i++)
        {
            Buildings.Asset fbxAsset = (from asset in submissions[i].assets
                                        where asset.mimeType == "application/octet-stream"
                                        select asset).FirstOrDefault();
            Debug.Log("fbx asset is " + JsonConvert.SerializeObject(fbxAsset));
            if (fbxAsset.id != null)
            {
                string baseFolder = @"C:\Users\" + user + @"\Documents\earth9\eeb52773-318c-4a4b-a16b-c5ff0bb72623\eeb52773-318c-4a4b-a16b-c5ff0bb72623\";
                if (submissions[i].isLive)
                {
                    baseFolder += @"live\";
                }
                else
                {
                    baseFolder += @"submissions\";
                }
                string fileLocation = baseFolder + fbxAsset.filename;
                await DownloadFileTaskAsync(WHConstants.S3_BUCKET_PATH + "/" + fbxAsset.location + "/" + fbxAsset.filename, fileLocation);
                fbxAsset.creatorSubmissionId = submissions[i].id;
                SubmissionDetail detail = new SubmissionDetail();
                detail.version = submissions[i].version;
                detail.isLive = submissions[i].isLive;
                submissionFiles.Add(fileLocation, detail);
            }
        }
    }

    public static async Task DownloadFileTaskAsync(string uriStr, string savePath)
    {
        if (!File.Exists(savePath))
        {

            using (var client = new HttpClient())
            {
                var uri = new Uri(uriStr);
                using (var s = await client.GetStreamAsync(uri))
                {
                    using (var fs = new FileStream(savePath, FileMode.CreateNew))
                    {
                        await s.CopyToAsync(fs);
                    }
                }
            }
        }
    }

    static void CreateDirs()
    {
        if (!Directory.Exists(@"C:\Users\" + user + @"\Documents\earth9\eeb52773-318c-4a4b-a16b-c5ff0bb72623\eeb52773-318c-4a4b-a16b-c5ff0bb72623\submissions"))
        {
            Directory.CreateDirectory(@"C:\Users\" + user + @"\Documents\earth9\eeb52773-318c-4a4b-a16b-c5ff0bb72623\eeb52773-318c-4a4b-a16b-c5ff0bb72623\submissions");
        }
        if (!Directory.Exists(@"C:\Users\" + user + @"\Documents\earth9\eeb52773-318c-4a4b-a16b-c5ff0bb72623\eeb52773-318c-4a4b-a16b-c5ff0bb72623\live"))
        {
            Directory.CreateDirectory(@"C:\Users\" + user + @"\Documents\earth9\eeb52773-318c-4a4b-a16b-c5ff0bb72623\eeb52773-318c-4a4b-a16b-c5ff0bb72623\live");
        }
    }
}