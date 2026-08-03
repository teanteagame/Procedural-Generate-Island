using System.Collections.Generic;
using UnityEngine;

namespace TNT
{
    public enum SpawnDirection
    {
        None, Inward, Outward,
        RoadBased
    }

    public class Structure : MonoBehaviour
    {
        [Header("Settings")]
        public string structureName = "Unknown structure";
        public SpawnDirection spawnDirection = SpawnDirection.None;
        public float directionJitter = 15f;
        public Vector2 spaceSize;
        public Vector2 heightRange;
        public Vector2 angleRange;
        public Vector2 radiusRange;
        public Vector2 countRange;
        public List<Effector> usedEffectors;

        public virtual void OnStructureSpawn(GenerationContext ctx) { Debug.Log(structureName + " spawned"); }

        public Quaternion GetRotation(Vector3 pos, Vector2 center, System.Random prng, GenerationContext ctx)
        {
            if (spawnDirection == SpawnDirection.None) return Quaternion.Euler(0, (float)prng.NextDouble() * 360f, 0);
            Vector3 dir = Vector3.zero;

            if (spawnDirection == SpawnDirection.RoadBased && ctx != null && ctx.roadSegments.Count > 0)
            {
                Vector2 p2D = new Vector2(pos.x, pos.z), closestRoad = p2D; float minDist = float.MaxValue;
                for (int i = 0; i < ctx.roadSegments.Count; i++)
                {
                    Footprint seg = ctx.roadSegments[i]; if (!seg.isSegment) continue;
                    Vector2 pt = GetClosestPointOnSegment(p2D, seg.p1, seg.p2); float d = Vector2.Distance(p2D, pt);
                    if (d < minDist) { minDist = d; closestRoad = pt; }
                }
                if (minDist < float.MaxValue) dir = new Vector3(closestRoad.x - pos.x, 0, closestRoad.y - pos.z);
            }
            else if (spawnDirection == SpawnDirection.Inward) dir = new Vector3(center.x - pos.x, 0, center.y - pos.z);
            else if (spawnDirection == SpawnDirection.Outward) dir = new Vector3(pos.x - center.x, 0, pos.z - center.y);

            if (dir == Vector3.zero) return Quaternion.Euler(0, (float)prng.NextDouble() * 360f, 0);
            float jitter = ((float)prng.NextDouble() * 2f - 1f) * directionJitter;
            return Quaternion.Euler(0, Quaternion.LookRotation(dir).eulerAngles.y + jitter, 0);
        }

        private Vector2 GetClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b) { float l2 = (b - a).sqrMagnitude; if (l2 == 0f) return a; float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / l2); return a + t * (b - a); }
    }
}