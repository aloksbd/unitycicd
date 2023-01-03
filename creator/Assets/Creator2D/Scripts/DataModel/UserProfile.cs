using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;

public class UserProfile
{
    private static readonly HttpClient _httpClient = new HttpClient();
    public static async Task<UserProfileData> GetUserProfileData()
    {
        if (_httpClient.DefaultRequestHeaders.Authorization == null)
        {
            string token = await TokenFetch.GetAccessToken();
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
        }
        using (var result = await _httpClient.GetAsync(new System.Uri(WHConstants.API_URL + "/user")))
        {
            try
            {
                string responseData = await result.Content.ReadAsStringAsync();
                UserProfileData response = JsonConvert.DeserializeObject<UserProfileData>(responseData);
                return response;
            }
            catch (Exception e)
            {
                throw new Exception("Unable to get user profile : ", e);
            }
        }
    }
}

[Serializable]
public class UserProfileData
{
    public string id { get; set; }
    public string username { get; set; }
    public string firstname { get; set; }
    public string lastname { get; set; }
    public List<string> roles { get; set; }
    public string email { get; set; }
    public string age { get; set; }
    public string phoneNumber { get; set; }
    public string city { get; set; }
    public string zipCode { get; set; }
    public string state { get; set; }
    public string country { get; set; }
    public string loginProvider { get; set; }
    public ProfilePicture profilePicture { get; set; }
    public string creatorCredits { get; set; }
}

[Serializable]
public class ProfilePicture
{
    public string id { get; set; }
    public string originalname { get; set; }
    public string filename { get; set; }
    public string location { get; set; }
    public string createdAt { get; set; }
    public string mimetype { get; set; }
    public string storageType { get; set; }
}