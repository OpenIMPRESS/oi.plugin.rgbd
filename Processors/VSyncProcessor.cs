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
using System.Threading;
using UnityEngine;
using System;
using System.Linq;

namespace oi.plugin.rgbd {

    public class SequencedFrame : FastFrame {
        public BitArray ReceivedFrames;

        private readonly Processor _processor;

        public SequencedFrame(Processor p) : base(p) {
            _processor = p;
            Reset();
        }

        public override void Release() {
            ((VSyncProcessor) _processor).ReturnFromRender(this);
        }

        public void Reset() {
            ReceivedFrames = new BitArray(_processor.TotalHeight, false);
        }

        public bool IsComplete() {
            for (var i = 0; i < ReceivedFrames.Count; i++) {
                if (!ReceivedFrames.Get(i)) return false;
            }
            return true;
        }

        public int CountMissing() {
            int res = 0;
            for (var i = 0; i < ReceivedFrames.Count; i++) {
                if (!ReceivedFrames.Get(i)) res++;
            }
            return res;
        }

        public void MarkAsLoaded(ushort sr, ushort er) {
            for (ushort l = sr; l < er; l++) {
                ReceivedFrames.Set(l, true);
            }
        }
    }

    public class VSyncProcessor : Processor {

        private readonly int _frameBufferSize = 16;
        private readonly Queue<SequencedFrame> _unusedQueue;
        private readonly Dictionary<ulong, SequencedFrame> _frameBuffer;

        private readonly object _unusedQueueLock = new object();
        private readonly object _frameBufferLock = new object();
        private readonly Thread _processThread;

        private bool _processing;
        private ulong _lastSequenceRendered = 0;

        public VSyncProcessor(StreamFrameSource fs, DepthDeviceType t, DepthCameraIntrinsics cI,
            ushort w, ushort h, ushort ml, string guid)
            : base(fs, t, cI, w, h, ml, guid) {
            _frameBuffer = new Dictionary<ulong, SequencedFrame>();
            _unusedQueue = new Queue<SequencedFrame>();
            for (int i = 0; i < _frameBufferSize; i++) {
                _unusedQueue.Enqueue(new SequencedFrame(this));
            }

            _processThread = new Thread(new ThreadStart(Process));
            _processThread.Start();
        }

        public void ReturnFromRender(SequencedFrame s) {
            lock (_unusedQueueLock) {
                _unusedQueue.Enqueue(s);
            }
        }

        public override void Close() {
            _processing = false;
            if (_processThread != null)
                _processThread.Join(1000);
        }

        private void Process() {
            _processing = true;
            try {
                while (_processing) {
                    lock (_frameBufferLock) {
                        ulong remove = 0;
                        foreach (KeyValuePair<ulong, SequencedFrame> sequencedFrame in _frameBuffer) {
                            if (sequencedFrame.Key < _lastSequenceRendered) {
                                remove = sequencedFrame.Key;
                                //Debug.Log("A newer frame has already been rendered: "+remove);
                                break;
                            }

                            if (sequencedFrame.Value.IsComplete()) {
                                sequencedFrame.Value.cameraPos = FrameSource.cameraPosition;
                                sequencedFrame.Value.cameraRot = FrameSource.cameraRotation;
                                _lastSequenceRendered = sequencedFrame.Key;
                                remove = sequencedFrame.Key;
                                break;
                            }
                        }

                        if (remove > 0) {
                            SequencedFrame removeFrame = _frameBuffer[remove];
                            _frameBuffer.Remove(remove);
                            if (remove == _lastSequenceRendered) {
                                FrameSource.frameQueue.Enqueue(removeFrame);
                            } else {
                                lock (_unusedQueueLock) {
                                    _unusedQueue.Enqueue(removeFrame);
                                }
                            }
                        }
                    }
                }
            } catch (Exception e) {
                Debug.Log(e);
            } finally {
                _processing = false;
            }

            Debug.Log("Process Thread Closed");
        }

        public override void HandleColorData(ulong timestamp, ref byte[] data, int dataOffset) {
            throw new NotImplementedException();
        }
        public override void HandleBodyIndexData(ulong timestamp, ref byte[] data, int dataOffset) {
            throw new NotImplementedException();
        }

        public override void HandleDepthData(ushort sr, ushort er, ulong timestamp, ref byte[] data, int dataOffset) {
            if (timestamp < _lastSequenceRendered) return;

            lock (_frameBufferLock)
            lock (_unusedQueueLock) {
                if (_frameBuffer.ContainsKey(timestamp)) {
                    _frameBuffer[timestamp].LoadDepthData(sr, er, ref data, dataOffset);
                    _frameBuffer[timestamp].MarkAsLoaded(sr, er);
                    //Debug.Log("Using old frame: "+seq);
                } else if (_unusedQueue.Count > 0) {
                    _frameBuffer[timestamp] = _unusedQueue.Dequeue();
                    _frameBuffer[timestamp].Reset();
                    _frameBuffer[timestamp].LoadDepthData(sr, er, ref data, dataOffset);
                    _frameBuffer[timestamp].MarkAsLoaded(sr, er);
                    //Debug.Log("Dequeued for: "+seq);
                } else if (_frameBuffer.Count > 0) {
                    ulong oldest = _frameBuffer.Keys.Min();
                    SequencedFrame old = _frameBuffer[oldest];
                    _frameBuffer.Remove(oldest);
                    Debug.LogWarning("Dropping frame with seq: " + oldest + ", missing: " +
                                     old.CountMissing() + " of " + TotalHeight);
                    old.Reset();
                    _frameBuffer[timestamp] = old;
                    _frameBuffer[timestamp].LoadDepthData(sr, er, ref data, dataOffset);
                    _frameBuffer[timestamp].MarkAsLoaded(sr, er);
                } else {
                    Debug.LogWarning("Not enough (unused) framebuffers.");
                }
            }
        }
    }

}