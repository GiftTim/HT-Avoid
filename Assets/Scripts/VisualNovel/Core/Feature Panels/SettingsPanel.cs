using System;
using AVOID;
using DIALOGUE;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using PlayerInputManager = DIALOGUE.PlayerInputManager;

/// <summary>
/// 설정창. VN 씬에 하나만 두고, VN(Menu 버튼 / Esc / 게임패드 Start)과
/// 피하기(Esc / Start) 양쪽에서 같은 창을 연다.
/// 열려 있는 동안 Time.timeScale 을 0 으로 만들고 VN 입력을 끈다(피하기 중이면 이미 꺼져 있음).
/// 조작은 EventSystem(UI 내비게이션) 기준 : 위/아래 = 항목 이동, 좌/우 = 값 변경, Esc/패드 B/Start = 닫기.
/// UI 는 지정하지 않으면 코드로 임시 생성한다 (정식 디자인 전까지 사용).
/// </summary>
public class SettingsPanel : MonoBehaviour
{
	private const string KEY_MASTER		= "Settings.MasterVolume";
	private const string KEY_MUSIC		= "Settings.MusicVolume";
	private const string KEY_SFX		= "Settings.SfxVolume";
	private const string KEY_TEXT_SPEED	= "Settings.TextSpeed";
	private const string KEY_AUTO_SPEED	= "Settings.AutoSpeed";

	private const string PARAM_MASTER	= "MasterVolume";	// Main.mixer 에 노출된 파라미터 이름
	private const string PARAM_MUSIC	= "MusicVolume";
	private const string PARAM_SFX		= "SFXVolume";

	public	static	SettingsPanel	instance	{ private set; get; }

	/// <summary>
	/// 설정창이 열려 있는지. 피하기 쪽 입력이 이 값을 보고 조작을 무시한다
	/// </summary>
	public	static	bool			IsOpen		=> instance != null && instance.isOpen;

	public	event	Action			onOpened;
	public	event	Action			onClosed;

	[SerializeField]
	private	Button		menuButton;				// VN 의 Menu 버튼 (비워두면 연결 안 함)

	private	bool			isOpen;
	private	float			prevTimeScale = 1;
	private	bool			prevVNInputEnabled;
	private	GameObject		root;
	private	Slider			firstSlider;
	private	Slider			masterSlider, musicSlider, sfxSlider, textSpeedSlider, autoSpeedSlider;
	private	readonly System.Collections.Generic.List<Action> refreshers = new System.Collections.Generic.List<Action>();

	private void Awake()
	{
		instance = this;
	}

	private void OnDestroy()
	{
		if ( instance == this )
		{
			instance = null;

			if ( isOpen ) Time.timeScale = prevTimeScale;
		}
	}

	private void Start()
	{
		BuildUI();

		masterSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(KEY_MASTER, 1));
		musicSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(KEY_MUSIC, 1));
		sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(KEY_SFX, 1));
		textSpeedSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(KEY_TEXT_SPEED, 1));
		autoSpeedSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(KEY_AUTO_SPEED, 1));

		// SetValueWithoutNotify 는 값 글자를 갱신하지 않으므로 따로 갱신
		foreach ( Action refresh in refreshers ) refresh();

		ApplyAll();

		if ( menuButton != null )
		{
			menuButton.onClick.AddListener(Open);
		}

		root.SetActive(false);
	}

	private void Update()
	{
		if ( isOpen )
		{
			if ( CancelPressed() || PausePressed() ) Close();
		}
		else if ( PausePressed() )
		{
			// 피하기 중에는 클리어/시간 초과 연출 중이 아닐 때만 열 수 있다
			if ( GameController.Instance == null || GameController.Instance.IsGamePlay )
			{
				Open();
			}
		}
	}

	public void Open()
	{
		if ( isOpen ) return;
		isOpen = true;

		prevTimeScale	= Time.timeScale;
		Time.timeScale	= 0;

		// VN 입력(Space/Enter 로 대사 넘기기)이 설정창 조작과 겹치지 않게 끈다. 피하기 중이면 이미 꺼져 있다
		PlayerInputManager input = PlayerInputManager.instance;
		prevVNInputEnabled = input != null && input.IsInputEnabled;
		if ( input != null ) input.SetInputEnabled(false);

		root.SetActive(true);

		if ( EventSystem.current != null )
		{
			EventSystem.current.SetSelectedGameObject(null);
			EventSystem.current.SetSelectedGameObject(firstSlider.gameObject);
		}

		onOpened?.Invoke();
	}

	public void Close()
	{
		if ( isOpen == false ) return;
		isOpen = false;

		root.SetActive(false);
		PlayerPrefs.Save();

		PlayerInputManager input = PlayerInputManager.instance;
		if ( input != null && prevVNInputEnabled ) input.SetInputEnabled(true);

		if ( EventSystem.current != null ) EventSystem.current.SetSelectedGameObject(null);

		Time.timeScale = prevTimeScale;

		onClosed?.Invoke();
	}

	// 열기/닫기 입력. VN/피하기의 PlayerInput 과 별개로 직접 정의한다
	// (VN 입력은 피하기 중 꺼져 있고, 설정창은 어느 쪽이든 열려야 하므로).
	// Keyboard.current 폴링 대신 InputAction 을 써서 모든 키보드 장치의 입력을 받는다
	private readonly InputAction pauseAction	= new InputAction("Pause",	InputActionType.Button);
	private readonly InputAction cancelAction	= new InputAction("Cancel",	InputActionType.Button);

	private void OnEnable()
	{
		if ( pauseAction.bindings.Count == 0 )
		{
			pauseAction.AddBinding("<Keyboard>/escape");
			pauseAction.AddBinding("<Gamepad>/start");
			cancelAction.AddBinding("<Gamepad>/buttonEast");
		}

		pauseAction.Enable();
		cancelAction.Enable();
	}

	private void OnDisable()
	{
		pauseAction.Disable();
		cancelAction.Disable();
	}

	private bool PausePressed()		=> pauseAction.WasPressedThisFrame();
	private bool CancelPressed()	=> cancelAction.WasPressedThisFrame();

	// ------------------------------------------------------------------ 값 적용

	private void ApplyAll()
	{
		ApplyVolume(PARAM_MASTER,	masterSlider.value);
		ApplyVolume(PARAM_MUSIC,	musicSlider.value);
		ApplyVolume(PARAM_SFX,		sfxSlider.value);
		ApplyTextSpeed(textSpeedSlider.value);
		ApplyAutoSpeed(autoSpeedSlider.value);
	}

	private static void ApplyVolume(string parameter, float value)
	{
		if ( AudioManager.instance == null || AudioManager.instance.musicMixer == null ) return;

		// 슬라이더 0~1 → 데시벨(-80 ~ 0)
		float decibel = value <= 0.0001f ? -80 : Mathf.Log10(value) * 20;
		AudioManager.instance.musicMixer.audioMixer.SetFloat(parameter, decibel);
	}

	private static void ApplyTextSpeed(float value)
	{
		if ( DialogueSystem.instance == null || DialogueSystem.instance.conversationManager == null ) return;

		DialogueSystem.instance.conversationManager.architect.speed = value;
	}

	private static void ApplyAutoSpeed(float value)
	{
		AutoReader autoReader = FindFirstObjectByType<AutoReader>();
		if ( autoReader != null ) autoReader.speed = value;
	}

	// ------------------------------------------------------------------ 임시 UI 생성

	private void BuildUI()
	{
		TMP_FontAsset font = null;
		if ( DialogueSystem.instance != null && DialogueSystem.instance.config != null )
		{
			font = DialogueSystem.instance.config.defaultFont;
		}

		root = new GameObject("SettingsPanel-UI", typeof(RectTransform));
		root.transform.SetParent(transform, false);

		Canvas canvas		= root.AddComponent<Canvas>();
		canvas.renderMode	= RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder	= 500;	// 피하기 HUD(100)/VN 위

		CanvasScaler scaler				= root.AddComponent<CanvasScaler>();
		scaler.uiScaleMode				= CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution		= new Vector2(1920, 1080);
		scaler.matchWidthOrHeight		= 0.5f;

		root.AddComponent<GraphicRaycaster>();

		// 화면 전체를 어둡게
		Image dim = CreateImage("Dim", root.transform, new Color(0, 0, 0, 0.7f));
		Stretch(dim.rectTransform);

		// 가운데 창
		Image window = CreateImage("Window", root.transform, new Color(0.08f, 0.08f, 0.12f, 0.95f));
		SetRect(window.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 720));

		CreateText("Title", window.transform, font, "설정", 56, new Vector2(0, 290), new Vector2(800, 80), TextAlignmentOptions.Center);

		masterSlider	= CreateRow(window.transform, font, "전체 볼륨",	0, 1,	200, v => { PlayerPrefs.SetFloat(KEY_MASTER, v);	ApplyVolume(PARAM_MASTER, v); },	false);
		musicSlider		= CreateRow(window.transform, font, "배경음",		0, 1,	100, v => { PlayerPrefs.SetFloat(KEY_MUSIC, v);		ApplyVolume(PARAM_MUSIC, v); },		false);
		sfxSlider		= CreateRow(window.transform, font, "효과음",		0, 1,	0,	 v => { PlayerPrefs.SetFloat(KEY_SFX, v);		ApplyVolume(PARAM_SFX, v); },		false);
		textSpeedSlider	= CreateRow(window.transform, font, "텍스트 속도",	0.5f, 3, -100, v => { PlayerPrefs.SetFloat(KEY_TEXT_SPEED, v);	ApplyTextSpeed(v); },		true);
		autoSpeedSlider	= CreateRow(window.transform, font, "오토 속도",	0.5f, 3, -200, v => { PlayerPrefs.SetFloat(KEY_AUTO_SPEED, v);	ApplyAutoSpeed(v); },		true);

		firstSlider = masterSlider;

		// 닫기 버튼
		Button closeButton = CreateButton(window.transform, font, "닫기", new Vector2(0, -290), new Vector2(300, 80));
		closeButton.onClick.AddListener(Close);

		// 위/아래로만 이동하도록 내비게이션 고정 (좌/우는 슬라이더 값 변경에 쓰임)
		Selectable[] order = { masterSlider, musicSlider, sfxSlider, textSpeedSlider, autoSpeedSlider, closeButton };
		for ( int i = 0; i < order.Length; i ++ )
		{
			Navigation navigation	= new Navigation { mode = Navigation.Mode.Explicit };
			navigation.selectOnUp	= order[(i - 1 + order.Length) % order.Length];
			navigation.selectOnDown	= order[(i + 1) % order.Length];
			order[i].navigation		= navigation;
		}
	}

	private Slider CreateRow(Transform parent, TMP_FontAsset font, string label, float min, float max, float y, UnityEngine.Events.UnityAction<float> onChanged, bool showValue)
	{
		CreateText(label + "-Label", parent, font, label, 36, new Vector2(-300, y), new Vector2(300, 60), TextAlignmentOptions.Left);

		Slider slider = CreateSlider(label + "-Slider", parent, new Vector2(80, y), new Vector2(420, 40), min, max);

		TextMeshProUGUI valueText = CreateText(label + "-Value", parent, font, "", 32, new Vector2(400, y), new Vector2(120, 60), TextAlignmentOptions.Right);

		Action refresh = () => valueText.text = showValue ? $"x{slider.value:0.0}" : $"{Mathf.RoundToInt(slider.value * 100)}%";
		refreshers.Add(refresh);

		slider.onValueChanged.AddListener(v =>
		{
			refresh();
			onChanged(v);
		});

		return slider;
	}

	private static Slider CreateSlider(string name, Transform parent, Vector2 position, Vector2 size, float min, float max)
	{
		GameObject sliderObject = new GameObject(name, typeof(RectTransform));
		sliderObject.transform.SetParent(parent, false);
		SetRect((RectTransform)sliderObject.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);

		Image background = CreateImage("Background", sliderObject.transform, new Color(0.25f, 0.25f, 0.3f, 1));
		SetRect(background.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(0, 14));

		GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
		fillArea.transform.SetParent(sliderObject.transform, false);
		SetRect((RectTransform)fillArea.transform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(-20, 14));

		Image fill = CreateImage("Fill", fillArea.transform, new Color(0.9f, 0.35f, 0.4f, 1));
		fill.rectTransform.anchorMin	= Vector2.zero;
		fill.rectTransform.anchorMax	= Vector2.one;
		fill.rectTransform.sizeDelta	= new Vector2(10, 0);

		GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
		handleArea.transform.SetParent(sliderObject.transform, false);
		SetRect((RectTransform)handleArea.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-20, 0));

		Image handle = CreateImage("Handle", handleArea.transform, Color.white);
		handle.rectTransform.sizeDelta = new Vector2(28, 40);

		Slider slider			= sliderObject.AddComponent<Slider>();
		slider.fillRect			= fill.rectTransform;
		slider.handleRect		= handle.rectTransform;
		slider.targetGraphic	= handle;
		slider.direction		= Slider.Direction.LeftToRight;
		slider.minValue			= min;
		slider.maxValue			= max;

		// 선택(포커스)된 슬라이더가 눈에 띄도록 손잡이 색을 바꿈
		ColorBlock colors		= slider.colors;
		colors.normalColor		= Color.white;
		colors.selectedColor	= new Color(1f, 0.85f, 0.2f, 1);
		colors.highlightedColor	= new Color(1f, 0.95f, 0.6f, 1);
		slider.colors			= colors;

		return slider;
	}

	private static Button CreateButton(Transform parent, TMP_FontAsset font, string label, Vector2 position, Vector2 size)
	{
		Image image = CreateImage(label + "-Button", parent, new Color(0.25f, 0.25f, 0.3f, 1));
		SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);

		Button button		= image.gameObject.AddComponent<Button>();
		button.targetGraphic = image;

		ColorBlock colors		= button.colors;
		colors.selectedColor	= new Color(1f, 0.85f, 0.2f, 1);
		colors.highlightedColor	= new Color(0.5f, 0.5f, 0.6f, 1);
		button.colors			= colors;

		TextMeshProUGUI text = CreateText(label + "-Text", image.transform, font, label, 36, Vector2.zero, size, TextAlignmentOptions.Center);
		text.color = Color.black;
		text.raycastTarget = false;

		return button;
	}

	private static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, string content, float fontSize, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
	{
		GameObject textObject = new GameObject(name, typeof(RectTransform));
		textObject.transform.SetParent(parent, false);

		TextMeshProUGUI text	= textObject.AddComponent<TextMeshProUGUI>();
		if ( font != null ) text.font = font;
		text.text				= content;
		text.fontSize			= fontSize;
		text.alignment			= alignment;
		text.color				= Color.white;
		text.raycastTarget		= false;

		SetRect(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);

		return text;
	}

	private static Image CreateImage(string name, Transform parent, Color color)
	{
		GameObject imageObject = new GameObject(name, typeof(RectTransform));
		imageObject.transform.SetParent(parent, false);

		Image image = imageObject.AddComponent<Image>();
		image.color = color;

		return image;
	}

	private static void Stretch(RectTransform rect)
	{
		rect.anchorMin	= Vector2.zero;
		rect.anchorMax	= Vector2.one;
		rect.offsetMin	= Vector2.zero;
		rect.offsetMax	= Vector2.zero;
	}

	private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
	{
		rect.anchorMin			= anchorMin;
		rect.anchorMax			= anchorMax;
		rect.anchoredPosition	= position;
		rect.sizeDelta			= size;
	}
}
