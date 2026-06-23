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

            TerrainData terrainData = Terrain.activeTerrain.terrainData;
            TerrainLayer[] layers = new TerrainLayer[textures.Count];
            for (int i = 0; i < textures.Count; i++) layers[i] = textures[i].layer;
            terrainData.terrainLayers = layers;

            if (terrainData.alphamapResolution != terrainData.size.x) Debug.LogWarning("terrainData.alphamapResolution must fit terrain size");

            int alphaWidth = terrainData.alphamapWidth;
            int alphaHeight = terrainData.alphamapHeight;
            int layerCount = terrainData.alphamapLayers;
            float[,,] splatmaps = new float[alphaHeight, alphaWidth, layerCount];

            List<int> validIndices = new List<int>(layerCount);

            for (int y = 0; y < alphaHeight; y++)
            {
                for (int x = 0; x < alphaWidth; x++)
                {
                    float normX = (float)x / (alphaWidth - 1);
                    float normY = (float)y / (alphaHeight - 1);

                    float realHeight = terrainData.GetInterpolatedHeight(normX, normY);
                    float realSteepness = terrainData.GetSteepness(normX, normY);

                    validIndices.Clear();
                    float maxPriority = -1f;

                    for (int i = 0; i < layerCount; i++)
                    {
                        TerrainTexture tex = textures[i];
                        bool inHeight = realHeight >= tex.heightRange.x && realHeight <= tex.heightRange.y;
                        bool inAngle = realSteepness >= tex.angleRange.x && realSteepness <= tex.angleRange.y;

                        if (inHeight && inAngle)
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
            terrainData.SetAlphamaps(0, 0, splatmaps);
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