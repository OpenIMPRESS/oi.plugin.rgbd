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
using System.Threading;
using oi.core.network;

namespace oi.plugin.rgbd {

    [RequireComponent(typeof(UDPConnector))]
    public class WebcamFrameSource : MonoBehaviour {
        public MeshRenderer myrenderer;
        private WebcamParser listener;
        private UDPConnector udpClient;

        public LockingJPEGQueue frameQueue = new LockingJPEGQueue();

        private int tex_width = 0;
        private int tex_height = 0;
        public void SetTextureSize(int width, int height) {
            tex_width = width;
            tex_height = height;
        }

        private void Start() {
            udpClient = GetComponent<UDPConnector>();
            listener = new WebcamParser(udpClient, this);
        }
        Texture2D tx;

        private void Update() {
            byte[] jpeg = frameQueue.Poll();
            if (jpeg != null && jpeg.Length > 0) {
                if (tx == null) {
                    tx = new Texture2D(tex_width, tex_height);
                    tx.filterMode = FilterMode.Point;
                    tx.wrapMode = TextureWrapMode.Clamp;
                    myrenderer.material.SetTexture("_MainTex", tx);
                }
                ImageConversion.LoadImage(tx, jpeg);
            }
        }

        void OnApplicationQuit() {
            listener.Close();
        }

    }


    public class LockingJPEGQueue {
        private Queue<byte[]> _queue = new Queue<byte[]>();

        public byte[] Poll() {
            lock (_queue) {
                byte[] returnObj = null;
                while (_queue.Count > 0) {
                    returnObj = _queue.Dequeue();
                }
                return returnObj;
            }
        }

        public byte[] Dequeue() {
            lock (_queue) {
                return _queue.Dequeue();
            }
        }

        public void Enqueue(byte[] data) {
            lock (_queue) {
                _queue.Enqueue(data);
            }
        }
    }



    // De-serializes incomming depth data and forwards to processor.
    public class WebcamParser {
        private bool _listening;
        private readonly Thread _listenThread;
        UDPConnector udpClient;
        WebcamFrameSource _frameSource;

        const byte RGBD_DATA = 1 << 0;
        const byte AUDIO_DATA = 1 << 1;
        const byte LIVE_DATA = 1 << 2;
        const byte BODY_DATA = 1 << 3;
        const byte HD_DATA = 1 << 4;
        const byte BIDX_DATA = 1 << 5;


        public WebcamParser(UDPConnector _udpClient, WebcamFrameSource fs) {
            _listenThread = new Thread(new ThreadStart(Listen));
            _frameSource = fs;
            udpClient = _udpClient;
            _listenThread.Start();
        }

        private void Listen() {
            _listening = true;

            while (_listening) {
                OIMSG msg_in = udpClient.GetNewData();

                if (msg_in == null || msg_in.data == null) continue;
                if (msg_in.data.Length < 2) continue;

                byte[] receiveBytes = msg_in.data;
                byte frameType = msg_in.msgType;

                switch (frameType) {
                    case (byte)FrameType.Config:
                        ConfigMessage cm = new ConfigMessage();
                        cm.deviceType = (DepthDeviceType)receiveBytes[1];
                        byte dataFlags = receiveBytes[2];

                        ushort frameWidth = System.BitConverter.ToUInt16(receiveBytes, 4);
                        ushort frameHeight = System.BitConverter.ToUInt16(receiveBytes, 6);
                        ushort maxLines = System.BitConverter.ToUInt16(receiveBytes, 8);

                        float cx = System.BitConverter.ToSingle(receiveBytes, 12);
                        float cy = System.BitConverter.ToSingle(receiveBytes, 16);
                        float fx = System.BitConverter.ToSingle(receiveBytes, 20);
                        float fy = System.BitConverter.ToSingle(receiveBytes, 24);
                        float depthScale = System.BitConverter.ToSingle(receiveBytes, 28);
                        cm.intrinsics = new DepthCameraIntrinsics(
                            cx, cy, fx, fy, depthScale, frameWidth, frameHeight);

                        float Px = System.BitConverter.ToSingle(receiveBytes, 32);
                        float Py = System.BitConverter.ToSingle(receiveBytes, 36);
                        float Pz = System.BitConverter.ToSingle(receiveBytes, 40);
                        float Qx = System.BitConverter.ToSingle(receiveBytes, 44);
                        float Qy = System.BitConverter.ToSingle(receiveBytes, 48);
                        float Qz = System.BitConverter.ToSingle(receiveBytes, 52);
                        float Qw = System.BitConverter.ToSingle(receiveBytes, 56);
                        cm.extrinsics = new DepthCameraExtrinsics(
                                Px, Py, Pz, Qx, Qy, Qz, Qw
                        );

                        int guid_offset = 63;
                        cm.GUID = "";
                        for (int sOffset = 0; sOffset < 32; sOffset++) {
                            byte c = receiveBytes[guid_offset + sOffset];
                            if (c == 0x00) break;
                            cm.GUID += (char)c;
                        }

                        cm.filename = "";
                        cm.live = (dataFlags & LIVE_DATA) != 0;
                        cm.hasAudio = (dataFlags & AUDIO_DATA) != 0;
                        cm.hasBody = (dataFlags & BODY_DATA) != 0;
                        cm.hasRGBD = (dataFlags & RGBD_DATA) != 0;

                        _frameSource.SetTextureSize(frameWidth, frameHeight);
                        Debug.Log("Config:\n\tFrame: " + frameWidth + " " + frameHeight + " " + maxLines +
                                  "\n\tIntrinsics: " + cx + " " + cy + " " + fx + " " + fy + " " + depthScale +
                                  "\n\tExtrinsics: " + cm.extrinsics.position.x + " " + cm.extrinsics.position.y + " " + cm.extrinsics.position.z +
                                  " " + cm.extrinsics.rotation.x + " " + cm.extrinsics.rotation.y + " " + cm.extrinsics.rotation.z + " " + cm.extrinsics.rotation.w +
                                  "\n\tGUID: " + cm.GUID);
                        break;
                    case (byte)FrameType.JPEG:
                        int header_size = 8;
                        int jpegLength = (int) System.BitConverter.ToUInt32(receiveBytes, 0);
                        int dataLength = receiveBytes.Length - header_size;
                        if (jpegLength != dataLength) {
                            Debug.LogWarning("Unexpected amount of data.");
                            return;
                        }
                        byte[] JPEG_colors = new byte[dataLength];
                        System.Buffer.BlockCopy(receiveBytes, header_size, JPEG_colors, 0, JPEG_colors.Length);
                        _frameSource.frameQueue.Enqueue(JPEG_colors);
                        break;
                    default:
                        Debug.Log("Unknown DepthStreaming frame type: " + msg_in.msgType);
                        break;
                }

            }
        }

        public void Close() {
            _listening = false;
            if (_listenThread != null)
                _listenThread.Join(500);
        }
    }
}