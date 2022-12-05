using System;
using UnityEngine;

public class Trace
{
    public class Config
    {
        public bool enabled;
        public bool includeTimeStamp; 

        public Config( 
            bool enabled_ = true, 
            bool includeTimestamp_ = true)
        {
            enabled = enabled_;
            includeTimeStamp = includeTimestamp_;
        }
    };

    public static void Log(string format, params object[] args)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, format, args);
    }

    public static void Log(Config config, string format, params object[] args)
    {
        try
        {
            if (config != null && config.enabled)
            {
                if (config.includeTimeStamp)
                {
                    Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "[" + System.DateTime.Now.ToString("T") + "] " + format, args);
                }
                else
                {
                    Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, format, args);
                }
            }
        }
        catch(Exception e)
        {
            Debug.Log(e);
        }
    }

    public static void Warning(string format, params object[] args)
    {
        Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, format, args);
    }

    public static void Error(string format, params object[] args)
    {
        Debug.LogFormat(LogType.Error, LogOption.None, null, format, args);
    }

    public static void Exception(Exception e)
    {
        Debug.LogException(e.InnerException != null ? e.InnerException : e);
    }

    public static void Exception(Exception e, string format, params object[] args)
    {
        Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, format, args);
        Debug.LogException(e.InnerException != null ? e.InnerException : e);
    }

    public static void Exception(SystemException e)
    {
        Debug.LogException(e.InnerException != null ? e.InnerException : e);
    }
    public static void Exception(SystemException e, string format, params object[] args)
    {
        Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, format, args);
        Debug.LogException(e.InnerException != null ? e.InnerException : e);
    }

    public static void Assert(bool condition, string format, params object[] args)
    {
        if (!condition)
        {
            Debug.LogFormat(LogType.Assert, LogOption.None, null, format, args);
#if !DEBUG
            Debug.DebugBreak();
#else
            throw new System.Exception("Assertion failed: " + System.String.Format(format, args));
#endif
        }
    }
}
