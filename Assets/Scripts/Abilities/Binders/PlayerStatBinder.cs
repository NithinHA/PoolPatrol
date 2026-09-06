using Abilities;
using Movement;
using PTL.Framework;
using PTL.Framework.Services;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Pushes run modifiers into the live player components, and reports run state back to the
    /// ability service for eligibility checks. Attach to the player root (alongside
    /// PlayerController).
    ///
    /// Base values are captured in Awake so every re-apply recomputes from the original
    /// inspector values rather than compounding.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerStatBinder : MonoBehaviour, IRunStateProvider
    {
        [Header("Caps")]
        [Tooltip("Upper bound for the \"+1 Max Life\" ability (doc §13).")]
        [SerializeField] private int m_MaxLivesCap = 8;

        private PlayerController _controller;
        private PlayerHealth _health;
        private PlayerCombo _combo;
        private ImpulseMover _mover;

        private IRunModifierService _modifiers;
        private IAbilityService _abilities;

        // Captured inspector baselines.
        private int _baseMaxLives;
        private float _baseTargetSpeed;
        private float _baseImpulseStrength;

        public int CurrentLives => _health != null ? _health.CurrentHealth : 0;
        public int MaxLives     => _health != null ? _health.MaxHealth : 0;
        public int MaxLivesCap  => m_MaxLivesCap;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _health = _controller.PlayerHealth;
            _combo  = _controller.PlayerCombo;
            _mover  = GetComponent<ImpulseMover>();

            if (_health != null)
                _baseMaxLives = _health.MaxHealth;
            if (_mover != null)
            {
                _baseTargetSpeed     = _mover.m_TargetSpeed;
                _baseImpulseStrength = _mover.m_ImpulseStrength;
            }
        }

        private void Start()
        {
            _modifiers = ServiceLocator.GetRunModifierService();
            _abilities = ServiceLocator.GetAbilityService();

            if (_modifiers != null)
                _modifiers.OnModifiersChanged += ApplyModifiers;

            if (_abilities != null)
            {
                _abilities.SetRunStateProvider(this);
                _abilities.OnInstantEffect += OnInstantEffect;
            }

            ApplyModifiers();
        }

        private void OnDestroy()
        {
            if (_modifiers != null)
                _modifiers.OnModifiersChanged -= ApplyModifiers;
            if (_abilities != null)
                _abilities.OnInstantEffect -= OnInstantEffect;
        }

        /// <summary>Recomputes every player stat from its baseline plus current modifiers.</summary>
        private void ApplyModifiers()
        {
            if (_modifiers == null)
                return;

            if (_health != null)
                _health.SetMaxHealth(_modifiers.GetInt(StatId.MaxLives, _baseMaxLives));

            if (_mover != null)
            {
                _mover.m_TargetSpeed     = _modifiers.GetFloat(StatId.MoveSpeed, _baseTargetSpeed);
                _mover.m_ImpulseStrength = _modifiers.GetFloat(StatId.Propulsion, _baseImpulseStrength);
            }

            if (_combo != null)
                _combo.SetThresholdReduction(Mathf.RoundToInt(_modifiers.GetFlatSum(StatId.ComboThresholdReduction)));
        }

        private void OnInstantEffect(InstantEffectType effect)
        {
            switch (effect)
            {
                case InstantEffectType.RestoreOneLife:
                    _health?.Heal(1);
                    break;

                case InstantEffectType.RefillMagazine:
                    _controller.PlayerWeaponController?.ActiveWeapon?.Magazine?.Refill();
                    break;
            }
        }
    }
}
