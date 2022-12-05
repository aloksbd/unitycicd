using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Pegasus;
using Evereal.VideoCapture;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;
using System.IO;

public class PegasusGameController : MonoBehaviour
{
    [Header("Video Capture")]
    [SerializeField] Camera videoCaptureCamera;
    [SerializeField] VideoCapture captureUIPrefab;
    private VideoCapture captureUI;
    private PegasusManager manager;
    private VideoCapture videoCapture;
    private GameObject pegasusTarget;
    private Text text;
    private bool videoUploadStarted;

    private SubmissionDetail submissionDetail;

    private ISet<SubmissionDetail> details;
        private TerrainEngine.TerrainController runTime;

    private bool videoCatpureStarted;

    private bool videoCaptureStopped;

    private string filename;
    void Start()
    {
#if ADMIN
        runTime = TerrainEngine.TerrainController.Get();

        // instantiate pegasus camera along with target object for video capture.
        GameObject pegasusCamera = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pegasusCamera.name = ObjectName.PEGASUS_CAMERA_GAMEOBJECT;
        pegasusTarget = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pegasusTarget.name = ObjectName.PEGASUS_TARGET_GAMEOBJECT;
        pegasusTarget.transform.position = new Vector3(0, 5f, 0);
        pegasusTarget.transform.parent = pegasusCamera.transform;
        GameObject videoCamera = SceneObject.Create(SceneObject.Mode.Player,ObjectName.PEGASUS_VIDEO_CAMERA);
        // GameObject videoCamera = new GameObject("VideoCamera");
        videoCamera.AddComponent<Camera>();
        videoCamera.transform.parent = pegasusCamera.transform;
        videoCamera.SetActive(false);
        videoCaptureCamera = videoCamera.GetComponent<Camera>();
        pegasusCamera.GetComponentInChildren<Renderer>().enabled = false;
        pegasusTarget.GetComponentInChildren<Renderer>().enabled = false;

        // set video capture prefab to captureUIPrefab instance of the controller
        VideoCapture videoCapturePrefab = Instantiate(Resources.Load("Prefabs/VideoCapture", typeof(VideoCapture))) as VideoCapture;
        captureUIPrefab =  videoCapturePrefab;
        captureUI = videoCapturePrefab;
        captureUI.regularCamera = videoCaptureCamera;
        captureUI.saveFolder = "C:\\Users\\"+System.Windows.Forms.SystemInformation.UserName.ToString()+"\\Documents\\earth9\\videoProcessing\\";
        System.DateTime foo = System.DateTime.Now;
        long unixTime = ((System.DateTimeOffset)foo).ToUnixTimeSeconds();


        GameObject pegasusGo = SceneObject.Create(SceneObject.Mode.Player,ObjectName.PEGASUS_MANAGER_GAMEOBJECT);
        manager = pegasusGo.AddComponent<PegasusManager>();
        manager.m_target = pegasusCamera;

        manager.SetDefaults();
        manager.m_flythroughType = PegasusConstants.FlythroughType.SingleShot;
        manager.AddPOI(new Vector3(0.0f, 20.0f, 60.0f), new Vector3(0.0f, 20.0f, 0.0f));
        manager.AddPOI(new Vector3(-60.0f, 20.0f, 0.0f), new Vector3(-10.0f, 20.0f, 0.0f));
        manager.AddPOI(new Vector3(0.0f, 20.0f, -60.0f), new Vector3(0.0f, 20.0f, 0.0f));
        manager.AddPOI(new Vector3(60.0f, 20.0f, 0.0f), new Vector3(10.0f, 20.0f, 0.0f));

        // since terrain will be loading so not starting at first. 
        // we trigger pegasus start on key press.
        manager.m_autoStartAtRuntime = false;

        // text showing capture status
        GameObject textGO = SceneObject.Create(
            SceneObject.Mode.Player,
            ObjectName.UPLOADTEXT);

        textGO.transform.localPosition = new Vector3(50,10,0);
        textGO.AddComponent<Text>();
        text = textGO.GetComponent<Text>();

        Font arial;
        arial = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
        text.text = "";
        text.font = arial;
        text.fontSize = 10;
        text.alignment = TextAnchor.LowerLeft;
          var jsonText = File.ReadAllText("C:\\Users\\"+System.Windows.Forms.SystemInformation.UserName.ToString()+"\\Documents\\earth9\\videoProcessing\\creator_versions.json");
        details = JsonConvert.DeserializeObject<ISet<SubmissionDetail>>(jsonText);

        submissionDetail = details.FirstOrDefault();
        filename = "myCreation_" + unixTime.ToString();
        captureUI.SetCustomFileName(filename);
        pegasusTarget.GetComponentInChildren<Renderer>().enabled = true;
    if(details.Count > 0){
        WHFbxImporter.ImportObjects(submissionDetail.location);
    }
#endif
    }

    async void Update()
    {
#if ADMIN

        if (details != null && details.Count > 0 &&
            !videoCatpureStarted &&
            runTime.IsInState(TerrainEngine.TerrainController.TerrainState.Running) &&
            captureUI.status != CaptureStatus.STARTED)
        {
            pegasusTarget.GetComponentInChildren<Renderer>().enabled = true;
            text.text = "Capturing video";
            videoCatpureStarted = true;
            StartPegasusCapture();
        }
        // once the flythrough of pegasus is ended (reached all the POIs), we stop the video capture
        if(manager.m_currentState == PegasusConstants.FlythroughState.Stopped && captureUI.status == CaptureStatus.STARTED)
        {
            text.text = "Generating video";
            videoCaptureStopped = captureUI.StopCapture();
        }
        // if the pegasus flystate is stopped and if video capture is done, we load new scene to creator mode.
        if(videoCaptureStopped && manager.m_currentState == PegasusConstants.FlythroughState.Stopped && captureUI.status == CaptureStatus.READY && File.Exists(captureUI.saveFolder+filename+".mp4"))
        {
            Destroy(SceneObject.Find(
                SceneObject.Mode.Player,
                ObjectName.PEGASUS_CAMERA_GAMEOBJECT));

            if(!videoUploadStarted)
            {
                text.text = "Uploading video";
                videoUploadStarted = true;
                details.Remove(submissionDetail);
                File.WriteAllText("C:\\Users\\"+System.Windows.Forms.SystemInformation.UserName.ToString()+"\\Documents\\earth9\\videoProcessing\\creator_versions.json", JsonConvert.SerializeObject(details));
                
                CreatorUploadRequest request = new CreatorUploadRequest();
                request.creatorSubmissionId = submissionDetail.id;
                request.creatorAssetType = ObjectName.CREATOR_ASSET_TYPE_VIDEO;
                request.filePath = captureUI.saveFolder+filename+".mp4";
                await UploadCreatorAssets.UploadCreatorSubmissionAssets(request);
                Application.Quit();
            }
        }
        if(details != null && details.Count == 0){
            text.text = "No videos to process";
        }
#endif
    }

    public void StartPegasusCapture()
    {
        manager.StartFlythrough();
        captureUI.StartCapture();
    }

    public struct SubmissionDetail
    {
        public string id;
        public string version;
        public string location;
        public string filename;
    }


}


