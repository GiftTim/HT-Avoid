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
        }

        private static IEnumerator StartAvoid(string[] data)
        {
            yield return DialogueSystem.instance.Hide(0f, true);

            // VN의 Next/HistoryBack 등(Space/Enter 등)이 Avoid의 WASD/클릭과
            // 동시에 같은 PlayerInput 체계에 걸려있어 입력이 먹히지 않는 문제를
            // 막기 위해, Avoid로 넘어가는 동안 VN 쪽 입력을 꺼둔다.
            PlayerInputManager.instance?.SetInputEnabled(false);

            Enemy.ResetClearState();

            yield return SceneManager.LoadSceneAsync(AVOID_SCENE_NAME, LoadSceneMode.Additive);

            Debug.Log("[CMD_DatabaseExtension_Avoid] Avoid 씬 로드 완료");
        }

        private static IEnumerator EndAvoid(string[] data)
        {
            yield return ReturnToVN();
        }

        // Enemy가 처치되는 등 대본 명령이 아니라 게임플레이 이벤트로 VN에
        // 복귀해야 할 때 씀 (endAvoid 커맨드와 동일한 절차를 공유)
        public static IEnumerator ReturnToVN()
        {
            yield return SceneManager.UnloadSceneAsync(AVOID_SCENE_NAME);

            PlayerInputManager.instance?.SetInputEnabled(true);

            yield return DialogueSystem.instance.Show(0f, true);
        }
    }
}
