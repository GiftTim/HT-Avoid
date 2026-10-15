using System;
using UnityEngine;

namespace RHYTHM
{
	public enum Rank { C = 0, B, A, S }

	/// <summary>
	/// 리듬 판의 점수 / 콤보 / 피버 / 정확도 / 랭크. NoteManager 의 판정 결과(OnJudged)만 구독한다.
	/// HP 는 RhythmBattle 담당이라 서로 모른다.
	///  - 기본 점수 : 탄환 크기별 200/300/400. Great 는 greatScoreRate 배. Dodged/Miss 는 0점
	///  - 피버 : 반격 탄환(J)만 게이지를 채운다. Perfect = 크기별 충전량(1~2), Great = 절반. 60 이 되면 5초간 점수 ×1.5
	///  - 정확도 = (Perfect + Great×0.5 + Pass) / 판정된 노트 수 × 100. Dodged/Miss 는 0
	///  - 풀 콤보(Miss 0) 면 정확도와 관계없이 A 이상
	/// </summary>
	public class RhythmScore : MonoBehaviour
	{
		[SerializeField] private RhythmConductor	conductor;
		[SerializeField] private NoteManager		noteManager;

		[Header("Score")]
		[SerializeField] private float	greatScoreRate		= 0.5f;		// Great 점수 비율 (미확정 — 수치 조정용)
		[SerializeField] private bool	dodgedBreaksCombo	= false;	// 반격 탄환을 피해서 지나간 경우 콤보를 끊을지

		[Header("Fever")]
		[SerializeField] private float	feverMax			= 60f;
		[SerializeField] private float	feverDuration		= 5f;		// 초 (곡 시간 기준)
		[SerializeField] private float	feverScoreRate		= 1.5f;

		[Header("Rank (정확도 %, 확정 전 임시값)")]
		[SerializeField] private float	rankS				= 95f;
		[SerializeField] private float	rankA				= 85f;
		[SerializeField] private float	rankB				= 70f;

		private float	feverEndTime;
		private float	accuracyPoints;

		public	int		Score			{ get; private set; }
		public	int		Combo			{ get; private set; }
		public	int		MaxCombo		{ get; private set; }
		public	float	FeverGauge		{ get; private set; }
		public	bool	IsFever			{ get; private set; }
		public	float	FeverMax		=> feverMax;

		public	int		PerfectCount	{ get; private set; }
		public	int		GreatCount		{ get; private set; }
		public	int		PassCount		{ get; private set; }
		public	int		DodgedCount		{ get; private set; }
		public	int		MissCount		{ get; private set; }
		public	int		JudgedCount		=> PerfectCount + GreatCount + PassCount + DodgedCount + MissCount;

		public	bool	IsFullCombo		=> MissCount == 0;

		/// <summary> 정확도 (%). 지금까지 판정된 노트 기준 </summary>
		public float Accuracy => JudgedCount > 0 ? accuracyPoints / JudgedCount * 100f : 100f;

		public Rank Rank
		{
			get
			{
				float acc = Accuracy;
				Rank rank = acc >= rankS ? Rank.S : acc >= rankA ? Rank.A : acc >= rankB ? Rank.B : Rank.C;
				if ( IsFullCombo && rank < Rank.A ) rank = Rank.A;
				return rank;
			}
		}

		/// <summary> (점수, 콤보) </summary>
		public event Action<int, int>	OnScoreChanged;
		/// <summary> (피버 게이지, 최대) </summary>
		public event Action<float, float> OnFeverGaugeChanged;
		public event Action				OnFeverStarted;
		public event Action				OnFeverEnded;

		/// <summary>
		/// 기록을 초기화하고 판정 이벤트를 구독한다. 곡 재생(Conductor.Play) 전에 호출
		/// </summary>
		public void Begin()
		{
			Score = Combo = MaxCombo = 0;
			PerfectCount = GreatCount = PassCount = DodgedCount = MissCount = 0;
			accuracyPoints	= 0f;
			FeverGauge		= 0f;
			IsFever			= false;

			noteManager.OnJudged -= HandleJudged;
			noteManager.OnJudged += HandleJudged;

			OnScoreChanged?.Invoke(Score, Combo);
			OnFeverGaugeChanged?.Invoke(FeverGauge, feverMax);
		}

		private void OnDestroy()
		{
			if ( noteManager != null ) noteManager.OnJudged -= HandleJudged;
		}

		private void Update()
		{
			if ( IsFever && conductor != null && conductor.SongTime >= feverEndTime )
			{
				IsFever = false;
				FeverGauge = 0f;
				OnFeverGaugeChanged?.Invoke(FeverGauge, feverMax);
				OnFeverEnded?.Invoke();
			}
		}

		private void HandleJudged(NoteEntry note, Judgment judgment, float delta)
		{
			float baseScore = note.size.BaseScore();

			switch ( judgment )
			{
				case Judgment.Perfect:
					PerfectCount ++;
					accuracyPoints += 1f;
					AddScore(baseScore);
					AddCombo();
					Charge(note.size.FeverCharge());
					break;

				case Judgment.Great:
					GreatCount ++;
					accuracyPoints += 0.5f;
					AddScore(baseScore * greatScoreRate);
					AddCombo();
					Charge(note.size.FeverCharge() * 0.5f);
					break;

				case Judgment.Pass:
					PassCount ++;
					accuracyPoints += 1f;
					AddScore(baseScore);
					AddCombo();
					break;

				case Judgment.Dodged:
					DodgedCount ++;
					if ( dodgedBreaksCombo ) Combo = 0;
					break;

				case Judgment.Miss:
					MissCount ++;
					Combo = 0;
					break;
			}

			OnScoreChanged?.Invoke(Score, Combo);
		}

		private void AddScore(float value)
		{
			if ( IsFever ) value *= feverScoreRate;
			Score += Mathf.RoundToInt(value);
		}

		private void AddCombo()
		{
			Combo ++;
			if ( Combo > MaxCombo ) MaxCombo = Combo;
		}

		private void Charge(float amount)
		{
			if ( IsFever ) return;	// 피버 중에는 충전하지 않는다

			FeverGauge = Mathf.Min(feverMax, FeverGauge + amount);
			OnFeverGaugeChanged?.Invoke(FeverGauge, feverMax);

			if ( FeverGauge >= feverMax )
			{
				IsFever = true;
				feverEndTime = ( conductor != null ? conductor.SongTime : 0f ) + feverDuration;
				OnFeverStarted?.Invoke();
			}
		}
	}
}
