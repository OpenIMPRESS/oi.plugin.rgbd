using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace oi.plugin.rgbd {

    [ExecuteInEditMode]
    public class RGBDBody : MonoBehaviour {
        public Transform[] Rig = new Transform[(int) JointType.Count];
        public Color DebugColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        RGBDBodyFrame currentFrame;

        private float cosScale = 0.05f;
        private bool _destroy = false;

        private float lastFrameUpdate = 0.0f;

        // Use this for initialization
        void Start() {

        }

        void Update() {
            if (_destroy) {
                Destroy(this);
            }

            DrawToChildren(Rig[0]);
            DrawLocalCOS(transform);

            if (lastFrameUpdate+3.0f < Time.time) {
                _destroy = true;
            }
        }

        void DrawToChildren(Transform t) {
            if (t == null) return;
            foreach (Transform child in t) {
                Debug.DrawLine(t.position, child.position, DebugColor);
                DrawToChildren(child);
                DrawLocalCOS(child);
            }
        }

        void DrawLocalCOS(Transform t) {
            Debug.DrawLine(t.position, t.position + t.right * cosScale, Color.red);
            Debug.DrawLine(t.position, t.position + t.up * cosScale, Color.green);
            Debug.DrawLine(t.position, t.position + t.forward * cosScale, Color.blue);
        }

        public void ApplyFrame(RGBDBodyFrame frame) {
            lastFrameUpdate = Time.time;
            for (int i = 0; i < (int) JointType.Count; i++) {
                if (frame.jointTrackingState[i] != TrackingState.NotTracked) { 
                    Rig[i].position = transform.TransformPoint(frame.jointPosition[i]);
                }
            }
            currentFrame = frame;
        }

        public void DestroyNextFrame() {
            _destroy = true;
        }
    }


    public class RGBDBodyFrame {
        public ulong timestamp;
        public uint trackingID;
        public Vector2 lean;
        public TrackingState leanTrackingState;
        public TrackingState[] jointTrackingState = new TrackingState[(int)JointType.Count];
        public Vector3[] jointPosition = new Vector3[(int)JointType.Count];
        public HandState handStateLeft;
        public HandState handStateRight;
    }

    public enum HandState {
        Unknown = 0,
        NotTracked = 1,
        Open = 2,
        Closed = 3,
        Lasso = 4
    };

    public enum TrackingState {
        NotTracked = 0,
        Inferred = 1,
        Tracked = 2
    };

    public enum JointType {
        SpineBase = 0,
        SpineMid = 1,
        Neck = 2,
        Head = 3,
        ShoulderLeft = 4,
        ElbowLeft = 5,
        WristLeft = 6,
        HandLeft = 7,
        ShoulderRight = 8,
        ElbowRight = 9,
        WristRight = 10,
        HandRight = 11,
        HipLeft = 12,
        KneeLeft = 13,
        AnkleLeft = 14,
        FootLeft = 15,
        HipRight = 16,
        KneeRight = 17,
        AnkleRight = 18,
        FootRight = 19,
        SpineShoulder = 20,
        HandTipLeft = 21,
        ThumbLeft = 22,
        HandTipRight = 23,
        ThumbRight = 24,
        Count = (ThumbRight + 1)
    };
}