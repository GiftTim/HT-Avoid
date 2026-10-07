using UnityEngine;

namespace AVOID
{
	/// <summary>
	/// 바닥 평면의 텍스처를 플레이어 쪽으로 흘려보내 "앞으로 나아가는" 느낌을 만든다.
	/// 바닥 오브젝트 자체는 움직이지 않고 머티리얼의 UV 오프셋만 바꾼다.
	/// 속도는 GameController.StageSpeed 를 따르므로 피격(멈춤/넉백) 시 바닥도 같이 멈추거나 뒤로 흐른다.
	/// </summary>
	[RequireComponent(typeof(Renderer))]
	public class GroundScroller : MonoBehaviour
	{
		// Unity 기본 Plane 메쉬의 한 변 길이 (Scale 1 기준)
		private const float PLANE_MESH_SIZE = 10;

		[SerializeField]
		private	bool		reverse = false;	// 무늬가 반대로(멀어지는 쪽으로) 흐르면 체크

		private	Material	material;			// 이 바닥 전용 머티리얼 인스턴스
		private	float		offset;

		private void Awake()
		{
			material = GetComponent<Renderer>().material;
		}

		private void Update()
		{
			// 월드 속도를 UV 속도로 환산 : 바닥 길이(Z) 안에 무늬가 Tiling.y 번 반복됨
			float length	= PLANE_MESH_SIZE * transform.lossyScale.z;
			float uvSpeed	= GameController.StageSpeed * material.mainTextureScale.y / length;
			float sign		= reverse ? 1 : -1;

			offset = Mathf.Repeat(offset + sign * uvSpeed * Time.deltaTime, 1);

			material.mainTextureOffset = new Vector2(0, offset);
		}

		private void OnDestroy()
		{
			// renderer.material 로 만들어진 인스턴스는 직접 파괴해야 누수되지 않는다
			if ( material != null )
			{
				Destroy(material);
			}
		}
	}
}
