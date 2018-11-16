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
        RGBDAudioFrame leftOver = null;

        public float bufferedSecs;
        public float SampleSec;
        private int sampleSum = 0;
        private float lastSampleAvg = 0.0f;
        private float sampleAvgInterval = 2.0f;

        AudioSource aud;
        private int clipFreq = 16000;
        private int clipChan = 1;
        private int clipLen = 16000*30;

        float SecondsPreBuffered() {
            float res = 0.0f;
            res += _sampleSampleBuffer.Count / 16000.0f;
            /*
            RGBDAudioFrame[] _frames;
            lock (_sampleBufferLock) {
                _frames =  _sampleBuffer.ToArray();
            }

            if (_frames != null) { 
                foreach (RGBDAudioFrame f in _frames) {
                    res += f.samples.Length / 16000.0f;
                }
            }

            if (leftOver != null)
                res += leftOver.samples.Length / 16000.0f;
                */
            return res;
        }

        private int _position;
        void OnAudioSetPosition(int newPosition) {
            _position = newPosition;
        }

        void OnAudioRead(float[] data) {
            int count = 0;

            lock (_sampleBufferLock) {
                while (count < data.Length) {
                    if (_sampleSampleBuffer.Count > 0) {
                        data[count] = _sampleSampleBuffer.Dequeue();
                    } else {
                        data[count] = 0.0f;
                    }
                    count++;
                }
            }

            /*
            int read = 0;
            while (count < data.Length) {
                if (leftOver != null) {
                    data[count] = leftOver.samples[read];
                    if (read == leftOver.samples.Length - 1) {
                        leftOver = null;
                    }
                } else {
                    leftOver = DequeueBuffer();
                    if (leftOver == null) { // We're out of audio data...
                        data[count] = 0.0f;
                    } else {
                        read = 0;
                        data[count] = leftOver.samples[read];
                    }
                }

                count++;
                read++;
            }

            if (leftOver != null) {
                if (read >= leftOver.samples.Length) {
                    leftOver = null;
                } else {
                    int n_samples_left = leftOver.samples.Length - read;
                    float[] leftOverSamples = new float[n_samples_left];
                    System.Array.Copy(leftOver.samples, read, leftOverSamples, 0, n_samples_left);
                    leftOver.samples = leftOverSamples;
                }
            }*/
        }

        void Awake() {
            _sampleSampleBuffer = new Queue<float>();
            _sampleBuffer = new Queue<RGBDAudioFrame>();
            sampleSum = 0;
        }

        void Start() {
            aud = GetComponent<AudioSource>();
            aud.clip = AudioClip.Create("RemoteAudio", clipLen, clipChan, clipFreq, true, OnAudioRead, OnAudioSetPosition);
            aud.loop = true;
            aud.Play();
        }

        void EnqueueSamples() {
            RGBDAudioFrame f = DequeueBuffer();
            while (f != null) {
                lock (_sampleBufferLock) {
                    foreach (float s in f.samples) {
                        _sampleSampleBuffer.Enqueue(s);
                    }
                }

                sampleSum += f.samples.Length;
                f = DequeueBuffer();
            }
        }

        void Update() {

            EnqueueSamples();

            bufferedSecs = SecondsPreBuffered();

            if (lastSampleAvg + sampleAvgInterval <= Time.time) {
                lastSampleAvg = Time.time;
                SampleSec = sampleSum / 2.0f;
                sampleSum = 0;
            }
        }

        private RGBDAudioFrame DequeueBuffer() {
            RGBDAudioFrame res = null;
            lock (_sampleBufferLock) {
                if (_sampleBuffer.Count > 0) {
                    res = _sampleBuffer.Dequeue();
                }
            }
            return res;
        }

        public void Clear() {
            lock (_sampleBufferLock) {
                _sampleSampleBuffer.Clear();
                _sampleBuffer.Clear();
            }
        }

        public void QueueBuffer(RGBDAudioFrame frame) {
            lock (_sampleBufferLock) {
                _sampleBuffer.Enqueue(frame);
            }
        }
    }


    /*
    [RequireComponent(typeof(AudioSource))]
    public class RGBDAudio : MonoBehaviour {
        private Queue<RGBDAudioFrame> _sampleBuffer;
        private readonly object _sampleBufferLock = new object();

        public int SampleSec;
        private int sampleSum = 0;
        private float lastSampleAvg = 0.0f;
        private float sampleAvgInterval = 2.0f;

        AudioSource aud;
        private int clipFreq = -1;
        private int clipChan = -1;
        private int clipLen = 441000;
        private int lastSamplePos = 0;



        
        //void OnAudioRead(float[] data) {
        //    int count = 0;
        //    while (count < data.Length) {
        //        data[count] = 
        //    }
        //}

        void Awake() {
            _sampleBuffer = new Queue<RGBDAudioFrame>();
            sampleSum = 0;
        }

        void Start() {
            aud = GetComponent<AudioSource>();
            aud.loop = true;
        }

        void Update() {
            RGBDAudioFrame aframe = DequeueBuffer();
            while (aframe != null) {
                if (clipFreq != aframe.frequency || clipChan != aframe.channels) {
                    clipFreq = aframe.frequency;
                    clipChan = aframe.channels;
                    clipLen = aframe.frequency * 10;
                    aud.clip = AudioClip.Create("RemoteAudio",
                        clipLen, clipChan, clipFreq, false);
                    lastSamplePos = 0;
                    aud.timeSamples = 0;
                    Debug.Log("(Re)initialized audio: "+clipFreq+" "+clipChan);
                }

                sampleSum += aframe.samples.Length;
                aud.clip.SetData(aframe.samples, lastSamplePos);

                if (lastSamplePos > aud.timeSamples + clipFreq/2 ||
                   (lastSamplePos < aud.timeSamples && lastSamplePos > clipFreq/2
                    && aud.timeSamples < clipLen - clipFreq/2)) {
                    aud.timeSamples = lastSamplePos;
                }

                if (!aud.isPlaying) aud.Play();

                lastSamplePos += aframe.samples.Length;
                if (lastSamplePos >= clipLen)
                    lastSamplePos -= clipLen;

                aframe = DequeueBuffer();
            }


            UpdatePlayer();

            if (lastSampleAvg+sampleAvgInterval <= Time.time) {
                lastSampleAvg = Time.time;
                SampleSec = sampleSum / 2;
                sampleSum = 0;
            }
        }


        void UpdatePlayer() {
            if (!aud.isPlaying) {
                return;
            }

            if (aud.timeSamples > lastSamplePos && aud.timeSamples - lastSamplePos < clipFreq/2) {
                aud.Pause();
                Debug.Log("PAUSED");
            }
        }

        private RGBDAudioFrame DequeueBuffer() {
            RGBDAudioFrame res = null;
            lock (_sampleBufferLock) {
                if (_sampleBuffer.Count > 0) {
                    res = _sampleBuffer.Dequeue();
                }
            }
            return res;
        }

        public void QueueBuffer(RGBDAudioFrame frame) {
            lock (_sampleBufferLock) {
                _sampleBuffer.Enqueue(frame);
            }
        }
    } */

}