using UnityEngine;

namespace PTL.Framework
{
	public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		private static T _instance = null;
        public bool DontDestroyOnLoad = false;
		
		public static T Instance
		{
			get
			{
				if (_instance == null)
					_instance = FindObjectOfType<T>();

				return _instance;
			}
		}

#region Unity callbacks

		protected virtual void Awake()
		{
			if (DontDestroyOnLoad)
				DontDestroyOnLoad(gameObject);
		}

		protected virtual void Start()
		{
			T[] instance = FindObjectsOfType<T>();
			if (instance.Length > 1)
			{
				Debug.LogError($"More than one items found of type {typeof(T)}. Destroying {name}.");
				Destroy(gameObject);
			}
		}

		protected virtual void OnDestroy()
		{
			if (this == _instance)
				_instance = null;
		}

#endregion
	}
}
