using System.Collections;
using UnityEngine;

namespace GraphTheory
{
    public class Voronoi_Beacon : MonoBehaviour
    {
        EdgeBehavior edgeBehavior;
        public LineRenderer lineRenderer;

        public Voronoi_Beacon[] adjascentBeacons;
        // Use this for initialization
        void Start()
        {
            edgeBehavior = GetComponent<EdgeBehavior>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void DrawConnections()
        {
            if (adjascentBeacons.Length < 2) return;
            lineRenderer.positionCount = adjascentBeacons.Length;
            for (int i = 0; i < adjascentBeacons.Length; i++)
            {
                lineRenderer.SetPosition(i, adjascentBeacons[i].transform.position);
            }
        }

        public void ClearConnections()
        {
            lineRenderer.positionCount = 0;
        }

        public void SetAdjascentBeacons(Voronoi_Beacon[] beacons)
        {
            adjascentBeacons = beacons;
        }

        public void SetLineColor(Color color)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }

        public void SetLineWidth(float width)
        {
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }

        public void MoveTo(Vector3 pos)
        {
            transform.position = pos;
        }
    }
}