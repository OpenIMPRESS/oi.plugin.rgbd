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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace oi.plugin.rgbd {

    public abstract class Processor {
        public readonly DepthDeviceType DeviceType;
        public readonly ushort TotalWidth;
        public readonly ushort TotalHeight;
        public readonly ushort MaxLinesPerBlock;
        public readonly string DeviceGUID;
        public readonly DepthCameraIntrinsics CameraIntrinsics;

        public readonly StreamFrameSource FrameSource;

        protected Processor(StreamFrameSource fs, DepthDeviceType t, DepthCameraIntrinsics cameraIntrinsics,
            ushort w, ushort h, ushort ml, string guid) {
            DeviceType = t;
            TotalWidth = w;
            TotalHeight = h;
            MaxLinesPerBlock = ml;
            DeviceGUID = guid;
            CameraIntrinsics = cameraIntrinsics;
            FrameSource = fs;
        }

        // How do we make explicit that a DepthStreamingProcessor is (now) responsible for calling:
        //  frameSource.frameQueue.enqueue(...)

        public abstract void HandleDepthData(ushort startRow, ushort endRow,
            ulong timestamp, ref byte[] data, int dataOffset);

        public abstract void HandleColorData(ulong timestamp, ref byte[] data, int dataOffset);

        public abstract void HandleBodyIndexData(ulong timestampBI, ref byte[] receiveBytes, int rgbd_header_size);

        /*
        public abstract byte[] GetRawColorData();
        public abstract ushort[] GetRawDepthData();
        public abstract Color[] GetDepthData();
        */

        public abstract void Close();
    }

}