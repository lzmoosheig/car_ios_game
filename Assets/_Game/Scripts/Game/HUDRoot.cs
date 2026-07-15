using UnityEngine;

namespace Overhaul.Game
{
    public sealed class HUDRoot : MonoBehaviour
    {
        [SerializeField] private RectTransform safeAreaRoot;

        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        public RectTransform SafeAreaRoot => safeAreaRoot;

        public void Configure(RectTransform safeRoot)
        {
            safeAreaRoot = safeRoot;
            ApplySafeArea(true);
        }

        private void Awake()
        {
            if (safeAreaRoot == null) safeAreaRoot = transform as RectTransform;
            ApplySafeArea(true);
        }

        private void Update() => ApplySafeArea(false);

        private void ApplySafeArea(bool force)
        {
            if (safeAreaRoot == null) return;

            var screenSize = new Vector2Int(Screen.width, Screen.height);
            var safeArea = Screen.safeArea;
            if (!force && _lastSafeArea == safeArea && _lastScreenSize == screenSize) return;

            _lastSafeArea = safeArea;
            _lastScreenSize = screenSize;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            safeAreaRoot.anchorMin = anchorMin;
            safeAreaRoot.anchorMax = anchorMax;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
        }
    }
}
