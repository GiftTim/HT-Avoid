using System;
using UnityEngine;

namespace RHYTHM
{
	/// <summary>
	/// 리듬 판의 체력 규칙. NoteManager 의 판정 결과(OnJudged)를 받아 플레이어 HP 를 관리한다.
	///  - Miss (맞음) : 플레이어 HP 가 탄환 크기만큼(20/30/40) 감소. 0 이 되면 실패
	///  - Pass / Dodged : HP 유지 (Dodged 는 점수/정확도만 감점 — RhythmScore 담당)
	///  - 모든 노트가 판정되면 클리어 (곡 완주)
	/// 보스는 죽지 않는다 (보스 HP 없음). 반격 성공의 연출/점수/피버는 OnJudged 를 받는 쪽에서 처리한다.
	/// </summary>
	public class RhythmBattle : MonoBehaviour
	{
		[SerializeField] private RhythmConductor	conductor;
		[SerializeField] private NoteManager		noteManager;

		[Header("Player")]
		[SerializeField] private int	playerMaxHp	= 300;	// 기본 캐릭터 (확정 26.10.15)

		public	int		PlayerHp		{ get; private set; }
		public	int		PlayerMaxHp		=> playerMaxHp;
		public	bool	IsFinished		{ get; private set; }

		/// <summary> (현재 HP, 최대 HP) </summary>
		public event Action<int, int>	OnPlayerHpChanged;
		public event Action				OnFailed;
		public event Action				OnCleared;

		/// <summary>
		/// HP 를 초기화하고 판정 이벤트를 구독한다. 곡 재생(Conductor.Play) 전에 호출
		/// </summary>
		public void Begin()
		{
			PlayerHp	= playerMaxHp;
			IsFinished	= false;

			noteManager.OnJudged -= HandleJudged;
			noteManager.OnJudged += HandleJudged;

			OnPlayerHpChanged?.Invoke(PlayerHp, playerMaxHp);
		}

		private void OnDestroy()
		{
			if ( noteManager != null ) noteManager.OnJudged -= HandleJudged;
		}

		private void HandleJudged(NoteEntry note, Judgment judgment, float delta)
		{
			if ( IsFinished ) return;

			if ( judgment == Judgment.Miss )
			{
				PlayerHp = Mathf.Max(0, PlayerHp - note.size.Damage());
				OnPlayerHpChanged?.Invoke(PlayerHp, playerMaxHp);

				if ( PlayerHp <= 0 )
				{
					Finish();
					OnFailed?.Invoke();
					return;
				}
			}

			// 마지막 노트까지 판정이 끝나면 클리어 (마지막 노트의 Miss 로 쓰러지는 경우는 위에서 이미 처리)
			if ( noteManager.JudgedCount >= noteManager.TotalCount )
			{
				Finish();
				OnCleared?.Invoke();
			}
		}

		private void Finish()
		{
			IsFinished = true;
			if ( conductor != null ) conductor.Stop();
		}
	}
}
