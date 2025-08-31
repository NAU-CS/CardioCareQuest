using UnityEngine;

namespace Utility
{
    /// <summary>
    /// Singleton pattern for MonoBehaviour. Does not persist between scenes.
    /// </summary>
    /// <typeparam name="T">The type of the singleton.</typeparam>
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        public static T Instance { get; private set; }

        protected void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this as T;
            }
        }
    }

    /// <summary>
    /// Singleton pattern for MonoBehaviour. Persists between scenes.
    /// </summary>
    /// <typeparam name="T">The type of the singleton.</typeparam>
    public class SingletonPersistent<T> : MonoBehaviour where T : Component
    {
        public static T Instance { get; private set; }

        protected void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this as T;
                DontDestroyOnLoad(gameObject);
                
                Setup();
            }
        }

        protected virtual void Setup()
        {
            // Override this method to add custom setup logic
        }
    }
}