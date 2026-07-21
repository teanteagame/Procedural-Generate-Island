using System.Collections.Generic;
using UnityEngine;

namespace TNT
{
    public enum SpawnMode { Fill, Noise }

    public class ResourcesGenerator : MonoBehaviour
    {
        public ResourceObject[] resources;
        public float pointSpacing = 5f;
        public float chunkSize = 50f;

        public void Generate(GenerationContext ctx)
        {
            if (resources == null || resources.Length == 0) return;
            if (pointSpacing <= 0.5) pointSpacing = 0.5f;
            System.Random prng = new System.Random(ctx.seed);
            TerrainData tData = Terrain.activeTerrain.terrainData;
            Vector3 tSize = tData.size;
            Vector3 tOrigin = Terrain.activeTerrain.transform.position;

            System.Array.Sort(resources, (a, b) => b.priority.CompareTo(a.priority));
            List<ResourceObject> validCandidates = new List<ResourceObject>();
            Dictionary<Vector2Int, Transform> chunks = new Dictionary<Vector2Int, Transform>();

            Vector2[] noiseOffsets = new Vector2[resources.Length];
            for (int i = 0; i < resources.Length; i++) noiseOffsets[i] = new Vector2(prng.Next(-10000, 10000), prng.Next(-10000, 10000));

            Transform masterParent = new GameObject("Resources").transform;
            masterParent.SetParent(transform);

            for (float z = 0; z < tSize.z; z += pointSpacing)
            {
                for (float x = 0; x < tSize.x; x += pointSpacing)
                {
                    float jX = x + (float)(prng.NextDouble() * pointSpacing * 0.8f);
                    float jZ = z + (float)(prng.NextDouble() * pointSpacing * 0.8f);
                    if (jX > tSize.x || jZ > tSize.z) continue;

                    float nX = jX / tSize.x, nZ = jZ / tSize.z;
                    float h = tData.GetInterpolatedHeight(nX, nZ);
                    float s = tData.GetSteepness(nX, nZ);

                    validCandidates.Clear();
                    float maxPrio = -1f;

                    for (int i = 0; i < resources.Length; i++)
                    {
                        ResourceObject res = resources[i];
                        if (res.priority < maxPrio) break;

                        if (h >= res.heightRange.x && h <= res.heightRange.y && s >= res.angleRange.x && s <= res.angleRange.y)
                        {
                            bool canSpawn = true;
                            if (res.mode == SpawnMode.Noise)
                            {
                                float noiseVal = Mathf.PerlinNoise((tOrigin.x + jX) * res.noiseScale + noiseOffsets[i].x, (tOrigin.z + jZ) * res.noiseScale + noiseOffsets[i].y);
                                if (noiseVal < res.noiseThreshold) canSpawn = false;
                            }

                            if (canSpawn && prng.NextDouble() <= res.spawnRate) { validCandidates.Add(res); maxPrio = res.priority; }
                        }
                    }

                    if (validCandidates.Count > 0)
                    {
                        ResourceObject chosen = validCandidates[prng.Next(validCandidates.Count)];
                        if (IsValid(chosen, jX, jZ, ctx.occupiedFootprints))
                        {
                            Vector2Int chunkKey = new Vector2Int(Mathf.FloorToInt(jX / chunkSize), Mathf.FloorToInt(jZ / chunkSize));
                            if (!chunks.TryGetValue(chunkKey, out Transform chunkParent))
                            {
                                chunkParent = new GameObject($"Chunk_{chunkKey.x}_{chunkKey.y}").transform;
                                chunkParent.SetParent(masterParent);
                                chunks.Add(chunkKey, chunkParent);
                            }

                            Vector3 normal = tData.GetInterpolatedNormal(nX, nZ);
                            Spawn(chosen, new Vector3(jX, h, jZ), normal, tOrigin, prng, ctx, chunkParent);
                        }
                    }
                }
            }
        }

        private bool IsValid(ResourceObject res, float x, float z, List<Footprint> fp)
        {
            float radius = res.spaceSize.magnitude * 0.5f;
            Vector2 pos = new Vector2(x, z);
            for (int i = 0; i < fp.Count; i++) if (Vector2.Distance(pos, fp[i].center) < (radius + fp[i].radius)) return false;
            return true;
        }

        private void Spawn(ResourceObject res, Vector3 pos, Vector3 normal, Vector3 tOrigin, System.Random prng, GenerationContext ctx, Transform parentGroup)
        {
            if (res.resourceModel == null) return;
            Quaternion terrainRot = Quaternion.FromToRotation(Vector3.up, normal);
            float tiltAngle = Quaternion.Angle(Quaternion.identity, terrainRot);
            if (tiltAngle > 15f) terrainRot = Quaternion.Slerp(Quaternion.identity, terrainRot, 15f / tiltAngle);
            Quaternion finalRot = terrainRot * Quaternion.Euler(0, (float)prng.NextDouble() * 360f, 0);
            float sX = Mathf.Lerp(res.minSize.x, res.maxSize.x, (float)prng.NextDouble());
            float sY = Mathf.Lerp(res.minSize.y, res.maxSize.y, (float)prng.NextDouble());
            float sZ = Mathf.Lerp(res.minSize.z, res.maxSize.z, (float)prng.NextDouble());
            pos.y += res.offset;
            GameObject go = Instantiate(res.resourceModel, tOrigin + pos, finalRot, parentGroup);
            go.transform.localScale = new Vector3(sX, sY, sZ);
            ctx.occupiedFootprints.Add(new Footprint { center = new Vector2(pos.x, pos.z), radius = res.spaceSize.magnitude * 0.5f });
        }
    }

    [System.Serializable]
    public class ResourceObject
    {
        public GameObject resourceModel;
        public SpawnMode mode;
        public float noiseScale = 0.05f;
        public float noiseThreshold = 0.5f;
        [Range(0, 1)] public float spawnRate;
        [Range(0, 1)] public float priority = 0.5f;
        public Vector2 spaceSize;
        public Vector2 heightRange;
        public Vector2 angleRange;
        public Vector3 minSize, maxSize;
        public float offset;
    }
}
