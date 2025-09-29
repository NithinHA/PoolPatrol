using System.Collections.Generic;
using UnityEngine;

namespace Enemy.Death
{
    public class EnemyDeathEffectHandler : EnemyComponentBase
    {
        private IOnDeathEffect[] _effects;

        protected override void Awake()
        {
            base.Awake();
            _effects = GetComponents<IOnDeathEffect>();
        }

        public void TriggerDeathEffects(Dictionary<string, object> parameters)
        {
            foreach (var effect in _effects)
            {
                effect.Execute(parameters);
            }
        }
    }

    public interface IOnDeathEffect
    {
        void Execute(Dictionary<string, object> parameters);
    }
}