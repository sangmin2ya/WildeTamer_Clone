using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WildTamer
{
    /// <summary>
    /// 도감 항목 1개의 UI를 표시하는 컴포넌트입니다.
    /// EncyclopediaUI가 Refresh()를 호출하여 해금 상태에 따라 내용을 갱신합니다.
    /// </summary>
    public class EncyclopediaEntryUI : MonoBehaviour
    {
        #region SerializeField 필드

        [Header("UI 참조")]
        [SerializeField, Tooltip("초상화 또는 잠금 스프라이트를 표시하는 Image")]
        private Image portraitImage;

        [SerializeField, Tooltip("몬스터 이름 텍스트 — 잠금 시 ???로 표시")]
        private TMP_Text nameText;

        [SerializeField, Tooltip("최대 체력 텍스트 — 잠금 시 HP ???로 표시")]
        private TMP_Text hpText;

        [SerializeField, Tooltip("공격력 텍스트 — 잠금 시 ATK ???로 표시")]
        private TMP_Text atkText;

        #endregion

        #region 갱신

        /// <summary>
        /// 항목 UI를 해금 여부에 따라 갱신합니다.
        /// </summary>
        /// <param name="entry">도감 항목 데이터</param>
        /// <param name="isUnlocked">해금 여부</param>
        public void Refresh(EncyclopediaEntry entry, bool isUnlocked)
        {
            if (isUnlocked)
            {
                portraitImage.sprite = entry.portrait;
                nameText.text        = entry.monsterData.monsterName;
                hpText.text          = $"HP  {entry.monsterData.stat.maxHp}";
                atkText.text         = $"ATK  {entry.monsterData.stat.attackDamage}";
            }
            else
            {
                portraitImage.sprite = EncyclopediaManager.Instance != null
                    ? EncyclopediaManager.Instance.LockedSprite
                    : null;

                nameText.text = "???";
                hpText.text   = "HP  ???";
                atkText.text  = "ATK  ???";
            }
        }

        #endregion
    }
}
