using UnityEngine;
using System.Collections.Generic;

namespace TNT
{
    public class RoadEffector : Effector
    {
        public float drivewayWidth = 3f;
        public TerrainLayer drivewayLayer;

        public override void Execute(GenerationContext ctx)
        {
            base.Execute(ctx);
            if (ctx.roadSegments == null || ctx.roadSegments.Count == 0) return;

            Vector2 pos = new Vector2(transform.position.x, transform.position.z), closestRoadPt = pos; float minDist = float.MaxValue;

            for (int i = 0; i < ctx.roadSegments.Count; i++)
            {
                Footprint seg = ctx.roadSegments[i]; if (!seg.isSegment) continue;
                Vector2 pt = GetClosestPointOnSegment(pos, seg.p1, seg.p2); float d = Vector2.Distance(pos, pt);
                if (d < minDist) { minDist = d; closestRoadPt = pt; }
            }

            if (minDist < float.MaxValue)
            {
                RoadsGenerator rg = FindAnyObjectByType<RoadsGenerator>();
                if (rg != null)
                {
                    TerrainData tData = Terrain.activeTerrain.terrainData; Vector3 tSize = tData.size;
                    Vector3 start = new Vector3(pos.x, tData.GetInterpolatedHeight(pos.x / tSize.x, pos.y / tSize.z), pos.y);
                    Vector3 end = new Vector3(closestRoadPt.x, tData.GetInterpolatedHeight(closestRoadPt.x / tSize.x, closestRoadPt.y / tSize.z), closestRoadPt.y);

                    float origWidth = rg.roadWidth; TerrainLayer origLayer = rg.roadLayer;
                    rg.roadWidth = drivewayWidth; rg.roadLayer = drivewayLayer;
                    rg.DrawPath(start, end, ctx);
                    rg.roadWidth = origWidth; rg.roadLayer = origLayer;
                }
            }

            Debug.Log("Start this path");
        }

        private Vector2 GetClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b) { float l2 = (b - a).sqrMagnitude; if (l2 == 0f) return a; float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / l2); return a + t * (b - a); }

        public override void OnDrawGizmos() { Gizmos.color = new Color(0.54f, 0.17f, 0.89f); Gizmos.DrawWireSphere(transform.position, 3); }
    }
}