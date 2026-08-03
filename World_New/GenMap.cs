using UnityEngine;

namespace TNT
{
    public class FalloffMap
    {
        public float FalloffDirection;
        public float FalloffRange;
        public int Size;

        public float[,] Generate()
        {
            float[,] map = new float[Size, Size];
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    float x = i / (float)Size * 2 - 1;
                    float y = j / (float)Size * 2 - 1;
                    float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                    map[i, j] = Evaluate(value);
                }
            }
            return map;
        }

        float Evaluate(float value)
        {
            return Mathf.Pow(value, FalloffDirection) / (Mathf.Pow(value, FalloffDirection) + Mathf.Pow(FalloffRange - FalloffRange * value, FalloffDirection));
        }
    }

    public class PerlinMap
    {
        public int Size { get; set; }
        public int Octaves { get; set; }
        public float Scale { get; set; }
        public float Offset { get; set; }
        public float Persistance { get; set; }
        public float Lacunarity { get; set; }
        public int Seed { get; set; }

        public float[,] Generate(out float maxLocalNoiseHeight, out float minLocalNoiseHeight)
        {
            float[,] noiseMap = new float[Size, Size];
            System.Random prng = new System.Random(Seed);
            Vector2[] octaveOffsets = new Vector2[Octaves];

            float maxPossibleHeight = 0;
            float amplitude = 1;
            float frequency = 1;

            for (int i = 0; i < Octaves; i++)
            {
                float offsetX = prng.Next(-100000, 100000) + Offset;
                float offsetY = prng.Next(-100000, 100000) + Offset;
                octaveOffsets[i] = new Vector2(offsetX, offsetY);

                maxPossibleHeight += amplitude;
                amplitude *= Persistance;
            }

            if (Scale <= 0) Scale = 0.0001f;

            maxLocalNoiseHeight = float.MinValue;
            minLocalNoiseHeight = float.MaxValue;
            float halfSize = Size / 2f;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    amplitude = 1;
                    frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i < Octaves; i++)
                    {
                        float sampleX = (x - halfSize + octaveOffsets[i].x) / Scale * frequency;
                        float sampleY = (y - halfSize + octaveOffsets[i].y) / Scale * frequency;

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= Persistance;
                        frequency *= Lacunarity;
                    }

                    if (noiseHeight > maxLocalNoiseHeight) maxLocalNoiseHeight = noiseHeight;
                    else if (noiseHeight < minLocalNoiseHeight) minLocalNoiseHeight = noiseHeight;

                    noiseMap[x, y] = noiseHeight;
                }
            }
            return noiseMap;
        }
    }
}