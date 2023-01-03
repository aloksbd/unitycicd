using System.Diagnostics;
using System.Net;
using TerrainEngine;

public class WebRequestRetries
{
    private static WebRequestRetries instance;
    public static WebRequestRetries Get()
    {
        if (instance == null)
        {
            instance = new WebRequestRetries();
        }
        return instance;
    }
    private const int GIS_SERVICEREQUEST_RETRYCOUNT = 5;
    public delegate WebExceptionStatus WebRequestHandler(string x);
    public static WebExceptionStatus WebRequestMethod(WebRequestHandler webRequestHandler, string index)
    {
        WebExceptionStatus status = WebExceptionStatus.Success;
        string methodName = webRequestHandler.Method.Name;

        Abortable abortable = new Abortable(string.Format("WebRequestMethod({0}({0}))", methodName, index));

        Trace.Log(TerrainTrace.Config(TerrainTrace.Flag.WebRequests), "--- TERRAIN WebRequestMethod(" + methodName + "()) ENTER");

        for (int tries = 0; tries < GIS_SERVICEREQUEST_RETRYCOUNT; tries++)
        {
            if (abortable.shouldAbort)
            {
                return WebExceptionStatus.RequestCanceled;
            }

            if (tries > 0)
            {
                Trace.Warning("Request {0}({1}) failed with status {2}. Retrying.",
                    methodName, index, status);
            }

            status = webRequestHandler(index);

            if (status == WebExceptionStatus.Success)
            {
                if (tries > 0)
                {
                    Trace.Warning("Request {0}({1}) succeeded on retry.",
                        methodName, index);
                }
                break;
            }
        }

        if (status != WebExceptionStatus.Success)
        {
            Trace.Warning("Request {0}({1}) failed after {2} tries. Status: {3}",
                methodName, index, GIS_SERVICEREQUEST_RETRYCOUNT, status);
            
            TerrainController.Get().ReportFatalWebServiceError(status);
        }

        Trace.Log(TerrainTrace.Config(TerrainTrace.Flag.WebRequests), "--- TERRAIN WebRequestMethod(" + methodName + "()) EXIT with WebExceptionStatus {0}", status);
        return status;
    }

    public delegate WebExceptionStatus WebRequestHandlerFar();

    public static WebExceptionStatus WebRequestMethodFar(WebRequestHandlerFar webRequestHandler)
    {
        WebExceptionStatus status = WebExceptionStatus.Success;
        string methodName = webRequestHandler.Method.Name;

        Abortable abortable = new Abortable(string.Format("WebRequestMethodFar({0})", methodName));

        Trace.Log(TerrainTrace.Config(TerrainTrace.Flag.WebRequests), "--- TERRAIN WebRequestMethodFar(" + methodName + "()) ENTER");

        for (int tries = 0; tries < GIS_SERVICEREQUEST_RETRYCOUNT; tries++)
        {
            if (abortable.shouldAbort)
            {
                return WebExceptionStatus.RequestCanceled;
            }

            if (tries > 0)
            {
                Trace.Warning("Request {0}() failed with status {1}. Retrying",
                    methodName, status);
            }

            status = webRequestHandler();

            if (status == WebExceptionStatus.Success)
            {
                if (tries > 0)
                {
                    Trace.Warning("Request {0}() succeeded on retry.",
                        methodName);
                }
                break;
            }
        }

        if (status != WebExceptionStatus.Success)
        {
            Trace.Warning("Request {0}() failed after {1} tries. Status: {2}",
                methodName, GIS_SERVICEREQUEST_RETRYCOUNT, status);

            TerrainController.Get().ReportFatalWebServiceError(status);
        }

        Trace.Log(TerrainTrace.Config(TerrainTrace.Flag.WebRequests), "--- TERRAIN WebRequestMethodFar(" + methodName + "()) EXIT with WebExceptionStatus {0}", status);

        return status;
    }
}

