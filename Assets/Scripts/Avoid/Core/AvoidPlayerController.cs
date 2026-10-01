using UnityEngine;
using UnityEngine.InputSystem;

namespace AVOID
{
	/// <summary>
	/// AvoidInput(Input Actions)에서 읽은 입력을 MovementRigidbody2D의
	/// 공개 API(MoveTo/JumpTo/IsLongJump)로 넘겨주는 연결 컴포넌트.
	/// 또한 Obstacle 태그와 닿으면 PlayerHP에 피해를 주고, 사망하면 GameController에 알린다.
	/// </summary>
	[RequireComponent(typeof(PlayerInput))]
	[RequireComponent(typeof(MovementRigidbody2D))]
	public class AvoidPlayerController : MonoBehaviour
	{
		// CompareTag는 TagManager에 등록되지 않은 태그를 쓰면 예외가 나므로,
		// 태그를 만들기 전에도 안전하도록 문자열 비교를 쓴다
		private const string OBSTACLE_TAG = "Obstacle";

		[SerializeField]
		private GameController		gameController;	// 사망 시 GameOver 호출 (없으면 로그만 출력)

		private PlayerInput			input;
		private MovementRigidbody2D	movement;
		private PlayerHP			playerHP;		// 없으면 피격 판정을 건너뜀

		private InputAction			moveAction;
		private InputAction			jumpAction;

		private void Awake()
		{
			input		= GetComponent<PlayerInput>();
			movement	= GetComponent<MovementRigidbody2D>();
			playerHP	= GetComponent<PlayerHP>();

			moveAction	= input.actions["Move"];
			jumpAction	= input.actions["Jump"];
		}

		private void OnEnable()
		{
			jumpAction.started		+= OnJumpStarted;
			jumpAction.canceled	+= OnJumpCanceled;
		}

		private void OnDisable()
		{
			jumpAction.started		-= OnJumpStarted;
			jumpAction.canceled	-= OnJumpCanceled;
		}

		private void FixedUpdate()
		{
			// Move는 키를 누르고 있는 동안 계속 값이 필요하므로
			// 이벤트 콜백이 아니라 매 FixedUpdate마다 값을 폴링해서 읽는다.
			float x = moveAction.ReadValue<float>();
			movement.MoveTo(x);
		}

		// 점프 버튼을 누른 순간: 점프 시작 + 롱 점프 판정 시작
		private void OnJumpStarted(InputAction.CallbackContext context)
		{
			movement.IsLongJump = true;
			movement.JumpTo();
		}

		// 점프 버튼을 뗀 순간: 롱 점프 판정 종료 (이후 중력이 높은 값으로 전환됨)
		private void OnJumpCanceled(InputAction.CallbackContext context)
		{
			movement.IsLongJump = false;
		}

		private void OnTriggerEnter2D(Collider2D collision)
		{
			if ( playerHP == null || collision.tag != OBSTACLE_TAG ) return;

			bool isDie = playerHP.TakeDamage();
			if ( isDie == true )
			{
				Debug.Log("플레이어 사망");

				if ( gameController != null )
				{
					gameController.GameOver();
				}
			}
		}
	}
}
