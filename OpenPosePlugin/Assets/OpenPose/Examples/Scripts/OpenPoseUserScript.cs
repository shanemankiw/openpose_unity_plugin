// OpenPose Unity Plugin v1.0.0alpha-1.5.0
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OpenPose.Example {
    /*
     * User example of using OPWrapper
     */
    public class OpenPoseUserScript : MonoBehaviour {

        // HumanController2D prefab
        [SerializeField] GameObject humanPrefab;

        // UI elements
        [SerializeField] RectTransform outputTransform;
        [SerializeField] ImageRenderer bgImageRenderer;
        [SerializeField] Transform humanContainer;
        [SerializeField] Text fpsText;
        [SerializeField] Text peopleText;
        [SerializeField] Text stateText;
        [SerializeField] Text PointText;

        // Output
        private OPDatum 
        datum, // current data of the frame, the fifth datum
        datum_1;

        private float reference_torso_length = 382.0f;
        private float real_torso_length = 0.8f; // m

        private int num_of_people_1 = 0;

        // OpenPose settings
        public ProducerType inputType = ProducerType.Video;
        public string producerString = "D://projects_in_class//openpose_unity_plugin//openpose-binary//examples//media//video.avi";
        public int maxPeople = -1;
        public float renderThreshold = 0.05f;
        public bool
            handEnabled = false,
            faceEnabled = false;
        public Vector2Int
            netResolution = new Vector2Int(-1, 16),
            handResolution = new Vector2Int(16, 16),
            faceResolution = new Vector2Int(16, 16);
        
        
        public float alpha = 4; // the hyper-parameter we use to balance two types of scores

        // What we are looking for.
        public float
        abs_x, // cordinate of x
        abs_y, // cordinate of y
        abs_z; // cordinate of z

        public void SetHandEnabled(bool enabled) { handEnabled = enabled; }
        public void SetFaceEnabled(bool enabled) { faceEnabled = enabled; }
        public void SetRenderThreshold(string s){float res; if (float.TryParse(s, out res)){renderThreshold = res;};}
        public void SetMaxPeople(string s){int res; if (int.TryParse(s, out res)){maxPeople = res;};}
        public void SetPoseResX(string s){int res; if (int.TryParse(s, out res)){netResolution.x = res;};}
        public void SetPoseResY(string s){int res; if (int.TryParse(s, out res)){netResolution.y = res;};}
        public void SetHandResX(string s){int res; if (int.TryParse(s, out res)){handResolution.x = res;};}
        public void SetHandResY(string s){int res; if (int.TryParse(s, out res)){handResolution.y = res;};}
        public void SetFaceResX(string s){int res; if (int.TryParse(s, out res)){faceResolution.x = res;};}
        public void SetFaceResY(string s){int res; if (int.TryParse(s, out res)){faceResolution.y = res;};}

        public void ApplyChanges(){
            // Restart OpenPose
            StartCoroutine(UserRebootOpenPoseCoroutine());
        }

        // Bg image
        public bool renderBgImg = true;
        public void ToggleRenderBgImg(){
            renderBgImg = !renderBgImg;
            bgImageRenderer.FadeInOut(renderBgImg);
        }

        // Number of people
        int numberPeople = 0;

        // Frame rate calculation
        private int queueMaxCount = 20; 
        private Queue<float> frameTimeQueue = new Queue<float>();
        private float avgFrameRate = 0f;
        private int frameCounter = 0;

        private void Start() {
            // Register callbacks
            OPWrapper.OPRegisterCallbacks();
            // Enable OpenPose log to unity (default true)
            OPWrapper.OPEnableDebug(true);
            // Enable OpenPose output to unity (default true)
            OPWrapper.OPEnableOutput(true);
            // Enable receiving image (default false)
            OPWrapper.OPEnableImageOutput(true);

            // Configure OpenPose with default value, or using specific configuration for each
            /* OPWrapper.OPConfigureAllInDefault(); */
            UserConfigureOpenPose();

            // Start OpenPose
            OPWrapper.OPRun();
        }

        // Parameters can be set here
        private void UserConfigureOpenPose(){
            OPWrapper.OPConfigurePose(
                /* poseMode */ PoseMode.Enabled, /* netInputSize */ netResolution, /* outputSize */ null,
                /* keypointScaleMode */ ScaleMode.InputResolution,
                /* gpuNumber */ -1, /* gpuNumberStart */ 0, /* scalesNumber */ 1, /* scaleGap */ 0.25f,
                /* renderMode */ RenderMode.Auto, /* poseModel */ PoseModel.BODY_25,
                /* blendOriginalFrame */ true, /* alphaKeypoint */ 0.6f, /* alphaHeatMap */ 0.7f,
                /* defaultPartToRender */ 0, /* modelFolder */ null,
                /* heatMapTypes */ HeatMapType.None, /* heatMapScaleMode */ ScaleMode.ZeroToOne,
                /* addPartCandidates */ false, /* renderThreshold */ renderThreshold, /* numberPeopleMax */ maxPeople,
                /* maximizePositives */ false, /* fpsMax fps_max */ -1.0,
                /* protoTxtPath */ "", /* caffeModelPath */ "", /* upsamplingRatio */ 0f);

            OPWrapper.OPConfigureHand(
                /* enable */ handEnabled, /* detector */ Detector.Body, /* netInputSize */ handResolution,
                /* scalesNumber */ 1, /* scaleRange */ 0.4f, /* renderMode */ RenderMode.Auto,
                /* alphaKeypoint */ 0.6f, /* alphaHeatMap */ 0.7f, /* renderThreshold */ 0.2f);

            OPWrapper.OPConfigureFace(
                /* enable */ faceEnabled, /* detector */ Detector.Body, 
                /* netInputSize */ faceResolution, /* renderMode */ RenderMode.Auto,
                /* alphaKeypoint */ 0.6f, /* alphaHeatMap */ 0.7f, /* renderThreshold */ 0.4f);

            OPWrapper.OPConfigureExtra(
                /* reconstruct3d */ false, /* minViews3d */ -1, /* identification */ false, /* tracking */ -1,
                /* ikThreads */ 0);

            OPWrapper.OPConfigureInput(
                /* producerType */ inputType, /* producerString */ producerString,
                /* frameFirst */ 0, /* frameStep */ 1, /* frameLast */ ulong.MaxValue,
                /* realTimeProcessing */ false, /* frameFlip */ false,
                /* frameRotate */ 0, /* framesRepeat */ false,
                /* cameraResolution */ null, /* cameraParameterPath */ null,
                /* undistortImage */ false, /* numberViews */ -1);

            OPWrapper.OPConfigureOutput(
                /* verbose */ -1.0, /* writeKeypoint */ "", /* writeKeypointFormat */ DataFormat.Xml,
                /* writeJson */ "", /* writeCocoJson */ "", /* writeCocoJsonVariants */ 1,
                /* writeCocoJsonVariant */ 1, /* writeImages */ "", /* writeImagesFormat */ "png",
                /* writeVideo */ "", /* writeVideoFps */ -1.0, /* writeVideoWithAudio */ false,
                /* writeHeatMaps */ "", /* writeHeatMapsFormat */ "png", /* writeVideo3D */ "",
                /* writeVideoAdam */ "", /* writeBvh */ "", /* udpHost */ "", /* udpPort */ "8051");

            OPWrapper.OPConfigureGui(
                /* displayMode */ DisplayMode.NoDisplay, /* guiVerbose */ false, /* fullScreen */ false);
            
            OPWrapper.OPConfigureDebugging(
                /* loggingLevel */ Priority.High, /* disableMultiThread */ false, /* profileSpeed */ 1000);
        }

        private IEnumerator UserRebootOpenPoseCoroutine() {
            if (OPWrapper.state == OPState.None) yield break;
            // Shutdown if running
            if (OPWrapper.state == OPState.Running) {
                OPWrapper.OPShutdown();
                // Reset framerate calculator
                frameTimeQueue.Clear();
                frameCounter = 0;
            }
            // Wait until fully stopped
            yield return new WaitUntil( ()=>{ return OPWrapper.state == OPState.Ready; } );
            // Configure and start
            UserConfigureOpenPose();
            OPWrapper.OPRun();
        }

        private void Update() {
            // Update state in UI
            stateText.text = OPWrapper.state.ToString();

            // Try getting new frame
            if (OPWrapper.OPGetOutput(out datum)){ // true: has new frame data

                // Update background image
                bgImageRenderer.UpdateImage(datum.cvInputData);

                // Rescale output UI
                Vector2 outputSize = outputTransform.sizeDelta;
                Vector2 screenSize = Camera.main.pixelRect.size;
                float scale = Mathf.Min(screenSize.x / outputSize.x, screenSize.y / outputSize.y);
                outputTransform.localScale = new Vector3(scale, scale, scale);

                // Update number of people in UI
                if (datum.poseKeypoints == null || datum.poseKeypoints.Empty()) numberPeople = 0;
                else numberPeople = datum.poseKeypoints.GetSize(0);
                peopleText.text = "People: " + numberPeople;

                // Draw human
                while (humanContainer.childCount < numberPeople) { // Make sure no. of HumanControllers no less than numberPeople
                    Instantiate(humanPrefab, humanContainer);
                }
                int i = 0;
                int spotlight_human_index = -1;
                //float temp_highest_point = 999;
                float most_score = 0;
                
                foreach (var human in humanContainer.GetComponentsInChildren<HumanController2D>()) {
                    // 2 ways
                    // 1. motion score
                    // 2. size score
                    //}
                    float motion_score = 0;
                    float size_score = 0;
                    bool if_datum_1_exist = false;
                    if (frameCounter >= 2){
                        //Motion score
                        if(datum.poseKeypoints.GetSize(1) > 1 && datum.poseKeypoints != null && datum_1.poseKeypoints != null && !datum.poseKeypoints.Empty() && !datum_1.poseKeypoints.Empty()){
                            if_datum_1_exist = true;
                            Vector2 neck_current = new Vector2(datum.poseKeypoints.Get(i, 1, 0), datum.poseKeypoints.Get(i, 1, 1));
                            float diff_min = 999;
                            float diff_temp = 999;
                            int formerID = -1;
                            Vector2 motion_vector_temp = new Vector2(0, 0);

                            //first, a simple reID process
                            for (int index = 0; index < num_of_people_1;index++){
                                if (datum_1.poseKeypoints == null || index >= datum_1.poseKeypoints.GetSize(0)) {
                                    if_datum_1_exist = false;
                                    break;
                                    }
                                Vector2 neck_4 = new Vector2(datum_1.poseKeypoints.Get(index, 1, 0),datum_1.poseKeypoints.Get(index, 1, 1));
                                diff_temp = (neck_current - neck_4)[0]*(neck_current - neck_4)[0]  +  (neck_current - neck_4)[1]*(neck_current - neck_4)[1];             
                                if(diff_temp < diff_min){
                                    diff_min = diff_temp;
                                    formerID = index;
                                    motion_vector_temp[0] = (neck_current - neck_4)[0];
                                    motion_vector_temp[1] = (neck_current - neck_4)[1];
                                }
                            }
                            // calculate the absolute motions
                            if (formerID != -1) {
                                motion_score = human.CalMotionScore(ref datum, ref datum_1, i, formerID, motion_vector_temp);
                            }
                            
                        }           
                        else{
                            motion_score = 0;
                        }             
                        
                    }

                    // size score
                    if (datum.poseKeypoints != null){
                        size_score = human.CalSizeScore(ref datum, i);
                        size_score = size_score * size_score * alpha; // regularization
                    }
                    else{
                        size_score = 0;
                    }
                    
                    
                    if (((motion_score + size_score) > most_score) && if_datum_1_exist){
                            most_score = motion_score + size_score;
                            spotlight_human_index = i;
                        }
                    i = i + 1;
                    
                }
                if (frameCounter>=1) num_of_people_1 = i;
                
                /*
                // get the spotlight_human_index
                foreach (var human in humanContainer.GetComponentsInChildren<HumanController2D>()) {
                    // When i >= no. of human, the human will be hidden
                    human.DrawHuman(ref datum, i++, renderThreshold);
                    
                    if (human.HighestPoint < temp_highest_point) {
                    temp_highest_point =  human.HighestPoint;
                    spotlight_human_index = i - 1;
                    }
                }*/

                // try to calculate the depth of the point
                float torso_length = 0;
                float reference_torso_length_scaled = reference_torso_length;
                reference_torso_length_scaled = reference_torso_length * scale;
                if (frameCounter >= 1){ // Already have five frames stored
                    if (spotlight_human_index != -1 && datum.poseKeypoints != null) {
                        if(datum.poseKeypoints.GetSize(1) > 8){
                            Vector2 neck = new Vector2(datum.poseKeypoints.Get(spotlight_human_index, 1, 0),datum.poseKeypoints.Get(spotlight_human_index, 1, 1));
                            Vector2 bottom = new Vector2(datum.poseKeypoints.Get(spotlight_human_index, 8, 0),datum.poseKeypoints.Get(spotlight_human_index, 8, 1));
                            torso_length = bottom[1] - neck[1]; //assuming a vertical torso
                            if (torso_length != 0){
                                abs_z = reference_torso_length_scaled / torso_length; // unit: m
                            }
                            else{
                                PointText.text = "Point:" + "unReferenced" + "," + "unReferenced" + "," + "unReferenced";
                            }
                            

                            float f = reference_torso_length_scaled * real_torso_length;// f
                            Vector2 c = new Vector2(screenSize.x/2, screenSize.y/2); // (c0,c1)
                            Vector2 neck_normalized = neck - c;
                            abs_x = abs_z * neck_normalized[0] / f;
                            abs_y = abs_z * neck_normalized[1] / f;
                            PointText.text = "Point:" + abs_x + "," + abs_y + "," + abs_z;
                        }
                        else{
                            PointText.text = "Point:" + "nothing" + "," + "nothing" + "," + "nothing";
                        }
                        
                        
                    }
                }

                // update and store the whole scene
                
                datum_1 = datum;

                // Update framerate in UI
                frameTimeQueue.Enqueue(Time.time);
                frameCounter++;
                if (frameTimeQueue.Count > queueMaxCount){ // overflow
                    frameTimeQueue.Dequeue();
                }
                if (frameCounter >= queueMaxCount || frameTimeQueue.Count <= 5){ // update frame rate
                    frameCounter = 0;
                    avgFrameRate = frameTimeQueue.Count / (Time.time - frameTimeQueue.Peek());
                    fpsText.text = avgFrameRate.ToString("F1") + " FPS";
                }
            }
        }
    }
}
