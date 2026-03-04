using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WildTamer
{
    /// <summary>
    /// 몬스터 체력 바 UI를 제어합니다.
    /// 부모 계층에서 Monster 컴포넌트를 탐색하여 OnHpChanged 이벤트를 구독하고,
    /// Image.fillAmount를 갱신하여 현재 체력 비율을 표시합니다.
    /// 체력이 변경된 후 hideDelay 초가 경과하면 자동으로 숨깁니다.
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

        [SerializeField, Tooltip("체력이 0일 때 HP 바를 숨깁니다")]
        private bool hideWhenZero = true;

        [SerializeField, Tooltip("마지막 체력 변경 후 HP 바를 숨기기까지의 대기 시간 (초). 0 이하이면 자동 숨김 비활성화")]
        private float hideDelay = 3f;

        #endregion

        #region Private 필드

        private Monster _monster;
        private Coroutine _hideCoroutine;
        private WaitForSeconds _hideWait;

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

            if (hideDelay > 0f)
            {
                _hideWait = new WaitForSeconds(hideDelay);
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

        private void OnDisable()
        {
            StopHideCoroutine();
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
        /// hideWhenZero가 true이면 HP가 0일 때 숨깁니다.
        /// hideWhenFull이 true이면 체력이 최대일 때 숨깁니다.
        /// 그 외에는 hideDelay 후 자동으로 숨깁니다.
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

            // HP 0 — hideWhenZero이면 숨김
            if (hideWhenZero && ratio <= 0f)
            {
                StopHideCoroutine();
                gameObject.SetActive(false);
                return;
            }

            // 체력 최대 — hideWhenFull이면 숨김
            if (hideWhenFull && ratio >= 1f)
            {
                StopHideCoroutine();
                gameObject.SetActive(false);
                return;
            }

            // HP 바 표시 후 hideDelay 초 뒤에 자동 숨김 시작
            gameObject.SetActive(true);
            RestartHideCoroutine();
        }

        #endregion

        #region 자동 숨김 코루틴

        /// <summary>
        /// 진행 중인 숨김 코루틴을 중단합니다.
        /// </summary>
        private void StopHideCoroutine()
        {
            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }
        }

        /// <summary>
        /// 숨김 코루틴을 재시작합니다.
        /// hideDelay가 0 이하이면 자동 숨김을 수행하지 않습니다.
        /// </summary>
        private void RestartHideCoroutine()
        {
            StopHideCoroutine();

            if (hideDelay > 0f)
            {
                _hideCoroutine = StartCoroutine(HideAfterDelay());
            }
        }

        /// <summary>
        /// hideDelay 초 대기 후 HP 바를 숨깁니다.
        /// </summary>
        private IEnumerator HideAfterDelay()
        {
            yield return _hideWait;
            gameObject.SetActive(false);
            _hideCoroutine = null;
        }

        #endregion
    }
}
