using System.Collections.Generic;
using UnityEngine;

namespace AVOID
{
	// 공격 패턴의 종류
	//  Low  : 낮은 장애물. 점프로 넘거나 옆으로 피함. 맞으면 멈춤
	//  High : 높은 장애물. 옆으로만 피할 수 있음. 맞으면 뒤로 튕김
	//  Slow : 감속 장애물. 맞으면 잠시 전진 속도가 느려짐
	//  Wall : 높은 기둥이 길 폭만큼 늘어서고 한 칸만 비어 있음. 빈 칸(gapIndex)으로 지나가야 함
	public enum PatternType { Low = 0, High, Slow, Wall }

	[System.Serializable]
	public struct PatternEntry
	{
		public	float		distance;	// 이 패턴이 플레이어에게 닿는 진행 거리 (목적지까지의 거리 기준, 월드 고정 위치)
		public	PatternType	type;
		public	float		x;			// 좌우 위치 (Wall 은 무시)
		public	int			gapIndex;	// Wall 에서 비워 둘 칸 번호 (0 ~ WALL_SLOT_COUNT-1)
	}

	/// <summary>
	/// 한 스테이지(= 한 챕터의 피하기 한 판)의 데이터. 제한시간, 목적지 거리, 패턴 목록을 담는다.
	/// 패턴은 "진행 거리 → 패턴" 목록이라, 장애물이 월드에 고정된 채 플레이어가 다가가는 구조가 된다.
	/// 그래서 지나간 패턴 수/남은 패턴 수/진행률을 이 목록에서 바로 계산할 수 있다
	/// (나중에 실패 화면에서 "어디까지 달렸는지, 패턴이 몇 개 남았는지" 표시용).
	/// 에셋은 Resources/Stages/ 에 두고 startAvoid(에셋이름) 으로 고른다.
	/// </summary>
	[CreateAssetMenu(fileName = "Stage", menuName = "Avoid/Stage Profile")]
	public class StageProfile : ScriptableObject
	{
		public const int	WALL_SLOT_COUNT	= 4;	// Wall 이 길 폭을 나누는 칸 수
		public const float	ROAD_HALF_WIDTH	= 4;	// 길 반폭 (플레이어 좌우 이동 한계와 같음)

		public	string	stageName		= "Stage 1";
		public	string	characterKey	= "";		// 이 판의 상대(보스) 캐릭터. VN 캐릭터와 키를 공유 (미정이면 비워 둠)
		public	float	timeLimit		= 60;		// 제한시간 (초)
		public	float	goalDistance	= 600;		// 목적지까지의 거리

		public	List<PatternEntry>	patterns = new List<PatternEntry>();

		public int PatternCount => patterns.Count;

		/// <summary>
		/// 진행 거리 distance 까지 지나간 패턴 수
		/// </summary>
		public int CountPassed(float distance)
		{
			int count = 0;
			for ( int i = 0; i < patterns.Count; i ++ )
			{
				if ( patterns[i].distance <= distance ) count ++;
			}
			return count;
		}

		/// <summary>
		/// 기본 패턴을 자동 생성한다 (인스펙터 우클릭 → Generate Default Patterns).
		/// 같은 seed 면 같은 결과. 뒤로 갈수록 간격이 좁아지고, 150m 이후부터 Wall 이 나온다
		/// </summary>
		[ContextMenu("Generate Default Patterns")]
		public void GenerateDefaultPatterns() => GenerateDefaultPatterns(1);

		public void GenerateDefaultPatterns(int seed)
		{
			Random.State previous = Random.state;
			Random.InitState(seed);

			patterns.Clear();

			float start		= 60;
			float end		= goalDistance - 40;
			float spacingFrom = 24, spacingTo = 14;

			float distance = start;
			while ( distance < end )
			{
				float progress = Mathf.InverseLerp(start, end, distance);

				PatternEntry entry	= new PatternEntry();
				entry.distance		= distance;
				entry.type			= PickType(distance);
				entry.x				= Mathf.Round(Random.Range(-3.5f, 3.5f) * 2) / 2;	// 0.5 단위
				entry.gapIndex		= Random.Range(0, WALL_SLOT_COUNT);
				patterns.Add(entry);

				distance += Mathf.Lerp(spacingFrom, spacingTo, progress);
			}

			Random.state = previous;
		}

		private static PatternType PickType(float distance)
		{
			float value = Random.value;

			if ( distance >= 150 && value < 0.2f ) return PatternType.Wall;
			if ( value < 0.45f ) return PatternType.Low;
			if ( value < 0.75f ) return PatternType.High;
			return PatternType.Slow;
		}
	}
}
