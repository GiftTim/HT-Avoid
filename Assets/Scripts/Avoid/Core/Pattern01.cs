using System.Collections;
using UnityEngine;

public class Pattern01 : MonoBehaviour
{
	[SerializeField]
	private	GameObject	enemyPrefab;	// 적 프리팹
	[SerializeField]
	private	float		spawnCycle;		// 생성 주기

	[SerializeField]
	private	AudioClip	voiceClip;		// 패턴 진행 중 반복 재생할 음성

	private	AudioSource	voiceSource;	// AudioManager가 재생 중인 음성

	private void OnEnable()
	{
		StartCoroutine(nameof(SpawnEnemies));
	}

	private void OnDisable()
	{
		StopCoroutine(nameof(SpawnEnemies));
		StopVoice();
	}

	private IEnumerator SpawnEnemies()
	{
		// 패턴 시작 전 잠시 대기하는 시간
		float waitTime = 1f;
		yield return new WaitForSeconds(waitTime);

		while ( true )
		{
			// 음성 사운드는 재생이 종료되면 다시 재생
			// (재생이 끝나면 AudioManager가 오브젝트를 파괴하므로 null 체크가 먼저)
			if ( voiceSource == null || voiceSource.isPlaying == false )
			{
				PlayVoice();
			}

			Vector3 position = new Vector3(Random.Range(Constants.min.x, Constants.max.x), Constants.max.y, 0);
			Instantiate(enemyPrefab, position, Quaternion.identity);

			yield return new WaitForSeconds(spawnCycle);
		}
	}

	private void PlayVoice()
	{
		if ( voiceClip == null || AudioManager.instance == null ) return;

		voiceSource = AudioManager.instance.PlayVoice(voiceClip);
	}

	private void StopVoice()
	{
		if ( voiceSource != null )
		{
			Destroy(voiceSource.gameObject);
		}
		voiceSource = null;
	}
}

