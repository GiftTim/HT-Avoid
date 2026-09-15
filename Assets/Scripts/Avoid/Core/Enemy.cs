using UnityEngine;

/// <summary>
/// 공격 판정 확인용 테스트 대상. 맞으면 콘솔에 로그를 남기고 즉시 사라진다.
/// </summary>
public class Enemy : MonoBehaviour, IDamageable
{
	public void TakeDamage(int amount)
	{
		Debug.Log($"{name} 공격당함 (피해량: {amount})");
		Destroy(gameObject);
	}
}
