using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;


public class ChoicePanel : MonoBehaviour
{
    public static ChoicePanel instance { get; private set; }

    private const float BUTTON_MIN_WIDTH = 1150f;
    private const float BUTTON_MAX_WIDTH = 1800f;
    private const float BUTTON_WIDTH_PADDING = 0f;

    private const float BUTTON_HEIGHT_PER_LINE = 50f;
    private const float BUTTON_HEIGHT_PADDING = 10f;

    private const string SPRITE_NORMAL = "Graphics/UI/Dialogue/Choice_List";
    private const string SPRITE_SELECTED = "Graphics/UI/Dialogue/Choice_Selected";
    private const float SELECTED_SCALE = 1.08f;   // 선택 중인 선택지를 살짝 키운다

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private LayoutGroup buttonLayoutGroup;

    private CanvasGroupController cg = null;
    private List<ChoiceButton> buttons = new List<ChoiceButton>();
    public  ChoicePanelDecision lastDecision { get; private set; } = null;
    public bool isWaitingOnUserChoice { get; private set; } = false;
    private GameObject selectedButton = null;   // 마지막으로 선택돼 있던 선택지
    private bool blockedByLog = false;          // 히스토리 로그가 열려 있어서 선택지를 잠가 둔 상태
    private Sprite normalSprite, selectedSprite;

    private void Awake()
    {
        instance = this;
        cg = new CanvasGroupController(this, canvasGroup);
        cg.alpha = 0f;
        cg.SetInteractableState(false);

        normalSprite = Resources.Load<Sprite>(SPRITE_NORMAL);
        selectedSprite = Resources.Load<Sprite>(SPRITE_SELECTED);
    }

    void Start()
    {

    }

    private void Update()
    {
        UpdateSelectionVisuals();

        // 히스토리 로그를 보는 동안에는 선택지 이동/확정을 막는다
        bool logOpen = History.HistoryManager.instance.logManager.isOpen;
        if (isWaitingOnUserChoice && logOpen != blockedByLog)
        {
            blockedByLog = logOpen;
            cg.SetInteractableState(active: !logOpen);
        }
        if (logOpen)
            return;

        // UI 모듈의 Submit 은 Enter/패드 A 뿐이라 Space 는 직접 처리한다
        if (isWaitingOnUserChoice && selectedButton != null && !SettingsPanel.IsOpen && !History.HistoryManager.instance.logManager.isOpen
            && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            selectedButton.GetComponent<Button>()?.onClick.Invoke();
            return;
        }

        if (!isWaitingOnUserChoice || selectedButton == null || EventSystem.current == null)
            return;

        GameObject current = EventSystem.current.currentSelectedGameObject;

        if (current == null)
        {
            // 선택이 풀리면(설정창을 닫은 직후 등) 키보드/게임패드로 고를 수 없으므로 되돌린다
            EventSystem.current.SetSelectedGameObject(selectedButton);
        }
        else if (current.transform.parent == buttonLayoutGroup.transform)
        {
            selectedButton = current;
        }
    }

    // 선택된 선택지는 Choice_Selected 그림 + 살짝 확대, 나머지는 Choice_List 그림
    private void UpdateSelectionVisuals()
    {
        foreach (ChoiceButton choice in buttons)
        {
            bool isSelected = isWaitingOnUserChoice && choice.button.gameObject == selectedButton;
            Image image = choice.button.image;

            Sprite sprite = isSelected ? selectedSprite : normalSprite;
            if (image != null && sprite != null)
                image.sprite = sprite;

            choice.button.transform.localScale = isSelected ? Vector3.one * SELECTED_SCALE : Vector3.one;
        }
    }

    public void Show(string question, string[] choices)
    {
        lastDecision = new ChoicePanelDecision(question, choices);

        isWaitingOnUserChoice = true;
        selectedButton = null;
        blockedByLog = false;

        // 선택지가 떠 있는 동안 계속 진행 프롬프트는 숨긴다
        DIALOGUE.DialogueSystem.instance.prompt.Hide();

        cg.Show();
        cg.SetInteractableState(active: true);

        titleText.text = question;

        StartCoroutine(GenerateChoices(choices));

    }

    private IEnumerator GenerateChoices(string[] choices)
    {
        float maxWidth = 0f;

        for (int i = 0; i < choices.Length; i++)
        {
            ChoiceButton choiceButton;
            if (i < buttons.Count)
            {
                choiceButton = buttons[i];
            }
            else
            {
                GameObject newButtonObject = Instantiate(choiceButtonPrefab, buttonLayoutGroup.transform);
                newButtonObject.SetActive(true);

                Button newButton = newButtonObject.GetComponent<Button>();
                TextMeshProUGUI newTitle = newButton.GetComponentInChildren<TextMeshProUGUI>();
                LayoutElement newLayout = newButton.GetComponent<LayoutElement>();

                newButton.transition = Selectable.Transition.None;   // 색 대신 그림 교체로 표시 (UpdateSelectionVisuals)

                choiceButton = new ChoiceButton { button = newButton, layout = newLayout, title = newTitle};

                buttons.Add(choiceButton);
            }

            choiceButton.button.onClick.RemoveAllListeners();
            int buttonIndex = i;
            choiceButton.button.onClick.AddListener(() => AcceptAnswer(buttonIndex));
            choiceButton.title.text = choices[i];

            float buttonWidth = Mathf.Clamp(BUTTON_WIDTH_PADDING + choiceButton.title.preferredWidth, BUTTON_MIN_WIDTH, BUTTON_MAX_WIDTH);
            maxWidth = Mathf.Max(maxWidth, buttonWidth);
        }
    
        foreach(var button in buttons)
        {
            button.layout.preferredWidth = maxWidth;
        }

        for(int i =0; i < buttons.Count; i++)
        {
            bool show = i < choices.Length;
            buttons[i].button.gameObject.SetActive(show);

            yield return new WaitForEndOfFrame();

            foreach(var button in buttons)
            {
                int lines = button.title.textInfo.lineCount;
                button.layout.preferredHeight = BUTTON_HEIGHT_PADDING + (lines * BUTTON_HEIGHT_PER_LINE);
            }
        }

        SetupNavigation(choices.Length);
    }

    // 마우스 없이 키보드/게임패드로 고른다 : 위/아래로 이동(끝에서 반대쪽 끝으로 넘어감), Space/Enter/패드 A 로 확정.
    // 확정은 EventSystem 의 Submit 이 선택된 버튼의 onClick 을 호출한다
    private void SetupNavigation(int count)
    {
        if (count <= 0 || !isWaitingOnUserChoice)
            return;

        for (int i = 0; i < count; i++)
        {
            Navigation navigation = new Navigation { mode = Navigation.Mode.Explicit };
            navigation.selectOnUp = buttons[(i - 1 + count) % count].button;
            navigation.selectOnDown = buttons[(i + 1) % count].button;
            buttons[i].button.navigation = navigation;
        }

        selectedButton = buttons[0].button.gameObject;

        // 설정창이 열려 있으면 그쪽 선택을 뺏지 않는다. 닫히면 Update 가 선택해 준다
        if (EventSystem.current != null && !SettingsPanel.IsOpen)
        {
            EventSystem.current.SetSelectedGameObject(selectedButton);
        }
    }

    public void Hide()
    {
        cg.Hide();
        cg.SetInteractableState(false);

        if (selectedButton != null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == selectedButton)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        selectedButton = null;
    }

    private void AcceptAnswer(int index)
    {
        if(index < 0 || index > lastDecision.choices.Length - 1)
        {
            Debug.LogError($"Invalid choice index: {index}");
            return;
        }
        lastDecision.answerIndex = index;
        isWaitingOnUserChoice = false;
        Hide();
    }

    public class ChoicePanelDecision
    {
        public string question = string.Empty;
        public int answerIndex = -1;
        public string[] choices = new string[0];

        public ChoicePanelDecision(string question, string[] choices)
        {
            this.question = question;
            this.choices = choices;
            answerIndex = -1;
        }
    }
    
    private struct ChoiceButton
    {
        public Button button;
        public TextMeshProUGUI title;
        public LayoutElement layout;

    }
}
