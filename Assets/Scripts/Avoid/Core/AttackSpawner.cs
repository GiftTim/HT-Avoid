using UnityEngine;

namespace AVOID
{
	/// <summary>
	/// 테스트용 공격 패턴. 맞은편(먼 Z)에서 임시 공격 오브젝트를 일정 간격으로 날려 보낸다.
	///  - 낮은 장애물(빨강) : 점프로 넘거나 옆으로 피함. 맞으면 멈춤
	///  - 높은 장애물(주황) : 옆으로만 피할 수 있음. 맞으면 뒤로 튕김
	///  - 감속 장애물(보라) : 맞으면 잠시 전진 속도가 느려짐 (멈추거나 밀려나지는 않음)
	/// 정식 패턴/스테이지 프로필(3-a, 3-d)이 생기면 이 스크립트는 교체된다.
	/// </summary>
	public class AttackSpawner : MonoBehaviour
	{
		[SerializeField]
		private	float	spawnCycle = 1.2f;			// 공격 생성 간격 (초)
		[SerializeField]
		private	float	rangeX = 4;					// 좌우 생성 범위 (플레이어의 limitX 와 맞출 것)
		[SerializeField]
		private	float	spawnZ = 45;				// 생성 위치 (바닥의 먼 쪽 끝)
		[SerializeField]
		private	float	despawnZ = -8;				// 삭제 위치 (카메라 뒤)
		[SerializeField]
		private	float	attackSpeed = 8;			// 전진 속도에 더해지는 공격 자체의 속도

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
		private	float	slowTime = 2;				// 감속이 이어지는 시간 (초)
		[SerializeField]
		[Range(0, 1)]
		private	float	slowMultiplier = 0.4f;		// 감속 중 전진 속도 비율

		private	Material	lowMaterial;
		private	Material	highMaterial;
		private	Material	slowMaterial;
		private	float		elapsed;

		private void Awake()
		{
			Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

			lowMaterial		= new Material(shader) { color = lowColor };
			highMaterial	= new Material(shader) { color = highColor };
			slowMaterial	= new Material(shader) { color = slowColor };
		}

		private void Update()
		{
			GameController gameController = GameController.Instance;
			if ( gameController != null && gameController.IsGamePlay == false ) return;

			elapsed += Time.deltaTime;
			if ( elapsed < spawnCycle ) return;

			elapsed -= spawnCycle;

			float value = Random.value;
			if ( value < 0.35f )
			{
				Spawn(lowSize, lowMaterial, HitReaction.Stun, stunTime);
			}
			else if ( value < 0.7f )
			{
				Spawn(highSize, highMaterial, HitReaction.Knockback, knockbackTime);
			}
			else
			{
				Spawn(slowSize, slowMaterial, HitReaction.Slow, slowTime);
			}
		}

		private void Spawn(Vector3 size, Material material, HitReaction reaction, float reactionTime)
		{
			GameObject clone = GameObject.CreatePrimitive(PrimitiveType.Cube);
			clone.name = $"Attack ({reaction})";

			clone.transform.SetParent(transform);
			clone.transform.localScale	= size;
			clone.transform.position	= new Vector3(Random.Range(-rangeX, rangeX), size.y * 0.5f, spawnZ);

			clone.GetComponent<Renderer>().sharedMaterial	= material;
			clone.GetComponent<Collider>().isTrigger		= true;

			clone.AddComponent<ApproachingObject>().Setup(attackSpeed, despawnZ);
			clone.AddComponent<AttackObject>().Setup(reaction, reactionTime, knockbackDistance, slowMultiplier);
		}

		private void OnDestroy()
		{
			Destroy(lowMaterial);
			Destroy(highMaterial);
			Destroy(slowMaterial);
		}
	}
}
