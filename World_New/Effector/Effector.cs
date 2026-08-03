using UnityEngine;

namespace TNT
{
    public class Effector : MonoBehaviour
    {
        public Vector2 effectSize;

        public virtual void Execute(GenerationContext ctx) { }

        public virtual void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(effectSize.x, 10, effectSize.y));
            Gizmos.matrix = oldMatrix;
        }
    }
}