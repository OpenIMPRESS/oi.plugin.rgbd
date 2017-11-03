using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using oi.core.network;

namespace oi.plugin.rgbd {

	[RequireComponent(typeof(UDPConnector))]
	public class RGBDControl : MonoBehaviour {

		public bool sendHelloWorld = false;

		UDPConnector oiudp;
		void Start () {
        	oiudp = GetComponent<UDPConnector>();

		}
		
		// Update is called once per frame
		void Update () {
			if (sendHelloWorld) {
				RGBDControlMsg msg = new RGBDControlMsg();
				msg.param = "record";
				msg.value = "start";
				string json = JsonUtility.ToJson(msg);
				byte[] sendBytes = System.Text.Encoding.ASCII.GetBytes(json);
				oiudp.SendData(sendBytes);
				sendHelloWorld = false;
			}
		}
	}

	[System.Serializable]
	public class RGBDControlMsg {
		public string param;
		public string value;
	}

}