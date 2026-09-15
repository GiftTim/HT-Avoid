using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 클릭 시 마우스 조준 방향으로 공격 판정을 발생시킨다.
/// 플레이어 위치에서 조준 방향으로 attackDistance만큼 떨어진 지점에
/// attackRadius 크기의 원형 판정을 만들고, enemyLayer에 속하면서
/// IDamageable을 구현한 대상에게 damage만큼 피해를 준다.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class PlayerAttack : MonoBehaviour
{
	[Header("Attack")]
	[SerializeField]
	private	int				damage = 10;			// 공격력
	[SerializeField]
	private	float			attackDistance = 1.2f;	// 플레이어 기준 공격 판정까지의 거리
	[SerializeField]
	private	float			attackRadius = 0.6f;	// 공격 판정 반지름
	[SerializeField]
	private	float			attackCooldown = 0.4f;	// 공격 간 최소 간격
	[SerializeField]
	private	LayerMask		enemyLayer;				// 공격 판정에 포함될 레이어

	[Header("Attack Effect")]
	[SerializeField]
	private	Transform		attackEffectTransform;	// Player 자식 AttackEffect
	[SerializeField]
	private	SpriteRenderer	attackEffectRenderer;	// AttackEffect 표시/숨김 제어용
	[SerializeField]
	private	float			effectDuration = 1f;	// AttackEffect가 켜져 있는 시간

	private	PlayerInput		input;
	private	InputAction		attackAction;
	private	Camera			mainCamera;
	private	float			lastAttackTime = float.NegativeInfinity;
	private	Coroutine		hideEffectCoroutine;

	private void Awake()
	{
		input			= GetComponent<PlayerInput>();
		attackAction	= input.actions["Attack"];
		mainCamera		= Camera.main;
	}

	private void OnEnable()
	{
		attackAction.started += OnAttackStarted;
	}

	private void OnDisable()
	{
		attackAction.started -= OnAttackStarted;
	}

	private void OnAttackStarted(InputAction.CallbackContext context)
	{
		if ( Time.time - lastAttackTime < attackCooldown )
			return;

		if ( TryGetAimWorldPoint(out Vector2 aimPoint) == false )
			return;

		lastAttackTime = Time.time;

		Vector2 origin		= transform.position;
		Vector2 toAim		= aimPoint - origin;
		Vector2 direction	= toAim.sqrMagnitude > 0.0001f ? toAim.normalized : Vector2.right;
		Vector2 hitCenter	= origin + direction * attackDistance;

		Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, attackRadius, enemyLayer);
		foreach ( Collider2D hit in hits )
		{
			IDamageable damageable = hit.GetComponentInParent<IDamageable>();
			damageable?.TakeDamage(damage);
		}

		ShowAttackEffect(direction);
	}

	// AttackEffect를 Player 중심 ~ 사거리 끝의 중점에 배치하고 사거리만큼
	// 늘린 뒤, effectDuration 후 다시 숨긴다
	private void ShowAttackEffect(Vector2 direction)
	{
		if ( attackEffectTransform == null || attackEffectRenderer == null )
			return;

		float angle			= Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		// 자식 오브젝트라 부모(Player)의 월드 스케일이 로컬 값에 또 곱해지므로 미리 상쇄
		float parentScaleX	= transform.lossyScale.x;

		attackEffectTransform.localRotation	= Quaternion.Euler(0, 0, angle);
		attackEffectTransform.localPosition	= (Vector3)direction * (attackDistance * 0.5f / parentScaleX);

		Vector3 scale	= attackEffectTransform.localScale;
		scale.x			= attackDistance / parentScaleX;
		attackEffectTransform.localScale = scale;

		attackEffectRenderer.enabled = true;

		if ( hideEffectCoroutine != null )
			StopCoroutine(hideEffectCoroutine);
		hideEffectCoroutine = StartCoroutine(HideEffectAfterDelay());
	}

	private IEnumerator HideEffectAfterDelay()
	{
		yield return new WaitForSeconds(effectDuration);

		attackEffectRenderer.enabled	= false;
		hideEffectCoroutine				= null;
	}

	// 마우스 스크린 좌표를, 플레이어와 같은 z 평면 위의 월드 좌표로 변환
	private bool TryGetAimWorldPoint(out Vector2 worldPoint)
	{
		worldPoint = Vector2.zero;

		if ( Mouse.current == null || mainCamera == null )
			return false;

		Ray		ray		= mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
		Plane	plane	= new Plane(Vector3.forward, new Vector3(0, 0, transform.position.z));

		if ( plane.Raycast(ray, out float distance) == false )
			return false;

		worldPoint = ray.GetPoint(distance);
		return true;
	}
}
