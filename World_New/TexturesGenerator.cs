using System;
using System.Collections.Generic;
using UnityEngine;

namespace TNT
{
    public class TexturesGenerator : MonoBehaviour
    {
        public List<TerrainTexture> textures = new List<TerrainTexture>();

        public void Generate(GenerationContext ctx)
        {
            if (textures == null || textures.Count == 0) throw new NullReferenceException("Textures list not set or empty");

            TerrainData tData = Terrain.activeTerrain.terrainData;
            List<TerrainLayer> allLayers = new List<TerrainLayer>();
            for (int i = 0; i < textures.Count; i++) allLayers.Add(textures[i].layer);

            List<LayerMark> marks = new List<LayerMark>();
            if (ctx.sharedData.TryGetValue("LayerMarks", out object marksObj)) marks = (List<LayerMark>)marksObj;
            for (int i = 0; i < marks.Count; i++) if (marks[i].layer != null && !allLayers.Contains(marks[i].layer)) allLayers.Add(marks[i].layer);

            tData.terrainLayers = allLayers.ToArray();
            if (tData.alphamapResolution != tData.size.x) Debug.LogWarning("terrainData.alphamapResolution must fit terrain size");

            int alphaW = tData.alphamapWidth;
            int alphaH = tData.alphamapHeight;
            int layerCount = allLayers.Count;
            float[,,] splatmaps = new float[alphaH, alphaW, layerCount];

            List<int> validIndices = new List<int>(textures.Count);

            for (int y = 0; y < alphaH; y++)
            {
                for (int x = 0; x < alphaW; x++)
                {
                    float normX = (float)x / (alphaW - 1);
                    float normY = (float)y / (alphaH - 1);
                    float height = tData.GetInterpolatedHeight(normX, normY);
                    float steepness = tData.GetSteepness(normX, normY);
                    float maxPriority = -1f;
                    validIndices.Clear();

                    for (int i = 0; i < textures.Count; i++)
                    {
                        TerrainTexture tex = textures[i];
                        if (height >= tex.heightRange.x && height <= tex.heightRange.y && steepness >= tex.angleRange.x && steepness <= tex.angleRange.y)
                        {
                            if (tex.priority > maxPriority) { maxPriority = tex.priority; validIndices.Clear(); validIndices.Add(i); }
                            else if (Mathf.Approximately(tex.priority, maxPriority)) validIndices.Add(i);
                        }
                    }

                    if (validIndices.Count > 0)
                    {
                        float weight = 1.0f / validIndices.Count;
                        for (int i = 0; i < validIndices.Count; i++) splatmaps[y, x, validIndices[i]] = weight;
                    }
                    else splatmaps[y, x, 0] = 1.0f;
                }
            }

            for (int i = 0; i < marks.Count; i++)
            {
                LayerMark mark = marks[i];
                if (mark.layer == null) continue;
                int lIdx = allLayers.IndexOf(mark.layer);
                if (lIdx == -1) continue;

                float normX = mark.center.x / tData.size.x, normZ = mark.center.y / tData.size.z;
                int r = Mathf.CeilToInt((mark.radius / tData.size.x) * alphaW);
                int cX = Mathf.RoundToInt(normX * (alphaW - 1)), cY = Mathf.RoundToInt(normZ * (alphaH - 1));
                int sX = Mathf.Max(0, cX - r), sY = Mathf.Max(0, cY - r);
                int eX = Mathf.Min(alphaW - 1, cX + r), eY = Mathf.Min(alphaH - 1, cY + r);

                for (int y = sY; y <= eY; y++)
                {
                    for (int x = sX; x <= eX; x++)
                    {
                        if ((x - cX) * (x - cX) + (y - cY) * (y - cY) <= r * r)
                        {
                            for (int l = 0; l < layerCount; l++) splatmaps[y, x, l] = (l == lIdx) ? 1f : 0f;
                        }
                    }
                }
            }
            tData.SetAlphamaps(0, 0, splatmaps);
        }
    }

    [Serializable]
    public class TerrainTexture
    {
        public TerrainLayer layer;
        public Vector2 heightRange;
        public Vector2 angleRange;
        [Range(0, 1)] public float priority = 0.5f;
    }
}
