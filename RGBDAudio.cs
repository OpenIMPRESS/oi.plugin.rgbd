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

using System.Collections.Generic;
using UnityEngine;

namespace oi.plugin.rgbd {

    public class RGBDAudioFrame {
        public float[] samples;
        public ushort channels;
        public ushort frequency;
        public ulong timestamp;
    }

    [RequireComponent(typeof(AudioSource))]
    public class RGBDAudio : MonoBehaviour {

        private Queue<float> _sampleSampleBuffer;
        private Queue<RGBDAudioFrame> _sampleBuffer;
        private readonly object _sampleBufferLock = new object();

        void Awake() {
            _sampleSampleBuffer = new Queue<float>();
            _sampleBuffer = new Queue<RGBDAudioFrame>();
        }

        void Start() {
            if (AudioSettings.outputSampleRate / 16000 != 3) {
                Debug.LogWarning("oop[s");
            }
        }

        void FixedUpdate() {
            EnqueueSamples();
        }


        // OI Frames from audio frame parser/reader:
        public void QueueBuffer(RGBDAudioFrame frame) {
            lock (_sampleBufferLock) {
                _sampleBuffer.Enqueue(frame);
            }
        }

        // Empty the buffer
        public void Clear() {
            lock (_sampleBufferLock) {
                _sampleSampleBuffer.Clear();
                _sampleBuffer.Clear();
            }
        }

        // Read all audio samples from buffered audio frames into queue
        private void EnqueueSamples() {
            RGBDAudioFrame f = PollSampleBuffer();
            while (f != null) {
                foreach (float s in f.samples) {
                    //dataPosition++;
                    _sampleSampleBuffer.Enqueue(s);
                }
                f = PollSampleBuffer();
            }
        }

        private RGBDAudioFrame PollSampleBuffer() {
            RGBDAudioFrame res = null;
            lock (_sampleBufferLock) {
                if (_sampleBuffer.Count > 0) {
                    res = _sampleBuffer.Dequeue();
                }
            }
            return res;
        }
        
        void OnAudioFilterRead(float[] data, int channels) {
            int dataLen = data.Length / channels;
            int srcLen = dataLen / 3;

            if (_sampleSampleBuffer.Count <= srcLen) return;
            while (_sampleSampleBuffer.Count > srcLen * 8) {
                _sampleSampleBuffer.Dequeue();
            }

            int t = 0;
            float val = 0f;
            while (t < dataLen) {
                if (t % 3 == 0)
                    val = _sampleSampleBuffer.Dequeue();
                int c = 0;
                while (c < channels) {
                    data[t * channels + c] += val;
                    c++;
                }
                t++;
            }
        }
    }
    
}