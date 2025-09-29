using System;

namespace PTL.Framework.Services
{
    public interface ISceneService : IService
    {
        void LoadScene(string sceneName, Action onComplete = null);
    }
}