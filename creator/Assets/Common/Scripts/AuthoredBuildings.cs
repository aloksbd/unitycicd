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

public class AuthoredBuildings
{
    private static readonly HttpClient _httpClient = new HttpClient();

    public static async Task<List<AuthoredBuildingData>> GetLiveAuthoredBuildings(BoundryBoxCoordinates boundryBoxCoordinates)
    {
        if (_httpClient.DefaultRequestHeaders.Authorization == null)
        {
            string token = await TokenFetch.GetAccessToken();
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
        }

        var uri = new Uri(WHConstants.API_URL + "/buildings/live");

        var requestData = JsonConvert.SerializeObject(boundryBoxCoordinates);
        using (var content = new StringContent(requestData, Encoding.UTF8, "application/json"))
        {
            HttpResponseMessage result = _httpClient.PostAsync(uri, content).Result;

            string responseData = await result.Content.ReadAsStringAsync();
            Debug.Log("live ======== "+responseData);
            try
            {
                APIResponse response = JsonConvert.DeserializeObject<APIResponse>(responseData);
                return response.liveBuildings;
            }
            catch (JsonReaderException e)
            {
                Trace.LogTextToFile("GetLiveAuthoredBuildings_Exception", e.ToString(), responseData);
                Trace.Exception(e);
                return null;
            }
        }
    }

    public class APIResponse
    {
        public List<AuthoredBuildingData> liveBuildings { get; set; }
        public int statusCode { get; set; }
    }

    public class Creation
    {
        public string id { get; set; }
        public string liveAt { get; set; }
        public List<OsmBuildings.Asset> assets { get; set; }
    }


    [Serializable]
    public class AuthoredBuildingData
    {
        public string id { get; set; }

        public Point center { get; set; }

        public Polygon geometry { get; set; }

        public string createdAt { get; set; }

        public List<Creation> creations { get; set; }
    }
}

