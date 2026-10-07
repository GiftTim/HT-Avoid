using UnityEngine;
using UnityEngine.InputSystem;

namespace AVOID
{
	/// <summary>
	/// AvoidInput(Input Actions)에서 읽은 입력을 AvoidPlayerMovement의
	/// 공개 API(MoveTo/JumpTo)로 넘겨주는 연결 컴포넌트.
	/// 전진(Forward) 입력은 GameController 에 넘긴다.
	/// </summary>
	[RequireComponent(typeof(PlayerInput))]
	[RequireComponent(typeof(AvoidPlayerMovement))]
	public class AvoidPlayerController : MonoBehaviour
	{
		private PlayerInput			input;
		private AvoidPlayerMovement	movement;

		private InputAction			moveAction;
		private InputAction			jumpAction;
		private InputAction			forwardAction;

		private void Awake()
		{
			input		= GetComponent<PlayerInput>();
			movement	= GetComponent<AvoidPlayerMovement>();

			moveAction	= input.actions["Move"];
			jumpAction	= input.actions["Jump"];
			forwardAction = input.actions["Forward"];
		}

		private void OnEnable()
		{
			jumpAction.started		+= OnJumpStarted;
		}

		private void OnDisable()
		{
			jumpAction.started		-= OnJumpStarted;
		}

		private void Update()
		{
			// Move는 키를 누르고 있는 동안 계속 값이 필요하므로
			// 이벤트 콜백이 아니라 매 프레임 값을 폴링해서 읽는다.
			// 피격 반응(멈춤/넉백) 중에는 좌우로 움직일 수 없고, 공중이었다면 바로 떨어진다
			// 설정창이 열려 있는 동안(일시정지)에는 입력을 전부 무시한다
			bool isPaused = SettingsPanel.IsOpen;

			float x = ( IsReacting || isPaused ) ? 0 : moveAction.ReadValue<float>();
			movement.MoveTo(x);

			if ( IsReacting )
			{
				movement.Fall();
			}

			// 앞 방향키를 누르고 있는 동안에만 전진한다
			if ( GameController.Instance != null )
			{
				GameController.Instance.IsAdvancing = isPaused == false && forwardAction.IsPressed();
			}
		}

		// 점프 버튼을 누른 순간: 점프 시작 (높이는 고정)
		private void OnJumpStarted(InputAction.CallbackContext context)
		{
			if ( IsReacting || SettingsPanel.IsOpen ) return;

			movement.JumpTo();
		}

		// GameController 가 없는 씬(테스트용)에서는 항상 조작 가능
		private bool IsReacting => GameController.Instance != null && GameController.Instance.IsReacting;
	}
}
