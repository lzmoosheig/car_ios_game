using UnityEngine;

namespace Overhaul.Game
{
    /// <summary>
    /// The station signs use our own depth-tested material instead of the font's built-in
    /// one, which means Unity no longer repoints them when the dynamic font atlas is
    /// rebuilt (the OnGUI HUD shares the same font, so rebuilds do happen). Without this,
    /// a rebuild would leave the signs sampling a stale atlas and render garbled glyphs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SignFontSync : MonoBehaviour
    {
        [SerializeField] private Material signMaterial;
        [SerializeField] private Font font;

        public void Configure(Material material, Font f)
        {
            signMaterial = material;
            font = f;
        }

        private void OnEnable()
        {
            Font.textureRebuilt += OnFontRebuilt;
            Sync();
        }

        private void OnDisable() => Font.textureRebuilt -= OnFontRebuilt;

        private void OnFontRebuilt(Font rebuilt)
        {
            if (font == null || rebuilt == font) Sync();
        }

        private void Sync()
        {
            if (signMaterial == null || font == null || font.material == null) return;
            var atlas = font.material.mainTexture;
            if (atlas != null && signMaterial.mainTexture != atlas) signMaterial.mainTexture = atlas;
        }
    }
}
