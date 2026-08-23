using System.Collections.Generic;
using UnityEngine;

namespace Economy
{
    /// <summary>
    /// Screen-space UI destinations a collectable flies toward after being granted
    /// (e.g. the gem wallet, the lives panel).
    /// </summary>
    public enum CollectionTargetId
    {
        GemWallet,
        Health,
    }

    /// <summary>
    /// Lightweight registry mapping a <see cref="CollectionTargetId"/> to the on-screen
    /// UI element it represents. UI elements register themselves via
    /// <see cref="CollectionTargetMarker"/>; collectables query the world-space point to
    /// tween toward. Decouples world-space pickups from the HUD hierarchy.
    /// </summary>
    public static class CollectionTargets
    {
        private static readonly Dictionary<CollectionTargetId, RectTransform> _targets = new();

        public static void Register(CollectionTargetId id, RectTransform rect) => _targets[id] = rect;

        public static void Unregister(CollectionTargetId id) => _targets.Remove(id);

        /// <summary>
        /// Converts the registered UI element for <paramref name="id"/> into a world position
        /// on the z = 0 gameplay plane, as seen by <paramref name="worldCamera"/>.
        /// Returns false if no (live) target is registered.
        /// </summary>
        public static bool TryGetWorldPosition(CollectionTargetId id, Camera worldCamera, out Vector3 worldPos)
        {
            worldPos = default;

            if (!_targets.TryGetValue(id, out RectTransform rect) || rect == null)
                return false;

            if (worldCamera == null)
                worldCamera = Camera.main;
            if (worldCamera == null)
                return false;

            // Screen position of the UI element (handles both Overlay and Camera-space canvases).
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera
                : null;
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, rect.position);

            // Project onto the gameplay plane. For a 2D camera looking down +z from negative z,
            // the distance to z = 0 is |camera.z|.
            float depth = Mathf.Abs(worldCamera.transform.position.z);
            worldPos = worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
            worldPos.z = 0f;
            return true;
        }
    }
}
