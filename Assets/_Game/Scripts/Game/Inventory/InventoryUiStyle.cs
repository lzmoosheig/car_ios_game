using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Overhaul.Game
{
    internal static class InventoryUiStyle
    {
        private static Sprite _roundedSprite;
        private static readonly Dictionary<int, Sprite> _glass = new();

        public static void Round(Image image)
        {
            if (image == null) return;
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
        }

        /// <summary>
        /// Applies a frosted-glass panel look: a faint translucent fill with a bright glowing
        /// rim and a soft outer glow, sliced so it scales to any size. <paramref name="radius"/>
        /// picks the corner rounding (bigger for the window, smaller for pills/buttons).
        /// The image keeps its own colour = white so the baked translucency survives; tint the
        /// hue (alpha 1) to recolour without losing the glass.
        /// </summary>
        public static void Glass(Image image, int radius = 24)
        {
            if (image == null) return;
            image.sprite = GlassSprite(radius);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        private static Sprite GlassSprite(int radius)
        {
            radius = Mathf.Clamp(radius, 8, 48);
            if (_glass.TryGetValue(radius, out var cached) && cached != null) return cached;

            const int glow = 9;                 // px of soft outer halo
            int border = radius + glow;
            int size = border * 2 + 6;          // a few px of stretchable centre
            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.5f;
            float half = size * 0.5f - glow;    // half-extent out to the rim
            float hx = half - radius;           // straight-edge half length
            float hy = half - radius;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "InventoryGlass",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x - cx) - hx;
                    float qy = Mathf.Abs(y - cy) - hy;
                    float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) +
                                               Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
                    float d = outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius; // 0 at the edge

                    float cover = Mathf.Clamp01(0.5f - d);                 // inside coverage (1px AA)
                    float top = 1f + 0.30f * ((y / (float)size) - 0.5f);   // subtle top-lit gradient
                    float fill = 140f * cover * top;                       // milky frosted fill
                    float rim = 245f * Mathf.Exp(-Mathf.Pow((d + 1.3f) / 1.9f, 2f)); // bright glowing edge
                    float halo = 100f * Mathf.Exp(-Mathf.Max(d, 0f) / 5.5f) * (1f - cover); // outer glow
                    float a = Mathf.Min(255f, Mathf.Max(fill, Mathf.Max(rim, halo)));

                    px[y * size + x] = new Color32(216, 232, 248, (byte)Mathf.RoundToInt(a));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);

            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.name = "InventoryGlass_" + radius;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            _glass[radius] = sprite;
            return sprite;
        }

        private static Sprite RoundedSprite
        {
            get
            {
                if (_roundedSprite != null) return _roundedSprite;

                const int size = 64;
                const float radius = 15f;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = "InventoryRoundedRect",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };

                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(radius - x - 0.5f, 0f, x + 0.5f - (size - radius));
                        float dy = Mathf.Max(radius - y - 0.5f, 0f, y + 0.5f - (size - radius));
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        byte alpha = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(radius + 0.5f - distance));
                        pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                _roundedSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                    new Vector4(radius, radius, radius, radius));
                _roundedSprite.name = "InventoryRoundedRect";
                _roundedSprite.hideFlags = HideFlags.HideAndDontSave;
                return _roundedSprite;
            }
        }
    }
}
