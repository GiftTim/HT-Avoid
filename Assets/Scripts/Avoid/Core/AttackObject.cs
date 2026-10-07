using UnityEngine;

namespace AVOID
{
	// 공격에 맞았을 때 플레이어가 보이는 반응. 어느 쪽이든 결과는 시간 손실이다
	public enum HitReaction { Stun = 0, Knockback, Slow }

	/// <summary>
	/// 플레이어가 맞으면 시간을 잃는 공격 오브젝트의 피격 데이터.
	/// 이동은 ApproachingObject 가 맡고, 이 컴포넌트는 "맞으면 어떻게 되는지"만 담는다.
	/// Collider 는 Trigger 로 둘 것 (판정은 PlayerHit 쪽에서 처리).
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class AttackObject : MonoBehaviour
	{
		[SerializeField]
		private	HitReaction	reaction = HitReaction.Stun;
		[SerializeField]
		private	float		reactionTime = 0.8f;		// 멈춤 시간 / 넉백에 걸리는 시간
		[SerializeField]
		private	float		knockbackDistance = 15;		// 넉백으로 잃는 진행 거리 (Knockback 일 때만)
		[SerializeField]
		[Range(0, 1)]
		private	float		slowMultiplier = 0.4f;		// 감속 중 전진 속도 비율 (Slow 일 때만. reactionTime 이 감속 지속 시간)

		// 피격 직후 다른 공격을 막는 시간의 기준. 멈춤/넉백은 반응이 끝날 때까지,
		// 감속은 플레이어가 계속 움직이므로 지속 시간 동안 무적을 걸지 않는다
		public	float		ReactionTime => reaction == HitReaction.Slow ? 0 : reactionTime;

		public void Setup(HitReaction reaction, float reactionTime, float knockbackDistance, float slowMultiplier = 0.4f)
		{
			this.reaction			= reaction;
			this.reactionTime		= reactionTime;
			this.knockbackDistance	= knockbackDistance;
			this.slowMultiplier		= slowMultiplier;
		}

		/// <summary>
		/// 이 공격의 피격 반응을 진행 상태에 적용한다
		/// </summary>
		public void Apply(GameController gameController)
		{
			if ( reaction == HitReaction.Knockback )
			{
				gameController.Knockback(knockbackDistance, reactionTime);
			}
			else if ( reaction == HitReaction.Slow )
			{
				gameController.Slow(slowMultiplier, reactionTime);
			}
			else
			{
				gameController.Stun(reactionTime);
			}
		}
	}
}
