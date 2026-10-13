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
            CoroutineRunner.instance.StartCoroutine(LoadSceneAsync(sceneName, null, onComplete));
        }

        public void LoadScene(string sceneName, Action<float> onProgress, Action onComplete = null)
        {
            CoroutineRunner.instance.StartCoroutine(LoadSceneAsync(sceneName, onProgress, onComplete));
        }

        private IEnumerator LoadSceneAsync(string sceneName, Action<float> onProgress, Action onComplete)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

            // Unity stalls activation at 0.9; normalise so the bar can reach 1.
            while (!operation.isDone)
            {
                onProgress?.Invoke(Mathf.Clamp01(operation.progress / 0.9f));
                yield return null;
            }

            onProgress?.Invoke(1f);
            onComplete?.Invoke();
        }
    }
}