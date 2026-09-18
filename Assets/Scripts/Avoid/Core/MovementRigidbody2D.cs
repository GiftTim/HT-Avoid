using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovementRigidbody2D : MonoBehaviour
{
    [Header("Move Horizontal")]
    [SerializeField]
    private float moveSpeed = 8;            // 이동 속도
    [SerializeField]
    private float groundAcceleration = 120; // 지상 가속 (클수록 즉각 반응, 작을수록 부드럽게 미끄러짐)
    [SerializeField]
    private float airAcceleration = 80;     // 공중 가속 (공중 방향 전환 반응)

    [Header("Move Vertical (Jump)")]
    [SerializeField]
    private float jumpForce = 12;           // 점프 힘
    [SerializeField]
    private float riseGravity = 4;          // 상승 중, 점프키를 누르고 있을 때의 중력 (높은 점프)
    [SerializeField]
    private float jumpCutGravity = 8;       // 상승 중, 점프키를 뗐을 때의 중력 (짧은 점프)
    [SerializeField]
    private float fallGravity = 7;          // 하강 중력 → 상승보다 세게 줘서 '붕붕' 뜨는 느낌 제거의 핵심
    [SerializeField]
    private float maxFallSpeed = 18;        // 최대 낙하 속도 (종단 속도 제한)
    [SerializeField]
    private int maxJumpCount = 1;           // 최대 점프 횟수 (메이플 기본: 1. 더블점프 원하면 2)
    private int currentJumpCount;           // 현재 남아있는 점프 횟수

    [Header("Collision")]
    [SerializeField]
    private LayerMask groundLayer;          // 바닥 충돌 체크를 위한 레이어

    private bool isGrounded;                // 바닥 체크 (발이 바닥에 닿아있으면 true)
    private Vector2 footPosition;           // 바닥 체크용 발 위치
    private Vector2 footArea;               // 바닥 체크용 발 인식 범위

    private Rigidbody2D rigid2D;            // 속력 제어를 위한 Rigidbody2D
    private new Collider2D collider2D;      // 현재 오브젝트의 충돌 범위 정보

    public bool IsLongJump { set; get; } = false;

    private void Awake()
    {
        rigid2D = GetComponent<Rigidbody2D>();
        collider2D = GetComponent<Collider2D>();

        // 벽에 붙어서 안 미끄러지는 현상 방지를 위해 마찰을 0으로 둔다.
        rigid2D.sharedMaterial = new PhysicsMaterial2D { friction = 0, bounciness = 0 };
    }

    private void FixedUpdate()
    {
        // 발 위치/범위 계산 후 바닥 체크
        Bounds bounds = collider2D.bounds;
        footPosition = new Vector2(bounds.center.x, bounds.min.y);
        footArea = new Vector2((bounds.max.x - bounds.min.x) * 0.5f, 0.1f);
        isGrounded = Physics2D.OverlapBox(footPosition, footArea, 0, groundLayer);

        // 바닥에 닿았고 하강/정지 상태이면 점프 횟수 초기화
        if (isGrounded && rigid2D.linearVelocity.y <= 0)
        {
            currentJumpCount = maxJumpCount;
        }

        ApplyGravityScale();
        ClampFallSpeed();

        // 플레이어가 화면 경계(Constants.min/max) 밖으로 나가지 않도록 위치 제한
        rigid2D.position = new Vector2(
            Mathf.Clamp(rigid2D.position.x, Constants.min.x, Constants.max.x),
            Mathf.Clamp(rigid2D.position.y, Constants.min.y, Constants.max.y));
    }

    /// <summary>
    /// 상승/하강 구간에 따라 중력 계수를 다르게 적용.
    /// 하강 중력을 상승보다 세게 줘서 올라간 것보다 빠르게 떨어지게 만든다 → 붕뜨는 느낌 제거.
    /// </summary>
    private void ApplyGravityScale()
    {
        if (rigid2D.linearVelocity.y > 0.01f)
        {
            // 상승 중: 점프키를 누르고 있으면 낮은 중력(높은 점프), 뗐으면 높은 중력(짧은 점프)
            rigid2D.gravityScale = IsLongJump ? riseGravity : jumpCutGravity;
        }
        else
        {
            // 하강 중: 더 강한 중력으로 묵직하게 떨어진다
            rigid2D.gravityScale = fallGravity;
        }
    }

    /// <summary>
    /// 낙하 속도가 너무 빨라지지 않도록 종단 속도를 제한.
    /// </summary>
    private void ClampFallSpeed()
    {
        if (rigid2D.linearVelocity.y < -maxFallSpeed)
        {
            rigid2D.linearVelocity = new Vector2(rigid2D.linearVelocity.x, -maxFallSpeed);
        }
    }

    /// <summary>
    /// x 이동 방향 설정 (외부 클래스에서 호출).
    /// 즉시 속도를 바꾸지 않고 가속을 줘서 메이플 특유의 살짝 미끄러지는 이동감을 낸다.
    /// </summary>
    public void MoveTo(float x)
    {
        float targetSpeed = x * moveSpeed;
        float accel = isGrounded ? groundAcceleration : airAcceleration;
        float newX = Mathf.MoveTowards(rigid2D.linearVelocity.x, targetSpeed, accel * Time.fixedDeltaTime);
        rigid2D.linearVelocity = new Vector2(newX, rigid2D.linearVelocity.y);
    }

    /// <summary>
    /// 점프 (외부 클래스에서 호출)
    /// </summary>
    public bool JumpTo()
    {
        if (currentJumpCount > 0)
        {
            rigid2D.linearVelocity = new Vector2(rigid2D.linearVelocity.x, jumpForce);
            currentJumpCount--;

            return true;
        }

        return false;
    }

    /// <summary>
    /// 지정 위치로 즉시 이동시키고 속도를 초기화 (리스폰 등)
    /// </summary>
    public void Teleport(Vector2 position)
    {
        rigid2D.position = position;
        rigid2D.linearVelocity = Vector2.zero;
    }
}
