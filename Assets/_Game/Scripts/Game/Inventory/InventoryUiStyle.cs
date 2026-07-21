using UnityEngine;
using UnityEngine.UI;

namespace Overhaul.Game
{
    internal static class InventoryUiStyle
    {
        private static Sprite _roundedSprite;

        public static void Round(Image image)
        {
            if (image == null) return;
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
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
