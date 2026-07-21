namespace Overhaul.Game
{
    /// <summary>
    /// A tiny global gate: while a modal inventory screen (e.g. the container transfer window)
    /// is open, gameplay systems that would fight it - player movement, first-person mouse-look,
    /// world tap-selection - check <see cref="IsOpen"/> and stand down. A counter (not a bool)
    /// so nested/overlapping opens can't leave the gate stuck.
    /// </summary>
    public static class InventoryUiModal
    {
        private static int _open;

        public static bool IsOpen => _open > 0;

        public static void Push() => _open++;
        public static void Pop() => _open = _open > 0 ? _open - 1 : 0;
        public static void Reset() => _open = 0;
    }
}
