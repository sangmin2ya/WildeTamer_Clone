using TMPro;
using UnityEngine;

namespace WildTamer
{
    /// <summary>
    /// 플레이어 스쿼드의 현재 테이머 개체수를 "현재 / 최대" 형식으로 표시합니다.
    /// Squad.OnMemberCountChanged 이벤트를 구독하여 변경 시점에만 갱신합니다.
    /// </summary>
    public class TamerCountUI : MonoBehaviour
    {
        #region SerializeField 필드

        [Header("UI 참조")]
        [SerializeField, Tooltip("개체수를 표시할 TMP 텍스트 (예: '3 / 10')")]
        private TMP_Text countText;

        #endregion

        #region Private 필드

        private Squad _playerSquad;

        #endregion

        #region Unity 메소드

        private void Start()
        {
            _playerSquad = Squad.GetPlayerSquad();

            if (_playerSquad == null)
            {
                Debug.LogWarning("[TamerCountUI] 씬에서 플레이어 스쿼드를 찾을 수 없습니다.");
                return;
            }

            _playerSquad.OnMemberCountChanged += UpdateText;
            UpdateText(_playerSquad.MemberCount, _playerSquad.MaxMembers);
        }

        private void OnDestroy()
        {
            if (_playerSquad != null)
            {
                _playerSquad.OnMemberCountChanged -= UpdateText;
            }
        }

        #endregion

        #region UI 갱신

        /// <summary>
        /// 멤버 수 변경 시 텍스트를 "현재 / 최대" 형식으로 갱신합니다.
        /// </summary>
        /// <param name="current">현재 멤버 수</param>
        /// <param name="max">최대 멤버 수</param>
        private void UpdateText(int current, int max)
        {
            if (countText != null)
            {
                countText.text = $"{current} / {max}";
            }
        }

        #endregion
    }
}
