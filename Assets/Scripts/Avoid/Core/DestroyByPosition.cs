using UnityEngine;

public class DestroyByPosition : MonoBehaviour
{
	// 화면 밖 일정 범위를 벗어났을 때 삭제하기 위한 가중치 값
	private float	destroyWeight = 2;

	private void LateUpdate()
	{
		if ( transform.position.x < Constants.min.x - destroyWeight ||
			 transform.position.x > Constants.max.x + destroyWeight ||
			 transform.position.y < Constants.min.y - destroyWeight ||
			 transform.position.y > Constants.max.y + destroyWeight )
		{
			Destroy(gameObject);
		}
	}
}


