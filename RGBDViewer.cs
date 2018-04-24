using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace oi.plugin.rgbd {

    public class RGBDViewer : MonoBehaviour {
        public Material m_material;
        public FrameSource frameSource;
        public bool fadeColorIfInactive = true;

        private Vector2 resolution = new Vector2();
        private List<GameObject> meshes = new List<GameObject>();
        private int m_maxPointsInInstance = 60000;
        private int m_maxPoints = 0;

        public float FPS = 0.0f;

        private float fpsSampleInterval = 2.0f;
        private float lastSample = 0.0f;
        private int fpsCounter = 0;

        private float colorFade;
        private float lastFrame;
        private float fadeDuration = 0.8f; // 800ms fade
        private float fadeAfter = 0.2f;

        // Use this for initialization
        void Start() {
            colorFade = 0.0f;
            lastFrame = float.MinValue;
            /*
            if (transform.parent == null) {
                transform.position = Vector3.zero;
                transform.rotation = Quaternion.identity;
            } else {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }*/
        }

        // Update is called once per frame
        public void RenderFrame(FrameObj frame) {
            float now = Time.time;
            if (lastSample + fpsSampleInterval < now) {
                FPS = fpsCounter / fpsSampleInterval;
                fpsCounter = 0;
                lastSample = now;
            }

            if (frame != null) {
                fpsCounter++;
                lastFrame = now;

                Vector2 _resolution = new Vector2(frame.posTex.width, frame.posTex.height);
                if (!resolution.Equals(_resolution)) {
                    resolution = _resolution;
                    m_maxPoints = (int) (_resolution.x * _resolution.y);
                    CreateMesh();
                }

                //transform.localPosition = frame.cameraPos;
                //transform.localRotation = frame.cameraRot;
                //transform.position = frame.cameraPos;
                //transform.rotation = frame.cameraRot;

                Texture2D newColTex = frame.colTex;
                if (newColTex != null) {
                    Destroy(m_material.GetTexture("_ColorTex"));
                    m_material.SetTexture("_ColorTex", newColTex);
                }

                Texture2D newPosTex = frame.posTex;
                if (newPosTex != null) {
                    Destroy(m_material.GetTexture("_PositionTex"));
                    m_material.SetTexture("_PositionTex", newPosTex);
                }

                Texture2D newBidxTex = frame.bidxTex;
                if (newBidxTex != null) {
                    Destroy(m_material.GetTexture("_BidxTex"));
                    m_material.SetTexture("_BidxTex", newBidxTex);
                }
            }

            if (fadeColorIfInactive) {
                if ((now - lastFrame) >= fadeAfter) {
                    float deltaFade = (now - lastFrame) - fadeAfter;
                    colorFade = Mathf.Max(0.0f, 1.0f - (deltaFade / fadeDuration));
                } else if (colorFade < 1.0f) {
                    colorFade += Time.deltaTime / fadeDuration;
                }

                m_material.SetFloat("_Fade", colorFade);
            } else {
                m_material.SetFloat("_Fade", 1.0f);
            }
        }

        void CreateMesh() {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            foreach (GameObject mesh in meshes) {
                Destroy(mesh);
            }

            int totIndex = 0;
            int meshId = 0;

            while (totIndex < m_maxPoints) {
                GameObject go = new GameObject();
                meshes.Add(go);
                go.name = "Mesh" + meshId++;
                MeshFilter mf = go.AddComponent<MeshFilter>();
                Mesh msh = new Mesh();
                mf.sharedMesh = msh;
                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                mr.material = m_material;
                go.transform.parent = transform;

                int pointsInMesh = Math.Min(m_maxPointsInInstance, m_maxPoints - totIndex);
                int[] indices = new int[pointsInMesh];
                Vector3[] verts = new Vector3[pointsInMesh];
                int[] faces = new int[pointsInMesh * 3];
                Vector2[] uvs0 = new Vector2[pointsInMesh]; // pos
                Vector2[] uvs1 = new Vector2[pointsInMesh]; // col

                for (int i = 0; i < pointsInMesh; ++i) {
                    indices[i] = i;
                    verts[i] = new Vector3(0f, 0f, 0f);
                    faces[i * 3] =
                        i; // make sure every vertex is at least once in the faces/triangle array, or it won't get rendered

                    float xx = totIndex % resolution.x;
                    float yy = totIndex / resolution.x;

                    xx /= resolution.x;
                    yy /= resolution.y;

                    uvs0[i] = new Vector2(xx, yy);
                    uvs1[i] = new Vector2(xx, 1-yy);

                    totIndex++;
                }

                //Debug.Log(msh.GetTopology (0));
                msh.vertices = verts;
                msh.triangles = faces;
                msh.uv = uvs0;
                msh.uv2 = uvs1;

                msh.SetIndices(indices, MeshTopology.Points, 0);
                msh.bounds = new Bounds(Vector3.zero, new Vector3(100f, 100f, 100f));
            }
        }
    }

}