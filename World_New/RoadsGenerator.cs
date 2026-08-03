using System.Collections.Generic;
using UnityEngine;

namespace TNT
{
    public class RoadsGenerator : MonoBehaviour
    {
        public float radius = 200f;
        public float noiseScale = 3f;
        public float noiseMagnitude = 30f;
        public int resolution = 100;
        public float roadWidth = 5f;
        public float minHeight = 2f;
        public float maxHeight = 1000f;
        public float maxSteepness = 30f;
        public float searchStep = 10f;
        public float maxSearchDistance = 300f;
        public int smoothingWindow = 4;
        public int trimPoints = 4;
        public TerrainLayer roadLayer;

        public void Generate(GenerationContext ctx)
        {
            TerrainData tData = Terrain.activeTerrain.terrainData; Vector3 tSize = tData.size; Vector2 center = new Vector2(tSize.x * 0.5f, tSize.z * 0.5f);
            System.Random prng = new System.Random(ctx.seed); float noiseOffset = prng.Next(-10000, 10000);
            float[] rawR = new float[resolution + 1]; bool[] validR = new bool[resolution + 1];

            for (int i = 0; i <= resolution; i++)
            {
                float angle = (i % resolution) / (float)resolution * Mathf.PI * 2f, nX = Mathf.Cos(angle), nY = Mathf.Sin(angle), n = Mathf.PerlinNoise(nX * noiseScale + noiseOffset, nY * noiseScale + noiseOffset) * 2f - 1f, idealR = radius + n * noiseMagnitude;
                float foundR = -1f;

                for (float step = 0; step <= maxSearchDistance; step += searchStep)
                {
                    if (IsValidPoint(center.x + nX * (idealR - step), center.y + nY * (idealR - step), tData, tSize)) { foundR = idealR - step; break; }
                    if (step > 0 && IsValidPoint(center.x + nX * (idealR + step), center.y + nY * (idealR + step), tData, tSize)) { foundR = idealR + step; break; }
                }
                if (foundR > 0) { rawR[i] = foundR; validR[i] = true; }
            }

            bool[] trimmed = new bool[resolution + 1];
            for (int i = 0; i <= resolution; i++)
            {
                int next = (i + 1) % resolution;
                if (validR[i] && validR[next] && Mathf.Abs(rawR[i] - rawR[next]) > searchStep * 1.5f) for (int k = 0; k <= trimPoints; k++) { trimmed[(i - k + resolution) % resolution] = true; trimmed[(next + k) % resolution] = true; }
                else if (validR[i] && !validR[next]) for (int k = 0; k <= trimPoints; k++) trimmed[(i - k + resolution) % resolution] = true;
                else if (!validR[i] && validR[next]) for (int k = 0; k <= trimPoints; k++) trimmed[(next + k) % resolution] = true;
            }
            for (int i = 0; i <= resolution; i++) if (trimmed[i]) validR[i] = false;

            for (int i = 0; i <= resolution * 2; i++)
            {
                int idx = i % resolution, nextIdx = (i + 1) % resolution;
                if (validR[idx] && !validR[nextIdx])
                {
                    int endGap = -1, gapLen = 0; for (int j = 1; j <= resolution; j++) { int searchIdx = (i + j) % resolution; if (validR[searchIdx]) { endGap = searchIdx; gapLen = j; break; } }
                    if (endGap != -1 && gapLen <= trimPoints * 3 + 4)
                    {
                        float startR = rawR[idx], endR = rawR[endGap]; bool gapResolved = false; float[] finalGapR = new float[gapLen - 1];
                        Vector3 endPt = new Vector3(center.x + Mathf.Cos(endGap / (float)resolution * Mathf.PI * 2f) * endR, 0, center.y + Mathf.Sin(endGap / (float)resolution * Mathf.PI * 2f) * endR);

                        for (float offset = 0; offset <= maxSearchDistance; offset += searchStep)
                        {
                            bool inValid = true, outValid = true; float[] inR = new float[gapLen - 1], outR = new float[gapLen - 1];
                            Vector3 lastIn = new Vector3(center.x + Mathf.Cos(idx / (float)resolution * Mathf.PI * 2f) * startR, 0, center.y + Mathf.Sin(idx / (float)resolution * Mathf.PI * 2f) * startR); Vector3 lastOut = lastIn;

                            for (int j = 1; j < gapLen; j++)
                            {
                                float t = (float)j / gapLen, smoothT = t * t * (3f - 2f * t), baseR = Mathf.Lerp(startR, endR, smoothT), arc = Mathf.Sin(t * Mathf.PI) * offset, rIn = baseR - arc, rOut = baseR + arc, angle = ((i + j) % resolution) / (float)resolution * Mathf.PI * 2f;
                                if (inValid) { Vector3 pt = new Vector3(center.x + Mathf.Cos(angle) * rIn, 0, center.y + Mathf.Sin(angle) * rIn); if (!IsValidPoint(pt.x, pt.z, tData, tSize) || !IsSegmentValid(lastIn, pt, tData, tSize)) inValid = false; else { inR[j - 1] = rIn; lastIn = pt; } }
                                if (outValid) { Vector3 pt = new Vector3(center.x + Mathf.Cos(angle) * rOut, 0, center.y + Mathf.Sin(angle) * rOut); if (!IsValidPoint(pt.x, pt.z, tData, tSize) || !IsSegmentValid(lastOut, pt, tData, tSize)) outValid = false; else { outR[j - 1] = rOut; lastOut = pt; } }
                            }

                            if (inValid && !IsSegmentValid(lastIn, endPt, tData, tSize)) inValid = false;
                            if (outValid && !IsSegmentValid(lastOut, endPt, tData, tSize)) outValid = false;
                            if (inValid) { finalGapR = inR; gapResolved = true; break; }
                            if (outValid) { finalGapR = outR; gapResolved = true; break; }
                        }
                        if (gapResolved) for (int j = 1; j < gapLen; j++) { int currIdx = (i + j) % resolution; rawR[currIdx] = finalGapR[j - 1]; validR[currIdx] = true; }
                    }
                    i += gapLen > 0 ? gapLen - 1 : 0;
                }
            }

            rawR[resolution] = rawR[0]; validR[resolution] = validR[0]; float[] smoothR = new float[resolution + 1];
            for (int i = 0; i <= resolution; i++)
            {
                if (!validR[i]) continue; float sum = 0; int count = 0;
                for (int j = -smoothingWindow; j <= smoothingWindow; j++) { int idx = (i + j + resolution) % resolution; if (validR[idx]) { sum += rawR[idx]; count++; } }
                smoothR[i] = count > 0 ? sum / count : rawR[i];
            }
            smoothR[resolution] = smoothR[0];

            List<List<Vector3>> paths = new List<List<Vector3>>(); List<Vector3> currentPath = new List<Vector3>(); float maxDist = (radius * Mathf.PI * 2f / resolution) * 4f;
            for (int i = 0; i <= resolution; i++)
            {
                if (validR[i])
                {
                    float angle = (i % resolution) / (float)resolution * Mathf.PI * 2f, px = center.x + Mathf.Cos(angle) * smoothR[i], pz = center.y + Mathf.Sin(angle) * smoothR[i];
                    Vector3 pt = new Vector3(px, tData.GetInterpolatedHeight(px / tSize.x, pz / tSize.z), pz);
                    bool breakPath = currentPath.Count > 0 && (Vector3.Distance(currentPath[currentPath.Count - 1], pt) > maxDist || !IsSegmentValid(currentPath[currentPath.Count - 1], pt, tData, tSize));
                    if (breakPath) { if (currentPath.Count > 1) paths.Add(new List<Vector3>(currentPath)); currentPath.Clear(); }
                    currentPath.Add(pt);
                }
                else { if (currentPath.Count > 1) paths.Add(new List<Vector3>(currentPath)); currentPath.Clear(); }
            }
            if (currentPath.Count > 1) paths.Add(new List<Vector3>(currentPath));

            if (paths.Count > 1 && validR[0] && validR[resolution])
            {
                Vector3 firstPt = paths[0][0], lastPt = paths[paths.Count - 1][paths[paths.Count - 1].Count - 1];
                if (Vector3.Distance(firstPt, lastPt) <= maxDist && IsSegmentValid(lastPt, firstPt, tData, tSize)) { paths[paths.Count - 1].AddRange(paths[0]); paths.RemoveAt(0); }
            }

            if (paths.Count == 0) return;

            if (ctx.roadSegments == null) ctx.roadSegments = new List<Footprint>();
            for (int p = 0; p < paths.Count; p++) { for (int i = 0; i < paths[p].Count - 1; i++) { Vector3 p1 = paths[p][i], p2 = paths[p][i + 1]; ctx.roadSegments.Add(new Footprint { isSegment = true, p1 = new Vector2(p1.x, p1.z), p2 = new Vector2(p2.x, p2.z), radius = roadWidth }); } }

            if (!ctx.sharedData.ContainsKey("LayerMarks")) ctx.sharedData["LayerMarks"] = new List<LayerMark>();
            List<LayerMark> marks = (List<LayerMark>)ctx.sharedData["LayerMarks"]; int hRes = tData.heightmapResolution; float[,] heights = tData.GetHeights(0, 0, hRes, hRes);

            for (int p = 0; p < paths.Count; p++)
            {
                List<Vector3> waypoints = paths[p];
                for (int i = 0; i < waypoints.Count - 1; i++)
                {
                    Vector3 p1 = waypoints[i], p2 = waypoints[i + 1]; int segments = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(p1, p2) / (roadWidth * 0.5f)));
                    for (int j = 0; j <= segments; j++)
                    {
                        float t = (float)j / segments; Vector3 pt = Vector3.Lerp(p1, p2, t);
                        if (roadLayer != null) marks.Add(new LayerMark { center = new Vector2(pt.x, pt.z), radius = roadWidth, layer = roadLayer });
                        ctx.occupiedFootprints.Add(new Footprint { center = new Vector2(pt.x, pt.z), radius = roadWidth });
                        int hX = Mathf.Clamp(Mathf.RoundToInt((pt.x / tSize.x) * (hRes - 1)), 0, hRes - 1), hY = Mathf.Clamp(Mathf.RoundToInt((pt.z / tSize.z) * (hRes - 1)), 0, hRes - 1), cr = Mathf.CeilToInt((roadWidth / tSize.x) * hRes);
                        int sX = Mathf.Max(0, hX - cr), sY = Mathf.Max(0, hY - cr), eX = Mathf.Min(hRes - 1, hX + cr), eY = Mathf.Min(hRes - 1, hY + cr); float targetH = pt.y / tSize.y;
                        for (int y = sY; y <= eY; y++) for (int x = sX; x <= eX; x++) { float dist = Mathf.Sqrt((x - hX) * (x - hX) + (y - hY) * (y - hY)); if (dist <= cr) { float b = dist < cr * 0.5f ? 1f : Mathf.SmoothStep(1f, 0f, (dist - cr * 0.5f) / (cr * 0.5f)); heights[y, x] = Mathf.Lerp(heights[y, x], targetH, b); } }
                    }
                }
            }
            tData.SetHeights(0, 0, heights);

            if (ctx.heightMap != null) { int mapW = ctx.heightMap.GetLength(0), mapH = ctx.heightMap.GetLength(1); for (int y = 0; y < hRes && y < mapH; y++) for (int x = 0; x < hRes && x < mapW; x++) ctx.heightMap[x, y] = heights[y, x]; }
        }

        public void DrawPath(Vector3 start, Vector3 end, GenerationContext ctx)
        {
            TerrainData tData = Terrain.activeTerrain.terrainData; Vector3 tSize = tData.size; int hRes = tData.heightmapResolution; float[,] heights = tData.GetHeights(0, 0, hRes, hRes);
            if (!ctx.sharedData.ContainsKey("LayerMarks")) ctx.sharedData["LayerMarks"] = new List<LayerMark>();
            List<LayerMark> marks = (List<LayerMark>)ctx.sharedData["LayerMarks"]; List<Vector3> waypoints = FindDrivewayPath(start, end, tData, tSize);

            for (int w = 0; w < waypoints.Count - 1; w++)
            {
                Vector3 p1 = waypoints[w], p2 = waypoints[w + 1]; int segments = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(p1, p2) / (roadWidth * 0.5f)));
                for (int j = 0; j <= segments; j++)
                {
                    float t = (float)j / segments; Vector3 pt = Vector3.Lerp(p1, p2, t); pt.y = tData.GetInterpolatedHeight(pt.x / tSize.x, pt.z / tSize.z);
                    if (roadLayer != null) marks.Add(new LayerMark { center = new Vector2(pt.x, pt.z), radius = roadWidth, layer = roadLayer });
                    ctx.occupiedFootprints.Add(new Footprint { center = new Vector2(pt.x, pt.z), radius = roadWidth });
                    int hX = Mathf.Clamp(Mathf.RoundToInt((pt.x / tSize.x) * (hRes - 1)), 0, hRes - 1), hY = Mathf.Clamp(Mathf.RoundToInt((pt.z / tSize.z) * (hRes - 1)), 0, hRes - 1), cr = Mathf.CeilToInt((roadWidth / tSize.x) * hRes);
                    int sX = Mathf.Max(0, hX - cr), sY = Mathf.Max(0, hY - cr), eX = Mathf.Min(hRes - 1, hX + cr), eY = Mathf.Min(hRes - 1, hY + cr); float targetH = pt.y / tSize.y;
                    for (int y = sY; y <= eY; y++) for (int x = sX; x <= eX; x++) { float dist = Mathf.Sqrt((x - hX) * (x - hX) + (y - hY) * (y - hY)); if (dist <= cr) { float b = dist < cr * 0.5f ? 1f : Mathf.SmoothStep(1f, 0f, (dist - cr * 0.5f) / (cr * 0.5f)); heights[y, x] = Mathf.Lerp(heights[y, x], targetH, b); } }
                }
            }
            tData.SetHeights(0, 0, heights);
            if (ctx.heightMap != null) { int mapW = ctx.heightMap.GetLength(0), mapH = ctx.heightMap.GetLength(1); for (int y = 0; y < hRes && y < mapH; y++) for (int x = 0; x < hRes && x < mapW; x++) ctx.heightMap[x, y] = heights[y, x]; }
        }

        private List<Vector3> FindDrivewayPath(Vector3 start, Vector3 end, TerrainData tData, Vector3 tSize)
        {
            if (IsSegmentValid(start, end, tData, tSize)) return new List<Vector3> { start, end };
            float step = searchStep; Vector2Int s = new Vector2Int(Mathf.RoundToInt(start.x / step), Mathf.RoundToInt(start.z / step)), e = new Vector2Int(Mathf.RoundToInt(end.x / step), Mathf.RoundToInt(end.z / step));
            List<Vector2Int> open = new List<Vector2Int>() { s }; HashSet<Vector2Int> closed = new HashSet<Vector2Int>();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>() { { s, 0 } }, fScore = new Dictionary<Vector2Int, float>() { { s, Vector2Int.Distance(s, e) } };

            while (open.Count > 0 && closed.Count < 2000)
            {
                Vector2Int curr = open[0]; float minF = fScore.ContainsKey(curr) ? fScore[curr] : float.MaxValue;
                for (int i = 1; i < open.Count; i++) { float f = fScore.ContainsKey(open[i]) ? fScore[open[i]] : float.MaxValue; if (f < minF) { minF = f; curr = open[i]; } }
                if (curr == e || Vector2Int.Distance(curr, e) < 2)
                {
                    List<Vector3> path = new List<Vector3>() { end }; Vector2Int trace = curr;
                    while (cameFrom.ContainsKey(trace)) { trace = cameFrom[trace]; path.Add(new Vector3(trace.x * step, tData.GetInterpolatedHeight((trace.x * step) / tSize.x, (trace.y * step) / tSize.z), trace.y * step)); }
                    path.Reverse(); path[0] = start; return path;
                }
                open.Remove(curr); closed.Add(curr);
                Vector2Int[] dirs = new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 1) };
                foreach (Vector2Int d in dirs)
                {
                    Vector2Int n = curr + d; if (closed.Contains(n)) continue;
                    Vector3 nW = new Vector3(n.x * step, 0, n.y * step); if (!IsValidPoint(nW.x, nW.z, tData, tSize)) continue;
                    float tG = gScore[curr] + Vector2Int.Distance(curr, n);
                    if (!gScore.ContainsKey(n) || tG < gScore[n]) { cameFrom[n] = curr; gScore[n] = tG; fScore[n] = tG + Vector2Int.Distance(n, e); if (!open.Contains(n)) open.Add(n); }
                }
            }
            return new List<Vector3> { start, end };
        }

        private bool IsValidPoint(float x, float z, TerrainData tData, Vector3 tSize)
        {
            if (x < 0 || x >= tSize.x || z < 0 || z >= tSize.z) return false; float normX = x / tSize.x, normZ = z / tSize.z, h = tData.GetInterpolatedHeight(normX, normZ);
            if (h < minHeight || h > maxHeight || tData.GetSteepness(normX, normZ) > maxSteepness) return false;
            return true;
        }

        private bool IsSegmentValid(Vector3 p1, Vector3 p2, TerrainData tData, Vector3 tSize)
        {
            float dist = Vector3.Distance(p1, p2); int steps = Mathf.Max(1, Mathf.CeilToInt(dist / searchStep));
            for (int i = 1; i < steps; i++) { Vector3 pt = Vector3.Lerp(p1, p2, (float)i / steps); if (!IsValidPoint(pt.x, pt.z, tData, tSize)) return false; }
            return true;
        }
    }
}