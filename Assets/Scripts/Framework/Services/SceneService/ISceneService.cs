using System;

namespace PTL.Framework.Services
{
    public interface ISceneService : IService
    {
        void LoadScene(string sceneName, Action onComplete = null);

        /// <summary>
        /// Loads a scene asynchronously, reporting load progress in 0..1 (useful for a loading bar).
        /// </summary>
        void LoadScene(string sceneName, Action<float> onProgress, Action onComplete = null);
    }
}
