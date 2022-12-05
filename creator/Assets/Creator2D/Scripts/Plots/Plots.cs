using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using System.IO;
// using CI.HttpClient;
using UnityEngine.UI;
using System.Text;
using Newtonsoft.Json;
// using CI.HttpClient.Helpers;
using System.Net.Http;


public class Plots
{
    // todo : create separate file for this.    
    [Serializable]
    public class Plot
    {
        public string id;
        public string status;
        public string propertyType;
        public string description;
        public string name_main;
        public string google_maps_place_id;
        public string formatted_address;
        public List<string> types;
        public string street_name;
        public string street_number;
        public string apt_suite;
        public string city;
        public string state_province_long;
        public string state_province_short;
        public Point center;
        public Point viewport_south_west;
        public Point viewport_north_east;
        public Polygon boundary_polygon;
        public string boundaryType;
        public string propertyClass;
        public List<Asset> assets;
        public List<Building> buildings;

    }


    [Serializable]
    public class Point
    {
        public string type;
        public List<float> coordinates;
    }



    [Serializable]
    public class Polygon
    {
        public string type;
        public List<List<List<float>>> coordinates;

    }

    [Serializable]
    public class Asset
    {
        public string id;
        public string originalname;

        public string filename;

        public string location;

        public string mimeType;

        public string storageType;
    }

    [Serializable]
    public class Building
    {
        public string id;
        public string name;

        public float creatorCredits;

        public string location;

        public string status;

        public Polygon boundaryPolygon;

        public string mapboxBuildingId;
    }

    private static readonly HttpClient _httpClient = new HttpClient();

    public static async Task<Plots.Plot> GetPlotDetail()
    {
        // todo: figure out where to get it : plot id or building id will be fetched from context.
        Plot plotInformation = null;
        string plotId = "a71e57e9-66ad-44d2-97d1-74df2d259e03";
        // string plotId = "fe59cb51-ccf7-4b3d-8b67-ffc98822249e";
        // string plotId = "52ef106b-35fa-4297-a255-27019e9d5825";
        Debug.Log("Start");
        using (var result = await _httpClient.GetAsync(new System.Uri("https://testingapi.app.earth9.net/plot/" + plotId)))
        {
            string responseData = await result.Content.ReadAsStringAsync();
            plotInformation = JsonConvert.DeserializeObject<Plot>(responseData);
            Debug.Log("End");
            return plotInformation;
        }
    }
}