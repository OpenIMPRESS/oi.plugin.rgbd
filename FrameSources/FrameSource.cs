﻿/*
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
using System;

namespace oi.plugin.rgbd {
    public class FrameSource : MonoBehaviour {

        public Transform cameraTransform;
        public Transform originCOS;

        [HideInInspector]
        public LockingQueue frameQueue = new LockingQueue();

        // Use this for initialization
        public void Start() {
            if (cameraTransform == null) {
                cameraTransform = transform;
            }
        }

        // Update is called once per frame
        void Update() { }


        public FrameObj GetNewFrame() {
            APreFrameObj preObj = frameQueue.Poll();
            if (preObj != null) {
                FrameObj newFrame = new FrameObj();
                newFrame.posTex = new Texture2D((int)preObj.posSize.x, (int)preObj.posSize.y, TextureFormat.RGBAFloat,
                    false);
                newFrame.posTex.wrapMode = TextureWrapMode.Repeat;
                newFrame.posTex.filterMode = FilterMode.Point;
                newFrame.posTex.SetPixels(preObj.positions);
                newFrame.posTex.Apply();

                if (preObj.colors != null) {
                    newFrame.colTex = new Texture2D((int)preObj.colSize.x, (int)preObj.colSize.y,
                        TextureFormat.RGBAFloat, false);
                    newFrame.colTex.wrapMode = TextureWrapMode.Repeat;
                    newFrame.colTex.filterMode = FilterMode.Point;
                    newFrame.colTex.SetPixels(preObj.colors);
                    newFrame.colTex.Apply();
                } else if (preObj.DXT1_colors != null) {
                    newFrame.colTex = new Texture2D((int)preObj.colSize.x, (int)preObj.colSize.y, TextureFormat.DXT1,
                        false);
                    newFrame.colTex.wrapMode = TextureWrapMode.Clamp;
                    newFrame.colTex.filterMode = FilterMode.Point;
                    newFrame.colTex.LoadRawTextureData(preObj.DXT1_colors);
                    newFrame.colTex.Apply();
                } else if (preObj.JPEG_colors != null) {
                    newFrame.colTex = new Texture2D((int)preObj.colSize.x, (int)preObj.colSize.y);
                    newFrame.colTex.wrapMode = TextureWrapMode.Clamp;
                    newFrame.colTex.filterMode = FilterMode.Point;
                    newFrame.colTex.LoadImage(preObj.JPEG_colors);
                    newFrame.colTex.Apply();
                }

                if (preObj.BodyIndexJPEG_colors != null) {
                    newFrame.bidxTex = new Texture2D((int)preObj.colSize.x, (int)preObj.colSize.y); //TextureFormat.Alpha8
                    newFrame.bidxTex.wrapMode = TextureWrapMode.Clamp;
                    newFrame.bidxTex.filterMode = FilterMode.Point;
                    newFrame.bidxTex.LoadImage(preObj.BodyIndexJPEG_colors);
                    newFrame.bidxTex.Apply();
                }

                newFrame.cameraPos = preObj.cameraPos;
                newFrame.cameraRot = preObj.cameraRot;

                newFrame.timeStamp = preObj.timeStamp;
                preObj.Release();
                return newFrame;
            } else
                return null;
        }

    }


    public class FrameObj {
        public Texture2D posTex;
        public Texture2D colTex;
        public Texture2D bidxTex;
        public Vector3 cameraPos;
        public Quaternion cameraRot;
        public float timeStamp;
    }

    public abstract class APreFrameObj {
        public Color[] positions;
        public Vector2 posSize;
        public Color[] colors;
        public byte[] DXT1_colors;
        public byte[] JPEG_colors;
        public byte[] BodyIndexJPEG_colors;
        public Vector2 colSize;
        public Vector3 cameraPos;
        public Quaternion cameraRot;
        public float timeStamp;

        public abstract void Release();
    }

    public class PreFrameObj : APreFrameObj {
        public override void Release() { }
    }


    public class LockingQueue {
        private Queue<APreFrameObj> _queue = new Queue<APreFrameObj>();

        public APreFrameObj Dequeue() {
            lock (_queue) {
                return _queue.Dequeue();
            }
        }

        public APreFrameObj Poll() {
            lock (_queue) {
                APreFrameObj returnObj = null;
                if (_queue.Count > 1) {
                    //Debug.Log("Skipping " + (_queue.Count - 1) + " Frames");
                } else if (_queue.Count == 0) {
                    return null;
                }

                while (_queue.Count > 1) {
                    returnObj = _queue.Dequeue();
                    returnObj.Release();
                }

                return _queue.Dequeue();
            }
        }

        public void Enqueue(APreFrameObj data) {
            if (data == null) throw new ArgumentNullException("data");

            lock (_queue) {
                _queue.Enqueue(data);
            }
        }
    }
}