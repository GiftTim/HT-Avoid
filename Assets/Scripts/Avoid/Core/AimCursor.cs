using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 마우스 커서 위치를 따라다니는 조준용 Sprite. 시스템 커서는 숨기고 이
/// Sprite로 대체한다. Player/Ground/Fly와 동일하게 월드 스페이스
/// SpriteRenderer로 orthographic 카메라 기준 좌표에 배치하므로, UI Canvas
/// 스케일링(CanvasScaler)의 영향을 받지 않아 해상도·화면비가 달라져도
/// 크기가 달라지지 않는다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AimCursor : MonoBehaviour
{
	[SerializeField]
	private	Camera	targetCamera;	// 비워두면 Camera.main 사용. VN·Avoid 씬이 동시에 로드되어 있으면 MainCamera 태그가 겹치므로 이 씬의 카메라를 직접 지정할 것

	private void Awake()
	{
		if ( targetCamera == null )
			targetCamera = Camera.main;
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
		if ( Mouse.current == null || targetCamera == null )
			return;

		Vector2	screenPos	= Mouse.current.position.ReadValue();
		screenPos.x	= Mathf.Clamp(screenPos.x, 0, Screen.width);
		screenPos.y	= Mathf.Clamp(screenPos.y, 0, Screen.height);

		Ray		ray		= targetCamera.ScreenPointToRay(screenPos);
		Plane	plane	= new Plane(Vector3.forward, new Vector3(0, 0, transform.position.z));

		if ( plane.Raycast(ray, out float distance) )
			transform.position = ray.GetPoint(distance);
	}
}
