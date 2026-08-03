using System.Collections.Generic;
using UnityEngine;

namespace TNT
{
    public class StructuresGenerator : MonoBehaviour
    {
        public Structure[] structures;
        public int maxSpawnAttempts = 100;
        public float roadClearance = 5f;

        public void Generate(GenerationContext ctx)
        {
            if (structures == null || structures.Length == 0) return;
            TerrainData tData = Terrain.activeTerrain.terrainData; Vector3 tSize = tData.size; Vector2 center = new Vector2(tSize.x * 0.5f, tSize.z * 0.5f); System.Random prng = new System.Random(ctx.seed);

            for (int i = 0; i < structures.Length; i++)
            {
                Structure prefab = structures[i]; if (prefab == null) continue;
                float radius = Mathf.Max(prefab.spaceSize.x, prefab.spaceSize.y) * 0.5f; int targetCount = prng.Next(Mathf.RoundToInt(prefab.countRange.x), Mathf.RoundToInt(prefab.countRange.y) + 1);

                for (int c = 0; c < targetCount; c++)
                {
                    for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
                    {
                        float x, z;
                        if (prefab.radiusRange == Vector2.zero) { x = (float)prng.NextDouble() * tSize.x; z = (float)prng.NextDouble() * tSize.z; }
                        else { float r = Mathf.Lerp(prefab.radiusRange.x, prefab.radiusRange.y, (float)prng.NextDouble()); float angle = (float)prng.NextDouble() * Mathf.PI * 2f; x = center.x + Mathf.Cos(angle) * r; z = center.y + Mathf.Sin(angle) * r; }

                        if (x < 0 || x >= tSize.x || z < 0 || z >= tSize.z) continue;
                        float nX = x / tSize.x, nZ = z / tSize.z, h = tData.GetInterpolatedHeight(nX, nZ), steep = tData.GetSteepness(nX, nZ);
                        if (h < prefab.heightRange.x || h > prefab.heightRange.y || steep < prefab.angleRange.x || steep > prefab.angleRange.y) continue;

                        bool overlap = false; Vector2 pos2D = new Vector2(x, z);
                        for (int k = 0; k < ctx.occupiedFootprints.Count; k++) if (Vector2.Distance(pos2D, ctx.occupiedFootprints[k].center) < (ctx.occupiedFootprints[k].radius + radius)) { overlap = true; break; }
                        if (overlap) continue;

                        float minDist = float.MaxValue, nearestRoadRadius = 0f;
                        for (int j = 0; j < ctx.roadSegments.Count; j++) { Footprint seg = ctx.roadSegments[j]; if (!seg.isSegment) continue; float d = Vector2.Distance(pos2D, GetClosestPointOnSegment(pos2D, seg.p1, seg.p2)); if (d < minDist) { minDist = d; nearestRoadRadius = seg.radius; } }
                        if (minDist < radius + nearestRoadRadius + roadClearance) continue;

                        Vector3 pos = new Vector3(x, h, z);
                        Quaternion rot = prefab.GetRotation(pos, center, prng, ctx);
                        Structure instance = Instantiate(prefab, pos, rot, transform);
                        instance.OnStructureSpawn(ctx);
                        ctx.occupiedFootprints.Add(new Footprint { center = pos2D, radius = radius, isSegment = false });
                        break;
                    }
                }
            }
        }

        private Vector2 GetClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b) { float l2 = (b - a).sqrMagnitude; if (l2 == 0f) return a; float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / l2); return a + t * (b - a); }
    }
}