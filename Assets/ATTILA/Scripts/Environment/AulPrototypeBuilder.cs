using ATTILA.CameraSystem;
using ATTILA.Player;
using ATTILA.UI;
using ATTILA.Data;
using ATTILA.NPC;
using ATTILA.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.Environment
{
    public sealed class AulPrototypeBuilder : MonoBehaviour
    {
        private readonly Color groundColor = new(.62f, .75f, .46f);
        private EnvironmentArtSet environmentArt;
        private readonly System.Collections.Generic.List<Mesh> groundMeshes = new();
        private void Start()
        {
            environmentArt = Resources.Load<EnvironmentArtSet>("Art/AulEnvironment");
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.90f, .93f, .86f);
            RenderSettings.fog = false;
            BuildGround(); BuildPath(); BuildYurts(); BuildNature(); BuildFence(); BuildNpcs(); var player = BuildPlayer(); BuildCamera(player.transform); BuildHud();
            var stage2 = gameObject.AddComponent<Stage2Game>(); stage2.Initialize(player.GetComponent<PlayerController>());
        }
        private void BuildGround() { var ground = Primitive(PrimitiveType.Plane, "Ground", Vector3.zero, new Vector3(5.5f, 1, 5.5f), groundColor);  }
        private void BuildPath()
        {
            // Retain the existing walkable surface; the new ribbons are visual overlays only.
            var path = Primitive(PrimitiveType.Cube, "Path", new Vector3(0, .025f, 0), new Vector3(3.5f, .05f, 44), new Color(.83f, .72f, .52f));
            path.GetComponent<Renderer>().enabled = false;
            Vector3[] route = { new(0,0,-22), new(-.2f,0,-16), new(.15f,0,-9), new(0,0,-2), new(-.15f,0,5), new(.1f,0,12), new(0,0,22) };
            PathRibbon("Soft path verge", route, 2.02f, .052f, new Color(.73f,.74f,.46f));
            PathRibbon("Main earth path", route, 1.78f, .056f, new Color(.84f,.74f,.55f));
            PathRibbon("Yurt approach west", new[] { new Vector3(-12,0,5.9f), new Vector3(-7,0,5.2f), new Vector3(0,0,5.5f) }, .85f, .058f, new Color(.84f,.74f,.55f));
            PathRibbon("Yurt approach east", new[] { new Vector3(0,0,5.5f), new Vector3(6,0,6f), new Vector3(12,0,6.9f) }, .85f, .058f, new Color(.84f,.74f,.55f));
        }
        private void BuildYurts()
        {
            CreateYurt(new(-12, 0, 9)); CreateYurt(new(12, 0, 10)); CreateYurt(new(-13, 0, -9)); CreateYurt(new(13, 0, -10));
        }
        private void CreateYurt(Vector3 p)
        {
            var root = new GameObject("Placeholder Yurt"); root.transform.position = p;
            GroundPatch("Yurt earth clearing", p, new Vector2(3.4f, 3.5f), new Color(.78f,.75f,.52f));
            Primitive(PrimitiveType.Cylinder, "Round base", p + Vector3.up * .95f, new Vector3(5.4f, .95f, 5.4f), new Color(.88f, .81f, .67f)).transform.SetParent(root.transform);
            CreateRoof(root.transform);
            Primitive(PrimitiveType.Cube, "Door placeholder", p + new Vector3(0, .8f, -2.71f), new Vector3(.95f, 1.6f, .12f), UiFactory.Primary).transform.SetParent(root.transform);
        }
        private void BuildNature()
        {
            // Sparse groups frame the settlement. The road, NPC approaches and mini-game clearing stay open.
            Vector3[] groves = { new(-17,0,13), new(17,0,14), new(-18,0,-14), new(18,0,-15), new(-5,0,18), new(7,0,-19), new(-15.5f,0,1.5f), new(15.5f,0,-.5f) };
            for (int i = 0; i < groves.Length; i++)
            {
                var p = groves[i];
                GroundPatch("Meadow group", p, new Vector2(2.5f,1.9f), new Color(.58f,.71f,.42f));
                Place(i % 3 == 0 ? environmentArt.smallTree : environmentArt.tree, p, i * 51f);
                Place(environmentArt.bush, p + new Vector3(1.4f,0,.6f), i * 27f);
                Place(environmentArt.smallBush, p + new Vector3(-1.1f,0,-.3f), i * 37f);
                Place(environmentArt.grass, p + new Vector3(.8f,0,-1.2f), i * 43f, 1.2f);
            }
            foreach (var p in new[] { new Vector3(-7,0,-5), new Vector3(7,0,4), new Vector3(-18,0,2), new Vector3(17,0,2) })
            {
                Place(environmentArt.rock, p, 25f);
                Place(environmentArt.smallRock, p + new Vector3(.65f,0,.4f), 140f);
                Place(environmentArt.grass, p + new Vector3(-.6f,0,.3f), 70f);
            }
            Place(environmentArt.logs, new Vector3(-8.5f,0,10f), 15f);
            Place(environmentArt.logs, new Vector3(16.2f,0,-9.5f), -20f);
            foreach (var p in new[] { new Vector3(-2.9f,0,10), new Vector3(3.1f,0,1), new Vector3(-3.3f,0,-5), new Vector3(4.6f,0,11) })
                Place(environmentArt.grass, p, p.z * 18f);
        }
        private void BuildFence()
        {
            // Keep the original fence line with a clear opening aligned to the path.
            foreach (float x in new[] { -9f, -6f, -3.5f, 3.5f, 6f, 9f })
                Place(environmentArt.fence, new Vector3(x,0,14));
        }
        private void BuildNpcs()
        {
            var content = Resources.Load<Stage2Content>("Data/Stage2Content");
            CreateNpc(new(-4, 0, 6), content.Npc("NPC_Father_01"));
            CreateNpc(new(5, 0, 7), content.Npc("NPC_Children_01"));
            CreateNpc(new(-7, 0, -7), content.Npc("NPC_Neutral_01"));
        }
        private void CreateNpc(Vector3 p, NpcDefinition definition)
        {
            var npc = new GameObject(definition.id); npc.transform.position = p + Vector3.up;
            npc.AddComponent<CharacterController>(); npc.AddComponent<PlayerVisual>(); npc.AddComponent<NpcInteractable>().Configure(definition);
        }
        private GameObject BuildPlayer()
        {
            var player = new GameObject("TEMP_HERO Player"); player.transform.position = new Vector3(0,1,0);
            player.AddComponent<CharacterController>(); player.AddComponent<PlayerVisual>(); player.AddComponent<PlayerController>(); return player;
        }
        private void BuildCamera(Transform target)
        {
            var cam = new GameObject("Gameplay Camera", typeof(Camera), typeof(AudioListener)); cam.tag = "MainCamera"; var controller = cam.AddComponent<CameraController>(); controller.SetTarget(target);  cam.GetComponent<Camera>().backgroundColor = new Color(.65f,.82f,.91f);
            var light = new GameObject("Sun", typeof(Light)); var sun = light.GetComponent<Light>(); sun.type = LightType.Directional; sun.intensity = .55f; sun.shadows = LightShadows.None; light.transform.rotation = Quaternion.Euler(50,-30,0);
        }
        private void BuildHud()
        {
            var canvas = UiFactory.CreateCanvas("Aul HUD"); var label = UiFactory.Text(canvas.transform,"WASD",20,TextAnchor.MiddleCenter,UiFactory.Background); var panel=UiFactory.Panel(canvas.transform,new Color(0,0,0,.28f)); UiFactory.SetRect(panel.rectTransform,new Vector2(.5f,.08f),new Vector2(150,42),Vector2.zero); label.transform.SetParent(panel.transform,false); UiFactory.Stretch(label.rectTransform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        }

        private readonly System.Collections.Generic.Dictionary<Color, Material> materials = new();
        private Mesh roofMesh;
        private Material FlatMaterial(Color color)
        {
            if (materials.TryGetValue(color, out var material)) return material;
            material = new Material(Resources.Load<Material>("Art/PlaceholderFlat"));
            material.SetColor("_BaseColor", color);
            materials.Add(color, material);
            return material;
        }
        private void CreateRoof(Transform parent)
        {
            // A shallow, twelve-sided temporary roof; the existing round base keeps collision.
            if (roofMesh == null)
            {
                const int sides = 12;
                var vertices = new Vector3[sides + 1];
                var triangles = new int[sides * 3];
                vertices[0] = new Vector3(0f, 3.4f, 0f);
                for (int i = 0; i < sides; i++)
                {
                    float angle = i * Mathf.PI * 2f / sides;
                    vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 2.85f, 1.85f, Mathf.Sin(angle) * 2.85f);
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = (i + 1) % sides + 1;
                    triangles[i * 3 + 2] = i + 1;
                }
                roofMesh = new Mesh { name = "Placeholder yurt roof", vertices = vertices, subMeshCount = 2 };
                var back = new int[18]; var front = new int[18];
                System.Array.Copy(triangles, 0, back, 0, 18);
                System.Array.Copy(triangles, 18, front, 0, 18);
                roofMesh.SetTriangles(back, 0); roofMesh.SetTriangles(front, 1);
                roofMesh.RecalculateNormals(); roofMesh.RecalculateBounds();
            }
            var roof = new GameObject("Simple placeholder roof", typeof(MeshFilter), typeof(MeshRenderer));
            roof.transform.SetParent(parent, false);
            roof.GetComponent<MeshFilter>().sharedMesh = roofMesh;
            var renderer = roof.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { FlatMaterial(new Color(.97f, .89f, .73f)), FlatMaterial(new Color(.89f, .79f, .61f)) };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        private void OnDestroy()
        {
            foreach (var material in materials.Values) Destroy(material);
            if (roofMesh != null) Destroy(roofMesh);
            foreach (var mesh in groundMeshes) Destroy(mesh);
        }
        private void Place(GameObject prefab, Vector3 position, float yaw = 0f, float scale = 1f)
        {
            var instance = Instantiate(prefab, position, Quaternion.Euler(0,yaw,0));
            instance.transform.localScale *= scale;
            // Scene-owned, not children of the gameplay root (the existing mini-game cleans that root).
        }
        private void GroundPatch(string name, Vector3 position, Vector2 size, Color color)
        {
            const int count = 16;
            var vertices = new Vector3[count + 1];
            var triangles = new int[count * 3];
            vertices[0] = position + Vector3.up * .014f;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                float radius = 1f + .06f * Mathf.Sin(i * 2.7f);
                vertices[i+1] = position + new Vector3(Mathf.Cos(angle)*size.x*radius, .014f, Mathf.Sin(angle)*size.y*radius);
                triangles[i*3] = 0; triangles[i*3+1] = (i+1)%count+1; triangles[i*3+2] = i+1;
            }
            GroundMesh(name, vertices, triangles, color);
        }
        private void PathRibbon(string name, Vector3[] points, float halfWidth, float height, Color color)
        {
            var vertices = new Vector3[points.Length * 2];
            var triangles = new int[(points.Length-1) * 6];
            for (int i=0; i<points.Length; i++)
            {
                Vector3 tangent = points[Mathf.Min(i+1,points.Length-1)] - points[Mathf.Max(0,i-1)];
                Vector3 side = Vector3.Cross(Vector3.up, tangent.normalized) * halfWidth;
                vertices[i*2] = points[i] - side + Vector3.up * height;
                vertices[i*2+1] = points[i] + side + Vector3.up * height;
                if (i == points.Length-1) continue;
                int a=i*2, t=i*6;
                triangles[t]=a; triangles[t+1]=a+2; triangles[t+2]=a+1;
                triangles[t+3]=a+1; triangles[t+4]=a+2; triangles[t+5]=a+3;
            }
            GroundMesh(name, vertices, triangles, color);
        }
        private void GroundMesh(string name, Vector3[] vertices, int[] triangles, Color color)
        {
            var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); groundMeshes.Add(mesh);
            var patch = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            patch.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = patch.GetComponent<MeshRenderer>(); renderer.sharedMaterial = FlatMaterial(color);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
        }
        private GameObject Primitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color)
        { var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.position = position; go.transform.localScale = scale; var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = FlatMaterial(color); renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false; return go; }
    }
}
