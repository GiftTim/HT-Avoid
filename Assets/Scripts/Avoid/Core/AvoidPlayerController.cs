using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// AvoidInput(Input Actions)에서 읽은 입력을 MovementRigidbody2D의
/// 공개 API(MoveTo/JumpTo/IsLongJump)로 넘겨주는 연결 컴포넌트.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(MovementRigidbody2D))]
public class AvoidPlayerController : MonoBehaviour
{
	private PlayerInput			input;
	private MovementRigidbody2D	movement;

	private InputAction			moveAction;
	private InputAction			jumpAction;

	private void Awake()
	{
		input		= GetComponent<PlayerInput>();
		movement	= GetComponent<MovementRigidbody2D>();

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
}
