using UnityEngine;
using DIALOGUE;
using COMMANDS;

namespace AVOID
{
	/// <summary>
	/// 공격 판정 확인용 테스트 대상. 맞으면 콘솔에 로그를 남기고 즉시 사라진다.
	/// Enemy1/2/3 중 아무거나 하나라도 처치되면 VN으로 복귀한다.
	/// </summary>
	public class Enemy : MonoBehaviour, IDamageable
	{
		// 한 번의 공격 판정(OverlapCapsuleAll)이 여러 Enemy를 동시에 맞힐 수 있어서,
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

			// 정상 흐름(VN → startAvoid)에서는 항상 존재하지만, Avoid 씬만 단독으로
			// Play해서 테스트할 때는 VN이 로드돼 있지 않아 instance가 null이다.
			// 이 경우 VN 복귀만 건너뛰고 나머지 공격 처리(PlayerAttack 이펙트 등)는
			// 계속 진행되게 둔다
			if ( DialogueSystem.instance == null )
			{
				Debug.LogWarning($"{name} DialogueSystem.instance가 없어 VN 복귀를 건너뜀 (Avoid 씬 단독 실행 중인지 확인)");
				return;
			}

			DialogueSystem.instance.StartCoroutine(CMD_DatabaseExtension_Avoid.ReturnToVN());
		}
	}
}
