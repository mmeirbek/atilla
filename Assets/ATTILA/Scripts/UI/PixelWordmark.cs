using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ATTILA.UI
{
    /// <summary>Small, code-drawn pixel wordmark used when the SANLAQ logo image is missing.</summary>
    public sealed class PixelWordmark : Graphic
    {
        private const int GlyphWidth = 5;
        private const int GlyphHeight = 7;
        private const int Gap = 1;
        private static readonly Dictionary<char, string[]> Glyphs = new()
        {
            ['A'] = new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" },
            ['I'] = new[] { "11111", "00100", "00100", "00100", "00100", "00100", "11111" },
            ['L'] = new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" },
            ['T'] = new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" },
            ['S'] = new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" },
            ['N'] = new[] { "10001", "11001", "10101", "10011", "10001", "10001", "10001" },
            ['Q'] = new[] { "01110", "10001", "10001", "10001", "10101", "10010", "01101" }
        };

        [SerializeField] private string value = "SANLAQ";

        public void SetValue(string newValue)
        {
            value = newValue;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            var characterCount = value.Length;
            if (characterCount == 0) return;

            var columns = characterCount * GlyphWidth + (characterCount - 1) * Gap;
            var size = Mathf.Min(rectTransform.rect.width / columns, rectTransform.rect.height / GlyphHeight);
            var drawingWidth = columns * size;
            var drawingHeight = GlyphHeight * size;
            var origin = new Vector2(-drawingWidth * .5f, -drawingHeight * .5f);

            for (var index = 0; index < characterCount; index++)
            {
                if (!Glyphs.TryGetValue(char.ToUpperInvariant(value[index]), out var glyph)) continue;
                var xOffset = index * (GlyphWidth + Gap) * size;
                for (var y = 0; y < GlyphHeight; y++)
                for (var x = 0; x < GlyphWidth; x++)
                {
                    if (glyph[y][x] != '1') continue;
                    AddPixel(vertices, origin + new Vector2(xOffset + x * size, (GlyphHeight - 1 - y) * size), size);
                }
            }
        }

        private void AddPixel(VertexHelper vertices, Vector2 position, float size)
        {
            var start = vertices.currentVertCount;
            var tint = color;
            vertices.AddVert(position, tint, Vector2.zero);
            vertices.AddVert(position + new Vector2(0, size), tint, Vector2.up);
            vertices.AddVert(position + new Vector2(size, size), tint, Vector2.one);
            vertices.AddVert(position + new Vector2(size, 0), tint, Vector2.right);
            vertices.AddTriangle(start, start + 1, start + 2);
            vertices.AddTriangle(start + 2, start + 3, start);
        }
    }
}
