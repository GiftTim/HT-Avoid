using UnityEngine;

namespace AVOID
{
	/// <summary>
	/// StageProfile 의 패턴 목록을 읽어 진행 거리에 맞춰 공격 오브젝트를 만든다.
	/// 공격은 월드에 고정되어 있고(자체 속도 0) 플레이어가 다가가는 구조라서,
	/// 패턴이 닿는 거리 = StageProfile 에 적힌 distance 로 정확히 정해진다.
	/// (이전 테스트용 랜덤 스포너 AttackSpawner 를 대체)
	/// 프로필은 AvoidSession.StageName(= [startAvoid(이름)] 인자)으로 Resources/Stages/ 에서 고르고,
	/// 이름이 없거나 못 찾으면 인스펙터의 defaultProfile, 그것도 없으면 기본 패턴을 자동 생성해서 쓴다.
	/// </summary>
	public class StageRunner : MonoBehaviour
	{
		private const string RESOURCES_PATH = "Stages/";

		public	static	StageRunner	Instance	{ private set; get; }

		[SerializeField]
		private	StageProfile	defaultProfile;			// Resources 에서 못 찾았을 때 쓰는 프로필

		[Header("Spawn")]
		[SerializeField]
		private	float	spawnZ = 45;					// 이 Z 보다 가까워지면 생성 (바닥의 먼 쪽 끝)
		[SerializeField]
		private	float	despawnZ = -8;					// 삭제 위치 (카메라 뒤)

		[Header("Low (Stun)")]
		[SerializeField]
		private	Vector3	lowSize = new Vector3(2.5f, 0.5f, 0.5f);
		[SerializeField]
		private	Color	lowColor = new Color(1, 0.3f, 0.3f);
		[SerializeField]
		private	float	stunTime = 0.8f;

		[Header("High (Knockback)")]
		[SerializeField]
		private	Vector3	highSize = new Vector3(1.2f, 2.5f, 0.5f);
		[SerializeField]
		private	Color	highColor = new Color(1, 0.65f, 0.2f);
		[SerializeField]
		private	float	knockbackTime = 0.5f;
		[SerializeField]
		private	float	knockbackDistance = 15;

		[Header("Slow (Speed Down)")]
		[SerializeField]
		private	Vector3	slowSize = new Vector3(2, 0.8f, 0.5f);
		[SerializeField]
		private	Color	slowColor = new Color(0.6f, 0.35f, 1);
		[SerializeField]
		private	float	slowTime = 2;
		[SerializeField]
		[Range(0, 1)]
		private	float	slowMultiplier = 0.4f;

		private	StageProfile	profile;
		private	PatternEntry[]	sortedPatterns;
		private	int				nextIndex;
		private	GameController	gameController;
		private	Material		lowMaterial, highMaterial, slowMaterial;

		public	StageProfile	Profile			=> profile;
		public	int				PatternTotal	=> sortedPatterns != null ? sortedPatterns.Length : 0;

		/// <summary>
		/// 지금까지 지나간 패턴 수 (진행 거리 기준)
		/// </summary>
		public	int				PatternPassed	=> ( gameController != null && profile != null ) ? profile.CountPassed(gameController.Distance) : 0;
		public	int				PatternLeft		=> PatternTotal - PatternPassed;

		private void Awake()
		{
			Instance		= this;
			gameController	= FindFirstObjectByType<GameController>();

			profile = ResolveProfile();

			// 패턴이 거리 순서대로 나가야 하므로 정렬한 복사본을 쓴다
			sortedPatterns = profile.patterns.ToArray();
			System.Array.Sort(sortedPatterns, (a, b) => a.distance.CompareTo(b.distance));

			// 제한시간/목적지는 프로필 값을 따른다 (GameController 의 Start 에서 GameStart 가 불리기 전에 적용됨)
			if ( gameController != null )
			{
				gameController.Configure(profile.timeLimit, profile.goalDistance);
			}

			Shader shader	= Shader.Find("Universal Render Pipeline/Unlit");
			lowMaterial		= new Material(shader) { color = lowColor };
			highMaterial	= new Material(shader) { color = highColor };
			slowMaterial	= new Material(shader) { color = slowColor };
		}

		private void OnDestroy()
		{
			if ( Instance == this ) Instance = null;

			Destroy(lowMaterial);
			Destroy(highMaterial);
			Destroy(slowMaterial);
		}

		private void Update()
		{
			if ( gameController == null || gameController.IsGamePlay == false ) return;

			// 닿는 거리(distance)까지 spawnZ 안쪽으로 들어온 패턴을 생성한다.
			// 생성 위치 z = distance - 현재 진행 거리 이므로 갑자기 튀어나오지 않고 정확한 위치에 놓인다
			while ( nextIndex < sortedPatterns.Length )
			{
				float z = sortedPatterns[nextIndex].distance - gameController.Distance;
				if ( z > spawnZ ) break;

				Spawn(sortedPatterns[nextIndex], z);
				nextIndex ++;
			}
		}

		private StageProfile ResolveProfile()
		{
			StageProfile found = null;

			if ( string.IsNullOrEmpty(AvoidSession.StageName) == false )
			{
				found = Resources.Load<StageProfile>(RESOURCES_PATH + AvoidSession.StageName);

				if ( found == null )
				{
					Debug.LogWarning($"[StageRunner] 스테이지 '{AvoidSession.StageName}' 을(를) Resources/{RESOURCES_PATH} 에서 찾지 못했습니다. 기본 프로필을 사용합니다");
				}
			}

			if ( found == null ) found = defaultProfile;

			if ( found == null )
			{
				found = ScriptableObject.CreateInstance<StageProfile>();
				found.GenerateDefaultPatterns();
			}

			return found;
		}

		private void Spawn(PatternEntry entry, float z)
		{
			switch ( entry.type )
			{
				case PatternType.Low:	SpawnOne(lowSize,	lowMaterial,	HitReaction.Stun,		stunTime,		entry.x, z); break;
				case PatternType.High:	SpawnOne(highSize,	highMaterial,	HitReaction.Knockback,	knockbackTime,	entry.x, z); break;
				case PatternType.Slow:	SpawnOne(slowSize,	slowMaterial,	HitReaction.Slow,		slowTime,		entry.x, z); break;
				case PatternType.Wall:	SpawnWall(entry.gapIndex, z); break;
			}
		}

		// 길 폭을 WALL_SLOT_COUNT 칸으로 나눠 한 칸만 비우고 높은 기둥을 세운다
		private void SpawnWall(int gapIndex, float z)
		{
			float slotWidth = StageProfile.ROAD_HALF_WIDTH * 2 / StageProfile.WALL_SLOT_COUNT;
			gapIndex = Mathf.Clamp(gapIndex, 0, StageProfile.WALL_SLOT_COUNT - 1);

			for ( int i = 0; i < StageProfile.WALL_SLOT_COUNT; i ++ )
			{
				if ( i == gapIndex ) continue;

				float x = -StageProfile.ROAD_HALF_WIDTH + slotWidth * (i + 0.5f);
				SpawnOne(new Vector3(slotWidth, highSize.y, highSize.z), highMaterial, HitReaction.Knockback, knockbackTime, x, z);
			}
		}

		private void SpawnOne(Vector3 size, Material material, HitReaction reaction, float reactionTime, float x, float z)
		{
			GameObject clone = GameObject.CreatePrimitive(PrimitiveType.Cube);
			clone.name = $"Attack ({reaction})";

			clone.transform.SetParent(transform);
			clone.transform.localScale	= size;
			clone.transform.position	= new Vector3(x, size.y * 0.5f, z);

			clone.GetComponent<Renderer>().sharedMaterial	= material;
			clone.GetComponent<Collider>().isTrigger		= true;

			// 자체 속도 0 : 월드에 고정되어 바닥과 같이 흐른다
			clone.AddComponent<ApproachingObject>().Setup(0, despawnZ);
			clone.AddComponent<AttackObject>().Setup(reaction, reactionTime, knockbackDistance, slowMultiplier);
		}
	}
}
