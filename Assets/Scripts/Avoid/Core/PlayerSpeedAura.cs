using UnityEngine;
using UnityEngine.Rendering;

namespace AVOID
{
	/// <summary>
	/// 가속 중임을 캐릭터 주변에서 보여주는 연출 (테일즈 러너식).
	/// 가속(GameController.AccelerationRatio)이 쌓일수록
	///  - 캐릭터 양옆에서 흐릿한 하얀 바람 자국이 뒤(카메라 쪽)로 길게 흘러가고
	///  - 발밑에서 먼지가 일어난다.
	/// 파티클은 월드 공간에서 시뮬레이션하므로 캐릭터가 좌우로 움직이면 뒤에 꼬리처럼 남는다.
	/// 맞아서 가속이 초기화되거나 전진 키를 떼면 부드럽게 사라진다.
	/// 코드로 만든 ParticleSystem 을 캐릭터 자식으로 붙여서 쓴다 (별도 에셋 없음).
	/// SpeedEffect(화면 전체 속도선 + FOV)와 함께 써도 되고 따로 써도 된다.
	/// </summary>
	public class PlayerSpeedAura : MonoBehaviour
	{
		private const float REFERENCE_SPEED = 20;	// 파티클 수명 계산용 기준 속도 (실제 흐름 속도는 StageSpeed 를 따름)

		[Header("Wind Trails")]
		[SerializeField]
		private	float	trailEmissionRate = 14;		// 가속이 최대일 때 한쪽 옆에서 초당 생성되는 바람 자국 수
		[SerializeField]
		private	Vector3	trailCenter = new Vector3(0, 0.8f, 0);	// 캐릭터 발 기준 몸 중심 위치 (바람 자국이 나오는 높이)
		[SerializeField]
		private	float	trailSideOffset = 0.8f;		// 몸 중심에서 좌우로 떨어진 거리
		[SerializeField]
		private	float	trailHeightRange = 1.0f;	// 바람 자국이 퍼지는 높이 범위
		[SerializeField]
		private	float	trailSpeedRatio = 1.6f;		// 바람 자국이 흐르는 속도 = 바닥 속도(StageSpeed) x 이 값 (1이면 바닥과 같은 속도)
		[SerializeField]
		private	float	trailLength = 3;			// 바람 자국 길이 (월드 단위)
		[SerializeField]
		private	float	trailWidth = 0.14f;
		[SerializeField]
		private	float	trailSpawnDistance = 1.5f;	// 캐릭터 앞쪽(진행 방향) 생성 거리
		[SerializeField]
		private	Color	trailColor = new Color(1, 1, 1, 0.5f);

		[Header("Foot Dust")]
		[SerializeField]
		private	float	dustEmissionRate = 18;		// 가속이 최대일 때 초당 생성되는 먼지 수
		[SerializeField]
		private	float	dustSize = 0.35f;
		[SerializeField]
		private	float	dustGrowth = 2.2f;			// 사라질 때까지 커지는 배율
		[SerializeField]
		private	Color	dustColor = new Color(1, 1, 1, 0.55f);

		[SerializeField]
		private	float	smoothSpeed = 4;			// 연출 세기가 따라가는 속도 (클수록 빠르게 반응)

		private	ParticleSystem	leftTrail;
		private	ParticleSystem	rightTrail;
		private	ParticleSystem	dust;
		private	Texture2D		softTexture;
		private	Material		softMaterial;
		private	float			strength;			// 부드럽게 따라가는 연출 세기 (0~1)

		private void Start()
		{
			softTexture	= CreateSoftTexture();
			softMaterial = CreateMaterial(softTexture);

			leftTrail	= CreateTrail("WindTrailLeft", -trailSideOffset);
			rightTrail	= CreateTrail("WindTrailRight", trailSideOffset);
			dust		= CreateDust();
		}

		private void Update()
		{
			GameController gameController = GameController.Instance;
			float target = gameController != null ? gameController.AccelerationRatio : 0;

			strength = Mathf.MoveTowards(strength, target, smoothSpeed * Time.deltaTime);

			SetRate(leftTrail,	trailEmissionRate * strength);
			SetRate(rightTrail,	trailEmissionRate * strength);
			SetRate(dust,		dustEmissionRate * strength);

			// 이 게임의 캐릭터는 제자리이고 바닥(GroundScroller)이 흘러온다.
			// 파티클도 같은 바닥 속도를 따라야 어긋나 보이지 않는다 (가속하면 같이 빨라진다)
			float stageSpeed = GameController.StageSpeed;

			SetFlowSpeed(leftTrail,		stageSpeed * trailSpeedRatio, stageSpeed * trailSpeedRatio);
			SetFlowSpeed(rightTrail,	stageSpeed * trailSpeedRatio, stageSpeed * trailSpeedRatio);
			SetFlowSpeed(dust,			stageSpeed * 0.9f, stageSpeed * 1.1f);		// 먼지는 바닥에 붙어 있으므로 바닥 속도 그대로
		}

		private static void SetFlowSpeed(ParticleSystem system, float min, float max)
		{
			ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
			velocity.z = new ParticleSystem.MinMaxCurve(-max, -min);
		}

		private void OnDestroy()
		{
			if ( softMaterial != null )	Destroy(softMaterial);
			if ( softTexture != null )	Destroy(softTexture);
		}

		private static void SetRate(ParticleSystem system, float rate)
		{
			ParticleSystem.EmissionModule emission = system.emission;
			emission.rateOverTime = rate;
		}

		// 양옆 허리 높이에서 앞→뒤로 길게 흐르는 흐릿한 바람 자국
		private ParticleSystem CreateTrail(string objectName, float sideX)
		{
			ParticleSystem system = CreateSystem(objectName, trailCenter + new Vector3(sideX, 0, 0));

			float life = (trailSpawnDistance * 2 + trailLength) / REFERENCE_SPEED;

			ParticleSystem.MainModule main = system.main;
			main.simulationSpace	= ParticleSystemSimulationSpace.World;
			main.startLifetime		= life;
			main.startSpeed			= 0;									// 속도는 Velocity over Lifetime 으로 준다
			main.startSize			= new ParticleSystem.MinMaxCurve(trailWidth * 0.6f, trailWidth);
			main.startColor			= trailColor;
			main.maxParticles		= 60;

			// 캐릭터 옆 높이 범위에 얇은 박스로 생성 (z 는 캐릭터 앞쪽)
			ParticleSystem.ShapeModule shape = system.shape;
			shape.shapeType			= ParticleSystemShapeType.Box;
			shape.scale				= new Vector3(0.3f, trailHeightRange, 0.01f);
			shape.position			= new Vector3(0, 0, trailSpawnDistance);

			ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
			velocity.enabled	= true;
			velocity.space		= ParticleSystemSimulationSpace.World;
			// x / y / z 는 같은 모드여야 한다. Update 의 SetFlowSpeed 가 z 를 TwoConstants 로 갱신하므로 모두 TwoConstants 로 맞춘다
			velocity.x			= new ParticleSystem.MinMaxCurve(0, 0);
			velocity.y			= new ParticleSystem.MinMaxCurve(0, 0);
			velocity.z			= new ParticleSystem.MinMaxCurve(-REFERENCE_SPEED, -REFERENCE_SPEED);		// 실제 속도는 Update 에서 StageSpeed 로 갱신

			ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
			colorOverLifetime.enabled	= true;
			colorOverLifetime.color		= FadeInOut(0.25f, 0.5f);

			ParticleSystemRenderer render = system.GetComponent<ParticleSystemRenderer>();
			render.renderMode		= ParticleSystemRenderMode.Stretch;
			render.velocityScale	= 0;
			render.lengthScale		= trailLength / Mathf.Max(trailWidth, 0.001f);	// 길이 = 크기 x lengthScale
			render.sharedMaterial	= softMaterial;

			system.Play();

			return system;
		}

		// 발밑에서 일어나 뒤로 밀려나며 커지고 옅어지는 먼지
		private ParticleSystem CreateDust()
		{
			ParticleSystem system = CreateSystem("FootDust", new Vector3(0, 0.1f, -0.1f));

			ParticleSystem.MainModule main = system.main;
			main.simulationSpace	= ParticleSystemSimulationSpace.World;
			main.startLifetime		= new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
			main.startSpeed			= 0;
			main.startSize			= new ParticleSystem.MinMaxCurve(dustSize * 0.7f, dustSize);
			main.startColor			= dustColor;
			main.startRotation		= new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
			main.maxParticles		= 60;

			ParticleSystem.ShapeModule shape = system.shape;
			shape.shapeType			= ParticleSystemShapeType.Box;
			shape.scale				= new Vector3(0.5f, 0.05f, 0.3f);

			// 스테이지 바닥이 흘러오는 방향(카메라 쪽)으로 밀려나고 살짝 위로 뜬다
			ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
			velocity.enabled	= true;
			velocity.space		= ParticleSystemSimulationSpace.World;
			velocity.x			= new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
			velocity.y			= new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
			velocity.z			= new ParticleSystem.MinMaxCurve(-12, -6);

			ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
			size.enabled	= true;
			size.size		= new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, dustGrowth));

			ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
			colorOverLifetime.enabled	= true;
			colorOverLifetime.color		= FadeInOut(0.1f, 0.2f);

			ParticleSystemRenderer render = system.GetComponent<ParticleSystemRenderer>();
			render.renderMode		= ParticleSystemRenderMode.Billboard;
			render.sharedMaterial	= softMaterial;

			system.Play();

			return system;
		}

		private ParticleSystem CreateSystem(string objectName, Vector3 localPosition)
		{
			GameObject go = new GameObject(objectName);
			go.transform.SetParent(transform, false);
			go.transform.localPosition	= localPosition;
			go.transform.localRotation	= Quaternion.identity;		// 카메라가 보는 방향(+z)을 바라본다

			ParticleSystem system = go.AddComponent<ParticleSystem>();
			system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

			ParticleSystem.MainModule main = system.main;
			main.loop			= true;
			main.playOnAwake	= false;

			ParticleSystem.EmissionModule emission = system.emission;
			emission.rateOverTime = 0;

			return system;
		}

		// 처음엔 투명 → 서서히 나타남 → 유지 → 사라짐
		private static Gradient FadeInOut(float fadeIn, float fadeOut)
		{
			Gradient gradient = new Gradient();
			gradient.SetKeys
			(
				new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
				new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, fadeIn), new GradientAlphaKey(1, 1 - fadeOut), new GradientAlphaKey(0, 1) }
			);

			return gradient;
		}

		// 가장자리로 갈수록 옅어지는 부드러운 원형 텍스처 (바람 자국/먼지가 흐릿하게 보이도록)
		private static Texture2D CreateSoftTexture()
		{
			const int size = 64;

			Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
			texture.wrapMode = TextureWrapMode.Clamp;

			Color[] pixels = new Color[size * size];
			for ( int y = 0; y < size; ++ y )
			{
				for ( int x = 0; x < size; ++ x )
				{
					float dx		= (x + 0.5f) / size * 2 - 1;
					float dy		= (y + 0.5f) / size * 2 - 1;
					float alpha		= Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy));

					pixels[y * size + x] = new Color(1, 1, 1, alpha * alpha);
				}
			}

			texture.SetPixels(pixels);
			texture.Apply();

			return texture;
		}

		private static Material CreateMaterial(Texture2D texture)
		{
			Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
			if ( shader == null )
			{
				shader = Shader.Find("Universal Render Pipeline/Unlit");
			}

			Material material = new Material(shader);
			material.SetTexture("_BaseMap", texture);

			// 반투명(알파 블렌드) 설정. URP 파티클 셰이더의 Surface Type = Transparent 에 해당
			material.SetFloat("_Surface", 1);
			material.SetFloat("_Blend", 0);
			material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
			material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
			material.SetFloat("_ZWrite", 0);
			material.SetOverrideTag("RenderType", "Transparent");
			material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			material.renderQueue = (int)RenderQueue.Transparent;

			return material;
		}
	}
}
