using System.Collections;
using TMPro;
using UnityEngine;

namespace Simple_GPT_Dialog.Demos
{
    public class SimpleDemo2 : MonoBehaviour
    {
        public TMP_InputField Prompt;
        public TMP_Text Response;
        public SimpleGPT GPT;
        private bool _chatStarted = false;
        private IEnumerator _loadingCoroutine;

        void OnEnable()
        {
            SimpleGPT.onGPTChatResponse += HandleChatResponse;
        }

        private void OnDisable()
        {
            SimpleGPT.onGPTChatResponse -= HandleChatResponse;
        }

        private IEnumerator ResponseWaitAnimation()
        {
            for (;;)
            {
                yield return new WaitForSeconds(0.35f);
                if (Response.text.Length < 5) Response.text += ".";
                else Response.text = ".";
            }
        }

        public void OnSubmitButtonClicked()
        {
            _loadingCoroutine = ResponseWaitAnimation();
            StartCoroutine(_loadingCoroutine);
            GPT.RequestChatResponse(Prompt.text);
        }

        void HandleChatResponse(string response, string fullText, string npcName)
        {
            StopCoroutine(_loadingCoroutine);
            Response.text = response;
        }
    }
}
