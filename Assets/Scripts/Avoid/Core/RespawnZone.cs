using UnityEngine;

/// <summary>
/// 이 오브젝트의 BoxCollider2D(Trigger)에 Player가 닿으면
/// Player를 지정된 위치로 되돌린다 (낙사 방지용 리스폰 존).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RespawnZone : MonoBehaviour
{
	[SerializeField]
	private	Vector2	respawnPosition = new Vector2(0, -3.75f);	// Player를 되돌릴 위치

	private void OnTriggerEnter2D(Collider2D other)
	{
		MovementRigidbody2D player = other.GetComponent<MovementRigidbody2D>();
		if ( player == null )
			return;

		player.Teleport(respawnPosition);
	}
}
