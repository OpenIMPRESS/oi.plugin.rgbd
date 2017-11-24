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