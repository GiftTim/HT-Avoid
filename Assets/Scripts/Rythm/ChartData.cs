using System.Collections.Generic;
using UnityEngine;

namespace RHYTHM
{
	// 노트의 종류. 종류마다 필요한 입력이 정해져 있다
	//  DodgeBullet  : 회피 탄환. 보스가 던진 탄환을 타이밍에 맞춰 점프(F)해서 피함
	//  AttackBullet : 반격 탄환. 타이밍에 맞춰 공격(J)해서 쳐내 반격함 (보스는 죽지 않음)
	public enum NoteType { DodgeBullet = 0, AttackBullet }

	// 탄환 크기. 피해량/기본 점수/피버 충전량이 이 등급에 묶여 있다 (확정 26.10.15)
	public enum NoteSize { Small = 0, Medium, Large }

	public static class NoteSizeExtension
	{
		public static int Damage(this NoteSize size)		=> 20 + 10 * (int)size;	// 20 / 30 / 40
		public static int BaseScore(this NoteSize size)		=> 200 + 100 * (int)size;	// 200 / 300 / 400
		public static int FeverCharge(this NoteSize size)	=> size == NoteSize.Large ? 2 : 1;	// /60
	}

	// 필요한 입력 버튼
	public enum NoteInput { Jump = 0, Attack }

	[System.Serializable]
	public struct NoteEntry
	{
		public	float		beat;	// 노트가 판정선에 닿는 박 번호 (0 = 곡의 첫 박, 소수 가능: 0.5 = 반 박)
		public	NoteType	type;
		public	NoteSize	size;

		public NoteInput RequiredInput => type == NoteType.AttackBullet ? NoteInput.Attack : NoteInput.Jump;
	}

	/// <summary>
	/// 한 곡의 채보. BPM, 첫 박 오프셋, 노트(박 번호 + 종류) 목록을 담는다.
	/// 박 번호 → 곡 시간(초) 변환은 이 에셋이 맡는다 (BPM 은 곡 내내 일정하다고 가정).
	/// </summary>
	[CreateAssetMenu(fileName = "Chart", menuName = "Rhythm/Chart Data")]
	public class ChartData : ScriptableObject
	{
		public	string		title		= "Temp Song";
		public	AudioClip	clip;
		public	float		bpm			= 120;
		public	float		offset		= 0;	// 곡 파일 시작에서 첫 박(박 0)까지의 시간 (초)

		public	List<NoteEntry>	notes = new List<NoteEntry>();

		public float SecondsPerBeat => 60f / bpm;

		public float BeatToTime(float beat) => offset + beat * SecondsPerBeat;

		public float TimeToBeat(float time) => (time - offset) / SecondsPerBeat;

		/// <summary>
		/// 노트 목록을 박 번호 순으로 정렬한다 (인스펙터에서 순서 없이 입력해도 됨)
		/// </summary>
		[ContextMenu("Sort Notes")]
		public void SortNotes() => notes.Sort((a, b) => a.beat.CompareTo(b.beat));

		private void OnValidate()
		{
			if ( bpm < 1 ) bpm = 1;
		}
	}
}
