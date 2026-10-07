using System.Collections;
using DIALOGUE;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AVOID
{
	/// <summary>
	/// 피하기 한 판의 진행을 관리한다 (남극 탐험식).
	/// 제한시간 카운트다운 + 목적지까지의 진행 거리 + 현재 전진 속도.
	/// 바닥/장식/공격 오브젝트는 전부 이 클래스의 전진 속도(StageSpeed)를 따라 움직이므로,
	/// 피격 반응(멈춤/넉백)은 이 속도만 바꾸면 화면 전체에 반영된다.
	/// </summary>
	public class GameController : MonoBehaviour
	{
		// GameController 가 없는 씬(테스트용)에서 쓰는 기본 전진 속도
		private const float DEFAULT_STAGE_SPEED = 10;

		public	static	GameController	Instance	{ private set; get; }

		// 현재 전진 속도 (월드 단위/초). 전진 입력이 없거나 멈춤이면 0, 넉백 중이면 음수
		public	static	float	StageSpeed => Instance != null ? Instance.CurrentSpeed : DEFAULT_STAGE_SPEED;

		[SerializeField]
		private	float	timeLimit = 90;			// 제한시간 (초)
		[SerializeField]
		private	float	goalDistance = 800;		// 목적지까지의 거리 (월드 단위)
		[SerializeField]
		private	float	moveSpeed = 10;			// 기본 전진 속도 (월드 단위/초)
		[SerializeField]
		private	float	maxSpeed = 20;			// 가속의 상한 (월드 단위/초)
		[SerializeField]
		private	float	acceleration = 1.5f;	// 맞지 않고 계속 전진할 때 초당 늘어나는 속도
		[SerializeField]
		private	bool	autoStart = true;		// 씬이 로드되면 바로 시작 (VN에서 [startAvoid]로 진입하므로 시작 버튼이 없음)
		[SerializeField]
		private	float	resultDelay = 1.5f;		// 클리어/시간 초과 문구를 보여주는 시간 (초)
		[SerializeField]
		private	UIController	uiController;	// 비워두면 같은 오브젝트에서 찾음

		private	float	reactionTime;			// 피격 반응이 끝날 때까지 남은 시간
		private	float	reactionSpeed;			// 피격 반응 중의 전진 속도 (멈춤 0, 넉백 음수)
		private	float	cruiseSpeed;			// 가속이 반영된 전진 속도 (피격/전진 입력 중단 시 moveSpeed 로 초기화)
		private	float	slowTime;				// 감속(Slow) 효과가 끝날 때까지 남은 시간
		private	float	slowMultiplier = 1;		// 감속 중 전진 속도에 곱해지는 값

		public	float	RemainingTime		{ private set; get; }
		public	float	Distance			{ private set; get; }
		public	float	RemainingDistance	=> Mathf.Max(0, goalDistance - Distance);
		public	float	CurrentSpeed		{ private set; get; }
		public	int		HitCount			{ private set; get; }	// 클리어 후 VN 대사 분기에 쓸 피격 횟수
		public	bool	IsGamePlay			{ private set; get; } = false;
		public	bool	IsAdvancing			{ set; get; } = false;	// 전진 입력(앞 방향키)이 눌려 있는지. AvoidPlayerController 가 매 프레임 갱신
		public	bool	IsReacting		=> IsGamePlay && reactionTime > 0;	// 피격 반응(멈춤/넉백) 중인지
		public	bool	IsSlowed		=> IsGamePlay && slowTime > 0;		// 감속 효과 중인지

		// 가속이 얼마나 쌓였는지 (0 = 기본 속도, 1 = 최대 속도). 가속 연출(SpeedEffect)이 읽는다
		public	float	AccelerationRatio => ( IsGamePlay && IsAdvancing && reactionTime <= 0 && maxSpeed > moveSpeed )
											? Mathf.InverseLerp(moveSpeed, maxSpeed, cruiseSpeed) : 0;

		private void Awake()
		{
			Instance		= this;

			if ( uiController == null )
			{
				uiController = GetComponent<UIController>();
			}

			RemainingTime	= timeLimit;
			cruiseSpeed		= moveSpeed;
		}

		private void OnDestroy()
		{
			if ( Instance == this )
			{
				Instance = null;
			}
		}

		private void Start()
		{
			if ( autoStart )
			{
				GameStart();
			}
		}

		/// <summary>
		/// 스테이지 프로필의 제한시간/목적지를 적용한다 (StageRunner 가 Awake 에서 호출)
		/// </summary>
		public void Configure(float newTimeLimit, float newGoalDistance)
		{
			timeLimit		= newTimeLimit;
			goalDistance	= newGoalDistance;
			RemainingTime	= timeLimit;
		}

		public void GameStart()
		{
			RemainingTime	= timeLimit;
			Distance		= 0;
			HitCount		= 0;
			reactionTime	= 0;
			slowTime		= 0;
			cruiseSpeed		= moveSpeed;
			CurrentSpeed	= 0;

			IsGamePlay = true;
		}

		private void Update()
		{
			if ( IsGamePlay == false ) return;

			if ( slowTime > 0 )
			{
				slowTime -= Time.deltaTime;
			}

			if ( reactionTime > 0 )
			{
				reactionTime	-= Time.deltaTime;
				CurrentSpeed	= reactionSpeed;
			}
			else if ( IsAdvancing )
			{
				// 맞지 않고 계속 전진하면 maxSpeed 까지 가속한다. 감속 중에는 그 속도에 비율만 곱함
				cruiseSpeed		= Mathf.MoveTowards(cruiseSpeed, maxSpeed, acceleration * Time.deltaTime);
				CurrentSpeed	= cruiseSpeed * (slowTime > 0 ? slowMultiplier : 1);
			}
			else
			{
				// 전진 키를 떼면 가속은 처음부터 다시 시작
				cruiseSpeed		= moveSpeed;
				CurrentSpeed	= 0;
			}

			Distance		= Mathf.Max(0, Distance + CurrentSpeed * Time.deltaTime);
			RemainingTime	-= Time.deltaTime;

			if ( Distance >= goalDistance )
			{
				GameClear();
			}
			else if ( RemainingTime <= 0 )
			{
				TimeOver();
			}
		}

		/// <summary>
		/// 피격 반응 : duration 초 동안 제자리에 멈춘다
		/// </summary>
		public void Stun(float duration)
		{
			if ( IsGamePlay == false ) return;

			HitCount ++;
			reactionTime	= duration;
			reactionSpeed	= 0;
			cruiseSpeed		= moveSpeed;
		}

		/// <summary>
		/// 피격 반응 : duration 초에 걸쳐 distance 만큼 뒤로 튕겨난다 (진행 거리 감소)
		/// </summary>
		public void Knockback(float distance, float duration)
		{
			if ( IsGamePlay == false ) return;

			HitCount ++;
			reactionTime	= duration;
			reactionSpeed	= -distance / Mathf.Max(duration, 0.01f);
			cruiseSpeed		= moveSpeed;
		}

		/// <summary>
		/// 피격 반응 : duration 초 동안 전진 속도가 multiplier 배(0~1)로 느려진다. 가속은 처음부터 다시 시작
		/// </summary>
		public void Slow(float multiplier, float duration)
		{
			if ( IsGamePlay == false ) return;

			HitCount ++;
			slowTime		= duration;
			slowMultiplier	= Mathf.Clamp01(multiplier);
			cruiseSpeed		= moveSpeed;
		}

		// 도착 : 잠시 CLEAR 를 보여준 뒤 결과를 AvoidSession 에 넘긴다.
		// VN 에서 [startAvoid] 로 들어왔다면 startAvoid 가 이를 감지해 VN 으로 복귀시킨다
		private void GameClear()
		{
			Stop();
			RemainingTime = Mathf.Max(0, RemainingTime);

			Debug.Log($"[GameController] 클리어 — 남은 시간 {RemainingTime:F1}초, 피격 {HitCount}회");

			StartCoroutine(ClearRoutine());
		}

		// 시간 초과 : 잠시 TIME OVER 를 보여준 뒤 그 판을 처음부터 다시 시작한다
		private void TimeOver()
		{
			Stop();
			RemainingTime = 0;

			Debug.Log($"[GameController] 시간 초과 — 남은 거리 {RemainingDistance:F0}");

			StartCoroutine(TimeOverRoutine());
		}

		private IEnumerator ClearRoutine()
		{
			if ( uiController != null ) uiController.ShowMessage("CLEAR!");

			yield return new WaitForSeconds(resultDelay);

			AvoidSession.Complete(HitCount, RemainingTime);
		}

		private IEnumerator TimeOverRoutine()
		{
			if ( uiController != null ) uiController.ShowMessage("TIME OVER");

			yield return new WaitForSeconds(resultDelay);

			StageRunner runner = StageRunner.Instance;
			AvoidSession.NotifyRetry(Mathf.Clamp01(Distance / goalDistance), runner != null ? runner.PatternLeft : 0);

			string sceneName = gameObject.scene.name;

			if ( DialogueSystem.instance != null )
			{
				// VN 위에 얹힌 상태 : 이 씬만 내렸다가 다시 올린다. 이 씬의 오브젝트는 곧 사라지므로
				// 코루틴은 VN 쪽(DialogueSystem)에서 돌려야 끝까지 실행된다
				DialogueSystem.instance.StartCoroutine(ReloadAdditive(sceneName));
			}
			else
			{
				// Avoid.unity 단독 Play
				SceneManager.LoadScene(sceneName);
			}
		}

		private static IEnumerator ReloadAdditive(string sceneName)
		{
			yield return SceneManager.UnloadSceneAsync(sceneName);
			yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
		}

		private void Stop()
		{
			IsGamePlay		= false;
			CurrentSpeed	= 0;
		}
	}
}
