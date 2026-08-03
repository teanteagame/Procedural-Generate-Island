using System.Collections.Generic;
using UnityEngine;

namespace TNT
{
    public class LayerEffector : Effector
    {
        public TerrainLayer effectLayer;

        public override void Execute(GenerationContext ctx)
        {
            if (effectLayer == null) return;
            if (!ctx.sharedData.ContainsKey("LayerMarks")) ctx.sharedData["LayerMarks"] = new List<LayerMark>();
            Vector3 localPos = transform.position - Terrain.activeTerrain.transform.position;
            float radius = Mathf.Max(effectSize.x, effectSize.y) * 0.5f;
            ((List<LayerMark>)ctx.sharedData["LayerMarks"]).Add(new LayerMark { center = new Vector2(localPos.x, localPos.z), radius = radius, layer = effectLayer });
        }

        public override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
        }
    }
}