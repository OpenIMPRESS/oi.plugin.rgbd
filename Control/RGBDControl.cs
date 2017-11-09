using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using oi.core.network;

namespace oi.plugin.rgbd {

	[RequireComponent(typeof(UDPConnector))]
	public class RGBDControl : MonoBehaviour {

		UDPConnector oiudp;
		void Start () {
        	oiudp = GetComponent<UDPConnector>();

		}
		
		// Update is called once per frame
		void Update () {
		}

        private void OnGUI() {
            if (GUI.Button(new Rect(10, 10, 110, 20), "KILL")) {
                RGBDControlApp msg = new RGBDControlApp();
                msg.val = "stop";
                SendMsg(msg);
            }

            if (GUI.Button(new Rect(10, 40, 110, 20), "START REC")) {
                RGBDControlRecord msg = new RGBDControlRecord();
                msg.val = "startrec";
                SendMsg(msg);
            }

            if (GUI.Button(new Rect(10, 70, 110, 20), "STOP REC")) {
                RGBDControlRecord msg = new RGBDControlRecord();
                msg.val = "stoprec";
                SendMsg(msg);
            }

            if (GUI.Button(new Rect(10, 100, 110, 20), "START PLAY")) {
                RGBDControlRecord msg = new RGBDControlRecord();
                msg.val = "startplay";
                SendMsg(msg);
            }


            if (GUI.Button(new Rect(10, 130, 110, 20), "STOP PLAY")) {
                RGBDControlRecord msg = new RGBDControlRecord();
                msg.val = "stopplay";
                SendMsg(msg);
            }
        }

        private void SendMsg(object msg) {
            string json = JsonUtility.ToJson(msg);
            byte[] sendBytes = System.Text.Encoding.ASCII.GetBytes(json);
            oiudp.SendData(sendBytes);
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
    }
}