using System;
using System.Collections;
using System.Collections.Generic;
using PTL.Framework.Services;
using UnityEngine;

namespace PTL.Framework
{
    public class Bootstrap : Singleton<Bootstrap>
    {
        protected override void Awake()
        {
            base.Awake();
            InitializeAllServices(() => StartCoroutine(OnInitialized()));
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
            ServiceLocator.GetService<ISceneService>().LoadScene(Constants.SceneNames.GAME, () =>
            {
                ServiceLocator.GetGameManager().SwitchState(GameState.MainMenu);
            });
        }

    }
}