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

namespace oi.plugin.rgbd {

    public class RandomFrameSource : FrameSource {
        private int frameWidth = 512;
        private int frameHeight = 424;

        private Vector3 cameraPos = new Vector3();
        private Quaternion cameraRot = new Quaternion();

        Thread thread;
        private bool running = false;

        // Use this for initialization
        private new void Start() {
            base.Start();
            thread = new Thread(Run);
            thread.Start();
        }

        private void Update() {
            cameraPos = cameraTransform.position;
            cameraRot = cameraTransform.rotation;
        }

        void Run() {
            System.Random random = new System.Random();
            running = true;
            while (running) {
                Color[] _positions = new Color[frameWidth * frameHeight];
                Color[] _colors = new Color[frameWidth * frameHeight];

                for (int y = 0; y < frameHeight; y++) {
                    for (int x = 0; x < frameWidth; x++) {
                        int fullIndex = (y * frameWidth) + x;

                        _positions[fullIndex] = new Color((float) random.NextDouble(), (float) random.NextDouble(),
                            (float) random.NextDouble());

                        _colors[fullIndex] = new Color((float) random.NextDouble(), (float) random.NextDouble(),
                            (float) random.NextDouble());
                        ;
                    }
                }

                PreFrameObj newFrame = new PreFrameObj();
                newFrame.colors = _colors;
                newFrame.colSize = new Vector2(frameWidth, frameHeight);
                newFrame.positions = _positions;
                newFrame.posSize = new Vector2(frameWidth, frameHeight);
                newFrame.cameraPos = cameraPos;
                newFrame.cameraRot = cameraRot;

                frameQueue.Enqueue(newFrame);
            }
        }

        void OnApplicationQuit() {
            running = false;
            thread.Join(); // block till thread is finished
        }
    }

}