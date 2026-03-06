using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WildTamer
{
    /// <summary>
    /// 도감 항목 1개의 UI를 표시하는 컴포넌트입니다.
    /// CollectionUI가 Refresh()를 호출하여 해금 상태에 따라 내용을 갱신합니다.
    /// </summary>
    public class CollectionEntryUI : MonoBehaviour
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

        [SerializeField, Tooltip("몬스터 타입 텍스트 (일반 / 보스) — 잠금 시 비워둠")]
        private TMP_Text typeText;

        #endregion

        #region 갱신

        /// <summary>
        /// 항목 UI를 해금 여부에 따라 갱신합니다.
        /// </summary>
        /// <param name="entry">도감 항목 데이터</param>
        /// <param name="isUnlocked">해금 여부</param>
        public void Refresh(CollectionEntry entry, bool isUnlocked)
        {
            if (isUnlocked)
            {
                portraitImage.sprite = entry.portrait;
                portraitImage.color  = Color.white;
                nameText.text        = entry.monsterData.monsterName;
                hpText.text          = entry.monsterData.stat.maxHp.ToString();
                atkText.text         = entry.monsterData.stat.attackDamage.ToString();

                if (typeText != null)
                {
                    typeText.text = entry.monsterData.monsterType.ToString();
                }
            }
            else
            {
                // 고유 초상화를 그대로 사용하되 검은색으로 실루엣 처리
                portraitImage.sprite = entry.portrait;
                portraitImage.color  = Color.black;

                nameText.text = "???";
                hpText.text   = "???";
                atkText.text  = "???";

                if (typeText != null)
                {
                    typeText.text = string.Empty;
                }
            }
        }

        #endregion
    }
}
