using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Simple_GPT_Dialog
{
    public class SimpleGPTAutoDialog : MonoBehaviour
    {
        #region Editor Variables
        
        public bool FirstPersonMode;
        public TMP_InputField Prompt;
        public KeyCode PromptKey = KeyCode.Return;
        public int MaxPromptLength = 100;
        
        #endregion

        #region Private Variables

        private bool _blockMovement;
        private bool _isTyping;
        private SimpleGPT _gpt;
        private SimpleGPTPlayer _player;
        private List<SimpleGPTCharacter> _characters = new();
        private SimpleGPTCharacter _lastTalkedCharacter;
        private IEnumerator _responseWaitAnimationCoroutine;
        private IEnumerator _responseTextAnimationCoroutine;
        private bool _responseTextAnimationActive;
        private bool _responseWaitAnimationActive;
        private string _currentResponseText;

        #endregion

        private void OnEnable()
        {
            SimpleGPT.onGPTChatResponse += HandleChatResponse;
            _gpt = FindObjectOfType<SimpleGPT>();
            _player = FindObjectOfType<SimpleGPTPlayer>();
            _characters = FindObjectsOfType<SimpleGPTCharacter>().ToList();
            Prompt.characterLimit = MaxPromptLength;
        }

        private void OnDisable()
        {
            SimpleGPT.onGPTChatResponse -= HandleChatResponse;
        }

        private void Update()
        {
            if (FirstPersonMode)
            {
                UsePromptWithoutMouse();
            }
            else if (Input.GetKeyUp(PromptKey))
            {
                SubmitChatRequest();
            }

            if (_lastTalkedCharacter && _lastTalkedCharacter.DialogActive)
            {
                CloseChatIfTooFarFromCharacter();
                
                if (_lastTalkedCharacter.TurnTowardsPlayer)
                {
                    LookAtPlayer();
                }
            }
        }

        private void CloseChatIfTooFarFromCharacter()
        {
            if (Vector3.Distance(_player.transform.position, _lastTalkedCharacter.transform.position) >
                _lastTalkedCharacter.ChatDistance)
            {
                ResetChatData();
            }
        }

        public bool MovementBlocked()
        {
            return _blockMovement;
        }

        private void UsePromptWithoutMouse()
        {
            if (_isTyping && Input.GetKeyUp(PromptKey))
            {
                _isTyping = false;
                _blockMovement = false;
                Prompt.DeactivateInputField();
                EventSystem.current.SetSelectedGameObject(null);
                if (!string.IsNullOrEmpty(Prompt.text) && !string.IsNullOrWhiteSpace(Prompt.text))
                    SubmitChatRequest();
            }
            else if (Input.GetKeyUp(PromptKey))
            {
                _blockMovement = true;
                _isTyping = true;
                Prompt.Select();
                Prompt.ActivateInputField();
            }
        }

        private void LookAtPlayer()
        {
            var targetDirection =
                Vector3.ProjectOnPlane(_player.transform.position - _lastTalkedCharacter.transform.position,
                    Vector3.up);
            var targetRotation = Quaternion.LookRotation(targetDirection);
            _lastTalkedCharacter.transform.rotation = Quaternion.Slerp(_lastTalkedCharacter.transform.rotation,
                targetRotation, 2 * Time.deltaTime);
        }

        private bool TryGetClosestCharacter(out SimpleGPTCharacter closestCharacter)
        {
            var closestDistance = float.MaxValue;
            closestCharacter = null;
            var characterFound = false;
            foreach (var character in _characters)
            {
                var dist = Vector3.Distance(_player.transform.position, character.transform.position);
                if (dist < closestDistance && dist < character.ChatDistance)
                {
                    closestDistance = Vector3.Distance(_player.transform.position, character.transform.position);
                    closestCharacter = character;
                    characterFound = true;
                }
            }

            return characterFound;
        }

        public void ResetChatData()
        {
            if (_responseWaitAnimationCoroutine != null)
            {
                _responseWaitAnimationActive = false;
                StopCoroutine(_responseWaitAnimationCoroutine);
            }

            if (_responseTextAnimationCoroutine != null)
            {
                _responseTextAnimationActive = false;
                StopCoroutine(_responseTextAnimationCoroutine);
            }
            
            _gpt.EndCurrentChat();
            _lastTalkedCharacter.SetDialogUIActive(false);
        }

        private string BuildDescriptionText(SimpleGPTCharacter character)
        {
            var descriptions = "Player name: " + _player.PlayerName + "\n \n"
                               + "Player description: " + _player.PlayerDescription + "\n \n"
                               + "NPC name: " + character.CharacterName + "\n \n"
                               + "NPC description: " + character.CharacterDescription + "\n \n"
                               + "NPC location: " + character.CharacterLocation;
            return descriptions;
        }

        private void SubmitChatRequest()
        {
            if (_responseTextAnimationActive || _responseWaitAnimationActive) return;

            if (!TryGetClosestCharacter(out var closestCharacter))
            {
                Prompt.text = "";
                return;
            }

            if (_lastTalkedCharacter && closestCharacter != _lastTalkedCharacter)
            {
                ResetChatData();
                _lastTalkedCharacter = closestCharacter;
            }

            closestCharacter.SetDialogUIActive(true);

            if (!closestCharacter.ChatStarted)
            {
                _gpt.StartNewChat(_player.PlayerName, closestCharacter, BuildDescriptionText(closestCharacter));
                closestCharacter.ChatStarted = true;
            }
            else if (!string.IsNullOrEmpty(_lastTalkedCharacter.ChatHistory))
            {
                _gpt.ReturnToChat(_player.PlayerName, closestCharacter);
            }

            _lastTalkedCharacter = closestCharacter;
            _gpt.RequestChatResponse(Prompt.text);
            Prompt.text = "";
            _responseWaitAnimationCoroutine = ResponseWaitAnimation();
            StartCoroutine(_responseWaitAnimationCoroutine);
        }

        private IEnumerator ResponseWaitAnimation()
        {
            _responseWaitAnimationActive = true;

            for (;;)
            {
                yield return new WaitForSeconds(0.35f);
                if (_lastTalkedCharacter.SpeechBox.text.Length < 5) _lastTalkedCharacter.SpeechBox.text += ".";
                else _lastTalkedCharacter.SpeechBox.text = ".";
                if (!_lastTalkedCharacter.DialogActive)
                {
                    _lastTalkedCharacter.SpeechBox.text = "";
                    _responseWaitAnimationActive = false;
                    break;
                }
            }
        }

        private IEnumerator CharacterResponseTextAnimation(string response)
        {
            _responseWaitAnimationActive = false;
            _responseTextAnimationActive = true;

            _lastTalkedCharacter.SpeechBox.text = "";
            foreach (var c in response)
            {
                yield return new WaitForSeconds(0.05f);
                _lastTalkedCharacter.SpeechBox.text += c;
                if (!_lastTalkedCharacter.DialogActive)
                {
                    _responseTextAnimationActive = false;
                    _lastTalkedCharacter.SpeechBox.text = "";
                    break;
                }
            }

            _responseTextAnimationActive = false;
        }
        private void HandleChatResponse(string response, string fullText, string npcName)
        {
            if (_lastTalkedCharacter.CharacterName != npcName) return;
            if (!_lastTalkedCharacter || !_lastTalkedCharacter.ChatStarted) return;
            
            StopCoroutine(_responseWaitAnimationCoroutine);
            _responseTextAnimationCoroutine = CharacterResponseTextAnimation(response);
            StartCoroutine(_responseTextAnimationCoroutine);

            _lastTalkedCharacter.AddResponse(response);
            _lastTalkedCharacter.ChatHistory = fullText;
        }
    }
}
