using UnityEngine;
using System.Collections;
using System.Threading;
using System;
using System.Collections.Generic;
using oi.core.network;

namespace HMIMR.DepthStreaming {

    [RequireComponent(typeof(UDPConnector))]
    public class DepthStreamingSource : FrameSource {
        private DepthStreamingListener listener;
        private UDPConnector udpClient;

        [HideInInspector]
        public Vector3 cameraPosition;

        [HideInInspector]
        public Quaternion cameraRotation;

        private new void Start() {
            base.Start();
            udpClient = GetComponent<UDPConnector>();
            listener = new DepthStreamingListener(udpClient,this);
        }

        void OnApplicationQuit() {
            listener.Close();
        }

        void Update() {
            cameraPosition = cameraTransform.position;
            cameraRotation = cameraTransform.rotation;
        }

    }

}