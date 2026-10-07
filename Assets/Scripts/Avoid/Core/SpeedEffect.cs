using UnityEngine;
using UnityEngine.Rendering;

namespace AVOID
{
	/// <summary>
	/// 가속 중임을 보여주는 연출. 가속(GameController.AccelerationRatio)이 쌓일수록
	///  - 화면 가장자리에서 카메라 쪽으로 흐르는 속도선이 늘어나고
	///  - 카메라 시야각(FOV)이 살짝 넓어진다.
	/// 맞아서 가속이 초기화되거나 전진 키를 떼면 부드럽게 사라진다.
	/// 속도선은 코드로 만든 ParticleSystem 을 카메라 자식으로 붙여서 쓴다 (별도 에셋 없음).
	/// </summary>
	public class SpeedEffect : MonoBehaviour
	{
		[Header("Speed Lines")]
		[SerializeField]
		private	float	maxEmissionRate = 70;		// 가속이 최대일 때 초당 생성되는 속도선 수
		[SerializeField]
		private	float	spawnDistance = 12;			// 카메라 앞쪽 생성 거리
		[SerializeField]
		private	float	ringRadius = 7;				// 속도선이 생성되는 원의 바깥 반지름 (안쪽은 비워 둠)
		[SerializeField]
		private	float	ringThickness = 0.45f;		// 바깥쪽에서 얼마나 두껍게 생성할지 (0~1, 작을수록 가장자리에 몰림)
		[SerializeField]
		private	float	lineSpeed = 55;				// 속도선이 카메라 쪽으로 날아오는 속도
		[SerializeField]
		private	float	lineLength = 2.5f;			// 속도선 길이 (월드 단위)
		[SerializeField]
		private	float	lineWidth = 0.05f;
		[SerializeField]
		private	Color	lineColor = new Color(1, 1, 1, 0.7f);

		[Header("Camera FOV")]
		[SerializeField]
		private	float	fovBoost = 8;				// 가속이 최대일 때 늘어나는 시야각 (도)

		[SerializeField]
		private	float	smoothSpeed = 4;			// 연출 세기가 따라가는 속도 (클수록 빠르게 반응)

		private	Camera			targetCamera;
		private	ParticleSystem	lines;
		private	Material		lineMaterial;
		private	float			baseFov;
		private	float			strength;			// 부드럽게 따라가는 연출 세기 (0~1)

		private void Start()
		{
			targetCamera = Camera.main;
			if ( targetCamera == null ) return;

			baseFov = targetCamera.fieldOfView;

			CreateLines();
		}

		private void Update()
		{
			if ( targetCamera == null ) return;

			GameController gameController = GameController.Instance;
			float target = gameController != null ? gameController.AccelerationRatio : 0;

			strength = Mathf.MoveTowards(strength, target, smoothSpeed * Time.deltaTime);

			targetCamera.fieldOfView = baseFov + fovBoost * strength;

			if ( lines != null )
			{
				ParticleSystem.EmissionModule emission = lines.emission;
				emission.rateOverTime = maxEmissionRate * strength;
			}
		}

		private void OnDestroy()
		{
			if ( targetCamera != null )
			{
				targetCamera.fieldOfView = baseFov;
			}

			if ( lineMaterial != null )
			{
				Destroy(lineMaterial);
			}
		}

		private void CreateLines()
		{
			GameObject go = new GameObject("SpeedLines");
			go.transform.SetParent(targetCamera.transform, false);
			go.transform.localPosition	= new Vector3(0, 0, spawnDistance);
			go.transform.localRotation	= Quaternion.identity;

			lines = go.AddComponent<ParticleSystem>();
			lines.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

			// 카메라가 움직이거나 FOV 가 바뀌어도 화면 기준으로 따라오도록 로컬 공간에서 시뮬레이션
			ParticleSystem.MainModule main = lines.main;
			main.simulationSpace	= ParticleSystemSimulationSpace.Local;
			main.loop				= true;
			main.playOnAwake		= false;
			main.startLifetime		= (spawnDistance + 4) / lineSpeed;		// 카메라 뒤 4 까지 날아간 뒤 사라짐
			main.startSpeed			= 0;									// 속도는 Velocity over Lifetime 으로 준다
			main.startSize			= lineWidth;
			main.startColor			= lineColor;
			main.maxParticles		= 200;

			ParticleSystem.EmissionModule emission = lines.emission;
			emission.rateOverTime	= 0;

			// 카메라 시선 방향을 바라보는 원형 테두리에서 생성 (안쪽 중앙은 비움)
			ParticleSystem.ShapeModule shape = lines.shape;
			shape.shapeType			= ParticleSystemShapeType.Circle;
			shape.radius			= ringRadius;
			shape.radiusThickness	= ringThickness;
			shape.arc				= 360;

			ParticleSystem.VelocityOverLifetimeModule velocity = lines.velocityOverLifetime;
			velocity.enabled	= true;
			velocity.space		= ParticleSystemSimulationSpace.Local;
			velocity.x			= 0;
			velocity.y			= 0;
			velocity.z			= -lineSpeed;

			ParticleSystem.ColorOverLifetimeModule colorOverLifetime = lines.colorOverLifetime;
			colorOverLifetime.enabled = true;
			Gradient gradient = new Gradient();
			gradient.SetKeys
			(
				new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
				new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) }
			);
			colorOverLifetime.color = gradient;

			ParticleSystemRenderer render = lines.GetComponent<ParticleSystemRenderer>();
			render.renderMode		= ParticleSystemRenderMode.Stretch;
			render.velocityScale	= 0;
			render.lengthScale		= lineLength / Mathf.Max(lineWidth, 0.001f);	// 길이 = 크기 x lengthScale
			render.sortingFudge		= -10;
			render.sharedMaterial	= CreateMaterial();

			lines.Play();
		}

		private Material CreateMaterial()
		{
			Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
			if ( shader == null )
			{
				shader = Shader.Find("Universal Render Pipeline/Unlit");
			}

			lineMaterial = new Material(shader);

			// 반투명(알파 블렌드) 설정. URP 파티클 셰이더의 Surface Type = Transparent 에 해당
			lineMaterial.SetFloat("_Surface", 1);
			lineMaterial.SetFloat("_Blend", 0);
			lineMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
			lineMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
			lineMaterial.SetFloat("_ZWrite", 0);
			lineMaterial.SetOverrideTag("RenderType", "Transparent");
			lineMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			lineMaterial.renderQueue = (int)RenderQueue.Transparent;

			return lineMaterial;
		}
	}
}
