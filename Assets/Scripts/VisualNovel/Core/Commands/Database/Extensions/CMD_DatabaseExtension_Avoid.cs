using AVOID;
using DIALOGUE;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace COMMANDS
{
    public class CMD_DatabaseExtension_Avoid : CMD_DatabaseExtension
    {
        private const string AVOID_SCENE_NAME = "Avoid";

        new public static void Extend(CommandDatabase database)
        {
            database.AddCommand("startAvoid", new Func<string[], IEnumerator>(StartAvoid));
            database.AddCommand("endAvoid", new Func<string[], IEnumerator>(EndAvoid));
            database.AddCommand("restartDialogue", new Action<string[]>(RestartDialogue));
        }

        /// <summary>
        /// 피하기 씬을 VN 위에 얹고, 플레이어가 클리어할 때까지 기다린 뒤 VN 으로 복귀한다.
        /// 대본에서는 [wait]startAvoid() 한 줄이면 되고, 끝나면 다음 줄로 이어진다.
        /// 시간 초과는 게임 쪽(GameController)이 같은 판을 다시 시작하므로 여기서는 클리어만 기다린다.
        /// 인자(스테이지명)는 3-a(스테이지 프로필)에서 쓸 예정이라 지금은 받기만 한다.
        /// 결과는 $Avoid.hitCount / $Avoid.retryCount / $Avoid.remainingTime / $Avoid.cleared 로 읽을 수 있다
        /// </summary>
        private static IEnumerator StartAvoid(string[] data)
        {
            yield return DialogueSystem.instance.Hide(0f, true);

            // VN의 Next/HistoryBack 등(Space/Enter 등)이 Avoid의 입력과
            // 동시에 같은 PlayerInput 체계에 걸려있어 입력이 먹히지 않는 문제를
            // 막기 위해, Avoid로 넘어가는 동안 VN 쪽 입력을 꺼둔다.
            PlayerInputManager.instance?.SetInputEnabled(false);

            // [startAvoid(스테이지이름)] : Resources/Stages/ 의 StageProfile 에셋 이름. 비우면 기본 프로필
            AvoidSession.Begin(data != null && data.Length > 0 ? data[0] : "");

            yield return SceneManager.LoadSceneAsync(AVOID_SCENE_NAME, LoadSceneMode.Additive);

            Debug.Log("[CMD_DatabaseExtension_Avoid] Avoid 씬 로드 완료 — 클리어를 기다리는 중");

            while ( !AvoidSession.IsFinished )
            {
                yield return null;
            }

            AvoidSession.WriteToVariables();

            yield return ReturnToVN();
        }

        // 예전 대본(startAvoid 와 endAvoid 를 따로 쓰던 방식) 호환용.
        // 이제는 startAvoid 가 스스로 복귀하므로, 이미 내려간 씬이면 아무것도 하지 않는다
        private static IEnumerator EndAvoid(string[] data)
        {
            if ( SceneManager.GetSceneByName(AVOID_SCENE_NAME).isLoaded )
            {
                yield return ReturnToVN();
            }
        }

        public static IEnumerator ReturnToVN()
        {
            yield return SceneManager.UnloadSceneAsync(AVOID_SCENE_NAME);

            PlayerInputManager.instance?.SetInputEnabled(true);

            yield return DialogueSystem.instance.Show(0f, true);
        }

        /// <summary>
        /// 지금 재생 중인 대본(대화 파일)을 첫 줄부터 다시 재생한다.
        /// 선택지 오답 줄에서 [wait]startAvoid() 다음에 써서 "피하기를 다시 깬 뒤 대화를 처음부터" 를 만든다.
        /// 큐의 마지막 Conversation 이 대본 전체(부모)이고, 앞쪽은 선택지/조건문 결과로 끼어든 조각이다
        /// </summary>
        private static void RestartDialogue(string[] data)
        {
            Conversation[] queue = DialogueSystem.instance.conversationManager.GetConversationQueue();

            if ( queue.Length == 0 ) return;

            // 끼어든 조각들은 끝으로 보내 버려서 엔진이 차례로 큐에서 빼게 한다
            for ( int i = 0; i < queue.Length - 1; i ++ )
            {
                queue[i].SetProgress(queue[i].Count);
            }

            // 부모는 첫 줄로. 부모 자신이 이 커맨드를 실행 중이면 줄이 끝나며 1이 더해지므로 -1 에서 시작
            Conversation parent = queue[queue.Length - 1];
            parent.SetProgress(queue.Length == 1 ? -1 : 0);
        }
    }
}
