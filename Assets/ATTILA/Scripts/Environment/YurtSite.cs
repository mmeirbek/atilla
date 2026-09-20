using UnityEngine;
using UnityEngine.Rendering;

namespace ATTILA.Environment
{
    /// <summary>
    /// A yurt that can be raised stage by stage: lattice wall (кереге), roof poles (уық) with the
    /// crown (шаңырақ), felt cover and finally the door. Used both for the finished aul and for the
    /// assembly task, so the player sees the real order in which a yurt goes up.
    /// </summary>
    public sealed class YurtSite : MonoBehaviour
    {
        public const int Stages = 4;
        private const float Radius = 2.85f;

        private GameObject wall, roof, door, banner;
        private string bannerId = "red";
        private int stage;

        public int Stage => stage;
        public bool Complete => stage >= Stages;

        public static YurtSite Create(Vector3 position, string banner, int startStage)
        {
            var site = new GameObject("Yurt").AddComponent<YurtSite>();
            if (WorldArt.Root != null) site.transform.SetParent(WorldArt.Root, true);
            site.transform.position = position;
            site.bannerId = banner;
            WorldArt.GroundPatch(site.transform, position, new Vector2(3.4f, 3.5f), MvpArtSet.Instance != null ? MvpArtSet.Instance.path : null);
            site.SetStage(startStage);
            return site;
        }

        /// <summary>Raises the yurt to the given stage; the visuals for every completed stage stay.</summary>
        public void SetStage(int value)
        {
            stage = Mathf.Clamp(value, 0, Stages);
            var art = MvpArtSet.Instance;
            bool covered = stage >= 3;
            if (stage >= 1 && wall == null) wall = BuildWall();
            if (wall != null)
                wall.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(covered ? art?.yurtWall : art?.yurtFrame, new Vector2(3f, 1f), Color.white);
            if (stage >= 2 && roof == null) roof = BuildRoof();
            if (roof != null)
                roof.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(covered ? art?.yurtRoof : art?.yurtPoles, Vector2.one, Color.white);
            if (stage >= Stages && door == null)
            {
                door = WorldArt.Primitive(PrimitiveType.Cube, "Yurt door", transform.position + new Vector3(0, .8f, -2.71f), new Vector3(.95f, 1.6f, .12f));
                door.GetComponent<Renderer>().sharedMaterial = WorldArt.Textured(art?.yurtDoor, Vector2.one, Color.white);
                door.transform.SetParent(transform);
                banner = WorldArt.Prop(art != null ? art.Banner(bannerId) : null, transform.position + new Vector3(2.2f, 0f, -2.4f), .5f);
                if (banner != null) banner.transform.SetParent(transform);
            }
        }

        private GameObject BuildWall()
        {
            var built = WorldArt.Primitive(PrimitiveType.Cylinder, "Yurt wall", transform.position + Vector3.up * .95f, new Vector3(5.4f, .95f, 5.4f));
            built.transform.SetParent(transform);
            return built;
        }

        private GameObject BuildRoof()
        {
            var built = new GameObject("Yurt roof", typeof(MeshFilter), typeof(MeshRenderer));
            built.transform.SetParent(transform, false);
            built.GetComponent<MeshFilter>().sharedMesh = WorldArt.RoofMesh(Radius);
            built.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            return built;
        }
    }
}
