using UnityEngine;

namespace RHYTHM
{
	/// <summary>
	/// 리듬 판의 음악 시계. 곡 재생 시각(AudioSettings.dspTime) 기준의 "곡 시간"을 제공한다.
	/// 다른 스크립트는 이 시계(SongTime/SongBeat)만 보고 움직인다.
	/// VN AudioManager 와는 별도 재생기이며, AudioSource 의 출력 믹서 그룹만 Music 에 연결해 둔다.
	/// 곡(clip)이 없어도 시계는 돈다 (메트로놈으로 동기화를 먼저 검증하기 위함).
	/// </summary>
	public class RhythmConductor : MonoBehaviour
	{
		[SerializeField] private ChartData		chart;
		[SerializeField] private AudioSource	audioSource;
		[SerializeField] private float			startDelay		= 1f;	// Play 후 곡이 실제로 시작하기까지의 여유 (초)
		[SerializeField] private float			latencyOffset	= 0f;	// 입력/출력 지연 보정 (초). 나중에 설정 UI 와 연결

		private double	songStartDsp;	// 곡 시간 0 이 되는 dspTime
		private bool	isPlaying;

		public	ChartData	Chart		=> chart;
		public	bool		IsPlaying	=> isPlaying;

		/// <summary>
		/// 곡이 시작된 뒤 지난 시간 (초). 시작 전의 여유 구간에서는 음수
		/// </summary>
		public float SongTime => isPlaying ? (float)(AudioSettings.dspTime - songStartDsp) - latencyOffset : 0f;

		/// <summary>
		/// 현재 박 번호 (소수). 첫 박 = 0
		/// </summary>
		public float SongBeat => chart != null ? chart.TimeToBeat(SongTime) : 0f;

		public void Play()
		{
			songStartDsp = AudioSettings.dspTime + startDelay;

			if ( audioSource != null && chart != null && chart.clip != null )
			{
				audioSource.clip	= chart.clip;
				audioSource.loop	= false;
				audioSource.PlayScheduled(songStartDsp);
			}

			isPlaying = true;
		}

		public void Stop()
		{
			isPlaying = false;
			if ( audioSource != null ) audioSource.Stop();
		}

		public void SetChart(ChartData value) => chart = value;
	}
}
