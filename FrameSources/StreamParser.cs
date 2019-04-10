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

using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using System;
using oi.core.network;
namespace oi.plugin.rgbd {

    // De-serializes incomming depth data and forwards to processor.
    public class StreamParser {

        const byte RGBD_DATA =  1 << 0;
        const byte AUDIO_DATA = 1 << 1;
        const byte LIVE_DATA =  1 << 2;
        const byte BODY_DATA =  1 << 3;
        const byte HD_DATA =    1 << 4;
        const byte BIDX_DATA =  1 << 5;

        public Processor processor;
        private RGBDAudio _audio;
        private RGBDControl _control;
        private bool _listening;
        private readonly Thread _listenThread;
        private int _rgbd_header_size = 8;
        private int _body_header_size = 16;
        private int _body_data_size = 344;
        private readonly StreamFrameSource _frameSource;
        UDPConnector udpClient;

        public StreamParser(UDPConnector _udpClient, RGBDAudio audio, RGBDControl control, StreamFrameSource fs) {
            _listenThread = new Thread(new ThreadStart(Listen));
            _frameSource = fs;
            udpClient = _udpClient;
            _listenThread.Start();
            _audio = audio;
            _control = control;
        }

        private void Listen() {
            _listening = true;
            
            while (_listening) {
                OIMSG msg_in = udpClient.GetNewData();
                if (msg_in == null || msg_in.data == null) continue;
                if (msg_in.data.Length < 2) continue;

                byte[] receiveBytes = msg_in.data;

                byte frameType = msg_in.msgType;
                byte deviceID = receiveBytes[0];

                switch (frameType) {
                    case (byte) FrameType.Config:
                        ConfigMessage cm = new ConfigMessage();
                        // Currently only one processor per port.
                        // We could support here:
                        //   - Changing configuration (based on throtteling, etc)
                        //   - Multiple devices on one port (routing DepthPackets to processor based on id)
                        //   - (...which would require some changes to how streaming source & render works)

                        // TODO: Parse config data
                        cm.deviceType = (DepthDeviceType) receiveBytes[1];
                        byte dataFlags = receiveBytes[2];

                        ushort frameWidth = BitConverter.ToUInt16(receiveBytes, 4);
                        ushort frameHeight = BitConverter.ToUInt16(receiveBytes, 6);
                        ushort maxLines = BitConverter.ToUInt16(receiveBytes, 8);

                        float cx = BitConverter.ToSingle(receiveBytes, 12);
                        float cy = BitConverter.ToSingle(receiveBytes, 16);
                        float fx = BitConverter.ToSingle(receiveBytes, 20);
                        float fy = BitConverter.ToSingle(receiveBytes, 24);
                        float depthScale = BitConverter.ToSingle(receiveBytes, 28);
                        cm.intrinsics = new DepthCameraIntrinsics(
                            cx, cy, fx, fy, depthScale, frameWidth, frameHeight);

                        float Px = BitConverter.ToSingle(receiveBytes, 32);
                        float Py = BitConverter.ToSingle(receiveBytes, 36);
                        float Pz = BitConverter.ToSingle(receiveBytes, 40);
                        float Qx = BitConverter.ToSingle(receiveBytes, 44);
                        float Qy = BitConverter.ToSingle(receiveBytes, 48);
                        float Qz = BitConverter.ToSingle(receiveBytes, 52);
                        float Qw = BitConverter.ToSingle(receiveBytes, 56);
                        cm.extrinsics = new DepthCameraExtrinsics(
                                Px, Py, Pz, Qx, Qy, Qz, Qw
                        );


                        int guid_offset = 63;
                        cm.GUID = "";
                        for (int sOffset = 0; sOffset < 32; sOffset++) {
                            byte c = receiveBytes[guid_offset + sOffset];
                            if (c == 0x00) break;
                            cm.GUID += (char) c;
                        }

                        //int filename_offset = 99;
                        cm.filename = "";
                        cm.live = (dataFlags & LIVE_DATA) != 0;
                        cm.hasAudio = (dataFlags & AUDIO_DATA) != 0;
                        cm.hasBody = (dataFlags & BODY_DATA) != 0;
                        cm.hasRGBD = (dataFlags & RGBD_DATA) != 0;

                        /*
                        if (!cm.live) {
                            for (int sOffset = 0; sOffset < 32; sOffset++) {
                                byte c = receiveBytes[filename_offset + sOffset];
                                if (c == 0x00) break;
                                cm.filename += (char)c;
                            }
                            Debug.Log("Replaying file: "+ cm.filename);
                        } */


                        Debug.Log("Config:\n\tFrame: " + frameWidth + " " + frameHeight + " " + maxLines +
                                  "\n\tIntrinsics: " + cx + " " + cy + " " + fx + " " + fy + " " + depthScale +
                                  "\n\tExtrinsics: " + cm.extrinsics.position.x + " " + cm.extrinsics.position.y + " " + cm.extrinsics.position.z + 
                                  " " + cm.extrinsics.rotation.x + " " + cm.extrinsics.rotation.y + " " + cm.extrinsics.rotation.z + " " + cm.extrinsics.rotation.w +
                                  "\n\tGUID: " + cm.GUID);

                        // We could also implement & choose a specific Processor 
                        // (i.e. with custom Proccess() function) based on DepthDeviceType...
                        if (processor == null) {
                            //processor = new DefaultDepthStreamingProcessor(
                            //processor = new VSyncProcessor(
                            processor = new FastProcessor(_frameSource, cm);
                        }

                        if (_control != null) _control.UpdateState(cm);

                        break;
                    case (byte) FrameType.DepthBlock:
                        if (processor == null) break;
                        ushort unused1 = BitConverter.ToUInt16(receiveBytes, 0);
                        ushort delta_t = BitConverter.ToUInt16(receiveBytes, 2);
                        ushort startRowD = BitConverter.ToUInt16(receiveBytes, 4);
                        ushort endRowD = BitConverter.ToUInt16(receiveBytes, 6);

                        //Debug.Log("Seq: "+sequence+" start: "+startRow+" end: "+endRow);
                        processor.HandleDepthData(startRowD, endRowD, msg_in.timestamp, ref receiveBytes, _rgbd_header_size);
                        break;
                    case (byte)FrameType.Color:
                        if (processor == null) break;
                        //ushort delta_t = BitConverter.ToUInt16(receiveBytes, 2);
                        //ushort startRowD = BitConverter.ToUInt16(receiveBytes, 4);
                        //ushort endRowD = BitConverter.ToUInt16(receiveBytes, 6);
                        //ulong timestampC = BitConverter.ToUInt32(receiveBytes, 8);
                        processor.HandleColorData(msg_in.timestamp, ref receiveBytes, _rgbd_header_size);
                        break;
                    case (byte)FrameType.BodyIndexBlock:
                        if (processor == null) break;
                        //ushort delta_t = BitConverter.ToUInt16(receiveBytes, 2);
                        //ushort startRowD = BitConverter.ToUInt16(receiveBytes, 4);
                        //ushort endRowD = BitConverter.ToUInt16(receiveBytes, 6);
                        ulong timestampBI = BitConverter.ToUInt32(receiveBytes, 8);
                        processor.HandleBodyIndexData(timestampBI, ref receiveBytes, _rgbd_header_size);
                        break;
                    case (byte)FrameType.AudioSamples:
                        if (processor == null) break;
                        RGBDAudioFrame aframe = new RGBDAudioFrame();
                        aframe.frequency = BitConverter.ToUInt16(receiveBytes, 2);
                        aframe.channels = BitConverter.ToUInt16(receiveBytes, 4);
                        ushort n_samples = BitConverter.ToUInt16(receiveBytes, 6);
                        aframe.timestamp = BitConverter.ToUInt64(receiveBytes, 8);
                        
                        aframe.samples = new float[n_samples];
                        for (int i = 0; i < n_samples; i++) {
                            aframe.samples[i] = BitConverter.ToSingle(receiveBytes, 12 +i*4);
                                // BitConverter.ToUInt16(receiveBytes, 12+i*2) / 32767.0f;
                        }

                        if (_audio != null) _audio.QueueBuffer(aframe);
                        break;
                    case (byte)FrameType.BodyData:
                        ushort nBodies = BitConverter.ToUInt16(receiveBytes, 2);
                        ulong timestampB = BitConverter.ToUInt64(receiveBytes, 8);
                        for (ushort i = 0; i < nBodies; i++) {
                            RGBDBodyFrame bodyFrame = new RGBDBodyFrame();
                            bodyFrame.timestamp = timestampB;

                            int dataOffset = _body_header_size + i * _body_data_size;

                            bodyFrame.trackingID = BitConverter.ToUInt32(receiveBytes, dataOffset + 0);
                            bodyFrame.handStateLeft = (HandState) receiveBytes[dataOffset + 4];
                            bodyFrame.handStateRight = (HandState) receiveBytes[dataOffset + 5];
                            bodyFrame.leanTrackingState = (TrackingState)receiveBytes[dataOffset + 7];
                            bodyFrame.lean = new Vector2(
                                BitConverter.ToSingle(receiveBytes, dataOffset + 8),
                                BitConverter.ToSingle(receiveBytes, dataOffset + 12));

                            int positionOffset = dataOffset + 16;
                            int positionDatSize = 3 * 4 * (int)JointType.Count;
                            int trackingStateOffset = 3 + positionOffset + positionDatSize;
                            for (int j = 0; j < (int) JointType.Count; j++) {
                                bodyFrame.jointPosition[j] = new Vector3(
                                    -BitConverter.ToSingle(receiveBytes, positionOffset + j * 3 * 4 + 0),
                                     BitConverter.ToSingle(receiveBytes, positionOffset + j * 3 * 4 + 4),
                                     BitConverter.ToSingle(receiveBytes, positionOffset + j * 3 * 4 + 8));
                                bodyFrame.jointTrackingState[j] = (TrackingState) receiveBytes[trackingStateOffset + j];
                            }

                            if (_control != null) _control.QueueBodyFrame(bodyFrame);
                        }
                        break;
                    default:
                        Debug.Log("Unknown DepthStreaming frame type: " + msg_in.msgType);
                        break;
                }
            }

            _listening = false;

            Debug.Log("Listen Thread Closed");
        }

        public void Close() {
            _listening = false;
            if (processor != null)
                processor.Close();
            if (_listenThread != null)
                _listenThread.Join(500);
        }
    }


    public enum FrameType {
        Config = 0x01,
        DepthBlock = 0x12,
        ColorBlock = 0x22,
        Color = 0x21,
        BodyData  = 0x13,
        AudioSamples = 0x11,
        BodyIndexBlock = 0x52,
        JPEG = 0x61
    }

    public enum DepthDeviceType {
        KinectV1 = 0x01,
        KinectV2 = 0x02,
        SR200 = 0x03
    };

    public struct DepthCameraExtrinsics {
        public readonly Vector3 position;
        public readonly Quaternion rotation;

        public DepthCameraExtrinsics(Vector3 p, Quaternion r) {
            position = p;
            rotation = r;
        }

        public DepthCameraExtrinsics(float x, float y, float z, float qx, float qy, float qz, float qw) {
            position = new Vector3(x, y, z);
            rotation = new Quaternion(qx, qy, qz, qw);
        }
    }

    public struct ConfigMessage {
        public DepthDeviceType deviceType;
        public DepthCameraIntrinsics intrinsics;
        public DepthCameraExtrinsics extrinsics;
        public string GUID;
        public string filename;
        public bool live;
        public bool hasAudio;
        public bool hasRGBD;
        public bool hasBody;
        public ushort maxLines;
    }

    public struct DepthCameraIntrinsics {
        public readonly ushort width;
        public readonly ushort height;
        public readonly float Cx;
        public readonly float Cy;
        public readonly float Fx;
        public readonly float Fy;
        public readonly float DepthScale;

        public DepthCameraIntrinsics(float cx, float cy, float fx, float fy, float depthScale, ushort w, ushort h) {
            Cx = cx;
            Cy = cy;
            Fx = fx;
            Fy = fy;
            DepthScale = depthScale;
            width = w;
            height = h;
        }
    }

}