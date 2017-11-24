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
            listener = new StreamParser(udpClient, GetComponent<RGBDAudio>(), GetComponent<RGBDControl>(),  this);
        }

        void OnApplicationQuit() {
            listener.Close();
        }

        void Update() {
            if (false && originCOS) { 
                cameraPosition = cameraTransform.position;
                cameraRotation = cameraTransform.rotation;
            } else {
                cameraPosition = originCOS.InverseTransformPoint(cameraTransform.position);
                cameraRotation = Quaternion.Inverse(originCOS.rotation) * cameraTransform.rotation;
                //cameraPosition = cameraTransform.TransformPoint()
            }
        }

    }

}