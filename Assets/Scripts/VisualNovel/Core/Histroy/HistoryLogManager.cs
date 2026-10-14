using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace History
{

    public class HistoryLogManager : MonoBehaviour
    {
        private const float LOG_STARTING_HEIGHT = 2f;
        private const float LOG_HEIGHT_PER_LINE = 2f;
        private const float LOG_DEFAULT_HEIGHT = 1f;
        private const float TEXT_DEFAULT_SCALE = 1f;

        private const string NAME_TEXT_NAME = "NameText";
        private const string DIALOGUE_TEXT_NAME = "DialogueText";

        private float logScaling = 1f;

        [SerializeField] private Animator anim;
        [SerializeField] private GameObject logPrefab;

        HistoryManager manager => HistoryManager.instance;
        private List<HistoryLog> logs = new List<HistoryLog>();

        public bool isOpen {get; private set;} = false;

        // [미사용 처리 - 26.10.12] 로그 글자 크기 슬라이더
        // 마우스 없이 키보드/패드 전용으로 조작하는 방향이라 슬라이더를 쓰지 않기로 함
        // 씬에서도 연결(None)돼 있었고 SetLogScaling()을 호출하는 곳도 없었음
        // 되살리려면 이 주석과 아래 SetLogScaling() 주석을 풀고 씬에서 Slider 연결 + On Value Changed 등록
        /*
        [SerializeField] private Slider logScaleSlider;
        */

        private float textScaling => logScaling * 3f;

        private const float TAP_SCROLL_STEP = 0.3f;     // 한 번 누를 때 화면(뷰포트) 높이의 몇 배만큼 스크롤할지
        private const float HOLD_REPEAT_DELAY = 0.4f;   // 꾹 누를 때 반복이 시작되기까지의 시간
        private const float HOLD_REPEAT_INTERVAL = 0.08f;

        private ScrollRect scrollRect;
        private float nextRepeatTime = 0f;

        // 마우스 없이 위/아래 방향키(또는 패드 D-Pad)로 로그를 스크롤한다
        // 한 번 누르면 조금씩, 꾹 누르면 반복해서 내려간다
        private void Update()
        {
            if (!isOpen)
                return;

            float dir = 0f;
            bool pressed = false;
            bool held = false;
            if (Keyboard.current != null)
            {
                var up = Keyboard.current.upArrowKey;
                var down = Keyboard.current.downArrowKey;
                pressed |= up.wasPressedThisFrame || down.wasPressedThisFrame;
                held |= up.isPressed || down.isPressed;
                if (up.isPressed) dir += 1f;
                if (down.isPressed) dir -= 1f;
            }
            if (Gamepad.current != null)
            {
                var dpad = Gamepad.current.dpad;
                pressed |= dpad.up.wasPressedThisFrame || dpad.down.wasPressedThisFrame;
                held |= dpad.up.isPressed || dpad.down.isPressed;
                if (dpad.up.isPressed) dir += 1f;
                if (dpad.down.isPressed) dir -= 1f;
            }

            if (Mathf.Approximately(dir, 0f))
                return;

            if (pressed)
                nextRepeatTime = Time.unscaledTime + HOLD_REPEAT_DELAY;
            else if (held && Time.unscaledTime >= nextRepeatTime)
                nextRepeatTime = Time.unscaledTime + HOLD_REPEAT_INTERVAL;
            else
                return;

            if (scrollRect == null)
                scrollRect = logPrefab.GetComponentInParent<ScrollRect>();
            if (scrollRect == null || scrollRect.content == null || scrollRect.viewport == null)
                return;

            float range = scrollRect.content.rect.height - scrollRect.viewport.rect.height;
            if (range <= 0f)
                return;

            float step = TAP_SCROLL_STEP * scrollRect.viewport.rect.height / range;
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + dir * step);
        }

        public void Open()
        {
            if (isOpen) 
                return;

            anim.Play("Open");
            isOpen = true;
        }

        public void Close()
        {
            if (!isOpen) 
                return;

            anim.Play("Close");
            isOpen = false;
        }
    
        public void AddLog(HistoryState state)
        {
            if(logs.Count >= HistoryManager.HISTORY_CACHE_LIMIT)
            {
                DestroyImmediate(logs[0].container);
                logs.RemoveAt(0);
            }
            
            CreateLog(state);
        }

        private void CreateLog(HistoryState state)
        {
            HistoryLog log = new HistoryLog();
            log.container = Instantiate(logPrefab, logPrefab.transform.parent);
            log.container.SetActive(true);

            log.nameText     = log.container.transform.Find(NAME_TEXT_NAME).GetComponent<TextMeshProUGUI>();
            log.dialogueText = log.container.transform.Find(DIALOGUE_TEXT_NAME).GetComponent<TextMeshProUGUI>();
        
            if(state.dialogue.currentSpeaker == string.Empty)
            {
                log.nameText.text = string.Empty;
            }
            else
            {
                log.nameText.text = state.dialogue.currentSpeaker;
                log.nameText.font = HistoryCache.LoadFont(state.dialogue.speakerFont);
                log.nameText.color = state.dialogue.speakerNameColor;
                log.nameFontSize = TEXT_DEFAULT_SCALE * state.dialogue.speakerScale;
                log.nameText.fontSize = log.nameFontSize +textScaling;
           }
        
            log.dialogueText.text = state.dialogue.currentDialogue;
            log.dialogueText.font = HistoryCache.LoadFont(state.dialogue.dialogueFont);
            log.dialogueText.color = state.dialogue.dialogueColor;
            log.dialogueFontSize = TEXT_DEFAULT_SCALE * state.dialogue.dialogueScale;
            log.dialogueText.fontSize = log.dialogueFontSize + textScaling;

            FitLogToText(log);

            logs.Add(log);
            
        }

        private void FitLogToText(HistoryLog log)
        {
            RectTransform rect = log.dialogueText.GetComponent<RectTransform>();
            ContentSizeFitter textCSF = log.dialogueText.GetComponent<ContentSizeFitter>();

            textCSF.SetLayoutVertical();

            LayoutElement logLayout = log.container.GetComponent<LayoutElement>();
            float height = rect.rect.height;

            float perc = height / LOG_DEFAULT_HEIGHT;
            float extraScale = (LOG_HEIGHT_PER_LINE * perc) - LOG_HEIGHT_PER_LINE;
            float scale = LOG_STARTING_HEIGHT + extraScale;

            logLayout.preferredHeight = scale + textScaling;

            logLayout.preferredHeight += 2f * logScaling;

        }

        // [미사용 처리 - 26.10.12] 글자 크기 슬라이더용 (위 logScaleSlider 주석 참고)
        // logScaling 은 기본값 1f 로 고정되어 로그는 항상 기본 크기로 표시됨
        /*
        public void SetLogScaling()
        {
            logScaling = logScaleSlider.value;

            foreach(HistoryLog log in logs)
            {
                log.nameText.fontSize = log.nameFontSize + textScaling;
                log.dialogueText.fontSize = log.dialogueFontSize + textScaling;

                FitLogToText(log);
            }
        }
        */

        public void Clear()
        {
            for(int i = 0; i < logs.Count; i++)
            {
                DestroyImmediate(logs[i].container);
            }
            
            logs.Clear();
        }

        public void Rebuild()
        {
            foreach(var state in manager.history)
            {
                CreateLog(state);
            }
        }
    }
}

