using System.Collections.Generic;
using UnityEngine;

namespace TNT
{   
    public class StructuresGenerator : MonoBehaviour
    {
        public StructureGroup[] groups;

        public void Generate(GenerationContext ctx)
        {
            if (groups == null || groups.Length == 0) return;

            System.Random prng = new System.Random(ctx.seed);
            TerrainData tData = Terrain.activeTerrain.terrainData;
            Vector3 tSize = tData.size;
            Vector3 tOrigin = Terrain.activeTerrain.transform.position;

            Transform masterParent = new GameObject("Structures").transform;
            masterParent.SetParent(transform);

            List<Vector2> gridCells = new List<Vector2>();
            int gridDivs = 20;
            float cX = tSize.x / gridDivs, cZ = tSize.z / gridDivs;
            for (int x = 0; x < gridDivs; x++) for (int z = 0; z < gridDivs; z++) gridCells.Add(new Vector2(x * cX, z * cZ));

            for (int i = 0; i < gridCells.Count; i++)
            {
                Vector2 temp = gridCells[i];
                int r = prng.Next(i, gridCells.Count);
                gridCells[i] = gridCells[r];
                gridCells[r] = temp;
            }
            int gridIdx = 0;
            List<int> validIndices = new List<int>();

            for (int i = 0; i < groups.Length; i++)
            {
                StructureGroup group = groups[i];
                int maxGroups = (int)group.groupCountRange.y;
                int minGroups = (int)group.groupCountRange.x;
                int maxObjects = (int)group.objectCountRange.y;
                int minObjects = (int)group.objectCountRange.x;
                int currentGroups = 0;
                int consecutiveFails = 0;

                while (currentGroups < maxGroups && consecutiveFails < 50)
                {
                    bool groupSpawned = false;
                    Transform instanceParent = new GameObject($"{group.spawnType}_{group.selectType}_Group_{i}_{currentGroups}").transform;
                    instanceParent.SetParent(masterParent);

                    int[] spawnCounts = new int[group.structureObjects.Length];
                    int[] failedPlacements = new int[group.structureObjects.Length];

                    if (group.spawnType == StructureGroup.SpawnType.Focusing && group.structureObjects.Length > 0)
                    {
                        if (TryFindValidPosition(group.structureObjects[0], prng, tData, tSize, ctx.occupiedFootprints, gridCells, ref gridIdx, cX, cZ, out Vector3 center, out Quaternion rot, 100))
                        {
                            SpawnObject(group.structureObjects[0], center, rot, tOrigin, ctx, instanceParent, tData, tSize);
                            spawnCounts[0]++;
                            int absoluteCursor = 1;
                            int currentSubSpawns = 0;
                            int targetSubSpawnsMax = maxObjects - 1;
                            int subFails = 0;

                            while (currentSubSpawns < targetSubSpawnsMax && subFails < 50)
                            {
                                int sIdx = -1;
                                if (group.selectType == StructureGroup.SelectType.Index)
                                {
                                    for (int k = 0; k < group.structureObjects.Length; k++)
                                    {
                                        int testIdx = (absoluteCursor + k) % group.structureObjects.Length;
                                        if ((group.structureObjects[testIdx].maxCount <= 0 || spawnCounts[testIdx] < group.structureObjects[testIdx].maxCount) && failedPlacements[testIdx] < 5) { sIdx = testIdx; break; }
                                    }
                                }
                                else
                                {
                                    validIndices.Clear();
                                    for (int v = 0; v < group.structureObjects.Length; v++) if ((group.structureObjects[v].maxCount <= 0 || spawnCounts[v] < group.structureObjects[v].maxCount) && failedPlacements[v] < 5) validIndices.Add(v);
                                    if (validIndices.Count > 0) sIdx = validIndices[prng.Next(0, validIndices.Count)];
                                }

                                if (sIdx == -1) break;
                                StructureObject subObj = group.structureObjects[sIdx];
                                float diagonal = subObj.spaceSize.magnitude;
                                bool placed = false;

                                for (int attempt = 0; attempt < 100; attempt++)
                                {
                                    float angle = (float)prng.NextDouble() * Mathf.PI * 2f;
                                    Vector3 offsetPos = center + new Vector3(Mathf.Cos(angle) * diagonal, 0, Mathf.Sin(angle) * diagonal);
                                    if (ValidatePosition(subObj, offsetPos, prng, tData, tSize, ctx.occupiedFootprints, out Vector3 finalPos, out Quaternion subRot)) { SpawnObject(subObj, finalPos, subRot, tOrigin, ctx, instanceParent, tData, tSize); spawnCounts[sIdx]++; placed = true; break; }
                                }

                                if (placed) { currentSubSpawns++; subFails = 0; }
                                else { failedPlacements[sIdx]++; subFails++; }

                                if (group.selectType == StructureGroup.SelectType.Index) absoluteCursor = sIdx + 1;
                            }
                            if (currentSubSpawns >= (minObjects - 1)) groupSpawned = true;
                        }
                    }
                    else if (group.spawnType == StructureGroup.SpawnType.Scattering && group.structureObjects.Length > 0)
                    {
                        int absoluteCursor = 0;
                        int currentObjSpawns = 0;
                        int objFails = 0;

                        while (currentObjSpawns < maxObjects && objFails < 50)
                        {
                            int sIdx = -1;
                            if (group.selectType == StructureGroup.SelectType.Index)
                            {
                                for (int k = 0; k < group.structureObjects.Length; k++)
                                {
                                    int testIdx = (absoluteCursor + k) % group.structureObjects.Length;
                                    if ((group.structureObjects[testIdx].maxCount <= 0 || spawnCounts[testIdx] < group.structureObjects[testIdx].maxCount) && failedPlacements[testIdx] < 5) { sIdx = testIdx; break; }
                                }
                            }
                            else
                            {
                                validIndices.Clear();
                                for (int v = 0; v < group.structureObjects.Length; v++) if ((group.structureObjects[v].maxCount <= 0 || spawnCounts[v] < group.structureObjects[v].maxCount) && failedPlacements[v] < 5) validIndices.Add(v);
                                if (validIndices.Count > 0) sIdx = validIndices[prng.Next(0, validIndices.Count)];
                            }

                            if (sIdx == -1) break;
                            StructureObject rndObj = group.structureObjects[sIdx];
                            bool placed = false;

                            if (TryFindValidPosition(rndObj, prng, tData, tSize, ctx.occupiedFootprints, gridCells, ref gridIdx, cX, cZ, out Vector3 pos, out Quaternion r, 100))
                            {
                                SpawnObject(rndObj, pos, r, tOrigin, ctx, instanceParent, tData, tSize);
                                spawnCounts[sIdx]++;
                                placed = true;
                            }

                            if (placed) { currentObjSpawns++; objFails = 0; }
                            else { failedPlacements[sIdx]++; objFails++; }

                            if (group.selectType == StructureGroup.SelectType.Index) absoluteCursor = sIdx + 1;
                        }
                        if (currentObjSpawns >= minObjects) groupSpawned = true;
                    }

                    if (groupSpawned) { currentGroups++; consecutiveFails = 0; }
                    else { consecutiveFails++; DestroyImmediate(instanceParent.gameObject); }
                }

                if (currentGroups < minGroups) Debug.LogWarning($"Group {i} spawned {currentGroups}/{minGroups} min required. Map is too crowded or rules are too strict.");
            }
        }

        private bool TryFindValidPosition(StructureObject obj, System.Random prng, TerrainData tData, Vector3 tSize, List<Footprint> globalFootprints, List<Vector2> gridCells, ref int gridIdx, float cX, float cZ, out Vector3 validPos, out Quaternion validRot, int maxAttempts)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                if (gridIdx >= gridCells.Count) gridIdx = 0;
                Vector2 cell = gridCells[gridIdx++];

                Vector3 testPos = new Vector3(cell.x + (float)prng.NextDouble() * cX, 0, cell.y + (float)prng.NextDouble() * cZ);
                if (ValidatePosition(obj, testPos, prng, tData, tSize, globalFootprints, out validPos, out validRot)) return true;
            }
            validPos = Vector3.zero; validRot = Quaternion.identity; return false;
        }

        private bool ValidatePosition(StructureObject obj, Vector3 pos, System.Random prng, TerrainData tData, Vector3 tSize, List<Footprint> globalFootprints, out Vector3 finalPos, out Quaternion finalRot)
        {
            finalPos = Vector3.zero; finalRot = Quaternion.identity;
            if (pos.x < 0 || pos.z < 0 || pos.x > tSize.x || pos.z > tSize.z) return false;

            float normX = pos.x / tSize.x;
            float normZ = pos.z / tSize.z;
            float height = tData.GetInterpolatedHeight(normX, normZ);
            float steepness = tData.GetSteepness(normX, normZ);

            if (height < obj.heightRange.x || height > obj.heightRange.y || steepness < obj.angleRange.x || steepness > obj.angleRange.y) return false;

            float radius = obj.spaceSize.magnitude * 0.5f;
            Vector2 checkPos = new Vector2(pos.x, pos.z);
            for (int i = 0; i < globalFootprints.Count; i++) if (Vector2.Distance(checkPos, globalFootprints[i].center) < (radius + globalFootprints[i].radius)) return false;

            Quaternion terrainRot = Quaternion.identity;
            if (!obj.needFlat)
            {
                Vector3 normal = tData.GetInterpolatedNormal(normX, normZ);
                terrainRot = Quaternion.FromToRotation(Vector3.up, normal);
                float tiltAngle = Quaternion.Angle(Quaternion.identity, terrainRot);
                if (tiltAngle > 15f) terrainRot = Quaternion.Slerp(Quaternion.identity, terrainRot, 15f / tiltAngle);
            }

            finalRot = terrainRot * Quaternion.Euler(0, (float)prng.NextDouble() * 360f, 0);
            finalPos = new Vector3(pos.x, height + obj.offset, pos.z);
            return true;
        }

        private void SpawnObject(StructureObject obj, Vector3 localPos, Quaternion rot, Vector3 tOrigin, GenerationContext ctx, Transform parentGroup, TerrainData tData, Vector3 tSize)
        {
            if (obj.structureModel == null) return;
            Instantiate(obj.structureModel, tOrigin + localPos, rot, parentGroup);
            float radius = obj.spaceSize.magnitude * 0.5f;
            ctx.occupiedFootprints.Add(new Footprint { center = new Vector2(localPos.x, localPos.z), radius = radius });

            if (obj.needFlat)
            {
                int res = tData.heightmapResolution;
                float normX = localPos.x / tSize.x, normZ = localPos.z / tSize.z, targetNormH = (localPos.y - obj.offset) / tSize.y;
                int r = Mathf.CeilToInt((radius / tSize.x) * res);
                int cX = Mathf.RoundToInt(normX * (res - 1)), cY = Mathf.RoundToInt(normZ * (res - 1));
                int sX = Mathf.Max(0, cX - r), sY = Mathf.Max(0, cY - r);
                int eX = Mathf.Min(res - 1, cX + r), eY = Mathf.Min(res - 1, cY + r);
                int w = eX - sX + 1, h = eY - sY + 1;
                float[,] heights = tData.GetHeights(sX, sY, w, h);
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) if ((sX + x - cX) * (sX + x - cX) + (sY + y - cY) * (sY + y - cY) <= r * r) heights[y, x] = targetNormH;
                tData.SetHeights(sX, sY, heights);
            }

            if (obj.affectLayer != null)
            {
                if (!ctx.sharedData.ContainsKey("LayerMarks")) ctx.sharedData["LayerMarks"] = new List<LayerMark>();
                ((List<LayerMark>)ctx.sharedData["LayerMarks"]).Add(new LayerMark { center = new Vector2(localPos.x, localPos.z), radius = radius, layer = obj.affectLayer });
            }
        }
    }

    [System.Serializable]
    public class StructureGroup
    {
        public StructureObject[] structureObjects;
        public Vector2 groupCountRange;
        public Vector2 objectCountRange;
        public SpawnType spawnType;
        public SelectType selectType;
        public enum SpawnType { Focusing, Scattering }
        public enum SelectType { Index, Random }
    }

    [System.Serializable]
    public class StructureObject
    {
        public GameObject structureModel;
        public Vector2 spaceSize;
        public Vector2 heightRange;
        public Vector2 angleRange;
        public float offset;
        public int maxCount = 1;
        public bool needFlat;
        public TerrainLayer affectLayer;
    }

    public struct LayerMark { public Vector2 center; public float radius; public TerrainLayer layer; }
}
