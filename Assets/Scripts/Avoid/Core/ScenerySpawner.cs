using UnityEngine;

namespace AVOID
{
	/// <summary>
	/// 길 양옆에 장식(기둥/나무 등)을 일정 간격으로 흘려보내 전진하는 느낌을 만든다.
	/// 판정과는 무관한 연출용. prefab 을 비워두면 임시 기둥(Cube)을 만들어 쓴다.
	/// 속도는 GameController.StageSpeed 를 따른다 (바닥과 같은 속도로 흐름).
	/// </summary>
	public class ScenerySpawner : MonoBehaviour
	{
		[SerializeField]
		private	GameObject	prefab;						// 장식 프리팹 (없으면 임시 기둥)
		[SerializeField]
		private	float		spacing = 6;				// 장식 사이의 간격 (월드 단위)
		[SerializeField]
		private	float		sideX = 6;					// 길 중앙에서 좌우로 떨어진 거리
		[SerializeField]
		private	float		spawnZ = 45;				// 생성 위치 (바닥의 먼 쪽 끝)
		[SerializeField]
		private	float		despawnZ = -8;				// 삭제 위치 (카메라 뒤)

		[Header("Placeholder Pillar")]
		[SerializeField]
		private	Vector3		pillarSize = new Vector3(0.5f, 3, 0.5f);
		[SerializeField]
		private	Color		pillarColor = new Color(0.78f, 0.88f, 1);

		private	Material	pillarMaterial;				// 임시 기둥들이 공유하는 머티리얼
		private	float		movedDistance;				// 마지막 생성 이후 전진한 거리

		private void Start()
		{
			// 시작하자마자 길이 비어 보이지 않도록 미리 깔아둔다
			for ( float z = spawnZ; z > despawnZ; z -= spacing )
			{
				SpawnPair(z);
			}
		}

		private void Update()
		{
			// 넉백 중에는 전진 속도가 음수라 거리가 줄어든다 → 그만큼 다시 전진해야 다음 장식이 나온다
			movedDistance += GameController.StageSpeed * Time.deltaTime;

			while ( movedDistance >= spacing )
			{
				movedDistance -= spacing;

				// 초과한 거리만큼 앞당겨 놓아야 프레임이 흔들려도 간격이 일정하다
				SpawnPair(spawnZ - movedDistance);
			}
		}

		private void SpawnPair(float z)
		{
			Spawn(-sideX, z);
			Spawn(sideX, z);
		}

		private void Spawn(float x, float z)
		{
			GameObject clone = prefab != null ? Instantiate(prefab) : CreatePillar();

			clone.transform.SetParent(transform);
			clone.transform.position = new Vector3(x, clone.transform.position.y, z);

			ApproachingObject approaching = clone.GetComponent<ApproachingObject>();
			if ( approaching == null )
			{
				approaching = clone.AddComponent<ApproachingObject>();
			}
			approaching.Setup(0, despawnZ);
		}

		private GameObject CreatePillar()
		{
			GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
			pillar.name = "Pillar";

			// 연출용이라 충돌이 필요 없다
			Destroy(pillar.GetComponent<Collider>());

			pillar.transform.localScale	= pillarSize;
			pillar.transform.position	= new Vector3(0, pillarSize.y * 0.5f, 0);

			if ( pillarMaterial == null )
			{
				pillarMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
				pillarMaterial.color = pillarColor;
			}
			pillar.GetComponent<Renderer>().sharedMaterial = pillarMaterial;

			return pillar;
		}

		private void OnDestroy()
		{
			if ( pillarMaterial != null )
			{
				Destroy(pillarMaterial);
			}
		}
	}
}
