using UnityEngine;

namespace AVOID
{
	/// <summary>
	/// 뒤에서 보는 유사 3D(남극 탐험식) 시점용 플레이어 이동.
	/// X축(좌우) 자유 이동 + 연출용 점프(Y축)만 처리하고 Z는 고정한다.
	/// 물리(Rigidbody)를 쓰지 않고 Transform을 직접 움직인다. 점프는 자체 계산한
	/// 수직 속도/중력으로 처리하므로 공중에서 다시 뛰거나(다단 점프) 도중에 떨어뜨릴 수 있다.
	/// (충돌 판정용 Rigidbody는 isKinematic으로 따로 붙인다)
	/// </summary>
	public class AvoidPlayerMovement : MonoBehaviour
	{
		[Header("Move Horizontal")]
		[SerializeField]
		private	float	moveSpeed = 8;			// 좌우 이동 속도
		[SerializeField]
		private	float	limitX = 4;				// 좌우 이동 한계 (중앙 기준 ±limitX)

		[Header("Jump")]
		[SerializeField]
		private	float	jumpHeight = 1.5f;		// 점프 한 번에 올라가는 높이
		[SerializeField]
		private	float	jumpDuration = 0.6f;	// 땅에서 뛰어서 착지할 때까지 걸리는 시간
		[SerializeField]
		private	int		maxJumpCount = 1;		// 착지 전까지 뛸 수 있는 횟수 (2면 2단 점프)
		[SerializeField]
		private	float	fallSpeed = 20;			// 공중에서 피격되어 떨어질 때의 낙하 속도

		private	float	groundY;				// 착지 높이 (시작 위치의 y)
		private	float	direction;				// 좌우 입력 (-1 ~ 1)
		private	float	velocityY;				// 수직 속도
		private	int		currentJumpCount;		// 착지 전까지 뛴 횟수

		public	bool	IsJumping	{ private set; get; } = false;

		// jumpDuration 동안 jumpHeight 까지 올라갔다 내려오는 포물선이 되는 중력/도약 속도
		private	float	Gravity			=> 8 * jumpHeight / (jumpDuration * jumpDuration);
		private	float	JumpVelocity	=> 4 * jumpHeight / jumpDuration;

		private void Awake()
		{
			groundY = transform.position.y;
		}

		private void Update()
		{
			Vector3 position = transform.position;

			position.x = Mathf.Clamp(position.x + direction * moveSpeed * Time.deltaTime, -limitX, limitX);
			position.y = UpdateJump(position.y);

			transform.position = position;
		}

		private float UpdateJump(float y)
		{
			if ( IsJumping == false ) return groundY;

			velocityY	-= Gravity * Time.deltaTime;
			y			+= velocityY * Time.deltaTime;

			if ( y <= groundY )
			{
				IsJumping			= false;
				velocityY			= 0;
				currentJumpCount	= 0;

				return groundY;
			}

			return y;
		}

		/// <summary>
		/// x 이동 방향 설정 (외부 클래스에서 호출)
		/// </summary>
		public void MoveTo(float x)
		{
			direction = Mathf.Clamp(x, -1, 1);
		}

		/// <summary>
		/// 점프 (외부 클래스에서 호출). 착지 전까지 maxJumpCount 번만 뛸 수 있다
		/// </summary>
		public bool JumpTo()
		{
			if ( currentJumpCount >= maxJumpCount ) return false;

			IsJumping	= true;
			velocityY	= JumpVelocity;
			currentJumpCount ++;

			return true;
		}

		/// <summary>
		/// 공중에 떠 있으면 점프를 끊고 바로 떨어뜨린다 (외부 클래스에서 호출, 피격 시)
		/// </summary>
		public void Fall()
		{
			if ( IsJumping == false ) return;

			velocityY			= -fallSpeed;
			currentJumpCount	= maxJumpCount;		// 착지할 때까지 다시 뛸 수 없다
		}
	}
}
