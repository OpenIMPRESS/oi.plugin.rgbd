using UnityEngine;
using System.Collections;
using System.Threading;
using System;
using System.Collections.Generic;
using oi.core.network;

namespace oi.plugin.rgbd {

    [RequireComponent(typeof(UDPConnector))]
    public class StreamFrameSource : FrameSource {
        private StreamParser listener;
        private UDPConnector udpClient;

        [HideInInspector]
        public Vector3 cameraPosition;

        [HideInInspector]
        public Quaternion cameraRotation;

        private new void Start() {
            base.Start();
            udpClient = GetComponent<UDPConnector>();
            listener = new StreamParser(udpClient, this);
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