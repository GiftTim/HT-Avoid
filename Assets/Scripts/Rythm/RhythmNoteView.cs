using System.Collections.Generic;
using UnityEngine;

namespace RHYTHM
{
	/// <summary>
	/// 임시 확인용 노트 표시. 채보의 노트를 색깔 있는 원으로 월드에 놓고, 판정되면 지운다.
	///  - 회피 탄환(DodgeBullet, F) : 빨강 / 반격 탄환(AttackBullet, J) : 하늘색
	///  - 크기 : 소 &lt; 중 &lt; 대
	/// 그림은 런타임에 만든 원이라 에셋/프리팹이 필요 없다. 보스/탄환 그림이 생기면 삭제한다.
	/// 위치는 플레이어와 같은 식(TimeToWorldX)이라 노트가 플레이어에게 닿는 순간이 판정 시각이다.
	/// </summary>
	public class RhythmNoteView : MonoBehaviour
	{
		[SerializeField] private RhythmConductor	conductor;
		[SerializeField] private NoteManager		noteManager;
		[SerializeField] private RhythmPlayerMotion	motion;
		[SerializeField] private float				attackBulletHeight	= 0.8f;	// 반격 탄환은 지면보다 위
		[SerializeField] private string				sortingLayer		= "Player";
		[SerializeField] private int				sortingOrder		= 10;

		private class View
		{
			public NoteEntry		entry;
			public GameObject		go;
			public bool				judged;
		}

		private readonly List<View>	views = new List<View>();
		private Sprite				circle;

		private void Start()
		{
			ChartData chart = conductor != null ? conductor.Chart : null;
			if ( chart == null || motion == null ) return;

			circle = MakeCircleSprite();

			foreach ( NoteEntry entry in chart.notes )
			{
				float x = motion.TimeToWorldX(chart.BeatToTime(entry.beat));
				float y = motion.GroundY + ( entry.type == NoteType.AttackBullet ? attackBulletHeight : 0f );

				GameObject go = new GameObject($"Note {entry.type} {entry.beat}");
				go.transform.SetParent(transform, false);
				go.transform.position	= new Vector3(x, y, 0f);
				go.transform.localScale	= Vector3.one * ( 0.5f + 0.25f * (int)entry.size );

				SpriteRenderer sr	= go.AddComponent<SpriteRenderer>();
				sr.sprite			= circle;
				sr.color			= entry.type == NoteType.AttackBullet ? new Color(0.2f, 0.8f, 1f) : new Color(1f, 0.25f, 0.25f);
				sr.sortingLayerName	= sortingLayer;
				sr.sortingOrder		= sortingOrder;

				views.Add(new View { entry = entry, go = go });
			}

			if ( noteManager != null ) noteManager.OnJudged += HandleJudged;
		}

		private void OnDestroy()
		{
			if ( noteManager != null ) noteManager.OnJudged -= HandleJudged;
		}

		private void HandleJudged(NoteEntry entry, Judgment judgment, float delta)
		{
			foreach ( View view in views )
			{
				if ( view.judged ) continue;
				if ( view.entry.type != entry.type || !Mathf.Approximately(view.entry.beat, entry.beat) ) continue;

				view.judged = true;
				if ( view.go != null ) Destroy(view.go);
				return;
			}
		}

		private static Sprite MakeCircleSprite()
		{
			const int size = 64;
			Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
			texture.filterMode = FilterMode.Bilinear;

			float r = size * 0.5f;
			for ( int y = 0; y < size; y ++ )
			{
				for ( int x = 0; x < size; x ++ )
				{
					float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
					texture.SetPixel(x, y, d <= r - 1f ? Color.white : Color.clear);
				}
			}
			texture.Apply();

			return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
		}
	}
}
