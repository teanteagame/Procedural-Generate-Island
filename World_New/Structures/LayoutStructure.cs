using System.Collections.Generic;
using UnityEngine;

namespace TNT
{
    public class LayoutStructure : Structure
    {
        public List<ChildSpawnPoint> spawnPoints;

        public override void OnStructureSpawn(GenerationContext ctx)
        {
            if (usedEffectors == null) usedEffectors = new List<Effector>();
            if (spawnPoints != null)
            {
                for (int i = 0; i < spawnPoints.Count; i++)
                {
                    ChildSpawnPoint sp = spawnPoints[i];
                    if (sp.allowedObject != null && sp.allowedObject.Length > 0)
                    {
                        GameObject prefab = sp.allowedObject[Random.Range(0, sp.allowedObject.Length)];
                        if (prefab != null)
                        {
                            Quaternion rot = sp.randomRotation ? Quaternion.Euler(0, Random.Range(0f, 360f), 0) : sp.position.rotation;
                            GameObject child = Instantiate(prefab, sp.position.position, rot, transform);
                            Effector[] childEffectors = child.GetComponentsInChildren<Effector>();
                            if (childEffectors != null) usedEffectors.AddRange(childEffectors);
                        }
                    }
                }
            }

            base.OnStructureSpawn(ctx);
            for (int i = 0; i < usedEffectors.Count; i++) if (usedEffectors[i] != null) usedEffectors[i].Execute(ctx);
        }
    }

    [System.Serializable]
    public struct ChildSpawnPoint
    {
        public GameObject[] allowedObject;
        public Transform position;
        public bool randomRotation;
    }
}