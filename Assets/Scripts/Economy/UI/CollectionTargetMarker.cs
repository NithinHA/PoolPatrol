using UnityEngine;

namespace Economy.UI
{
    /// <summary>
    /// Marks a HUD element as the destination collectables fly toward.
    /// Attach to the wallet icon, lives panel, etc., pick its <see cref="CollectionTargetId"/>,
    /// and it self-registers with <see cref="CollectionTargets"/> while enabled.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CollectionTargetMarker : MonoBehaviour
    {
        [SerializeField] private CollectionTargetId m_TargetId;

        private void OnEnable() => CollectionTargets.Register(m_TargetId, (RectTransform)transform);

        private void OnDisable() => CollectionTargets.Unregister(m_TargetId);
    }
}
