using UnityEngine;
using DIALOGUE;
using COMMANDS;

/// <summary>
/// 공격 판정 확인용 테스트 대상. 맞으면 콘솔에 로그를 남기고 즉시 사라진다.
/// Enemy1/2/3 중 아무거나 하나라도 처치되면 VN으로 복귀한다.
/// </summary>
public class Enemy : MonoBehaviour, IDamageable
{
	// 한 번의 공격 판정(OverlapCircleAll)이 여러 Enemy를 동시에 맞힐 수 있어서,
	// VN 복귀가 중복 실행되지 않도록 막는 플래그. Avoid 재진입 시 초기화됨
	private static bool hasCleared = false;

	public static void ResetClearState() => hasCleared = false;

	public void TakeDamage(int amount)
	{
		Debug.Log($"{name} 공격당함 (피해량: {amount})");
		Destroy(gameObject);

		if ( hasCleared )
			return;
		hasCleared = true;

		DialogueSystem.instance.StartCoroutine(CMD_DatabaseExtension_Avoid.ReturnToVN());
	}
}
