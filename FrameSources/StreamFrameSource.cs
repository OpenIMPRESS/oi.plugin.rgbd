/*
This file is part of the OpenIMPRESS project.

OpenIMPRESS is free software: you can redistribute it and/or modify
it under the terms of the Lesser GNU Lesser General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

OpenIMPRESS is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with OpenIMPRESS. If not, see <https://www.gnu.org/licenses/>.
*/

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