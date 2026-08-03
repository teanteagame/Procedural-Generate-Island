using System.Collections.Generic;
using UnityEngine;

namespace TNT
{
    public class ResourceEffector : Effector
    {
        public bool block;
        public List<ResourceObject> allowResource;

        public override void Execute(GenerationContext ctx)
        {
            ctx.resourceZones.Add(new ResourceZone { inverseMatrix = transform.worldToLocalMatrix, extents = effectSize * 0.5f, block = block, allowed = allowResource });
        }

        public override void OnDrawGizmos()
        {
            Gizmos.color = block ? Color.red : Color.cyan;
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(effectSize.x, 10, effectSize.y));
            Gizmos.matrix = oldMatrix;
        }
    }
}