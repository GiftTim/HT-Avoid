using UnityEngine;
using System;
using History;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace DIALOGUE
{
    public class PlayerInputManager : MonoBehaviour
    {
        public static PlayerInputManager instance { get; private set; }

        private PlayerInput input;
        private List<(InputAction action, Action<InputAction.CallbackContext> command)> actions
            = new List<(InputAction, Action<InputAction.CallbackContext>)>();

        void Awake()
        {
            instance = this;

            input = GetComponent<PlayerInput>();

            InitializeActions();
        }

        void Start()
        {
            DisableMouse();
        }

        // 조작은 키보드/게임패드 전용. 커서를 숨기고 UI 가 마우스(가리키기/클릭/휠)에
        // 반응하지 않게 한다. 방향키/확인/취소 내비게이션(move/submit/cancel)은 그대로 둔다
        private void DisableMouse()
        {
            Cursor.visible = false;
#if !UNITY_EDITOR
            // 에디터에서는 잠그면 Play 중 인스펙터를 만질 때마다 Esc(설정창 열기와 겹침)를 눌러야 해서 빌드에서만 잠근다
            Cursor.lockState = CursorLockMode.Locked;
#endif

            InputSystemUIInputModule uiModule = FindFirstObjectByType<InputSystemUIInputModule>();
            if (uiModule == null)
                return;

            uiModule.point = null;
            uiModule.leftClick = null;
            uiModule.middleClick = null;
            uiModule.rightClick = null;
            uiModule.scrollWheel = null;
        }

        // Avoid 등 다른 씬이 애디티브로 얹혀 키보드/마우스를 받아야 할 때 끔.
        // DeactivateInput()은 액션만 끄고 디바이스 페어링은 유지되어, 뒤이어
        // 켜지는 다른 PlayerInput(Avoid)이 같은 키보드/마우스를 페어링하지
        // 못하는 문제가 있었음 → 컴포넌트 자체를 껐다 켜서 페어링까지 해제/재획득
        public bool IsInputEnabled => input != null && input.enabled;

        public void SetInputEnabled(bool isEnabled)
        {
            input.enabled = isEnabled;
        }

        private void InitializeActions()
        {
            actions.Add((input.actions["Next"], OnNext));
            actions.Add((input.actions["HistoryBack"], OnHistoryBack));
            actions.Add((input.actions["HistoryForward"], OnHistoryForward));
            actions.Add((input.actions["HistoryLogs"], OnHistoryToggleLog));
        }

        private void OnEnable()
        {
            foreach (var inputAction in actions)
            {
                inputAction.action.performed += inputAction.command;
            }
        }

        private void OnDisable()
        {
            foreach (var inputAction in actions)
            {
                inputAction.action.performed -= inputAction.command;
            }
        }

        public void OnNext(InputAction.CallbackContext context)
        {
            DialogueSystem.instance.OnUserPrompt_Next();
        }
         public void OnHistoryBack(InputAction.CallbackContext context)
        {
            HistoryManager.instance.GoBack();
        }
        public void OnHistoryForward(InputAction.CallbackContext context)
        {
            HistoryManager.instance.GoForward();
        }

        public void OnHistoryToggleLog(InputAction.CallbackContext c)
        {
            var logs = HistoryManager.instance.logManager;

            if(!logs.isOpen)
            {
                logs.Open();
            }
            else
            {
                logs.Close();
            }
        }
    }
}