using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RHYTHM
{
	// Perfect/Great : 반격 탄환(J)을 쳐냄. Pass : 회피 탄환(F)을 피함.
	// Dodged : 반격 탄환을 쳐내지 않고 F 로 피해 지나감 (HP 유지, 점수/정확도만 감점).
	// Miss : 맞음 (HP 감소)
	public enum Judgment { Perfect = 0, Great, Miss, Pass, Dodged }

	/// <summary>
	/// 채보의 노트를 곡 시간에 맞춰 관리하고 입력을 판정한다.
	/// 판정 = (입력 시각 - 노트 시각) 의 차이. 충돌 판정이 아니다.
	/// 결과는 OnJudged 이벤트로 알린다 (체력/점수/이펙트는 이벤트를 받아서 처리).
	/// </summary>
	public class NoteManager : MonoBehaviour
	{
		[SerializeField] private RhythmConductor	conductor;
		[SerializeField] private float				perfectWindow	= 0.05f;	// ±초 (확정 26.10.15)
		[SerializeField] private float				greatWindow		= 0.13f;	// ±초. 이 밖으로 지나가면 Miss

		private class Note
		{
			public NoteEntry	entry;
			public float		time;
			public bool			judged;
			public bool			dodgePressed;	// 반격 탄환 판정 창 안에서 F 가 눌렸는가 (J 없이 끝나면 Dodged)
		}

		private readonly List<Note>	notes = new List<Note>();
		private int						firstPending;	// 아직 판정 안 된 가장 앞 노트의 위치

		/// <summary> (노트, 판정, 오차 초: 입력 - 노트. 양수면 늦음. Miss/Dodged 는 0) </summary>
		public event Action<NoteEntry, Judgment, float> OnJudged;

		/// <summary> 판정과 무관하게 점프/공격 버튼이 눌린 순간 (캐릭터 동작/효과음용) </summary>
		public event Action<NoteInput> OnInput;

		public int TotalCount	{ get; private set; }
		public int JudgedCount	{ get; private set; }

		/// <summary>
		/// 채보를 읽어 노트 목록을 만든다. 곡 재생(Conductor.Play) 전에 호출
		/// </summary>
		public void Load(ChartData chart)
		{
			notes.Clear();
			firstPending	= 0;
			JudgedCount		= 0;

			List<NoteEntry> sorted = new List<NoteEntry>(chart.notes);
			sorted.Sort((a, b) => a.beat.CompareTo(b.beat));

			foreach ( NoteEntry entry in sorted )
			{
				notes.Add(new Note { entry = entry, time = chart.BeatToTime(entry.beat) });
			}
			TotalCount = notes.Count;
		}

		private void Update()
		{
			if ( conductor == null || !conductor.IsPlaying ) return;

			float songTime = conductor.SongTime;

			// 입력 판정
			if ( WasPressed(NoteInput.Jump) )
			{
				OnInput?.Invoke(NoteInput.Jump);
				TryHit(NoteInput.Jump, songTime);
			}
			if ( WasPressed(NoteInput.Attack) )
			{
				OnInput?.Invoke(NoteInput.Attack);
				TryHit(NoteInput.Attack, songTime);
			}

			// 판정 창을 지나친 노트는 Miss
			for ( int i = firstPending; i < notes.Count; i ++ )
			{
				Note note = notes[i];
				if ( note.judged ) continue;
				if ( songTime - note.time <= greatWindow ) break;	// 시간순이라 뒤는 볼 필요 없음

				Judge(note, note.dodgePressed ? Judgment.Dodged : Judgment.Miss, 0);
			}

			while ( firstPending < notes.Count && notes[firstPending].judged ) firstPending ++;
		}

		private void TryHit(NoteInput input, float songTime)
		{
			if ( input == NoteInput.Jump )
			{
				TryJump(songTime);
				return;
			}

			// 이 입력으로 맞출 수 있는, 판정 창 안의 가장 이른 노트
			for ( int i = firstPending; i < notes.Count; i ++ )
			{
				Note note = notes[i];
				if ( note.judged ) continue;

				float delta = songTime - note.time;
				if ( delta < -greatWindow ) break;	// 아직 먼 노트. 뒤는 더 먼 노트
				if ( delta > greatWindow ) continue;	// 창을 이미 지난 노트 (이번 프레임에 Miss 처리됨)
				if ( note.entry.RequiredInput != input ) continue;

				Judge(note, Mathf.Abs(delta) <= perfectWindow ? Judgment.Perfect : Judgment.Great, delta);
				return;
			}
			// 판정 창 안에 맞는 노트가 없으면 무시 (허공 입력에 벌칙은 없음)
		}

		// F : 판정 창 안의 회피 탄환이 있으면 Pass. 없으면 반격 탄환에 "피했다" 표시만 해 두고
		// 창이 끝날 때까지 J 가 없으면 Dodged 로 처리한다 (F 후 J 로 쳐내는 것도 가능)
		private void TryJump(float songTime)
		{
			Note attackNote = null;

			for ( int i = firstPending; i < notes.Count; i ++ )
			{
				Note note = notes[i];
				if ( note.judged ) continue;

				float delta = songTime - note.time;
				if ( delta < -greatWindow ) break;
				if ( delta > greatWindow ) continue;

				if ( note.entry.type == NoteType.DodgeBullet )
				{
					Judge(note, Judgment.Pass, delta);
					return;
				}
				if ( attackNote == null ) attackNote = note;
			}

			if ( attackNote != null ) attackNote.dodgePressed = true;
		}

		private void Judge(Note note, Judgment judgment, float delta)
		{
			note.judged = true;
			JudgedCount ++;
			OnJudged?.Invoke(note.entry, judgment, delta);
		}

		// 키보드 F/J + 게임패드(Jump = A / 플스 ×, Attack = X / 플스 □)
		private static bool WasPressed(NoteInput input)
		{
			Keyboard keyboard = Keyboard.current;
			Gamepad  gamepad  = Gamepad.current;

			if ( input == NoteInput.Jump )
			{
				return ( keyboard != null && keyboard.fKey.wasPressedThisFrame )
					|| ( gamepad  != null && gamepad.buttonSouth.wasPressedThisFrame );
			}
			return ( keyboard != null && keyboard.jKey.wasPressedThisFrame )
				|| ( gamepad  != null && gamepad.buttonWest.wasPressedThisFrame );
		}
	}
}
