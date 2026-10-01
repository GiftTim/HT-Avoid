using System.Collections;
using UnityEngine;

namespace AVOID
{
	/// <summary>
	/// 플레이어 체력. imageHP 배열의 길이가 곧 최대 체력이며, 피격될 때마다
	/// 마지막 하트 이미지부터 하나씩 꺼진다. 피격 직후에는 잠시 무적이 되고
	/// 그동안 스프라이트가 붉게 깜빡인다.
	/// </summary>
	public class PlayerHP : MonoBehaviour
	{
		[SerializeField]
		private	GameObject[]	imageHP;					// 체력 UI (하트 이미지)
		private	int				currentHP;

		[SerializeField]
		private	float			invincibilityDuration;		// 무적 지속시간
		private	bool			isInvincibility = false;	// 무적 여부

		[SerializeField]
		private	AudioClip		hitClip;					// 피격 시 재생할 효과음
		[SerializeField]
		private	SpriteRenderer	spriteRenderer;				// 피격 깜빡임용 (Player 루트에는 없으므로 Body 지정)

		private	Color			originColor;

		private void Awake()
		{
			if ( spriteRenderer == null )
			{
				spriteRenderer = GetComponent<SpriteRenderer>();
			}
			if ( spriteRenderer != null )
			{
				originColor = spriteRenderer.color;
			}

			currentHP = imageHP.Length;
		}

		/// <summary>
		/// 피해를 입는다 (외부 클래스에서 호출).
		/// 체력이 더 남아있으면 false, 이번 피격으로 사망하면 true를 반환한다.
		/// </summary>
		public bool TakeDamage()
		{
			// 무적 상태일 때는 체력이 감소하지 않는다
			if ( isInvincibility == true ) return false;

			if ( currentHP > 1 )
			{
				PlayHitSound();
				StartCoroutine(nameof(OnInvincibility));

				currentHP --;
				imageHP[currentHP].SetActive(false);
			}
			else
			{
				return true;
			}

			return false;
		}

		private void PlayHitSound()
		{
			// AudioManager 오브젝트는 VisualNovel 씬에만 있어서 Avoid 단독 Play에서는 null
			if ( hitClip == null || AudioManager.instance == null ) return;

			AudioManager.instance.PlaySoundEffect(hitClip);
		}

		private IEnumerator OnInvincibility()
		{
			isInvincibility = true;

			float current = 0;
			float percent = 0;
			float colorSpeed = 10;

			while ( percent < 1 )
			{
				current += Time.deltaTime;
				percent = current / invincibilityDuration;

				if ( spriteRenderer != null )
				{
					spriteRenderer.color = Color.Lerp(originColor, Color.red, Mathf.PingPong(Time.time * colorSpeed, 1));
				}

				yield return null;
			}

			if ( spriteRenderer != null )
			{
				spriteRenderer.color = originColor;
			}
			isInvincibility = false;
		}
	}
}
