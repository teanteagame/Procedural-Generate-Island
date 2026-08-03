using System.Collections.Generic;
using UnityEngine;

namespace TNT
{
    public struct Footprint { public Vector2 center; public float radius; public bool isSegment; public Vector2 p1; public Vector2 p2; }
    public struct LayerMark { public Vector2 center; public float radius; public TerrainLayer layer; public bool isSegment; public Vector2 p1; public Vector2 p2; }

    public class ResourceZone { public Matrix4x4 inverseMatrix; public Vector2 extents; public bool block; public List<ResourceObject> allowed; public bool Contains(Vector3 wPos) { Vector3 l = inverseMatrix.MultiplyPoint3x4(wPos); return Mathf.Abs(l.x) <= extents.x && Mathf.Abs(l.z) <= extents.y; } }
    public class DetailZone { public Matrix4x4 inverseMatrix; public Vector2 extents; public bool block; public List<DetailTexture> allowed; public bool Contains(Vector3 wPos) { Vector3 l = inverseMatrix.MultiplyPoint3x4(wPos); return Mathf.Abs(l.x) <= extents.x && Mathf.Abs(l.z) <= extents.y; } }

    public class GenerationContext
    {
        public int seed;
        public float[,] heightMap;
        public List<Footprint> occupiedFootprints = new List<Footprint>();
        public List<Footprint> roadSegments = new List<Footprint>();
        public Dictionary<string, object> sharedData = new Dictionary<string, object>();
        public List<ResourceZone> resourceZones = new List<ResourceZone>();
        public List<DetailZone> detailZones = new List<DetailZone>();
    }

    public class WorldGenerator : MonoBehaviour
    {
        public string worldSeed;
        private HeightsGenerator heightsGenerator;
        private RoadsGenerator roadsGenerator;
        private TexturesGenerator texturesGenerator;
        private StructuresGenerator structuresGenerator;
        private ResourcesGenerator resourcesGenerator;
        private DetailsGenerator detailsGenerator;

        private void Awake()
        {
            heightsGenerator = GetComponent<HeightsGenerator>();
            roadsGenerator = GetComponent<RoadsGenerator>();
            texturesGenerator = GetComponent<TexturesGenerator>();
            structuresGenerator = GetComponent<StructuresGenerator>();
            resourcesGenerator = GetComponent<ResourcesGenerator>();
            detailsGenerator = GetComponent<DetailsGenerator>();
        }

        private void Start()
        {
            int seed = string.IsNullOrEmpty(worldSeed) ? Random.Range(0, 999999) : worldSeed.GetHashCode(); Random.InitState(seed);
            GenerationContext ctx = new GenerationContext { seed = seed };

            if (heightsGenerator != null) heightsGenerator.Generate(ctx);
            if (roadsGenerator != null) roadsGenerator.Generate(ctx);
            if (structuresGenerator != null) structuresGenerator.Generate(ctx);
            if (resourcesGenerator != null) resourcesGenerator.Generate(ctx);
            if (detailsGenerator != null) detailsGenerator.Generate(ctx);
            if (texturesGenerator != null) texturesGenerator.Generate(ctx);
        }
    }
}