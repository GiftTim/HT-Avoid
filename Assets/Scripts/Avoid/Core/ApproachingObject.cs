using UnityEngine;

namespace AVOID
{
	/// <summary>
	/// 화면 안쪽(+Z)에서 플레이어 쪽(-Z)으로 다가오는 오브젝트.
	/// 원근 카메라가 다가올수록 크게 그려주므로 크기 계산은 하지 않는다.
	/// 다가오는 속도 = 전진 속도(GameController.StageSpeed) + 자체 속도(ownSpeed).
	/// 장식은 ownSpeed 0 (바닥과 같이 흐름), 적의 공격은 ownSpeed 만큼 더 빠르게 날아온다.
	/// despawnZ 를 지나(카메라 뒤로 넘어가면) 스스로 사라진다.
	/// </summary>
	public class ApproachingObject : MonoBehaviour
	{
		[SerializeField]
		private	float	ownSpeed = 0;		// 전진 속도에 더해지는 자체 속도 (월드 단위/초)
		[SerializeField]
		private	float	despawnZ = -8;		// 이 Z 보다 뒤로 가면 삭제

		public void Setup(float ownSpeed, float despawnZ)
		{
			this.ownSpeed	= ownSpeed;
			this.despawnZ	= despawnZ;
		}

		private void Update()
		{
			float speed = GameController.StageSpeed + ownSpeed;

			transform.position += Vector3.back * speed * Time.deltaTime;

			if ( transform.position.z < despawnZ )
			{
				Destroy(gameObject);
			}
		}
	}
}
