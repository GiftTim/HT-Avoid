using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DIALOGUE
{
    public class DialogueContinuePrompt : MonoBehaviour
    {
        private RectTransform root;

        [SerializeField] private Animator anim;
        [SerializeField] private TextMeshProUGUI tmpro;
        [SerializeField] private float yOffset = 10f;

        public bool isShowing => anim.gameObject.activeSelf;

        // 기본은 숨김. 대사가 끝나 입력을 기다릴 때만 Show() 로 나타난다
        void Awake()
        {
            Hide();
        }

        // Start is called before the first frame update
        void Start()
        {
            root = GetComponent<RectTransform>();
        }

        public void Show()
        {
            if (tmpro.text == string.Empty)
            {
                if (isShowing)
                {
                    Hide();
                }
                return;
            }

            anim.gameObject.SetActive(true);

            // 씬에 배치한 위치에 그대로 둔다. 아래는 대사 마지막 글자 바로 뒤로 따라붙던 이전 방식 (나중에 쓸 수 있어 보관)
            /*
            tmpro.ForceMeshUpdate();

            root.transform.SetParent(tmpro.transform);
            int lastCharIndex = tmpro.textInfo.characterCount - 1;

            while (lastCharIndex > 0 &&
                  (tmpro.textInfo.characterInfo[lastCharIndex].character == ' ' ||
                   tmpro.textInfo.characterInfo[lastCharIndex].character == '\n'))
            {
                lastCharIndex--;
            }

            TMP_CharacterInfo finalCharacter = tmpro.textInfo.characterInfo[lastCharIndex];
            Vector3 targetPos = finalCharacter.bottomRight;
            float characterWidth = finalCharacter.pointSize * 0.5f;

            targetPos = new Vector3(targetPos.x + characterWidth, targetPos.y + yOffset, 0f);

            root.localPosition = targetPos;
            */
        }

        public void Hide()
        {
            anim.gameObject.SetActive(false);
        }

    }
}


