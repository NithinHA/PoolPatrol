using PTL.Framework;
using PTL.Framework.Services;
using UnityEngine;
using Weapon;

namespace Loadout
{
    /// <summary>
    /// Dresses the Game scene from the player's saved loadout: swaps the floatie, character head, and
    /// pool-texture sprites, and equips the chosen weapon. Reads <see cref="ILoadoutService"/> on
    /// Start. Every reference is optional so the scene degrades gracefully if art is still placeholder.
    /// </summary>
    public class LoadoutApplier : MonoBehaviour
    {
        [Header("Floatie")]
        [SerializeField] private SpriteRenderer m_FloatieBody;
        [SerializeField] private SpriteRenderer m_FloatieReflection;

        [Header("Character")]
        [SerializeField] private SpriteRenderer m_CharacterHead;
        [Tooltip("Optional mirrored head renderer used by the water reflection.")]
        [SerializeField] private SpriteRenderer m_CharacterHeadReflection;

        [Header("Pool")]
        [Tooltip("The pool background renderer (a MeshRenderer in the Game scene). Its material texture is swapped.")]
        [SerializeField] private Renderer m_PoolRenderer;
        [Tooltip("Texture property on the pool material to swap (URP Lit/Unlit: _BaseMap; custom shaders often _MainTex).")]
        [SerializeField] private string m_PoolTextureProperty = "_MainTex";

        [Header("Weapon")]
        [SerializeField] private PlayerWeaponController m_WeaponController;

        private void Start() => Apply();

        /// <summary>Applies the current loadout. Safe to call again if the loadout changes mid-scene.</summary>
        public void Apply()
        {
            ILoadoutService loadout = ServiceLocator.GetLoadoutService();
            if (loadout == null)
            {
                Debug.LogWarning("[LoadoutApplier] No LoadoutService — using scene defaults.");
                return;
            }

            ApplyFloatie(loadout.SelectedFloatie);
            ApplyCharacter(loadout.SelectedCharacter);
            ApplyPoolTexture(loadout.SelectedPoolTexture);
            ApplyWeapon(loadout.SelectedWeapon);
        }

        private void ApplyFloatie(FloatieDefinitionSO floatie)
        {
            if (floatie == null || floatie.BodySprite == null) return;

            if (m_FloatieBody != null) m_FloatieBody.sprite = floatie.BodySprite;
            if (m_FloatieReflection != null)
                m_FloatieReflection.sprite = floatie.ReflectionSprite != null ? floatie.ReflectionSprite : floatie.BodySprite;
        }

        private void ApplyCharacter(CharacterDefinitionSO character)
        {
            if (character == null || character.HeadSprite == null) return;

            if (m_CharacterHead != null) m_CharacterHead.sprite = character.HeadSprite;
            if (m_CharacterHeadReflection != null) m_CharacterHeadReflection.sprite = character.HeadSprite;
        }

        private void ApplyPoolTexture(PoolTextureDefinitionSO pool)
        {
            if (pool == null || pool.PoolSprite == null || pool.PoolSprite.texture == null || m_PoolRenderer == null)
                return;

            Material mat = m_PoolRenderer.material;   // instance, not the shared asset
            if (mat.HasProperty(m_PoolTextureProperty))
                mat.SetTexture(m_PoolTextureProperty, pool.PoolSprite.texture);
        }

        private void ApplyWeapon(WeaponDefinitionSO weapon)
        {
            if (weapon == null || m_WeaponController == null) return;
            m_WeaponController.SelectWeaponByKind(weapon.Kind);
        }
    }
}
