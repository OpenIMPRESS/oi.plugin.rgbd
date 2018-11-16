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

    public class MultiRGBDViewer : MonoBehaviour {
        public Material m_material;
        public List<FrameSource> frameSources = new List<FrameSource>();
        private List<RGBDViewer> pointCloudViewers = new List<RGBDViewer>();

        // Use this for initialization
        void Start() {
            foreach (FrameSource frameSource in frameSources) {
                GameObject newObj = new GameObject(frameSource.name + "Viewer", typeof(RGBDViewer));
                newObj.transform.SetParent(transform);
                RGBDViewer newViewer = newObj.GetComponent<RGBDViewer>();
                newViewer.frameSource = frameSource;
                newViewer.m_material = Instantiate(m_material);
                pointCloudViewers.Add(newViewer);
            }
        }

        // Update is called once per frame
        void Update() { }
    }

}