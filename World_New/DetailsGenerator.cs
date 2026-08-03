using System.Collections.Generic;
using UnityEngine;

namespace TNT
{
    public class DetailsGenerator : MonoBehaviour
    {
        public DetailTexture[] details;

        public void Generate(GenerationContext ctx)
        {
            if (details == null || details.Length == 0) return;
            System.Random prng = new System.Random(ctx.seed);
            TerrainData tData = Terrain.activeTerrain.terrainData;
            Vector3 tSize = tData.size;
            Vector3 tOrigin = Terrain.activeTerrain.transform.position;
            int res = tData.detailResolution;
            System.Array.Sort(details, (a, b) => b.priority.CompareTo(a.priority));
            DetailPrototype[] prototypes = new DetailPrototype[details.Length];
            int[][,] detailLayers = new int[details.Length][,];
            for (int i = 0; i < details.Length; i++)
            {
                DetailTexture dt = details[i];
                prototypes[i] = new DetailPrototype { prototypeTexture = dt.detailTexture, minWidth = dt.minSize.x, maxWidth = dt.maxSize.x, minHeight = dt.minSize.y, maxHeight = dt.maxSize.y, healthyColor = dt.healthyColor, dryColor = dt.dryColor, renderMode = DetailRenderMode.Grass };
                detailLayers[i] = new int[res, res];
            }
            tData.detailPrototypes = prototypes;
            bool[,] footprintMask = new bool[res, res];
            for (int i = 0; i < ctx.occupiedFootprints.Count; i++)
            {
                Footprint fp = ctx.occupiedFootprints[i];
                int minX = Mathf.Clamp(Mathf.FloorToInt((fp.center.x - fp.radius) / tSize.x * res), 0, res - 1);
                int maxX = Mathf.Clamp(Mathf.CeilToInt((fp.center.x + fp.radius) / tSize.x * res), 0, res - 1);
                int minZ = Mathf.Clamp(Mathf.FloorToInt((fp.center.y - fp.radius) / tSize.z * res), 0, res - 1);
                int maxZ = Mathf.Clamp(Mathf.CeilToInt((fp.center.y + fp.radius) / tSize.z * res), 0, res - 1);
                float sqrRad = fp.radius * fp.radius;
                for (int z = minZ; z <= maxZ; z++) for (int x = minX; x <= maxX; x++) if ((((float)x / (res - 1) * tSize.x - fp.center.x) * ((float)x / (res - 1) * tSize.x - fp.center.x) + ((float)z / (res - 1) * tSize.z - fp.center.y) * ((float)z / (res - 1) * tSize.z - fp.center.y)) <= sqrRad) footprintMask[z, x] = true;
            }
            int hRes = tData.heightmapResolution;
            float[,] heights = tData.GetHeights(0, 0, hRes, hRes);
            List<int> validIndices = new List<int>();
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    if (footprintMask[y, x]) continue;
                    float normX = (float)x / (res - 1);
                    float normY = (float)y / (res - 1);
                    float wX = normX * tSize.x;
                    float wZ = normY * tSize.z;
                    bool isBlocked = false;
                    List<DetailTexture> forcedAllowed = null;
                    for (int zIdx = 0; zIdx < ctx.detailZones.Count; zIdx++)
                    {
                        DetailZone z = ctx.detailZones[zIdx];
                        if (z.Contains(new Vector3(tOrigin.x + wX, tOrigin.y, tOrigin.z + wZ))) { if (z.block) { isBlocked = true; break; } if (z.allowed != null && z.allowed.Count > 0) forcedAllowed = z.allowed; }
                    }
                    if (isBlocked) continue;
                    int hX = Mathf.Clamp(Mathf.RoundToInt(normX * (hRes - 1)), 0, hRes - 1);
                    int hY = Mathf.Clamp(Mathf.RoundToInt(normY * (hRes - 1)), 0, hRes - 1);
                    float h = heights[hY, hX] * tSize.y;
                    float s = tData.GetSteepness(normX, normY);
                    validIndices.Clear();
                    float maxPrio = -1f;
                    for (int i = 0; i < details.Length; i++)
                    {
                        DetailTexture dt = details[i];
                        if (forcedAllowed != null && !forcedAllowed.Contains(dt)) continue;
                        if (dt.priority < maxPrio) break;
                        if (h >= dt.heightRange.x && h <= dt.heightRange.y && s >= dt.angleRange.x && s <= dt.angleRange.y) if (prng.NextDouble() <= dt.spawnRate) { validIndices.Add(i); maxPrio = dt.priority; }
                    }
                    if (validIndices.Count > 0)
                    {
                        int actualIdx = validIndices[prng.Next(validIndices.Count)];
                        detailLayers[actualIdx][y, x] = details[actualIdx].density;
                    }
                }
            }
            for (int i = 0; i < details.Length; i++) tData.SetDetailLayer(0, 0, i, detailLayers[i]);
        }
    }

    [System.Serializable]
    public class DetailTexture
    {
        public Texture2D detailTexture;
        [Range(0, 1)] public float spawnRate;
        [Range(0, 1)] public float priority = 0.5f;
        [Range(1, 16)] public int density = 16;
        public Vector2 heightRange;
        public Vector2 angleRange;
        public Vector2 minSize, maxSize;
        public Color healthyColor, dryColor;
    }
}