/// <summary>
/// 공격 판정에 맞아 피해를 받을 수 있는 대상(보스/적)이 구현하는 인터페이스.
/// </summary>
public interface IDamageable
{
	void TakeDamage(int amount);
}
