using System;
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
        CoinWallet,
    }

    /// <summary>
    /// Lightweight registry mapping a <see cref="CollectionTargetId"/> to the on-screen
    /// UI element it represents. UI elements register themselves (via
    /// <see cref="CollectionTargetMarker"/> or a wallet's <see cref="Economy.UI.CollectionIconTarget"/>);
    /// collectables query the world-space point to tween toward and report back with
    /// <see cref="NotifyArrived"/> when they get there. Decouples world-space pickups from the HUD.
    /// </summary>
    public static class CollectionTargets
    {
        private struct Entry
        {
            public RectTransform Rect;
            public Func<Vector3> UiPosition;
        }

        private static readonly Dictionary<CollectionTargetId, Entry> _targets = new();

        /// <summary>Raised when a collectable finishes its flight to <paramref name="id"/>'s target.</summary>
        public static event Action<CollectionTargetId> OnArrived;

        /// <summary>
        /// Registers <paramref name="rect"/> as the destination for <paramref name="id"/>.
        /// <paramref name="uiPosition"/> optionally overrides where on the HUD the flight ends (a UI
        /// world position, i.e. a RectTransform.position) — for targets that are off-screen right now
        /// but will be somewhere else by the time the item arrives, like the sliding coin wallet.
        /// </summary>
        public static void Register(CollectionTargetId id, RectTransform rect, Func<Vector3> uiPosition = null)
            => _targets[id] = new Entry { Rect = rect, UiPosition = uiPosition };

        public static void Unregister(CollectionTargetId id) => _targets.Remove(id);

        /// <summary>Tells listeners (the HUD icon) that an item has reached <paramref name="id"/>.</summary>
        public static void NotifyArrived(CollectionTargetId id) => OnArrived?.Invoke(id);

        /// <summary>
        /// Converts the registered UI element for <paramref name="id"/> into a world position
        /// on the z = 0 gameplay plane, as seen by <paramref name="worldCamera"/>.
        /// Returns false if no (live) target is registered.
        /// </summary>
        public static bool TryGetWorldPosition(CollectionTargetId id, Camera worldCamera, out Vector3 worldPos)
        {
            worldPos = default;

            if (!_targets.TryGetValue(id, out Entry entry) || entry.Rect == null)
                return false;

            if (worldCamera == null)
                worldCamera = Camera.main;
            if (worldCamera == null)
                return false;

            // Screen position of the UI element (handles both Overlay and Camera-space canvases).
            Canvas canvas = entry.Rect.GetComponentInParent<Canvas>();
            Camera uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera
                : null;
            Vector3 uiPosition = entry.UiPosition != null ? entry.UiPosition() : entry.Rect.position;
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, uiPosition);

            // Project onto the gameplay plane. For a 2D camera looking down +z from negative z,
            // the distance to z = 0 is |camera.z|.
            float depth = Mathf.Abs(worldCamera.transform.position.z);
            worldPos = worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
            worldPos.z = 0f;
            return true;
        }
    }
}
