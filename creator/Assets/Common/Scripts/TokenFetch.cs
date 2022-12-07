using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Net.Http;
using UnityEngine.Networking;
using Unity.Services.Core;
using System.Text;
using Newtonsoft.Json;


public class TokenFetch : MonoBehaviour
{
    [Serializable]
    public class TokenClassName
    {
        public string access_token;
    }
    private static readonly HttpClient httpClient = new HttpClient();

    public static async Task<string> GetAccessToken()
    {
        if (AuthenticationHandler.IsExpired())
        {
            AuthenticationHandler.Authenticate();
        }
        return AuthenticationHandler.AccessToken;
    }
}
