using System.Collections.Generic;
using UnityEngine;

namespace TNT
{
    public class GenerationContext
    {
        public int seed;
        public float[,] heightMap;
        public List<Footprint> occupiedFootprints = new List<Footprint>();
        public Dictionary<string, object> sharedData = new Dictionary<string, object>();
    }

    public struct Footprint { public Vector2 center; public float radius; }

    public class WorldGenerator : MonoBehaviour
    {
        public string worldSeed;

        private HeightsGenerator heightsGenerator;
        private TexturesGenerator texturesGenerator;
        private StructuresGenerator structuresGenerator;
        private ResourcesGenerator resourcesGenerator;
        private DetailsGenerator detailsGenerator;

        private void Awake()
        {
            heightsGenerator = GetComponent<HeightsGenerator>();
            texturesGenerator = GetComponent<TexturesGenerator>();
            structuresGenerator = GetComponent<StructuresGenerator>();
            resourcesGenerator = GetComponent<ResourcesGenerator>();
            detailsGenerator = GetComponent<DetailsGenerator>();
        }

        private void Start()
        {
            int seed = string.IsNullOrEmpty(worldSeed) ? Random.Range(0, 999999) : worldSeed.GetHashCode();
            Random.InitState(seed);
            GenerationContext ctx = new GenerationContext { seed = seed };

            if (heightsGenerator != null) heightsGenerator.Generate(ctx);
            if (structuresGenerator != null) structuresGenerator.Generate(ctx);
            if (resourcesGenerator != null) resourcesGenerator.Generate(ctx);
            if (detailsGenerator != null) detailsGenerator.Generate(ctx);
            if (texturesGenerator != null) texturesGenerator.Generate(ctx);
        }
    }
}
