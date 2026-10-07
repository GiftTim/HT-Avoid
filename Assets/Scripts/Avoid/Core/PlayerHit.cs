using System.Collections;
using UnityEngine;

namespace AVOID
{
	/// <summary>
	/// 플레이어의 피격 판정. AttackObject 와 닿으면 그 공격의 반응(멈춤/넉백)을
	/// GameController 에 적용하고, 잠시 무적 + 깜빡임 상태가 된다.
	/// 죽지 않고 시간만 잃는다 (체력 없음).
	/// 3D 트리거 판정에 필요한 Rigidbody(isKinematic)와 Collider 가 없으면 직접 붙인다.
	/// </summary>
	public class PlayerHit : MonoBehaviour
	{
		[Header("Hit Box (Collider 가 없을 때 자동 생성)")]
		[SerializeField]
		private	Vector3	hitBoxCenter = new Vector3(0, 0.44f, 0);
		[SerializeField]
		private	Vector3	hitBoxSize = new Vector3(0.5f, 1.8f, 0.5f);

		[Header("Invincible")]
		[SerializeField]
		private	float	invincibleTime = 1;			// 피격 반응이 끝난 뒤 추가로 이어지는 무적 시간
		[SerializeField]
		private	float	blinkInterval = 0.1f;		// 무적 중 깜빡이는 간격

		private	SpriteRenderer[]	renderers;
		private	bool				isInvincible = false;

		private void Awake()
		{
			renderers = GetComponentsInChildren<SpriteRenderer>();

			// Trigger 이벤트는 둘 중 하나에 Rigidbody 가 있어야 발생한다.
			// 이동은 Transform 으로 직접 하므로 물리에 밀리지 않게 isKinematic 으로 둔다
			Rigidbody rigid = GetComponent<Rigidbody>();
			if ( rigid == null )
			{
				rigid = gameObject.AddComponent<Rigidbody>();
			}
			rigid.isKinematic	= true;
			rigid.useGravity	= false;

			if ( GetComponent<Collider>() == null )
			{
				BoxCollider box	= gameObject.AddComponent<BoxCollider>();
				box.center		= hitBoxCenter;
				box.size		= hitBoxSize;
				box.isTrigger	= true;
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if ( isInvincible == true ) return;

			GameController gameController = GameController.Instance;
			if ( gameController == null || gameController.IsGamePlay == false ) return;

			AttackObject attack = other.GetComponent<AttackObject>();
			if ( attack == null ) return;

			attack.Apply(gameController);

			StartCoroutine(Invincible(attack.ReactionTime + invincibleTime));
		}

		// 연속 피격으로 시간이 한꺼번에 날아가지 않도록 잠시 판정을 끄고 깜빡인다
		private IEnumerator Invincible(float duration)
		{
			isInvincible = true;

			float	elapsed	= 0;
			bool	visible	= true;
			while ( elapsed < duration )
			{
				visible = !visible;
				SetVisible(visible);

				yield return new WaitForSeconds(blinkInterval);
				elapsed += blinkInterval;
			}

			SetVisible(true);
			isInvincible = false;
		}

		private void SetVisible(bool visible)
		{
			foreach ( SpriteRenderer renderer in renderers )
			{
				renderer.enabled = visible;
			}
		}
	}
}
