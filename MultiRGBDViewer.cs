using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace oi.plugin.rgbd {

    public class MultiRGBDViewer : MonoBehaviour {
        public Material m_material;


        public List<Transform> kinectTransforms = new List<Transform>();
        public List<Transform> kinectScaledTransforms = new List<Transform>();

        public List<FrameSource> frameSources = new List<FrameSource>();
        private List<RGBDViewer> pointCloudViewers = new List<RGBDViewer>();
        private List<RGBDViewer> pointCloudViewersScaled = new List<RGBDViewer>();

        // Use this for initialization
        void Start() {
            foreach (FrameSource frameSource in frameSources) {

                GameObject newObj = new GameObject(frameSource.name + "Viewer", typeof(RGBDViewer));
                newObj.transform.SetParent(transform);
                RGBDViewer newViewer = newObj.GetComponent<RGBDViewer>();
                newViewer.frameSource = frameSource;
                newViewer.m_material = Instantiate(m_material);
                pointCloudViewers.Add(newViewer);


                GameObject newObjScaled = new GameObject(frameSource.name + "ViewerScaled", typeof(RGBDViewer));
                newObjScaled.transform.SetParent(transform);
                RGBDViewer newViewerScaled = newObjScaled.GetComponent<RGBDViewer>();
                newViewerScaled.frameSource = frameSource;
                newViewerScaled.m_material = Instantiate(m_material);
                pointCloudViewersScaled.Add(newViewerScaled);

            }
        }

        // Update is called once per frame
        void Update() {

            int frameSourceId = 0;
            foreach (FrameSource frameSource in frameSources) {
                FrameObj frame = frameSource.GetNewFrame();
                pointCloudViewers[frameSourceId].transform.position = kinectTransforms[frameSourceId].transform.position;
                pointCloudViewers[frameSourceId].transform.rotation = kinectTransforms[frameSourceId].transform.rotation;
                pointCloudViewers[frameSourceId].transform.localScale = kinectTransforms[frameSourceId].transform.localScale;
                pointCloudViewers[frameSourceId].RenderFrame(frame);

                pointCloudViewersScaled[frameSourceId].transform.position = kinectScaledTransforms[frameSourceId].transform.position;
                pointCloudViewersScaled[frameSourceId].transform.rotation = kinectScaledTransforms[frameSourceId].transform.rotation;
                pointCloudViewersScaled[frameSourceId].transform.localScale = kinectScaledTransforms[frameSourceId].transform.localScale;
                pointCloudViewersScaled[frameSourceId].RenderFrame(frame);


                // set the particle setting of the instance of the shader of the point cloud viewer (RGBDViewer) 
                float defaultScale = 0.0033f;
                MeshRenderer[] renderers = pointCloudViewersScaled[frameSourceId].transform.GetComponentsInChildren<MeshRenderer>();
                if (renderers.Length > 0) { 
                    renderers[0].sharedMaterial.SetFloat("_ParticleSize", defaultScale * kinectScaledTransforms[frameSourceId].transform.localScale.x);
                }

                frameSourceId++;
            }

        }
    }

}