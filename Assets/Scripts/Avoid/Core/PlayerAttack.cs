using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AVOID
{
	/// <summary>
	/// Ctrl 키 입력 시 플레이어가 마지막으로 이동한 방향(좌/우)으로 공격 판정을 발생시킨다.
	/// 플레이어 위치에서 facing 방향으로 attackDistance만큼 뻗어나가는 캡슐형
	/// 판정을 만들고, enemyLayer에 속하면서 IDamageable을 구현한 대상에게
	/// damage만큼 피해를 준다. 이 캡슐은 ShowAttackEffect가 그리는 이펙트
	/// 스프라이트와 위치·길이·회전이 항상 같아서, 눈에 보이는 공격 범위와
	/// 실제로 맞는 범위가 어긋나지 않는다.
	/// </summary>
	[RequireComponent(typeof(PlayerInput))]
	public class PlayerAttack : MonoBehaviour
	{
		[Header("Attack")]
		[SerializeField]
		private	int				damage = 10;			// 공격력
		[SerializeField]
		private	float			attackDistance = 1.2f;	// 플레이어 기준 공격 판정 길이
		[SerializeField]
		private	float			attackRadius = 0.6f;	// 공격 판정 두께(캡슐 반지름)
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
		private	InputAction		moveAction;
		private	float			lastFacingDirection = 1f;	// 마지막으로 이동한 방향 (+1 오른쪽, -1 왼쪽)
		private	float			lastAttackTime = float.NegativeInfinity;
		private	Coroutine		hideEffectCoroutine;
		private	WaitForSeconds	hideEffectDelay;

		// ApplyDamage에서 재사용하는 버퍼. 콜라이더 하나가 여러 개인 대상이라도
		// 한 번의 공격에 피해를 중복으로 주지 않기 위해 씀
		private readonly HashSet<IDamageable> hitDamageables = new HashSet<IDamageable>();

		private void Awake()
		{
			input			= GetComponent<PlayerInput>();
			attackAction	= input.actions["Attack"];
			moveAction		= input.actions["Move"];

			hideEffectDelay	= new WaitForSeconds(effectDuration);
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

			lastAttackTime = Time.time;

			// 현재 이동 입력이 있으면 그 방향으로 facing 갱신, 없으면 마지막 방향 유지
			float move = moveAction.ReadValue<float>();
			if ( Mathf.Abs(move) > 0.1f )
				lastFacingDirection = Mathf.Sign(move);

			Vector2 origin		= transform.position;
			Vector2 direction	= new Vector2(lastFacingDirection, 0f);
			float	angle		= Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

			ApplyDamage(origin, direction, angle);
			ShowAttackEffect(direction, angle);
		}

		// origin에서 direction 방향으로 attackDistance만큼 뻗은 캡슐 판정.
		// size.x(캡슐 길이) = attackDistance + 반지름*2로 잡아서, 둥근 양 끝이
		// 원점(플레이어)과 사거리 끝을 각각 반지름만큼 자연스럽게 감싸게 한다
		private void ApplyDamage(Vector2 origin, Vector2 direction, float angle)
		{
			Vector2 capsuleCenter	= origin + direction * (attackDistance * 0.5f);
			Vector2 capsuleSize	= new Vector2(attackDistance + attackRadius * 2f, attackRadius * 2f);

			Collider2D[] hits = Physics2D.OverlapCapsuleAll(capsuleCenter, capsuleSize, CapsuleDirection2D.Horizontal, angle, enemyLayer);

			hitDamageables.Clear();
			foreach ( Collider2D hit in hits )
			{
				IDamageable damageable = hit.GetComponentInParent<IDamageable>();
				if ( damageable != null )
					hitDamageables.Add(damageable);
			}

			foreach ( IDamageable damageable in hitDamageables )
			{
				// TakeDamage 구현체(Enemy/Boss 등) 쪽에서 예외가 나더라도 다른 대상에게
				// 준 피해나 이어지는 ShowAttackEffect 호출까지 막히면 안 되므로 격리한다
				try
				{
					damageable.TakeDamage(damage);
				}
				catch ( System.Exception exception )
				{
					Debug.LogException(exception);
				}
			}
		}

		// AttackEffect를 Player 중심 ~ 사거리 끝의 중점에 배치하고 사거리만큼
		// 늘린 뒤, effectDuration 후 다시 숨긴다
		private void ShowAttackEffect(Vector2 direction, float angle)
		{
			if ( attackEffectTransform == null || attackEffectRenderer == null )
				return;

			// 자식 오브젝트라 부모(Player)의 월드 스케일이 로컬 값에 또 곱해지므로 미리 상쇄
			float parentScaleX	= transform.lossyScale.x;
			if ( Mathf.Approximately(parentScaleX, 0f) )
				return;	// 스케일 0이면 나눗셈이 무한대/NaN이 되어 이펙트가 깨짐

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
			yield return hideEffectDelay;

			attackEffectRenderer.enabled	= false;
			hideEffectCoroutine				= null;
		}

	}
}
