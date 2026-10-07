using UnityEngine;

namespace AVOID
{
	/// <summary>
	/// 피하기 한 번(= [startAvoid] 한 번)의 진행 상태와 결과를 VN과 주고받는 창구.
	/// 시간 초과로 씬이 다시 로드돼도 값이 유지돼야 하므로 static 이다.
	/// 게임(GameController)은 VN을 몰라도 되고, VN(startAvoid)이 IsFinished 를 기다렸다가 결과를 변수로 옮긴다.
	/// </summary>
	public static class AvoidSession
	{
		private const string DATABASE = "Avoid";

		public	static	string	StageName		{ private set; get; } = "";	// [startAvoid(이름)] 으로 고른 스테이지 (Resources/Stages/ 의 에셋 이름)
		public	static	float	LastProgress	{ private set; get; }	// 마지막 실패(시간 초과) 때 도달한 비율 (0~1). 나중에 실패 화면에서 사용
		public	static	int		LastPatternsLeft { private set; get; }	// 마지막 실패 때 남은 패턴 수
		public	static	bool	IsFinished		{ private set; get; }	// 클리어했는지 (true 가 되면 startAvoid 가 VN 으로 복귀)
		public	static	int		RetryCount		{ private set; get; }	// 이번 진입에서 시간 초과로 다시 시작한 횟수
		public	static	int		HitCount		{ private set; get; }	// 클리어한 판의 피격 횟수
		public	static	float	RemainingTime	{ private set; get; }	// 클리어한 판의 남은 시간

		// 에디터에서 도메인 리로드를 끈 경우에도 Play 시작마다 초기 상태로 맞춘다
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetOnPlay() => Begin("");

		/// <summary>
		/// [startAvoid] 로 새로 진입할 때 호출. 시간 초과 재도전에서는 호출하지 않는다.
		/// stageName 은 Resources/Stages/ 의 StageProfile 에셋 이름 (비워 두면 기본 프로필)
		/// </summary>
		public static void Begin(string stageName = "")
		{
			StageName		= stageName ?? "";
			LastProgress	= 0;
			LastPatternsLeft = 0;
			IsFinished		= false;
			RetryCount		= 0;
			HitCount		= 0;
			RemainingTime	= 0;
		}

		/// <summary>
		/// 시간 초과로 같은 판을 다시 시작할 때 호출. 어디까지 달렸는지/패턴이 몇 개 남았는지를 함께 남긴다
		/// (지금은 값만 기록. 컵헤드식 실패 화면은 나중에 이 값을 읽어 표시)
		/// </summary>
		public static void NotifyRetry(float progress = 0, int patternsLeft = 0)
		{
			RetryCount ++;
			LastProgress		= progress;
			LastPatternsLeft	= patternsLeft;
		}

		/// <summary>
		/// 목적지에 도착했을 때 호출. 결과를 저장하고 IsFinished 를 true 로 만든다
		/// </summary>
		public static void Complete(int hitCount, float remainingTime)
		{
			HitCount		= hitCount;
			RemainingTime	= remainingTime;
			IsFinished		= true;
		}

		/// <summary>
		/// 결과를 VN 변수($Avoid.hitCount 등)로 옮긴다. 변수가 없으면 만든다
		/// </summary>
		public static void WriteToVariables()
		{
			SetVariable("hitCount",		HitCount);
			SetVariable("retryCount",	RetryCount);
			SetVariable("remainingTime", RemainingTime);
			SetVariable("cleared",		true);
		}

		private static void SetVariable<T>(string name, T value)
		{
			string path = $"{DATABASE}{VariableStore.DATABASE_VARIABLE_RELATIONAL_ID}{name}";

			if ( VariableStore.HasVariable(path) )
			{
				VariableStore.TrySetValue(path, value);
			}
			else
			{
				VariableStore.CreateVariable(path, value);
			}
		}
	}
}
