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
using System;
using TerrainEngine;

public class VideoCaptureController : MonoBehaviour
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
    private static float rotateFactor = 1.0f;
    private GameObject pegasusContainer;
    private int fbxImported;
    private float constantSpeed = 25.0f;
    private float rotationMax = 360.0f;
    private OsmBuildingData buildingData;
    void Start()
    {
#if ADMIN
        runTime = TerrainEngine.TerrainController.Get();

        var jsonText = File.ReadAllText("C:\\Users\\"+System.Windows.Forms.SystemInformation.UserName.ToString()+"\\Documents\\earth9\\videoProcessing\\creator_versions.json");
        details = JsonConvert.DeserializeObject<ISet<SubmissionDetail>>(jsonText);

        submissionDetail = details.FirstOrDefault();

        if(details.Count > 0)
        {
            TerrainBootstrap.Latitude = Double.Parse(submissionDetail.center.coordinates.Split(" ")[1]);
            TerrainBootstrap.Longitude =  Double.Parse(submissionDetail.center.coordinates.Split(" ")[0]);
            runTime.latitudeUser = (submissionDetail.center.coordinates.Split(" ")[1]).ToString();
            runTime.longitudeUser = (submissionDetail.center.coordinates.Split(" ")[0]).ToString();
            TerrainEngine.TerrainController.Settings.latitudeUser = (submissionDetail.center.coordinates.Split(" ")[1]).ToString();
            TerrainEngine.TerrainController.Settings.longitudeUser = (submissionDetail.center.coordinates.Split(" ")[0]).ToString();
        
            System.DateTime foo = System.DateTime.Now;
            long unixTime = ((System.DateTimeOffset)foo).ToUnixTimeSeconds();   
        
            WHFbxImporter.ImportObjects(submissionDetail.location);

            // instantiate pegasus camera along with target object for video capture.
            pegasusContainer = SceneObject.Create(SceneObject.Mode.Player,"PegasusContainer");
            GameObject pegasusCamera = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pegasusCamera.name = ObjectName.PEGASUS_CAMERA_GAMEOBJECT;
            pegasusTarget = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pegasusTarget.name = ObjectName.PEGASUS_TARGET_GAMEOBJECT;
            pegasusTarget.transform.position = new Vector3(0, 5f, 0);
            pegasusTarget.transform.parent = pegasusCamera.transform;
            GameObject videoCamera = SceneObject.Create(SceneObject.Mode.Player,ObjectName.PEGASUS_VIDEO_CAMERA);
            pegasusCamera.transform.parent = pegasusContainer.transform;
            videoCamera.AddComponent<Camera>();
            videoCamera.transform.parent = pegasusCamera.transform;
            videoCamera.SetActive(false);
            videoCaptureCamera = videoCamera.GetComponent<Camera>();
            pegasusCamera.GetComponentInChildren<Renderer>().enabled = false;

            // set video capture prefab to captureUIPrefab instance of the controller
            VideoCapture videoCapturePrefab = Instantiate(Resources.Load("Prefabs/VideoCapture", typeof(VideoCapture))) as VideoCapture;
            captureUIPrefab =  videoCapturePrefab;
            captureUI = videoCapturePrefab;
            captureUI.regularCamera = videoCaptureCamera;
            // todo: figure out why video time reduced if following is uncommented. We dont need to capture audio.
            // captureUIPrefab.captureAudio = false;
            captureUI.saveFolder = "C:\\Users\\"+System.Windows.Forms.SystemInformation.UserName.ToString()+"\\Documents\\earth9\\videoProcessing\\";

            GameObject pegasusGo = SceneObject.Create(SceneObject.Mode.Player,ObjectName.PEGASUS_MANAGER_GAMEOBJECT);
            manager = pegasusGo.AddComponent<PegasusManager>();
            manager.m_target = pegasusCamera;

            manager.SetDefaults();
            manager.m_flythroughType = PegasusConstants.FlythroughType.SingleShot;

            GameObject floor = SceneObject.Find(SceneObject.Mode.Player,"Floor001");
            MeshRenderer renderer  = floor.GetComponent<MeshRenderer>();

            GameObject building = SceneObject.Find(SceneObject.Mode.Player,"Building");


            var maxBounds = GetMaxBounds(building);
            MeshRenderer[] meshRenderers = building.GetComponentsInChildren<MeshRenderer> ();
            GameObject wall = SceneObject.Find(SceneObject.Mode.Player,"Wall001");
            var totalFloorPlan = building.transform.childCount;
            var buildingHeight = building.transform.GetChild(totalFloorPlan -1).transform.position.y;
            var height = Math.Max(1.7f,buildingHeight/10.0f) * (buildingHeight);

            pegasusContainer.transform.position = new Vector3(renderer.bounds.center.x,renderer.bounds.center.y,renderer.bounds.center.z);
            pegasusCamera.transform.position = new Vector3(renderer.bounds.min.x,maxBounds.size.y*3.0f,renderer.bounds.min.z*2.0f-maxBounds.size.y);

            pegasusCamera.transform.Rotate(20.0f,20.0f,0f);

            // since terrain will be loading so not starting at first. 
            // we trigger pegasus start on terrain loaded.
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
            pegasusTarget.GetComponentInChildren<Renderer>().enabled = true;

            filename = "myCreation_" + unixTime.ToString();
            captureUI.SetCustomFileName(filename);
            pegasusTarget.GetComponentInChildren<Renderer>().enabled = true;
        }
#endif
    }


    Bounds GetMaxBounds(GameObject parent)
    {
        var total = new Bounds(parent.transform.position, Vector3.zero);
        foreach (var child in parent.GetComponentsInChildren<MeshRenderer>())
        {
            total.Encapsulate(child.bounds);
        }
        return total;
    }



    async void Update()
    {
#if ADMIN

        if (details != null &&
            details.Count > 0 &&
            !videoCatpureStarted &&
            !videoCaptureStopped &&
            runTime.IsInState(TerrainEngine.TerrainController.TerrainState.BuildingsGenerated) &&
            captureUI.status != CaptureStatus.STARTED)
        {

            pegasusTarget.GetComponentInChildren<Renderer>().enabled = true;
            text.text = "Capturing video";
            videoCatpureStarted = true;
            StartPegasusCapture();
        }
        if(details != null && details.Count > 0 &&
        (pegasusContainer.transform.eulerAngles.y >= rotationMax) &&
        captureUI.status == CaptureStatus.STARTED )
        {
            text.text = "Generating video";
            videoCaptureStopped = captureUI.StopCapture();
        }
        // if the pegasus flystate is stopped and if video capture is done, we load new scene to creator mode.
        if(videoCaptureStopped && captureUI.status == CaptureStatus.READY && File.Exists(captureUI.saveFolder+filename+".mp4"))
        {
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
                videoCatpureStarted = false;
        }
        if(details != null 
        && details.Count == 0
        && text != null){
            text.text = "No videos to process";
        }
        if(details != null && details.Count > 0 &&
            runTime.IsInState(TerrainEngine.TerrainController.TerrainState.BuildingsGenerated) &&
            (pegasusContainer.transform.eulerAngles.y <= rotationMax)){
            pegasusContainer.transform.Rotate(0f,constantSpeed * Time.deltaTime,0f,Space.Self);
        }
#endif
    }



    public void StartPegasusCapture()
    {
        captureUI.StartCapture();
    }

    public struct SubmissionDetail
    {
        public string id;
        public string version;
        public string location;
        public string filename;
        public string buildingId;
        public BuildingCenter center;
    }

    public struct BuildingCenter
    {
        public string type;
        public string coordinates;
    }
}


