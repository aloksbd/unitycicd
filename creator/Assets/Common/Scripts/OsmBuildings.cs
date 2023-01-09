using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using System.IO;
using UnityEngine.UI;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using TerrainEngine;

public class OsmBuildings
{
    public struct Asset
    {
        public string id;
        public string originalname;

        public string filename;

        public string location;

        public string mimeType;

        public string storageType;

        public string creatorSubmissionId;
    }

    // todo : create separate file for this.    
    private static readonly HttpClient _httpClient = new HttpClient();

    public static async Task<OsmBuildingData> GetBuildingDetail(string buildingId = null, bool fromWelcome = true)
    {
        // todo: figure out where to get it : plot id or building id will be fetched from context.
        if (_httpClient.DefaultRequestHeaders.Authorization == null)
        {
            string token = await TokenFetch.GetAccessToken();
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
        }
#if UNITY_EDITOR
        if (fromWelcome) {
            buildingId = WelcomeUIController.buildingID;
        }
#else
        if (DeeplinkHandler.Instance.isDeeplinkCalled && buildingId == null)
        {
            buildingId = DeeplinkHandler.BuildData.building_id != null ? DeeplinkHandler.BuildData.building_id : null;
            if (buildingId == null)
            {
                LoadingUIController.ActiveMode = LoadingUIController.Mode.NoBuildingDetected;
                LoadingUIController.existingbuildingid = null;
                LoadingUIController.newBuildingId = null;
                LoadingUIController.osmBuildingData = null;
                var loadingUI = SceneObject.Find(SceneObject.Mode.Welcome, ObjectName.LOADING_UI);
                loadingUI.SetActive(true);
            }
        }
#endif
        using (var result = await _httpClient.GetAsync(new System.Uri(WHConstants.API_URL + "/buildings/" + buildingId)))
        {
            try
            {
                string responseData = await result.Content.ReadAsStringAsync();
                APIResponse response = JsonConvert.DeserializeObject<APIResponse>(responseData);
                return response.building;
            }
            catch (Exception e)
            {
                throw new Exception("Unable to get building information : ", e);
            }
        }
    }

    public static async Task<List<OsmBuildingData>> GetBuildingByBoundryBox(BoundryBoxCoordinates boundryBoxCoordinates)
    {
        if (_httpClient.DefaultRequestHeaders.Authorization == null)
        {
            string token = await TokenFetch.GetAccessToken();
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
        }

        var uri = new Uri(WHConstants.API_URL + "/buildings/by-bbox");

        var requestData = JsonConvert.SerializeObject(boundryBoxCoordinates);
        using (var content = new StringContent(requestData, Encoding.UTF8, "application/json"))
        {
            HttpResponseMessage result = _httpClient.PostAsync(uri, content).Result;

            string responseData = await result.Content.ReadAsStringAsync();
            try
            {
                APIResponseData response = JsonConvert.DeserializeObject<APIResponseData>(responseData);
                return response.buildings;
            }
            catch (JsonReaderException e)
            {
                Trace.LogTextToFile("GetBuildingByBoundingBox_Exception", e.ToString(), responseData);
                Trace.Exception(e);
                return null;
            }
        }
    }
}

public class BoundryBoxCoordinates
{
    //  Usage:
    //  
    //  = new BoundryBoxCoordinates()
    //    {
    //        bbox = new double[] {
    //            latitudeBottom, longitudeLeft,
    //            latitudeTop, longitudeRight}
    //    };

    public double[] bbox { get; set; }
}

public class APIResponseData
{
    public List<OsmBuildingData> buildings { get; set; }
    public int statusCode { get; set; }
}

public class AreaBuildingData
{
    public List<OsmBuildingData> buildingData { get; set; }
    public Wgs84Bounds areaBounds;
}

[Serializable]
public class Point
{
    public string type { get; set; }
    public List<double> coordinates { get; set; }
}

public struct Polygon
{
    public string type;
    public List<List<List<float>>> coordinates;
}

public class APIResponse
{
    public OsmBuildingData building { get; set; }
    public int statusCode { get; set; }
}

public struct Detail
{
    public string building;
    public string buildingLevels;
    public string height;
    public string rootLevels;
    public string id;
    public string name;
}

[Serializable]
public class OsmBuildingData
{
    public string id { get; set; }

    public int osmId { get; set; }

    public Detail details { get; set; }

    public string status { get; set; }

    public Point center { get; set; }

    public Polygon geometry { get; set; }

    public string createdAt { get; set; }

    public Media asset { get; set; }

}

[Serializable]
public class Media
{
    public OsmBuildings.Asset fbx;
    public OsmBuildings.Asset video;
}
