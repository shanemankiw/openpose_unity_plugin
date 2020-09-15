// OpenPose Unity Plugin v1.0.0alpha-1.5.0
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenPose.Example {
    /*
     * HumanController2D translate the output data into 2D transforms
     * The Joints child gameObject contains all keypoints info
     * Transform (x, y): the x and y coordinates on the frame
     * Active: whether the score of that keypoint is larger than ScoreThres
     */
    public class HumanController2D : MonoBehaviour {

        public int PoseKeypointsCount = 25;
        public int HandKeypointsCount = 21;
        public int FaceKeypointsCount = 70;
        public float HighestPoint = 999;
        public float Biggest_Motion = -1;
        public bool ifHighest = false;

        [SerializeField] RectTransform PoseParent;
        [SerializeField] RectTransform LHandParent;
        [SerializeField] RectTransform RHandParent;
        [SerializeField] RectTransform FaceParent;
        [SerializeField] RectTransform LHandRectangle;
        [SerializeField] RectTransform RHandRectangle;
        [SerializeField] RectTransform FaceRectangle;
        private List<RectTransform> poseJoints = new List<RectTransform>();
        private List<RectTransform> lHandJoints = new List<RectTransform>();
        private List<RectTransform> rHandJoints = new List<RectTransform>();
        private List<RectTransform> faceJoints = new List<RectTransform>();

        public void DrawHuman(ref OPDatum datum, int bodyIndex, float scoreThres = 0){
            DrawBody(ref datum, bodyIndex, scoreThres);
            DrawHand(ref datum, bodyIndex, scoreThres);
            DrawFace(ref datum, bodyIndex, scoreThres);
            DrawRectangles(ref datum, bodyIndex);
        }

        public float CalMotionScore(ref OPDatum datum, ref OPDatum datum_old, int bodyIndex, int formerbodyindex,Vector2 motion_vector_temp){
            
            // for your protection
            if (datum.poseKeypoints == null || bodyIndex >= datum.poseKeypoints.GetSize(0) || datum_old.poseKeypoints == null || formerbodyindex >= datum_old.poseKeypoints.GetSize(0)) {
                PoseParent.gameObject.SetActive(false);
                return 0;
            }
            // Pose
            float temp = 0;
            int i = 0;
            for (int part = 0; part < poseJoints.Count; part++) {
                // Joints overflow
                if (part >= datum.poseKeypoints.GetSize(1) || part >= datum_old.poseKeypoints.GetSize(1)) {
                    poseJoints[part].gameObject.SetActive(false);
                    continue;
                }
                // Compare score
                if (datum.poseKeypoints.Get(bodyIndex, part, 2) > 0.05 && datum_old.poseKeypoints.Get(formerbodyindex, part, 2) > 0.05) { // bug spot
                    //poseJoints[part].gameObject.SetActive(true);
                    Vector2 motion = new Vector2(datum.poseKeypoints.Get(bodyIndex, part, 0) - datum_old.poseKeypoints.Get(formerbodyindex, part, 0) - motion_vector_temp[0],
                     datum.poseKeypoints.Get(bodyIndex, part, 1) - datum_old.poseKeypoints.Get(formerbodyindex, part, 1)-motion_vector_temp[1]);
                    
                    // Get the highest point in Y axis
                    i = i + 1;
                    temp = temp + motion[0] * motion[0] + motion[1] * motion[1];
                    //poseJoints[part].localPosition = pos;
                }
            }
            if (i != 0) {return temp/(i*(datum.poseKeypoints.Get(bodyIndex, 8, 2) - datum.poseKeypoints.Get(bodyIndex, 1, 2))*(datum.poseKeypoints.Get(bodyIndex, 8, 2) - datum.poseKeypoints.Get(bodyIndex, 1, 2)));} // try using i or not
            else return 0;
        }

        public float CalSizeScore(ref OPDatum datum, int bodyIndex){
            
            // for your protection
            if (datum.poseKeypoints == null || bodyIndex >= datum.poseKeypoints.GetSize(0)) {
                PoseParent.gameObject.SetActive(false);
                return 0;
            }
            // Pose
            float lowest_point = -1;
            float highest_point = 999;
            for (int part = 0; part < poseJoints.Count; part++) {
                // Joints overflow
                if (part >= datum.poseKeypoints.GetSize(1)) {
                    poseJoints[part].gameObject.SetActive(false);
                    continue;
                }
                // 
                if (datum.poseKeypoints.Get(bodyIndex, part, 2) > 0.05) {
                    //poseJoints[part].gameObject.SetActive(true);
                    if (datum.poseKeypoints.Get(bodyIndex, part, 1) > lowest_point){
                        lowest_point = datum.poseKeypoints.Get(bodyIndex, part, 1);
                    }

                    if (datum.poseKeypoints.Get(bodyIndex, part, 1) < highest_point){
                        highest_point = datum.poseKeypoints.Get(bodyIndex, part, 1);
                    }
                    
                }
            }
            return Mathf.Max(0, lowest_point - highest_point);
            
        }


        private void DrawBody(ref OPDatum datum, int bodyIndex, float scoreThres){
            if (datum.poseKeypoints == null || bodyIndex >= datum.poseKeypoints.GetSize(0)) {
                PoseParent.gameObject.SetActive(false);
                return;
            } else {
                PoseParent.gameObject.SetActive(true);
            }
            // Pose
            for (int part = 0; part < poseJoints.Count; part++) {
                // Joints overflow
                if (part >= datum.poseKeypoints.GetSize(1)) {
                    poseJoints[part].gameObject.SetActive(false);
                    continue;
                }
                // Compare score
                if (datum.poseKeypoints.Get(bodyIndex, part, 2) <= scoreThres) {
                    poseJoints[part].gameObject.SetActive(false);
                } else {
                    poseJoints[part].gameObject.SetActive(true);
                    Vector3 pos = new Vector3(datum.poseKeypoints.Get(bodyIndex, part, 0), datum.poseKeypoints.Get(bodyIndex, part, 1), 0f);
                    
                    // Get the highest point in Y axis
                    float temp = pos[1];
                    if (temp < HighestPoint) HighestPoint = temp;
                    poseJoints[part].localPosition = pos;
                }
            }
        }

        private void DrawHand(ref OPDatum datum, int bodyIndex, float scoreThres) {
            // Left
            if (datum.handKeypoints == null || bodyIndex >= datum.handKeypoints.left.GetSize(0)){
                LHandParent.gameObject.SetActive(false);
            } else {
                LHandParent.gameObject.SetActive(true);
                for (int part = 0; part < lHandJoints.Count; part++) {
                    // Joints overflow
                    if (part >= datum.handKeypoints.left.GetSize(1)) {
                        lHandJoints[part].gameObject.SetActive(false);
                        continue;
                    }
                    // Compare score
                    if (datum.handKeypoints.left.Get(bodyIndex, part, 2) <= scoreThres) {
                        lHandJoints[part].gameObject.SetActive(false);
                    } else {
                        lHandJoints[part].gameObject.SetActive(true);
                        Vector3 pos = new Vector3(datum.handKeypoints.left.Get(bodyIndex, part, 0), datum.handKeypoints.left.Get(bodyIndex, part, 1), 0f);
                        lHandJoints[part].localPosition = pos;
                    }
                }
            }
            // Right
            if (datum.handKeypoints == null || bodyIndex >= datum.handKeypoints.right.GetSize(0)){
                RHandParent.gameObject.SetActive(false);
            } else {
                RHandParent.gameObject.SetActive(true);
                for (int part = 0; part < rHandJoints.Count; part++) {
                    // Joints overflow
                    if (part >= datum.handKeypoints.right.GetSize(1)) {
                        rHandJoints[part].gameObject.SetActive(false);
                        continue;
                    }
                    // Compare score
                    if (datum.handKeypoints.right.Get(bodyIndex, part, 2) <= scoreThres) {
                        rHandJoints[part].gameObject.SetActive(false);
                    } else {
                        rHandJoints[part].gameObject.SetActive(true);
                        Vector3 pos = new Vector3(datum.handKeypoints.right.Get(bodyIndex, part, 0), datum.handKeypoints.right.Get(bodyIndex, part, 1), 0f);
                        rHandJoints[part].localPosition = pos;
                    }
                }
            }
        }

        private void DrawFace(ref OPDatum datum, int bodyIndex, float scoreThres){
            // Face
            if (datum.faceKeypoints == null || bodyIndex >= datum.faceKeypoints.GetSize(0)) {
                FaceParent.gameObject.SetActive(false);
                return;
            } else {
                FaceParent.gameObject.SetActive(true);

                for (int part = 0; part < faceJoints.Count; part++) {
                    // Joints overflow
                    if (part >= datum.faceKeypoints.GetSize(1)) {
                        faceJoints[part].gameObject.SetActive(false);
                        continue;
                    }
                    // Compare score
                    if (datum.faceKeypoints.Get(bodyIndex, part, 2) <= scoreThres) {
                        faceJoints[part].gameObject.SetActive(false);
                    } else {
                        faceJoints[part].gameObject.SetActive(true);
                        Vector3 pos = new Vector3(datum.faceKeypoints.Get(bodyIndex, part, 0), datum.faceKeypoints.Get(bodyIndex, part, 1), 0f);
                        faceJoints[part].localPosition = pos;
                    }
                }
            }
        }

        private void DrawRectangles(ref OPDatum datum, int bodyIndex){
            // Hand rect
            if (datum.handRectangles == null || bodyIndex >= datum.handRectangles.Count){
                LHandRectangle.gameObject.SetActive(false);
                RHandRectangle.gameObject.SetActive(false);
            } else {
                var rects = datum.handRectangles[bodyIndex];
                // Left
                LHandRectangle.gameObject.SetActive(true);
                LHandRectangle.localPosition = rects.left.center;
                LHandRectangle.sizeDelta = rects.left.size;
                // Right
                RHandRectangle.gameObject.SetActive(true);
                RHandRectangle.localPosition = rects.right.center;
                RHandRectangle.sizeDelta = rects.right.size;
            }

            // Face rect
            if (datum.faceRectangles == null || bodyIndex >= datum.faceRectangles.Count){
                FaceRectangle.gameObject.SetActive(false);
            } else {
                FaceRectangle.gameObject.SetActive(true);
                FaceRectangle.localPosition = datum.faceRectangles[bodyIndex].center;
                FaceRectangle.sizeDelta = datum.faceRectangles[bodyIndex].size;
            }
        }

        /*
        private void MotionScore(ref OPDatum datum, ref OPDatum datum_before, int bodyIndex, float scoreThres){
            if (datum.poseKeypoints == null || bodyIndex >= datum.poseKeypoints.GetSize(0)) {
                PoseParent.gameObject.SetActive(false);
                return;
            } else {
                PoseParent.gameObject.SetActive(true);
            }
            // Pose
            for (int part = 0; part < poseJoints.Count; part++) {
                // Joints overflow
                foreach (var human in humanContainer.GetComponentsInChildren<HumanController2D>()) {
                    // When i >= no. of human, the human will be hidden
                    human.DrawHuman(ref datum, i++, renderThreshold);
                    
                    if (human.HighestPoint < temp_highest_point) {
                    temp_highest_point =  human.HighestPoint;
                    spotlight_human_index = i - 1;
                    }
                }
            }
        }
        */
        // Use this for initialization
        void Start() {
            InitJoints();
        }

        private void InitJoints() {
            // Pose
            if (PoseParent) {
                Debug.Assert(PoseParent.childCount == PoseKeypointsCount, "Pose joint count not match");
                for (int i = 0; i < PoseKeypointsCount; i++) {
                    poseJoints.Add(PoseParent.GetChild(i) as RectTransform);
                }
            }
            // LHand
            if (LHandParent) {
                Debug.Assert(LHandParent.childCount == HandKeypointsCount, "LHand joint count not match");
                //LHandRectangle = LHandParent.GetChild(0) as RectTransform;
                for (int i = 0; i < HandKeypointsCount; i++) {
                    lHandJoints.Add(LHandParent.GetChild(i) as RectTransform);
                }
            }
            // RHand
            if (RHandParent) {
                Debug.Assert(RHandParent.childCount == HandKeypointsCount, "RHand joint count not match");
                //RHandRectangle = RHandParent.GetChild(0) as RectTransform;
                for (int i = 0; i < HandKeypointsCount; i++) {
                    rHandJoints.Add(RHandParent.GetChild(i) as RectTransform);
                }
            }
            // Face
            if (FaceParent){
                Debug.Assert(FaceParent.childCount == FaceKeypointsCount, "Face joint count not match");
                //FaceRectangle = FaceParent.GetChild(0) as RectTransform;
                for (int i = 0; i < FaceKeypointsCount; i++){
                    faceJoints.Add(FaceParent.GetChild(i) as RectTransform);
                }
            }
        }
    }
}

