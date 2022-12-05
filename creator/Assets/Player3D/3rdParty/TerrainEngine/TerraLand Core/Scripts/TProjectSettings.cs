#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

[InitializeOnLoad]
public class TProjectSettings 
{
    static TProjectSettings()
    {
        TFeedback.FeedbackEvent(EventCategory.Params, EventAction.Usage,"Run",1);
    }

    public static bool FeedbackSystem
    {
        set
        {
            if (value)
                SetRegKeyInt("FeedbackSystem", 1);
            else
                SetRegKeyInt("FeedbackSystem", 0);
        }
        get
        {
            if (PlayerPrefs.HasKey("FeedbackSystem"))
            {
                if (GetRegKeyInt("FeedbackSystem",1) == 0) return false; else return true;
            }
            else
            {
                SetRegKeyInt("FeedbackSystem", 1);
                return true;
            }
        }
    }
    
    public static bool SetRegKeyInt(string key, int value)
    {
        try
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public static void SetRegKeyStr(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }
    
    private static string GetRegKeyStr(string key, string defaultValue)
    {
        if (PlayerPrefs.HasKey(key))
            return PlayerPrefs.GetString(key);
        else
        {
            SetRegKeyStr(key, defaultValue);
            return defaultValue;
        }
    }
    
    private static void SetRegKeyDouble(string key, double value)
    {
        string valueStr = value.ToString();
        PlayerPrefs.SetString(key, valueStr);
        PlayerPrefs.Save();
    }
    
    private static double GetRegKeyDouble(string key, double defaultValue)
    {
        try
        {
            if (PlayerPrefs.HasKey(key)) return double.Parse(PlayerPrefs.GetString(key));
            else return defaultValue;
        }
        catch
        {
            return 0;
        }
    }
    
    public static int GetRegKeyInt(string key , int defaultValue)
    {
        if (PlayerPrefs.HasKey(key)) return PlayerPrefs.GetInt(key);
        else
        {
            SetRegKeyInt(key, defaultValue);
            return defaultValue;
        }

    }
    
    public static void SetRegKeyBool(string key, bool value)
    {
        if (value)
            PlayerPrefs.SetInt(key, 1);
        else
            PlayerPrefs.SetInt(key, 0);
        PlayerPrefs.Save();
    }
    
    private static bool GetRegKeyBool(string key, bool defaultValue)
    {
        if (PlayerPrefs.HasKey(key))
        {
            if (PlayerPrefs.GetInt(key) == 0) return false;
            else return true;
        }
        else
        {
            SetRegKeyBool(key, defaultValue);
            return defaultValue;
        }
    }
    
    public static bool InfoSystemSent
    {
        get
        {
            if (GetRegKeyInt("TLInfoSystemSent", 0) == 1) return true;
            else return false;
        }
        set
        {
            if (value)
                SetRegKeyInt("TLInfoSystemSent", 1);
            else
                SetRegKeyInt("TLInfoSystemSent", 0);

        }
    }

    public static int LastTeamMessageNum
    {
        set
        {
            SetRegKeyInt("LastTeamMessageNum", value);
        }
        get
        {
            return GetRegKeyInt("LastTeamMessageNum",0);
        }
    }

    public static int GetInstalledVersion()
    {
        return GetRegKeyInt("TWVERSION",0); ;
    }

    public enum PipelineType
    {
        Unsupported,
        BuiltInPipeline,
        UniversalPipeline,
        HDPipeline
    }

    /// <summary>
    /// Returns the type of renderpipeline that is currently running
    /// </summary>
    /// <returns></returns>
    public static PipelineType DetectPipeline()
    {
#if UNITY_2019_1_OR_NEWER
        if (GraphicsSettings.renderPipelineAsset != null)
        {
            // SRP
            var srpType = GraphicsSettings.renderPipelineAsset.GetType().ToString();

            if (srpType.Contains("HDRenderPipelineAsset"))
                return PipelineType.HDPipeline;
            else if (srpType.Contains("UniversalRenderPipelineAsset") || srpType.Contains("LightweightRenderPipelineAsset"))
                return PipelineType.UniversalPipeline;
            else
                return PipelineType.Unsupported;
        }

#elif UNITY_2017_1_OR_NEWER
        if (GraphicsSettings.renderPipelineAsset != null)
        {
            // SRP not supported before 2019
            return PipelineType.Unsupported;
        }
#endif
        // no SRP
        return PipelineType.BuiltInPipeline;
    }
}
#endif

