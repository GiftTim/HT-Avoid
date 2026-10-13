using UnityEngine;

namespace AVOID
{
	/// <summary>
	/// 맵을 직접 배치하기 위한 표식. 빈 오브젝트에 붙여 Scene 뷰에서 원하는 위치에 놓으면 된다.
	/// 위치 Z = 플레이어에게 닿는 진행 거리, 위치 X = 좌우 위치 (Wall 은 X 무시).
	/// 실행 시 StageRunner 가 읽어 실제 공격 오브젝트로 바꾸고 표식 자체는 사라진다.
	/// 놓는 위치 그대로 게임 시작 지점(Z=0) 기준으로 나타나므로 Scene 뷰에서 보이는 것이 곧 맵이다.
	/// </summary>
	public class PatternMarker : MonoBehaviour
	{
		[SerializeField]
		private	PatternType	type = PatternType.Low;
		[SerializeField]
		[Range(0, StageProfile.WALL_SLOT_COUNT - 1)]
		private	int			gapIndex = 0;		// Wall 에서 비워 둘 칸 번호 (왼쪽부터 0)

		public PatternEntry ToEntry()
		{
			PatternEntry entry	= new PatternEntry();
			entry.distance		= transform.position.z;
			entry.type			= type;
			entry.x				= transform.position.x;
			entry.gapIndex		= gapIndex;

			return entry;
		}

#if UNITY_EDITOR
		// Scene 뷰에서 종류별 색/크기로 보이게 한다 (크기는 StageRunner 기본값과 맞춤)
		private void OnDrawGizmos()
		{
			Vector3	pos = transform.position;

			switch ( type )
			{
				case PatternType.Low:	DrawBox(pos, new Vector3(2.5f, 0.5f, 0.5f),	new Color(1, 0.3f, 0.3f));		break;
				case PatternType.High:	DrawBox(pos, new Vector3(1.2f, 2.5f, 0.5f),	new Color(1, 0.65f, 0.2f));	break;
				case PatternType.Slow:	DrawBox(pos, new Vector3(2, 0.8f, 0.5f),	new Color(0.6f, 0.35f, 1));	break;
				case PatternType.Wall:	DrawWall(pos);	break;
			}
		}

		private void DrawWall(Vector3 pos)
		{
			float slotWidth = StageProfile.ROAD_HALF_WIDTH * 2 / StageProfile.WALL_SLOT_COUNT;

			for ( int i = 0; i < StageProfile.WALL_SLOT_COUNT; i ++ )
			{
				if ( i == gapIndex ) continue;

				float x = -StageProfile.ROAD_HALF_WIDTH + slotWidth * (i + 0.5f);
				DrawBox(new Vector3(x, pos.y, pos.z), new Vector3(slotWidth, 2.5f, 0.5f), new Color(1, 0.65f, 0.2f));
			}
		}

		private static void DrawBox(Vector3 pos, Vector3 size, Color color)
		{
			Gizmos.color = new Color(color.r, color.g, color.b, 0.35f);
			Gizmos.DrawCube(pos + Vector3.up * size.y * 0.5f, size);
			Gizmos.color = color;
			Gizmos.DrawWireCube(pos + Vector3.up * size.y * 0.5f, size);
		}
#endif
	}
}
