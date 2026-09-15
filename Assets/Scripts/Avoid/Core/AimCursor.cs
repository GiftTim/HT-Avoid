using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 마우스 커서 위치를 따라다니는 조준용 Sprite(UI Image). 시스템 커서는
/// 숨기고 이 Sprite로 대체한다. Screen Space - Overlay Canvas 아래
/// RectTransform이 붙은 오브젝트에 부착해서 쓴다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class AimCursor : MonoBehaviour
{
	private	RectTransform	rectTransform;
	private	RectTransform	parentRectTransform;
	private	Canvas			canvas;

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
		parentRectTransform = rectTransform.parent as RectTransform;
		canvas = GetComponentInParent<Canvas>();
	}

	private void OnEnable()
	{
		Cursor.visible = false;
	}

	private void OnDisable()
	{
		Cursor.visible = true;
	}

	private void Update()
	{
		if ( Mouse.current == null || canvas == null || parentRectTransform == null )
			return;

		// Screen Space - Overlay는 카메라가 필요 없고, Camera/World 모드는
		// 캔버스에 지정된 렌더 카메라 기준으로 스크린 좌표를 변환해야 한다.
		Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

		if ( RectTransformUtility.ScreenPointToLocalPointInRectangle( parentRectTransform, Mouse.current.position.ReadValue(), eventCamera, out Vector2 localPoint ) )
			rectTransform.anchoredPosition = localPoint;
	}
}
