using System;
using System.Collections;
using System.Collections.Generic;
using PTL.Framework.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PTL.Framework
{
    public class Bootstrap : Singleton<Bootstrap>
    {
        [Header("Content")]
        [Tooltip("Every ability the Pool Goddess can offer. Assign the AbilityDatabase asset.")]
        [SerializeField] private Abilities.AbilityDatabase m_AbilityDatabase;

        protected override void Awake()
        {
            base.Awake();
            InitializeAllServices(() => StartCoroutine(OnInitialized()));
            ConfigureServices();
        }

        /// <summary>
        /// Feeds scene-authored content into the freshly registered services.
        /// </summary>
        private void ConfigureServices()
        {
            IAbilityService abilities = ServiceLocator.GetAbilityService();
            if (abilities == null)
                return;

            if (m_AbilityDatabase == null)
                Debug.LogError("[Bootstrap] No AbilityDatabase assigned — the Goddess will have nothing to offer.");
            else
                abilities.SetDatabase(m_AbilityDatabase);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ServiceLocator.UnregisterAllServices();
        }

        /// <summary>
        /// Initialize all the persistent core game services here-
        /// </summary>
        private static void InitializeAllServices(Action onComplete = null)
        {
            Dictionary<Type, IService> map = new Dictionary<Type, IService>()
            {
                { typeof(IGameService), new GameManager() },
                { typeof(IHighscore), new HighscoreService() },
                { typeof(ISceneService), new SceneService() },
                { typeof(IEconomyService), new EconomyService() },
                { typeof(IRunModifierService), new RunModifierService() },
                { typeof(IAbilityService), new AbilityService() },
            };

            foreach (KeyValuePair<Type, IService> item in map)
            {
                ServiceLocator.RegisterService(item.Key, item.Value);
            }
            
            onComplete?.Invoke();
        }

        private IEnumerator OnInitialized()
        {
            yield return new WaitForSeconds(1);
            if (SceneManager.GetActiveScene().name == Constants.SceneNames.GAME)
            {
                yield break;
            }

            ServiceLocator.GetService<ISceneService>().LoadScene(Constants.SceneNames.GAME, () =>
            {
                ServiceLocator.GetGameManager().SwitchState(GameState.MainMenu);
            });
        }

    }
}