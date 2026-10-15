using UnityEngine;

namespace RHYTHM
{
	/// <summary>
	/// 임시 확인용. 씬 시작 시 채보를 읽어 곡을 재생하고 판정 결과를 Console 에 찍는다.
	/// 박마다 로그를 남기므로 메트로놈 대신 곡(또는 무음)과 박 번호가 맞는지 눈으로 확인할 수 있다.
	/// 게임 흐름(VN 연결)이 생기면 삭제한다.
	/// </summary>
	public class RhythmDebugRunner : MonoBehaviour
	{
		[SerializeField] private RhythmConductor	conductor;
		[SerializeField] private NoteManager		noteManager;
		[SerializeField] private RhythmBattle		battle;
		[SerializeField] private RhythmScore		score;	// 비워 두면 점수 로그를 찍지 않음
		[SerializeField] private bool				logBeats = true;

		private int lastBeat = -1;

		private void Start()
		{
			noteManager.OnJudged += (note, judgment, delta) =>
				Debug.Log($"[Rhythm] {note.type}({note.size}) -> {judgment} ({delta * 1000:+0;-0;0} ms)");

			battle.OnPlayerHpChanged	+= (hp, max) => Debug.Log($"[Rhythm] Player HP {hp}/{max}");
			battle.OnFailed				+= () => Debug.Log("[Rhythm] FAILED");
			battle.OnCleared			+= () => Debug.Log("[Rhythm] CLEARED" + ScoreSummary());

			if ( score != null )
			{
				score.OnFeverStarted += () => Debug.Log("[Rhythm] FEVER START");
				score.OnFeverEnded   += () => Debug.Log("[Rhythm] FEVER END");
				score.Begin();
			}

			noteManager.Load(conductor.Chart);
			battle.Begin();
			conductor.Play();
		}

		private string ScoreSummary()
		{
			if ( score == null ) return "";
			return $"  score {score.Score}  maxCombo {score.MaxCombo}  accuracy {score.Accuracy:0.00}%  rank {score.Rank}" +
				$"  (P {score.PerfectCount} / G {score.GreatCount} / Pass {score.PassCount} / Dodged {score.DodgedCount} / Miss {score.MissCount})";
		}

		private void Update()
		{
			if ( !logBeats || !conductor.IsPlaying ) return;

			int beat = Mathf.FloorToInt(conductor.SongBeat);
			if ( beat != lastBeat && conductor.SongBeat >= 0 )
			{
				lastBeat = beat;
				Debug.Log($"[Rhythm] beat {beat}  (t = {conductor.SongTime:0.000}s)");
			}
		}
	}
}
