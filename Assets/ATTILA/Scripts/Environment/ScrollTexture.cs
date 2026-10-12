using UnityEngine;

namespace ATTILA.Environment
{
    /// <summary>Slides a material's texture so the river looks like it is flowing.</summary>
    public sealed class ScrollTexture : MonoBehaviour
    {
        [SerializeField] private Vector2 speed = new(0f, -.08f);
        private Material material;
        private Vector2 offset;

        public void Configure(Vector2 value)
        {
            speed = value;
            var renderer = GetComponent<Renderer>();
            // An instance of the shared material, so only this river moves.
            material = renderer.material;
        }

        private void Update()
        {
            if (material == null) return;
            offset += speed * Time.deltaTime;
            material.SetTextureOffset("_BaseMap", offset);
        }
    }
}
