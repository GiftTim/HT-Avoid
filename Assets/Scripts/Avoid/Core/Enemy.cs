using UnityEngine;

namespace AVOID
{
	/// <summary>
	/// 공격 판정 확인용 테스트 대상 (옛 2D 시절 잔재, 씬에서는 비활성).
	/// VN 복귀 트리거는 26.10.04(3-c) 에 "목적지 도착" 으로 교체되어 더 이상 여기서 하지 않는다
	/// </summary>
	public class Enemy : MonoBehaviour, IDamageable
	{
		public void TakeDamage(int amount)
		{
			Debug.Log($"{name} 공격당함 (피해량: {amount})");
			Destroy(gameObject);
		}
	}
}
