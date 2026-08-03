using UnityEngine;

namespace TNT
{
    public class GenericStructure : Structure
    {
        public override void OnStructureSpawn(GenerationContext ctx)
        {
            base.OnStructureSpawn(ctx);
            if (usedEffectors != null) for (int i = 0; i < usedEffectors.Count; i++) if (usedEffectors[i] != null) usedEffectors[i].Execute(ctx);
        }
    }
}