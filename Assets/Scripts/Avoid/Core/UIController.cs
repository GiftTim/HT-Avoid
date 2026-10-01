using UnityEngine;
using TMPro;

namespace AVOID
{
	/// <summary>
	/// 피하기 화면의 UI 표시. 점수 텍스트를 갱신하고, 게임 시작/종료 시 패널을 토글한다.
	/// </summary>
	public class UIController : MonoBehaviour
	{
		[SerializeField]
		private	GameController	gameController;

		[SerializeField]
		private	TextMeshProUGUI	textScore;

		[SerializeField]
		private	GameObject		panelGameOver;	// 게임 오버 시 표시할 패널 (없으면 생략)

		private void Update()
		{
			textScore.text = gameController.CurrentScore.ToString("F0");
		}

		public void GameStart()
		{
			if ( panelGameOver != null )
			{
				panelGameOver.SetActive(false);
			}
		}

		public void GameOver()
		{
			if ( panelGameOver != null )
			{
				panelGameOver.SetActive(true);
			}
		}
	}
}
