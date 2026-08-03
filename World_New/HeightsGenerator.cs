using UnityEngine;

namespace TNT
{
    public class HeightsGenerator : MonoBehaviour
    {
        public int Width = 256;
        public int Height = 256;
        public int Depth = 10;
        public int Octaves = 4;
        public float Scale = 50f;
        public float Lacunarity = 2f;
        [Range(0, 1)] public float Persistance = 0.5f;
        public AnimationCurve HeightCurve;
        public float Offset = 100f;
        public float FalloffDirection = 3f;
        public float FalloffRange = 3f;

        private void OnValidate()
        {
            if (Width < 1) Width = 1;
            if (Height < 1) Height = 1;
            if (Lacunarity < 1) Lacunarity = 1;
            if (Octaves < 0) Octaves = 0;
            if (Scale <= 0) Scale = 0.0001f;
        }

        public void Generate(GenerationContext ctx)
        {
            TerrainData terrainData = Terrain.activeTerrain.terrainData;
            terrainData.heightmapResolution = Width + 1;
            terrainData.alphamapResolution = Width;
            terrainData.SetDetailResolution(Width, 8);
            terrainData.size = new Vector3(Width, Depth, Height);

            float[,] falloff = null;
            falloff = new FalloffMap { FalloffDirection = FalloffDirection, FalloffRange = FalloffRange, Size = Width }.Generate();

            float[,] noiseMap = GenerateNoise(ctx.seed, falloff);
            ctx.heightMap = noiseMap; 
            terrainData.SetHeights(0, 0, noiseMap);
        }

        float[,] GenerateNoise(int seed, float[,] falloffMap = null)
        {
            AnimationCurve heightCurve = new AnimationCurve(HeightCurve.keys);
            float[,] noiseMap = new PerlinMap { Size = Width, Octaves = Octaves, Scale = Scale, Offset = Offset, Persistance = Persistance, Lacunarity = Lacunarity, Seed = seed }.Generate(out float maxLocalNoiseHeight, out float minLocalNoiseHeight);

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var lerp = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                    if (falloffMap != null) lerp -= falloffMap[x, y];
                    noiseMap[x, y] = lerp >= 0 ? heightCurve.Evaluate(lerp) : 0;
                }
            }
            return noiseMap;
        }
    }
}