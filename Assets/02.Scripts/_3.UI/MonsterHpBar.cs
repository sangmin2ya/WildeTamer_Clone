using UnityEngine;
using UnityEngine.UI;

namespace WildTamer
{
    /// <summary>
    /// 몬스터 체력 바 UI를 제어합니다.
    /// 부모 계층에서 Monster 컴포넌트를 탐색하여 OnHpChanged 이벤트를 구독하고,
    /// Image.fillAmount를 갱신하여 현재 체력 비율을 표시합니다.
    ///
    /// Unity 설정:
    /// Monster 하위에 World Space Canvas → HpBar 오브젝트(이 스크립트 부착)를 구성하고
    /// fillImage에 Fill Image 컴포넌트를 연결합니다.
    /// </summary>
    public class MonsterHpBar : MonoBehaviour
    {
        #region SerializeField 필드

        [Header("UI 참조")]
        [SerializeField, Tooltip("체력 비율을 표시할 Fill Image (Image Type: Filled)")]
        private Image fillImage;

        [Header("표시 설정")]
        [SerializeField, Tooltip("체력이 최대일 때 HP 바를 숨깁니다")]
        private bool hideWhenFull = true;

        #endregion

        #region Private 필드

        private Monster _monster;

        #endregion

        #region Unity 메소드

        private void Awake()
        {
            _monster = GetComponentInParent<Monster>();

            if (_monster != null)
            {
                // C# 델리게이트로 구독 — MonoBehaviour 활성 여부와 무관하게 항상 수신
                _monster.OnHpChanged += UpdateDisplay;
            }
            else
            {
                Debug.LogWarning($"[MonsterHpBar] '{name}': 부모 계층에서 Monster 컴포넌트를 찾을 수 없습니다.", this);
            }
        }

        private void OnEnable()
        {
            // 풀에서 재사용되거나 활성화될 때 현재 체력으로 즉시 갱신
            if (_monster != null)
            {
                UpdateDisplay(_monster.CurrentHp, _monster.Data.stat.maxHp);
            }
        }

        private void OnDestroy()
        {
            if (_monster != null)
            {
                _monster.OnHpChanged -= UpdateDisplay;
            }
        }

        #endregion

        #region HP 표시 갱신

        /// <summary>
        /// fillAmount를 체력 비율로 갱신합니다.
        /// hideWhenFull이 true이면 체력이 최대일 때 이 오브젝트를 비활성화합니다.
        /// 이 메서드는 Monster가 비활성 상태여도 호출되며, 오브젝트를 다시 활성화할 수 있습니다.
        /// </summary>
        /// <param name="currentHp">현재 체력</param>
        /// <param name="maxHp">최대 체력</param>
        private void UpdateDisplay(float currentHp, float maxHp)
        {
            if (fillImage == null)
            {
                return;
            }

            float ratio = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
            fillImage.fillAmount = ratio;

            if (hideWhenFull)
            {
                gameObject.SetActive(ratio < 1f);
            }
        }

        #endregion
    }
}
