using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// MonoBehaviour 싱글턴 기반 추상 클래스
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        #region Public 프로퍼티

        public static T Instance { get; private set; }

        #endregion

        #region Unity 메소드

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this as T;
        }

        #endregion
    }
}
