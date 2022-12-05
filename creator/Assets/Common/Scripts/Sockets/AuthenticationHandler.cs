using Newtonsoft.Json;
using UnityEngine;
using System.Security.Cryptography;
using System;
using System.Text;

public class TokenDecoded
{
    public string username;
    public string[] roles;
    public string sub;
    public string iat;
    public string exp;
}

public class AuthenticationHandler
{
    public static bool IsAuthenticated
    {
        get;
        private set;
    }
    public static string AccessToken { get; private set; }
    private static TokenDecoded _tokenDecoded { get; set; }

    //Set the key and iv to be used for encryption
    private static string _key = "A60AB770FE12OV0024BA7Y0I3F1WE8B0";
    private static string _iv = "1234561237654321";

    public static void Init()
    {
        Trace.Log("AuthenticationHandler.Initialize");

        //Check if the user is already authenticated
        if (PlayerPrefs.HasKey("access_token"))
        {
            string token = PlayerPrefs.GetString("access_token");
            AccessToken = DecodeToken(token);

            if (AccessToken != null)
            {
                if (!IsExpired())
                {
                    IsAuthenticated = true;
                    return;
                }
            }
        }
        Authenticate();
    }

    private static void Authenticate()
    {
        Application.OpenURL("https://testing.app.earth9.net/auth");
        ServerSocket.StartServer((string token) => OnAuthenticationSuccess(token));
    }

    public static bool OnAuthenticationSuccess(string data)
    {
        AccessToken = data;
        IsAuthenticated = AccessToken != null;

        return IsAuthenticated;
    }

    public static bool IsExpired()
    {
        if (AccessToken == null) return true;

        var parts = AccessToken.Split('.');
        if (parts.Length > 2)
        {
            var decode = parts[1];
            var padLength = 4 - decode.Length % 4;
            if (padLength < 4)
            {
                decode += new string('=', padLength);
            }
            var bytes = System.Convert.FromBase64String(decode);
            var userInfo = System.Text.ASCIIEncoding.ASCII.GetString(bytes);

            _tokenDecoded = JsonConvert.DeserializeObject<TokenDecoded>(userInfo);

            var exp = _tokenDecoded.exp;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            return now > long.Parse(exp);
        }
        return true;
    }

    private static AesCryptoServiceProvider GetAesCryptoServiceProvider()
    {
        AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
        aes.BlockSize = 128;
        aes.KeySize = 256;
        aes.Key = Encoding.UTF8.GetBytes(_key);
        aes.IV = Encoding.UTF8.GetBytes(_iv);
        aes.Padding = PaddingMode.PKCS7;
        aes.Mode = CipherMode.CBC;

        return aes;
    }

    public static bool SecurelySaveToken()
    {
        AesCryptoServiceProvider AEScryptoProvider = GetAesCryptoServiceProvider();
        byte[] txtByteData = ASCIIEncoding.ASCII.GetBytes(AccessToken);
        ICryptoTransform trnsfrm = AEScryptoProvider.CreateEncryptor(AEScryptoProvider.Key, AEScryptoProvider.IV);

        byte[] result = trnsfrm.TransformFinalBlock(txtByteData, 0, txtByteData.Length);

        string encryp_text = Convert.ToBase64String(result);

        try
        {
            ServerSocket.CloseSocket();

            PlayerPrefs.SetString("access_token", encryp_text);
            PlayerPrefs.Save();
            return true;
        }
        catch
        {
            PlayerPrefs.DeleteKey("access_token");
            return false;
        }
    }

    private static string DecodeToken(string inputData)
    {
        try
        {
            AesCryptoServiceProvider AEScryptoProvider = GetAesCryptoServiceProvider();

            byte[] txtByteData = Convert.FromBase64String(inputData);
            ICryptoTransform trnsfrm = AEScryptoProvider.CreateDecryptor();

            byte[] result = trnsfrm.TransformFinalBlock(txtByteData, 0, txtByteData.Length);
            string reslt = ASCIIEncoding.ASCII.GetString(result);

            return reslt;
        }
        catch
        {
            return null;
        }
    }
}