using UnityEngine;

namespace TNT
{
    public class FlattenEffector : Effector
    {
        public float flatOffset;
        public float smoothRadius;

        public override void Execute(GenerationContext ctx)
        {
            TerrainData tData = Terrain.activeTerrain.terrainData;
            Vector3 tSize = tData.size;
            int res = tData.heightmapResolution;
            Vector3 tPos = Terrain.activeTerrain.transform.position;
            Vector3 localPos = transform.position - tPos;

            float targetNormH = (localPos.y + flatOffset) / tSize.y;
            int cX = Mathf.RoundToInt((localPos.x / tSize.x) * (res - 1));
            int cY = Mathf.RoundToInt((localPos.z / tSize.z) * (res - 1));

            float boundsRadius = effectSize.magnitude * 0.5f + smoothRadius;
            int extX = Mathf.CeilToInt((boundsRadius / tSize.x) * res), extY = Mathf.CeilToInt((boundsRadius / tSize.z) * res);
            int sX = Mathf.Max(0, cX - extX), sY = Mathf.Max(0, cY - extY);
            int eX = Mathf.Min(res - 1, cX + extX), eY = Mathf.Min(res - 1, cY + extY);
            int w = eX - sX + 1, h = eY - sY + 1;

            float[,] heights = tData.GetHeights(sX, sY, w, h);
            float halfX = effectSize.x * 0.5f, halfY = effectSize.y * 0.5f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float wX = ((sX + x) / (float)(res - 1)) * tSize.x + tPos.x, wZ = ((sY + y) / (float)(res - 1)) * tSize.z + tPos.z;
                    Vector3 localPt = transform.InverseTransformPoint(new Vector3(wX, transform.position.y, wZ));
                    float dX = Mathf.Max(0, Mathf.Abs(localPt.x) - halfX), dZ = Mathf.Max(0, Mathf.Abs(localPt.z) - halfY);
                    float dist = Mathf.Sqrt(dX * dX + dZ * dZ);

                    if (dist <= smoothRadius)
                    {
                        float t = smoothRadius > 0f ? Mathf.SmoothStep(0f, 1f, 1f - (dist / smoothRadius)) : 1f;
                        heights[y, x] = Mathf.Lerp(heights[y, x], targetNormH, t);
                        if (ctx.heightMap != null) ctx.heightMap[sX + x, sY + y] = heights[y, x];
                    }
                }
            }
            tData.SetHeights(sX, sY, heights);
        }

        public override void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position + Vector3.up * flatOffset, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(effectSize.x, 0.1f, effectSize.y));
            if (smoothRadius > 0) { Gizmos.color = new Color(1f, 1f, 0f, 0.3f); Gizmos.DrawWireCube(Vector3.zero, new Vector3(effectSize.x + smoothRadius * 2f, 0.1f, effectSize.y + smoothRadius * 2f)); }
            Gizmos.matrix = oldMatrix;
        }
    }
}