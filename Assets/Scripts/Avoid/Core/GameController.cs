using UnityEngine;

namespace AVOID
{
	/// <summary>
	/// 피하기 한 판의 진행 상태(시작/종료)와 점수(버틴 시간)를 관리한다.
	/// 시작 시 패턴을 켜고, 종료 시 패턴을 끈다.
	/// </summary>
	public class GameController : MonoBehaviour
	{
		[SerializeField]
		private	UIController	uiController;
		[SerializeField]
		private	GameObject		pattern01;
		[SerializeField]
		private	bool			autoStart = true;	// 씬이 로드되면 바로 시작 (VN에서 [startAvoid]로 진입하므로 시작 버튼이 없음)

		private readonly float	scoreScale = 20;	// 점수 증가 계수 (읽기전용)

		// 플레이어 점수 (죽지않고 버틴 시간)
		public	float	CurrentScore	{ private set; get; } = 0;

		public	bool	IsGamePlay		{ private set; get; } = false;

		private void Start()
		{
			if ( autoStart )
			{
				GameStart();
			}
		}

		public void GameStart()
		{
			uiController.GameStart();

			pattern01.SetActive(true);

			IsGamePlay = true;
		}

		public void GameExit()
		{
			#if UNITY_EDITOR
			UnityEditor.EditorApplication.ExitPlaymode();
			#else
			Application.Quit();
			#endif
		}

		public void GameOver()
		{
			// 사망 후 추가 피격 등으로 중복 호출되는 것을 막는다
			if ( IsGamePlay == false ) return;

			uiController.GameOver();

			pattern01.SetActive(false);

			IsGamePlay = false;
		}

		private void Update()
		{
			if ( IsGamePlay == false ) return;

			CurrentScore += Time.deltaTime * scoreScale;
		}
	}
}
