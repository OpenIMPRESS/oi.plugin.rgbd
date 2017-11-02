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