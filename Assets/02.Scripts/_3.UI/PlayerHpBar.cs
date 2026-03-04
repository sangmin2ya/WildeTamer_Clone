using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WildTamer
{
    /// <summary>
    /// 플레이어 체력 바 UI 컴포넌트입니다.
    /// 플레이어 하위 오브젝트가 아닌 하이어라키 상 독립 위치에 배치하고,
    /// Inspector에서 PlayerController를 직접 할당하여 사용합니다.
    ///
    /// PlayerController.OnHpChanged 이벤트를 구독하여 HP 변동 시에만 갱신합니다.
    /// fillAmount 바와 "현재 / 최대" 텍스트를 동시에 갱신합니다.
    /// </summary>
    public class PlayerHpBar : MonoBehaviour
    {
        #region SerializeField 필드

        [Header("참조")]
        [SerializeField, Tooltip("HP를 표시할 플레이어 컨트롤러")]
        private PlayerController player;

        [SerializeField, Tooltip("fillAmount로 체력을 표현할 Image 컴포넌트")]
        private Image fillImage;

        [SerializeField, Tooltip("현재 / 최대 체력을 표시할 텍스트 컴포넌트 (예: 100 / 100)")]
        private TMP_Text hpText;

        [Header("설정")]
        [SerializeField, Tooltip("체력이 최대일 때 바를 숨길지 여부")]
        private bool hideWhenFull = false;

        #endregion

        #region Private 필드

        // 마지막으로 수신한 현재·최대 체력 — OnEnable 시 복원에 사용
        private float _cachedCurrentHp;
        private float _cachedMaxHp = 1f;

        #endregion

        #region Unity 메소드

        private void Awake()
        {
            if (player == null)
            {
                Debug.LogWarning("[PlayerHpBar] PlayerController가 할당되지 않았습니다.", this);
                return;
            }

            // C# 이벤트 구독 — 비활성화 상태에서도 수신 가능
            player.OnHpChanged += OnHpChanged;
        }

        private void OnEnable()
        {
            // 활성화 직후 캐시된 값으로 즉시 갱신
            if (player != null)
            {
                Refresh(_cachedCurrentHp, _cachedMaxHp);
            }
        }

        private void OnDestroy()
        {
            if (player != null)
            {
                player.OnHpChanged -= OnHpChanged;
            }
        }

        #endregion

        #region 이벤트 핸들러

        /// <summary>
        /// HP가 변경될 때 호출됩니다.
        /// </summary>
        /// <param name="currentHp">현재 체력</param>
        /// <param name="maxHp">최대 체력</param>
        private void OnHpChanged(float currentHp, float maxHp)
        {
            _cachedCurrentHp = currentHp;
            _cachedMaxHp     = maxHp;
            Refresh(currentHp, maxHp);
        }

        #endregion

        #region UI 갱신

        /// <summary>
        /// fillAmount, 텍스트, 표시 여부를 한 번에 갱신합니다.
        /// </summary>
        /// <param name="currentHp">현재 체력</param>
        /// <param name="maxHp">최대 체력</param>
        private void Refresh(float currentHp, float maxHp)
        {
            float ratio = maxHp > 0f ? currentHp / maxHp : 0f;

            if (fillImage != null)
            {
                fillImage.fillAmount = ratio;
            }

            if (hpText != null)
            {
                // 소수점 없이 정수로 표시 — 현재 체력은 올림하여 1 이상인 동안 0으로 보이지 않도록
                hpText.text = $"{Mathf.CeilToInt(currentHp)} / {(int)maxHp}";
            }

            if (hideWhenFull)
            {
                gameObject.SetActive(ratio < 1f);
            }
        }

        #endregion
    }
}
