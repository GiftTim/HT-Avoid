using UnityEngine;
using System;
using History;
using System.Collections.Generic;
using UnityEngine.InputSystem;

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

        // Avoid 등 다른 씬이 애디티브로 얹혀 키보드/마우스를 받아야 할 때 끔.
        // DeactivateInput()은 액션만 끄고 디바이스 페어링은 유지되어, 뒤이어
        // 켜지는 다른 PlayerInput(Avoid)이 같은 키보드/마우스를 페어링하지
        // 못하는 문제가 있었음 → 컴포넌트 자체를 껐다 켜서 페어링까지 해제/재획득
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