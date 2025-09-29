using System;
using System.Collections.Generic;
using PTL.Framework.Services;
using UnityEngine;

namespace PTL.Framework
{
    public static class ServiceLocator
    {
        private static Dictionary<Type, IService> _services = new Dictionary<Type, IService>();     // Map of all game service- Managers, Controllers, etc.

#region Register & Unregister

        public static void RegisterService<T>(Type serviceType, T instance) where T : IService
        {
            if (!_services.ContainsKey(serviceType))
            {
                _services[serviceType] = instance;
                instance.Start();
            }
        }

        public static void UnregisterAllServices()
        {
            foreach (KeyValuePair<Type, IService> item in _services)
            {
                item.Value.OnDestroy();
            }
            _services.Clear();
        }

        public static void UnregisterService(Type type)
        {
            if (_services.TryGetValue(type, out var service)) {
                service.OnDestroy();
                _services.Remove(type);
            }
        }

#endregion

        public static bool HasService(Type type)
        {
            return _services.ContainsKey(type);
        }
        
        public static T GetService<T>()
        {
            return (T) GetService(typeof(T));
        }

        public static IService GetService(Type type)
        {
            bool found = _services.TryGetValue(type, out IService service);
            if (found)
                return service;
            else
            {
                Debug.LogError($"Could not find the service of type: {type}");
                return null;
            }
        }

#region Service Has and Get methods

        public static IGameService GetGameManager()
        {
            return HasService(typeof(IGameService)) ? GetService<IGameService>() : null;
        }
        
        public static IHighscore GetHighscoreService()
        {
            return HasService(typeof(IHighscore)) ? GetService<IHighscore>() : null;
        }

#endregion
    }
}