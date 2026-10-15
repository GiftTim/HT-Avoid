using UnityEngine;

namespace RHYTHM
{
	/// <summary>
	/// 리듬 판의 플레이어 움직임. 물리 없이 곡 시간만으로 위치를 정한다.
	///  - x = startX + 곡 시간 × runSpeed (자동 달리기)
	///  - y = 점프 중이면 박 단위로 고정된 포물선, 아니면 groundY
	/// 장애물/노트도 같은 식(NoteToWorldX)으로 배치하면 판정선과 화면이 항상 맞는다.
	/// </summary>
	public class RhythmPlayerMotion : MonoBehaviour
	{
		[SerializeField] private RhythmConductor	conductor;
		[SerializeField] private NoteManager		noteManager;

		[Header("Run")]
		[SerializeField] private float	startX		= 0f;
		[SerializeField] private float	runSpeed	= 6f;	// 초당 월드 거리
		[SerializeField] private float	groundY		= 0f;

		[Header("Jump")]
		[SerializeField] private float	jumpBeats	= 1f;	// 점프가 지속되는 박 수
		[SerializeField] private float	jumpHeight	= 2f;

		private float jumpStartTime	= float.NegativeInfinity;

		public float RunSpeed => runSpeed;
		public float GroundY  => groundY;

		/// <summary>
		/// 곡 시간(초) → 월드 x. 장애물 배치와 플레이어가 같은 식을 쓴다
		/// </summary>
		public float TimeToWorldX(float time) => startX + time * runSpeed;

		private void OnEnable()
		{
			if ( noteManager != null ) noteManager.OnInput += HandleInput;
		}

		private void OnDisable()
		{
			if ( noteManager != null ) noteManager.OnInput -= HandleInput;
		}

		private void HandleInput(NoteInput input)
		{
			if ( input != NoteInput.Jump || conductor == null ) return;

			// 점프 중 재입력은 무시 (공중 점프는 PoC 이후)
			float duration = JumpDuration();
			if ( conductor.SongTime - jumpStartTime < duration ) return;

			jumpStartTime = conductor.SongTime;
		}

		private float JumpDuration()
		{
			ChartData chart = conductor.Chart;
			return chart != null ? jumpBeats * chart.SecondsPerBeat : 0.5f;
		}

		private void Update()
		{
			if ( conductor == null || !conductor.IsPlaying ) return;

			float time		= conductor.SongTime;
			float duration	= JumpDuration();
			float t			= (time - jumpStartTime) / duration;

			float y = groundY;
			if ( t >= 0 && t < 1 )
			{
				y += jumpHeight * 4f * t * (1f - t);	// 0 → 1 → 0 포물선
			}

			transform.position = new Vector3(TimeToWorldX(Mathf.Max(0f, time)), y, transform.position.z);
		}
	}
}
