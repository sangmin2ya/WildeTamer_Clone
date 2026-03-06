using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 기절한 몬스터에 선택적으로 부착하는 테이밍 전용 컴포넌트입니다.
    /// 몬스터 월드 캔버스에 배치된 TameButtonUI를 직접 참조하며,
    /// 기절 상태에서 플레이어가 감지 반경 내에 들어올 때 DOTween 애니메이션으로 UI를 표시합니다.
    ///
    /// ■ 설정 방법
    ///   1. 몬스터 프리팹에 이 컴포넌트를 추가합니다.
    ///   2. tameButtonUI: 월드 캔버스 자식으로 배치된 TameButtonUI를 연결합니다.
    ///      (미연결 시 GetComponentInChildren으로 자동 탐색)
    ///   3. TameButtonUI 오브젝트는 기본 비활성화 상태로 설정해두어야 합니다.
    /// </summary>
    [RequireComponent(typeof(Monster))]
    public class TameController : MonoBehaviour
    {
        #region SerializeField 필드

        [Header("UI 참조")]
        [SerializeField, Tooltip("몬스터 월드 캔버스에 배치된 TameButtonUI (미연결 시 자동 탐색)")]
        private TameButtonUI tameButtonUI;

        [Header("감지 설정")]
        [SerializeField, Tooltip("UI를 표시할 플레이어 감지 반경 (월드 단위)")]
        private float detectionRadius = 2f;

        [Header("애니메이션 설정")]
        [SerializeField, Tooltip("UI 등장·퇴장 애니메이션 재생 시간 (초)")]
        private float showDuration = 0.25f;

        [SerializeField, Tooltip("UI 등장·퇴장 애니메이션 Ease 유형")]
        private Ease showEase = Ease.OutBack;

        #endregion

        #region Private 필드

        private Monster   _monster;
        private Transform _playerTransform;
        private bool      _isUIVisible;
        private Coroutine _proximityCoroutine;

        private readonly WaitForSeconds _checkInterval = new WaitForSeconds(0.1f);

        #endregion

        #region Unity 메소드

        private void Awake()
        {
            _monster = GetComponent<Monster>();

            if (tameButtonUI == null)
            {
                tameButtonUI = GetComponentInChildren<TameButtonUI>(true);
            }

            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                _playerTransform = playerObj.transform;
            }
        }

        private void OnDisable()
        {
            StopProximityCheck();
            tameButtonUI?.HideImmediate();
            _isUIVisible = false;
        }

        #endregion

        #region UI 제어

        /// <summary>
        /// 기절 상태에 진입할 때 호출합니다.
        /// 버튼 콜백을 초기화하고 플레이어 근접 감지 루프를 시작합니다.
        /// MonsterStunState.Enter()에서 호출됩니다.
        /// </summary>
        public void ShowTameUI()
        {
            if (tameButtonUI == null)
            {
                return;
            }

            tameButtonUI.Initialize(OnTameButtonClicked, OnRootButtonClicked, Squad.GetPlayerSquad());

            StopProximityCheck();
            _proximityCoroutine = StartCoroutine(ProximityCheckRoutine());
        }

        /// <summary>
        /// 기절 상태를 벗어날 때 호출합니다.
        /// 근접 감지 루프를 종료하고 UI를 즉시 숨깁니다.
        /// MonsterStunState.Exit()에서 호출됩니다.
        /// </summary>
        public void HideTameUI()
        {
            StopProximityCheck();
            tameButtonUI?.HideImmediate();
            _isUIVisible = false;
        }

        #endregion

        #region 근접 감지

        /// <summary>
        /// 플레이어와의 거리를 주기적으로 확인하여 UI 표시 여부를 결정합니다.
        /// 감지 반경 진입 시 Show 애니메이션, 이탈 시 Hide 애니메이션을 재생합니다.
        /// </summary>
        private IEnumerator ProximityCheckRoutine()
        {
            float radiusSqr = detectionRadius * detectionRadius;

            while (true)
            {
                bool inRange = _playerTransform != null &&
                               (transform.position - _playerTransform.position).sqrMagnitude <= radiusSqr;

                if (inRange && !_isUIVisible)
                {
                    _isUIVisible = true;
                    tameButtonUI.Show(showDuration, showEase);
                }
                else if (!inRange && _isUIVisible)
                {
                    _isUIVisible = false;
                    tameButtonUI.Hide(showDuration, showEase);
                }

                yield return _checkInterval;
            }
        }

        private void StopProximityCheck()
        {
            if (_proximityCoroutine != null)
            {
                StopCoroutine(_proximityCoroutine);
                _proximityCoroutine = null;
            }
        }

        #endregion

        #region 버튼 콜백

        /// <summary>
        /// "길들이기" 버튼 클릭 시 호출됩니다.
        /// </summary>
        private void OnTameButtonClicked()
        {
            _monster.Tame();
        }

        /// <summary>
        /// "수확하기" 버튼 클릭 시 호출됩니다.
        /// </summary>
        private void OnRootButtonClicked()
        {
            _monster.Root();
        }

        #endregion
    }
}
