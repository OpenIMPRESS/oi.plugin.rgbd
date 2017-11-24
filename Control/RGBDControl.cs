using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using oi.core.network;

namespace oi.plugin.rgbd {

    public enum RGBD_STATE {
        IDLE,
        LIVE,
        REPLAYING
    }


    public class RGBDStreamEventArgs : System.EventArgs {
        public RGBDStreamEventArgs(string s) {
            msg = s;
        }
        private string msg;
        public string Message {
            get { return msg; }
        }
    }

    [RequireComponent(typeof(StreamFrameSource))]
    [RequireComponent(typeof(UDPConnector))]
	public class RGBDControl : MonoBehaviour {
        private Transform _recordedTransform;
        private Transform _defaultTransform;
        StreamFrameSource sfs;
        UDPConnector oiudp;
        private RGBD_STATE _state;
        private RGBDAudio _audio;

        int n_bookmarks = 5;
        ulong[] bookmarks;

       //bool useRelativeTimes = true;
        

        private ulong _replay_t0;

        private DepthCameraExtrinsics dce;

        public Camera cameraKinectView;
        public GameObject bodyPrefab;

        private Dictionary<uint, RGBDBody> _trackedBodies;
        private Queue<RGBDBodyFrame> _bodyFrames;
        private readonly object _bodyFramesLock = new object();
        public float last_idle = 0;

        public delegate void RGBDStreamEventHandler(object sender, RGBDStreamEventArgs ev);
        public event RGBDStreamEventHandler RGBDStreamEvent;
        private Queue<RGBDStreamEventArgs> _eventQueue;
        private readonly object _eventQueueLock = new object();

        private RGBDBodyFrame DequeueBodyFrame() {
            RGBDBodyFrame res = null;
            lock (_bodyFramesLock) {
                if (_bodyFrames.Count > 0) {
                    res = _bodyFrames.Dequeue();
                }
            }
            return res;
        }

        public void ClearBodies() {
            lock (_bodyFramesLock) {
                while (_bodyFrames.Count > 0) {
                    _bodyFrames.Dequeue();
                }
            }

            foreach (KeyValuePair<uint,RGBDBody> kvp in _trackedBodies){
                kvp.Value.DestroyNextFrame();
            }

            _trackedBodies.Clear();
        }

        public void QueueBodyFrame(RGBDBodyFrame frame) {
            lock (_bodyFramesLock) {
                _bodyFrames.Enqueue(frame);
            }
        }

        void Start () {
            bookmarks = new ulong[n_bookmarks];
            _bodyFrames = new Queue<RGBDBodyFrame>();
            _trackedBodies = new Dictionary<uint, RGBDBody>();
            _eventQueue = new Queue<RGBDStreamEventArgs>();

            _state = RGBD_STATE.IDLE;
        	oiudp = GetComponent<UDPConnector>();
            sfs = GetComponent<StreamFrameSource>();
            _audio = GetComponent<RGBDAudio>();
            _defaultTransform = sfs.cameraTransform;
            _recordedTransform = (new GameObject("_kinect_recorded")).transform;
            _recordedTransform.parent = transform;
            //cameraKinectView.transform.parent = _recordedTransform;
            //cameraKinectView.transform.localPosition = Vector3.zero;
            //cameraKinectView.transform.localRotation = Quaternion.identity;
            last_idle = Time.time + 1.0f;
        }


        private void QueueEvent(RGBDStreamEventArgs ev) {
            lock (_bodyFramesLock) {
                _eventQueue.Enqueue(ev);
            }
        }

        private RGBDStreamEventArgs PollEventQueue() {
            RGBDStreamEventArgs res = null;
            lock (_eventQueueLock) {
                if (_eventQueue.Count > 0) {
                    res = _eventQueue.Dequeue();
                }
            }
            return res;
        }


        // Update is called once per frame
        void Update () {
            _recordedTransform.position = dce.position;
            _recordedTransform.rotation = dce.rotation;

            RGBDStreamEventArgs evArgs = PollEventQueue();
            if (evArgs != null) {
                RGBDStreamEvent(this, evArgs);
            }

            if (_state == RGBD_STATE.IDLE && last_idle + 2.0f < Time.time) {
                RequestConfig();
                last_idle = Time.time;
            }

            RGBDBodyFrame f = DequeueBodyFrame();
            while (f != null) {
                if (!_trackedBodies.ContainsKey(f.trackingID) || _trackedBodies[f.trackingID] == null) {
                    GameObject newBody = Instantiate(bodyPrefab, sfs.cameraTransform);
                    newBody.transform.localPosition = Vector3.zero;
                    newBody.transform.localRotation = Quaternion.identity;
                    _trackedBodies.Add(f.trackingID, newBody.GetComponent<RGBDBody>());
                }

                _trackedBodies[f.trackingID].ApplyFrame(f);
                f = DequeueBodyFrame();
            }

        }

        // CALLED FROM DIFFERENT THREAD(?)
        public void UpdateState(ConfigMessage cm) {
            ClearBodies();
            RGBD_STATE new_state = cm.live ? RGBD_STATE.LIVE : RGBD_STATE.REPLAYING;
            if (_state != RGBD_STATE.REPLAYING && new_state == RGBD_STATE.REPLAYING) {
                QueueEvent(new RGBDStreamEventArgs("REPLAY_STARTED"));
            }

            if (_state != RGBD_STATE.LIVE && new_state == RGBD_STATE.LIVE) {
                QueueEvent(new RGBDStreamEventArgs("REPLAY_STOPPED"));
                if (_audio != null) _audio.Clear();
            }

            if (new_state == RGBD_STATE.LIVE) {
                Debug.Log("Live");
                sfs.cameraTransform = _defaultTransform;
                _replay_t0 = 0;
            }

            if (new_state == RGBD_STATE.REPLAYING) {
                Debug.Log("Replaying");
                _replay_t0 = NOW();
                if (_audio != null) _audio.Clear();
                sfs.cameraTransform = _recordedTransform;
                dce = cm.extrinsics;
            }

            _state = new_state;
        }

        public static ulong NOW() {
            System.DateTime dateTime = System.DateTime.Now;
             System.DateTime unixStart = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            ulong unixTimeStampInTicks = (ulong) (dateTime.ToUniversalTime() - unixStart).Ticks;
            return unixTimeStampInTicks / System.TimeSpan.TicksPerMillisecond;
        }


        public void SendExtrinsics() {
            RGBDExtrinsics msg = new RGBDExtrinsics();
            msg.val = "update";
            msg.x = _defaultTransform.position.x;
            msg.y = _defaultTransform.position.y;
            msg.z = _defaultTransform.position.z;
            msg.qx = _defaultTransform.rotation.x;
            msg.qy = _defaultTransform.rotation.y;
            msg.qz = _defaultTransform.rotation.z;
            msg.qw = _defaultTransform.rotation.w;
            SendMsg(msg);
        }

        public void RequestConfig() {
            RGBDControlApp msg = new RGBDControlApp();
            msg.val = "requestconfig";
            SendMsg(msg);
        }

        public void StartRecording(ulong t, string name) {
            SendExtrinsics();
            RGBDControlRecord msg = new RGBDControlRecord();
            msg.val = "startrec";
            msg.file = name;
            msg.time = t;
            SendMsg(msg);
        }

        public void StopRecording(ulong t) {
            RGBDControlRecord msg = new RGBDControlRecord();
            msg.val = "stoprec";
            msg.time = t;
            SendMsg(msg);
        }

        public void StopPlay() {
            RGBDControlRecord msg = new RGBDControlRecord();
            msg.val = "stopplay";
            SendMsg(msg);
        }

        public void EnableDevice() {
            RGBDControlApp msg = new RGBDControlApp();
            msg.val = "enable_device";
            SendMsg(msg);
        }

        public void PlaySlice(string file, ulong tStart, ulong tEnd) {
            RGBDControlRecord msg = new RGBDControlRecord();
            msg.val = "startplay";
            msg.file = file;
            msg.slice = true;
            msg.loop = false;
            msg.time = NOW();
            msg.sliceStart = tStart;
            msg.sliceEnd = tEnd;
            SendMsg(msg);
        }

        public void DisableDevice() {
            RGBDControlApp msg = new RGBDControlApp();
            msg.val = "disable_device";
            SendMsg(msg);
        }

        private void SendMsg(object msg) {
            string json = JsonUtility.ToJson(msg);
            byte[] sendBytes = System.Text.Encoding.ASCII.GetBytes(json);
            oiudp.SendData(sendBytes);
        }

        private void OnGUI() {
            /*
            int xposA = 300;
            int width = 120;
            int xposB = xposA+width+10;
            int yposStart = 10;
            int height = 25;
            int width2 = 60;
            int yposInterval = 32;

            for (int i = 0; i < n_bookmarks; i++) {
                string disp = bookmarks[0].ToString();
                ulong diff = 0;
                if (i>0) {
                    if (bookmarks[i] <= bookmarks[0]) {
                        disp = "ERR";
                    } else {
                        diff = bookmarks[i] - bookmarks[0];
                        disp = "+" + diff;
                    }
                }
                if (GUI.Button(new Rect(xposA, yposStart+yposInterval*i, width, height), disp)) {
                    if (useRelativeTimes) { 
                        bookmarks[i] = NOW() - _replay_t0;
                    } else {
                        bookmarks[i] = NOW();
                    }
                }
            }

            for (int i = 0; i < n_bookmarks-1; i++) {
                if (GUI.Button(new Rect(xposB, (yposStart+yposInterval/2) + yposInterval * i, width2, height), "PLAY")) {
                    RGBDControlRecord msg = new RGBDControlRecord();
                    msg.val = "startplay";
                    msg.slice = true;
                    msg.loop = false;
                    msg.time = NOW(); // for multiple, synch up
                    msg.sliceStart = bookmarks[i];
                    msg.sliceEnd = bookmarks[i+1];
                    SendMsg(msg);
                }
            }




            if (GUI.Button(new Rect(10, 10, 110, 20), "KILL")) {
                RGBDControlApp msg = new RGBDControlApp();
                msg.val = "stop";
                SendMsg(msg);
            }

            if (GUI.Button(new Rect(10, 40, 110, 20), "START REC")) {
                SendExtrinsics();

                RGBDControlRecord msg = new RGBDControlRecord();
                msg.val = "startrec";

                System.TimeSpan span = (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc));
                msg.time = (NOW() + 1000);
                SendMsg(msg);
            }

            if (GUI.Button(new Rect(10, 70, 110, 20), "STOP REC")) {
                RGBDControlRecord msg = new RGBDControlRecord();
                msg.val = "stoprec";
                msg.time = (NOW() + 1000);
                SendMsg(msg);
            }

            if (GUI.Button(new Rect(10, 100, 110, 20), "PLAY A")) {
                RGBDControlRecord msg = new RGBDControlRecord();
                msg.val = "startplay";
                msg.file = "default";
                SendMsg(msg);
            }


            if (GUI.Button(new Rect(10, 130, 110, 20), "STOP PLAY")) {
                RGBDControlRecord msg = new RGBDControlRecord();
                msg.val = "stopplay";
                SendMsg(msg);
            }


            if (GUI.Button(new Rect(10, 160, 110, 20), "UPDATE EXTRINSICS")) {
                SendExtrinsics();
            }



            if (GUI.Button(new Rect(10, 220, 110, 20), "Turn Dev ON")) {
                RGBDControlApp msg = new RGBDControlApp();
                msg.val = "enable_device";
                SendMsg(msg);
            }

            if (GUI.Button(new Rect(10, 190, 110, 20), "Turn Dev OFF")) {
                RGBDControlApp msg = new RGBDControlApp();
                msg.val = "disable_device";
                SendMsg(msg);
            }*/
        }
    }

    [System.Serializable]
	public class RGBDControlApp {
		public string cmd = "application";
		public string val;
    }

    [System.Serializable]
    public class RGBDControlRecord {
        public string cmd = "record";
        public string val; // startrec, stoprec, startplay, stopplay
        public bool loop = true; // if startplay: should it loop
        public int maxframes = -1; // maximum frames to record;
        public string file = "default"; // file name
        public ulong time;
        public bool slice = false;
        public ulong sliceStart;
        public ulong sliceEnd;
    }

    [System.Serializable]
    public class RGBDExtrinsics {
        public string cmd = "extrinsics";
        public string val; // update
        public float x;
        public float y;
        public float z;
        public float qx;
        public float qy;
        public float qz;
        public float qw;
    }
}