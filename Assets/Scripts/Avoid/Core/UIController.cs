using UnityEngine;
using TMPro;

namespace AVOID
{
	/// <summary>
	/// 피하기 화면의 UI 표시. 남은 시간과 목적지까지 남은 거리를 갱신한다.
	/// 텍스트를 인스펙터에서 지정하지 않으면 임시 HUD(오버레이 캔버스)를 직접 만들어 쓴다.
	/// </summary>
	public class UIController : MonoBehaviour
	{
		[SerializeField]
		private	GameController	gameController;

		[SerializeField]
		private	TextMeshProUGUI	textTime;		// 남은 시간 (비워두면 임시 HUD 생성)
		[SerializeField]
		private	TextMeshProUGUI	textDistance;	// 남은 거리 (비워두면 임시 HUD 생성)

		[SerializeField]
		private	TextMeshProUGUI	textMessage;	// 클리어/시간 초과 문구 (비워두면 임시 HUD 생성)

		[SerializeField]
		private	float			warningTime = 10;	// 이 시간 이하로 남으면 시간 글자를 빨갛게

		private void Awake()
		{
			if ( gameController == null )
			{
				gameController = GetComponent<GameController>();
			}

			if ( textTime == null || textDistance == null || textMessage == null )
			{
				CreatePlaceholderHUD();
			}

			textMessage.gameObject.SetActive(false);
		}

		/// <summary>
		/// 화면 가운데에 큰 문구를 띄운다 (클리어/시간 초과). 한 판이 끝날 때까지 유지된다
		/// </summary>
		public void ShowMessage(string message)
		{
			textMessage.text = message;
			textMessage.gameObject.SetActive(true);
		}

		private void Update()
		{
			if ( gameController == null ) return;

			float time = Mathf.Max(0, gameController.RemainingTime);

			textTime.text		= $"{(int)time / 60}:{time % 60:00.0}";
			textTime.color		= time <= warningTime ? Color.red : Color.white;
			textDistance.text	= $"{gameController.RemainingDistance:F0}m";
		}

		// 정식 UI 를 만들기 전까지 쓰는 임시 HUD. VN 씬 위에 얹히므로 정렬 순서를 높게 준다
		private void CreatePlaceholderHUD()
		{
			GameObject canvasObject = new GameObject("Canvas-PlaceholderHUD");
			canvasObject.transform.SetParent(transform);

			Canvas canvas		= canvasObject.AddComponent<Canvas>();
			canvas.renderMode	= RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder	= 100;

			if ( textTime == null )
			{
				textTime = CreateText(canvas.transform, "TextTime", new Vector2(0.5f, 1), new Vector2(0, -30), 72);
			}
			if ( textDistance == null )
			{
				textDistance = CreateText(canvas.transform, "TextDistance", new Vector2(0.5f, 1), new Vector2(0, -110), 40);
			}
			if ( textMessage == null )
			{
				textMessage = CreateText(canvas.transform, "TextMessage", new Vector2(0.5f, 0.5f), Vector2.zero, 120);
				textMessage.alignment = TextAlignmentOptions.Center;
				textMessage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
				textMessage.rectTransform.sizeDelta = new Vector2(1200, 200);
			}
		}

		private TextMeshProUGUI CreateText(Transform parent, string name, Vector2 anchor, Vector2 position, float fontSize)
		{
			GameObject textObject = new GameObject(name);
			textObject.transform.SetParent(parent, false);

			TextMeshProUGUI text	= textObject.AddComponent<TextMeshProUGUI>();
			text.fontSize			= fontSize;
			text.alignment			= TextAlignmentOptions.Top;
			text.raycastTarget		= false;

			RectTransform rect		= text.rectTransform;
			rect.anchorMin			= anchor;
			rect.anchorMax			= anchor;
			rect.pivot				= new Vector2(0.5f, 1);
			rect.anchoredPosition	= position;
			rect.sizeDelta			= new Vector2(600, 100);

			return text;
		}
	}
}
