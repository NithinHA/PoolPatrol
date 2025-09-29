using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PTL.Framework.Services
{
    public class SceneService : ISceneService
    {

#region Default callbacks
        
        public void Start()
        { }

        public void OnDestroy()
        { }

#endregion

        public void LoadScene(string sceneName, Action onComplete = null)
        {
            CoroutineRunner.instance.StartCoroutine(LoadSceneAsync(sceneName, onComplete));
        }
        
        private IEnumerator LoadSceneAsync(string sceneName, Action onComplete)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            yield return new WaitUntil(() => operation.isDone);
            onComplete?.Invoke();
        }
    }
}