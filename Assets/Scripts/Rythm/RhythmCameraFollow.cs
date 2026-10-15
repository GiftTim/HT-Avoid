using UnityEngine;

namespace RHYTHM
{
	/// <summary>
	/// 플레이어를 가로로만 따라가는 카메라. y/z 는 고정 (z = -10 유지)
	/// 플레이어 위치가 곡 시간으로 정해지므로 LateUpdate 에서 그대로 따라가면 흔들림이 없다
	/// </summary>
	public class RhythmCameraFollow : MonoBehaviour
	{
		[SerializeField] private Transform	target;
		[SerializeField] private float		offsetX = 4f;	// 플레이어보다 앞쪽(오른쪽)을 보여 줌

		private void LateUpdate()
		{
			if ( target == null ) return;

			Vector3 position = transform.position;
			position.x = target.position.x + offsetX;
			transform.position = position;
		}
	}
}
