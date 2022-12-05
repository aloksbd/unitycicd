

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;


public enum EventCategory
{
    UX,
    Params,
    SystemInfo,
}

public enum EventAction
{
    Click,
    Usage,
    Mode,
    MapSource,
    LatLon,
    Grid,
    AreaSize,
    Search,
    OperatingSystem,
    DeviceType,
    SystemMemorySize,
    graphicsMemorySize,
    UnityVersion,
    Platform
}
struct FeedbackData
{
    public string EventCategory;
    public string EventAction;
    public string label;
    public int value;
}

public static class TFeedback
{
    private static string _GUID = UnityEngine.SystemInfo.deviceUniqueIdentifier;
    private static List<FeedbackData> FeedbackDataPool = new List<FeedbackData>();
    private static bool isSending = false ;
    
    public static void FeedbackSystemInfo()
    {
        FeedbackEvent(EventCategory.SystemInfo, EventAction.OperatingSystem, UnityEngine.SystemInfo.operatingSystem.ToString());
        FeedbackEvent(EventCategory.SystemInfo, EventAction.DeviceType, UnityEngine.SystemInfo.deviceType.ToString());
        FeedbackEvent(EventCategory.SystemInfo, EventAction.SystemMemorySize, UnityEngine.SystemInfo.systemMemorySize.ToString());
        FeedbackEvent(EventCategory.SystemInfo, EventAction.graphicsMemorySize, UnityEngine.SystemInfo.graphicsMemorySize.ToString());
        FeedbackEvent(EventCategory.SystemInfo, EventAction.UnityVersion, UnityEngine.Application.unityVersion.ToString());
        FeedbackEvent(EventCategory.SystemInfo, EventAction.Platform, UnityEngine.Application.platform.ToString());
    }

    public static void FeedbackEvent(EventCategory EventCategory, EventAction EventAction, string label , int? value = null)
    {
        FeedbackEvent(EventCategory.ToString(), EventAction.ToString(), label, value );
    }

    public static void FeedbackEvent(string EventCategory, string EventAction, string label, int? value = null)
    {
        if (FeedbackDataPool == null) FeedbackDataPool = new List<FeedbackData>();
        FeedbackData feedbackData = new FeedbackData();
        feedbackData.EventCategory = EventCategory;
        feedbackData.EventAction = EventAction;
        feedbackData.label = label;
        if (value != null) feedbackData.value = value.Value;
        else feedbackData.value = 0;
        FeedbackDataPool.Add(feedbackData);
        SendNextEvent();
    }

    private static void SendNextEvent()
    {
        if (isSending) return;
        if (FeedbackDataPool.Count > 0)
        {
            FeedbackData feedbackData = FeedbackDataPool[0];
            FeedbackEvent(feedbackData);
            FeedbackDataPool.RemoveAt(0);
        } 
    }

    private static void FeedbackEvent(FeedbackData feedbackData)
    {
        try
        {
            if (String.IsNullOrEmpty(_GUID))
                _GUID = UnityEngine.SystemInfo.deviceUniqueIdentifier;

            if (String.IsNullOrEmpty(_GUID)) return;
            if (String.IsNullOrEmpty(feedbackData.EventCategory)) return;
            if (String.IsNullOrEmpty(feedbackData.EventAction)) return;

            using (var wb = new WebClient())
            {
                var data = new NameValueCollection();
                data["v"] = "1";
                data["tid"] = "UA-219142714-1";
                data["cid"] = _GUID;
                data["t"] = "event";
                data["ec"] = feedbackData.EventCategory;
                data["ea"] = feedbackData.EventAction;
                if (!String.IsNullOrEmpty(feedbackData.label))
                {
                    data["el"] = feedbackData.label;
                    data["ev"] = feedbackData.value.ToString();
                }


                var url = @"http://www.google-analytics.com/collect";
                Uri URL = new Uri(url);
                wb.UploadValuesCompleted += client_UploadValuesCompleted;
                isSending = true;
                wb.UploadValuesAsync(URL, data);
            }
        }
        catch (Exception)
        {
            isSending = false;
            SendNextEvent();
        }

    }

    private static void client_UploadValuesCompleted(object sender, UploadValuesCompletedEventArgs e)
    {
        isSending = false;
        SendNextEvent();
    }


}
