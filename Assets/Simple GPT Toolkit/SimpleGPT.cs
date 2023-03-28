using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace Simple_GPT_Dialog
{
    public class SimpleGPT : MonoBehaviour
    {
        #region Editor Variables

        public enum ModelOptions
        {
            ChatGPT,
            Davinci,
            Curie,
            Babbage,
            Ada
        }

        [Tooltip(
            "Model used to generate the text. Davinci is by far the best, but slowest. Ada is worst, but fastest.")]
        public ModelOptions Model;

        private string _modelName;
        public bool AutoLoadOnAwake = true;

        [Header("Response generation")]
        [Range(0.0f, 1.0f)]
        [Tooltip("Controls randomness: Lowering results in less random completions.")]
        public float Temperature = 0.5f;

        [Range(1, 2048)] [Tooltip("Maximum number of tokens to generate. One token is around 4 characters.")]
        public int MaximumLength = 100;

        [Range(0.0f, 2.0f)] [Tooltip("Decreases the model's likelihood to repeat the same line verbatim.")]
        public float FrequencyPenalty = 0f;

        [Range(0.0f, 2.0f)] [Tooltip("Increases the model's likelihood to talk about new topics.")]
        public float PresencePenalty = 0f;

        #endregion

        #region Private Variables

        private string _apiKey;
        private string[] _stopSequences;
        private string _playerName = "Human";
        private string _npcName = "AI";
        private SimpleGPTCharacter _currentCharacter;
        private string _currentResponseHistory;
        private List<Message> _currentChatHistory;
        private string _response;
        private bool _waitingForResponse = false;
        private float _failedRequestWaitTime = 1f;

        #endregion

        #region Events

        // You can subscribe to this event from any custom class you want to hook into GPT!
        public delegate void OnGPTChatResponse(string response, string fullText, string npcName);

        public static event OnGPTChatResponse onGPTChatResponse;

        #endregion
        
        private void OnValidate()
        {
            CheckForModelChange();
        }
        
        private void CheckForModelChange()
        {
            if (Model == ModelOptions.ChatGPT) _modelName = "gpt-3.5-turbo";
            else if (Model == ModelOptions.Davinci) _modelName = "text-davinci-003";
            else if (Model == ModelOptions.Curie) _modelName = "text-curie-001";
            else if (Model == ModelOptions.Babbage) _modelName = "text-babbage-001";
            else if (Model == ModelOptions.Ada) _modelName = "text-ada-001";
        }

        private void Awake()
        {
            _apiKey = PlayerPrefs.GetString("OPENAI_API_KEY");
            if (String.IsNullOrEmpty(_apiKey))
            {
                Debug.LogAssertion(
                    "API key not set! You can set the API key inside Unity, at Window -> OpenAI API Key. If you don't have an API key, you need to register to OpenAI. ");
            }

            if (AutoLoadOnAwake) TestGPTConnection();
        }
        
        private void TestGPTConnection()
        {
            if(Model == ModelOptions.ChatGPT) StartCoroutine(TestRequest());
            else StartCoroutine(LegacyTestRequest());
        }

        #region Chat State Methods

        public void StartNewChat(string playerName, string npcName, string description)
        {
            _playerName = playerName;
            _npcName = npcName;
            _currentResponseHistory = "The following is a conversation between " + _playerName + " and " + _npcName +
                                      "." + "\n \n " + description +
                                      $"\n \n{_npcName} is aware of the following locations:";
            _currentChatHistory = new List<Message>();
            _currentChatHistory.Add(new Message("system", "The following is a conversation between user called " + _playerName + " and a character called " + _npcName +
                                                          ".\n \n" + description + ". In between, there are parts denoted with <REACTION>: keyword that describe the characters next reply and reaction. Only answers as the " + _npcName + " character. Never breaks character."));
        }

        public void StartNewChat(string playerName, SimpleGPTCharacter character, string description)
        {
            if (_waitingForResponse) return;

            _playerName = playerName;
            _npcName = character.CharacterName;
            
            _currentResponseHistory = description + "The following is a conversation between " + _playerName + " and " +
                                      _npcName + ". ";
            character.ChatHistory = _currentResponseHistory;
            _currentCharacter = character;
            _currentChatHistory = character.ChatHistoryList;
            _currentChatHistory.Add(new Message("system", "The following is a conversation between user called " + _playerName + " and a character called " + _npcName +
                                                          ".\n \n" + description));
            character.ChatHistoryList = _currentChatHistory;
        }

        public void ReturnToChat(string playerName, SimpleGPTCharacter character)
        {
            if (_waitingForResponse) return;

            _playerName = playerName;
            _npcName = character.CharacterName;
            _currentCharacter = character;
            _currentResponseHistory = character.ChatHistory;
            _currentChatHistory = character.ChatHistoryList;
            _stopSequences = new string[] { playerName + ":", character.CharacterName + ":" };
        }

        public void RequestChatResponse(string prompt)
        {
            if (_waitingForResponse) return;

            _currentResponseHistory += "\n \n" + _playerName + ": " + prompt + "\n \n" + _npcName + ": ";
            _currentChatHistory.Add(new Message("user", _playerName + ": " + prompt));
            if (Model == ModelOptions.ChatGPT)
            {
                StartCoroutine(CheckForStateChange2(_currentChatHistory, _currentCharacter));
            }
            else StartCoroutine(LegacyChatRequest(_currentResponseHistory));
        }
        
        public void EndCurrentChat()
        {
            _currentResponseHistory = "";
            _currentChatHistory = new List<Message>();
            _playerName = "";
            _npcName = "";
            _stopSequences = new string[2];
        }

        #endregion

        #region Web Requests

        public struct Message
        {
            public Message(string newRole, string newContent)
            {
                role = newRole;
                content = newContent;
            }
            
            public string role;
            public string content;
        }
        
        private string BuildRequestBody(List<Message> chatHistory)
        {
            var requestBody = new
            {
                model = _modelName,
                messages = chatHistory.ToArray(),
                temperature = Temperature,
                max_tokens = MaximumLength,
                frequency_penalty = FrequencyPenalty,
                presence_penalty = PresencePenalty
            };
            Debug.Log(JsonConvert.SerializeObject(requestBody));
            return JsonConvert.SerializeObject(requestBody);
        }

        private string BuildLegacyRequestBody(string p)
        {
            var requestBody = new
            {
                model = _modelName,
                prompt = p,
                temperature = Temperature,
                max_tokens = MaximumLength,
                frequency_penalty = FrequencyPenalty,
                presence_penalty = PresencePenalty,
                stop = _stopSequences
            };
            
            return JsonConvert.SerializeObject(requestBody);
        }
        

        // Remove trailing sentences and white spaces.
        private string FormatResponse(string response)
        {
            if (response.Contains(_npcName + ":")) response = response.Replace(_npcName + ":", "");
            
            response = response.Trim();
            if (response.Contains("\"")) response = response.Replace("\"", "");
            char[] trimCharacters = { '.', '!', '?' };
            int lastTrimIndex = response.LastIndexOfAny(trimCharacters);

            return lastTrimIndex > 1 ? response.Substring(0, lastTrimIndex + 1) : response;
        }

        private string FormatPrompt(string prompt)
        {
            if (prompt.Contains(_playerName + ":")) prompt = prompt.Replace(_playerName + ":", "");
            prompt = prompt.Trim();
            return prompt;
        }

        IEnumerator ChatRequest(List<Message> chatHistory)
        {
            Debug.Log("Chat request started!");
            byte[] body = Encoding.UTF8.GetBytes(BuildRequestBody(chatHistory));

            UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST");

            _waitingForResponse = true;

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + _apiKey);

            yield return request.SendWebRequest();
            
            _waitingForResponse = false;

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                request.Dispose();
                yield return new WaitForSeconds(0.5f);
                StartCoroutine(ChatRequest(chatHistory));
            }
            else
            {
                JObject responseObject = JObject.Parse(request.downloadHandler.text);
                _response = responseObject["choices"][0]["message"]["content"].ToString();
                _response = FormatResponse(_response);
                _currentChatHistory.Add(new Message(responseObject["choices"][0]["message"]["role"].ToString(), responseObject["choices"][0]["message"]["content"].ToString()));
                request.Dispose();

                if (onGPTChatResponse != null)
                {
                    onGPTChatResponse(_response, _currentResponseHistory, _npcName);
                }
                
                //yield return new WaitForSeconds(0.5f);
                //StartCoroutine(CheckForStateChange(chatHistory));
            }
        }

        IEnumerator CheckForStateChange(List<Message> chatHistory)
        {
            List<Message> checkableChatHistory = new List<Message>();
            checkableChatHistory.AddRange(chatHistory);
            checkableChatHistory[0] = new Message("system",
                "This is an assistant that analyzes words and sentences.");
            checkableChatHistory[^1] = new Message("user", checkableChatHistory[^1].content + "\n \n Analyze above chat for a state change. Possible state changes: 1. Dapper attacks, 2. Dapper does not attack, 3. Dapper is angry \n \n  Pick one from the numbered list. Answer format: STATE: <picked state>");

            byte[] body = Encoding.UTF8.GetBytes(BuildRequestBody(checkableChatHistory));

            UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST");

            _waitingForResponse = true;

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + _apiKey);

            yield return request.SendWebRequest();
            
            _waitingForResponse = false;

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                request.Dispose();
                yield return new WaitForSeconds(0.5f);
                StartCoroutine(CheckForStateChange(chatHistory));
            }
            else
            {
                JObject responseObject = JObject.Parse(request.downloadHandler.text);
                _response = responseObject["choices"][0]["message"]["content"].ToString();
                _response = FormatResponse(_response);
                Debug.Log(responseObject["choices"][0]["message"]["content"].ToString());
                request.Dispose();
            }
        }
        
        IEnumerator CheckForStateChange2(List<Message> chatHistory, SimpleGPTCharacter character)
        {
            List<Message> checkableChatHistory = new List<Message>();
            checkableChatHistory.AddRange(chatHistory);
            checkableChatHistory[0] = new Message("system",
                "This is an assistant that analyzes chat messages by picking topics from a provided list.");

            string formattedPrompt = FormatPrompt(chatHistory[^1].content);
            string prompt = checkableChatHistory[^1].content + "\n \nAnalyze the last reply from the user for its topic.\n";
            List<SimpleGPTCharacter.TopicReactionPair> topics = new List<SimpleGPTCharacter.TopicReactionPair>();
            int listIndex = 1;
            if (character.CurrentTopicReactionPair == null)
            {
                for (int i = 0; i < character.TopicReactionPairs.Count; i++)
                {
                    if (!character.TopicReactionPairs[i].IsRoot) continue;
                        
                    prompt += listIndex + ". " + character.TopicReactionPairs[i].Topic + "\n";
                    listIndex++;
                    topics.Add(character.TopicReactionPairs[i]);
                }
            }
            else
            {
                Debug.Log("character.CurrentTopicReactionPair !== null");
                for (int i = 0; i < character.CurrentTopicReactionPair.Children.Count; i++)
                {
                    prompt += listIndex + ". " + character.CurrentTopicReactionPair.Children[i].Topic + "\n";
                    listIndex++;
                    topics.Add(character.CurrentTopicReactionPair.Children[i]);
                }
            }

            prompt += "\nPick a topic from the above numbered list. Pick only from the list above, not other source. Output this choice exactly as it is written in the list.";
            checkableChatHistory[^1] = new Message("user", prompt);
            
            var requestBody = new
            {
                model = _modelName,
                messages = checkableChatHistory.ToArray(),
                temperature = 0
            };
            var jsonBody = JsonConvert.SerializeObject(requestBody);
            byte[] body = Encoding.UTF8.GetBytes(jsonBody);

            UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST");

            _waitingForResponse = true;

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + _apiKey);

            yield return request.SendWebRequest();
            
            _waitingForResponse = false;

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                request.Dispose();
                yield return new WaitForSeconds(0.5f);
                StartCoroutine(CheckForStateChange2(chatHistory, _currentCharacter));
            }
            else
            {
                JObject responseObject = JObject.Parse(request.downloadHandler.text);
                var response = responseObject["choices"][0]["message"]["content"].ToString();
                response = FormatResponse(response);
                request.Dispose();

                foreach (SimpleGPTCharacter.TopicReactionPair pair in topics)
                {
                    if (response.Contains(pair.Topic))
                    {
                        _currentChatHistory[^1] = new Message("user", _currentChatHistory[^1].content + "\n \n <REACTION>: " + pair.Reaction + "\n \n Output this reaction as a text reply: ");
                        character.CurrentTopicReactionPair = pair;
                        break;
                    }
                }
                StartCoroutine(ChatRequest(_currentChatHistory));
            }
        }
        
        IEnumerator LegacyChatRequest(string p)
        {
            byte[] body = Encoding.UTF8.GetBytes(BuildLegacyRequestBody(p));

            UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/completion", "POST");

            _waitingForResponse = true;

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + _apiKey);

            yield return request.SendWebRequest();

            _waitingForResponse = false;

            if (request.isNetworkError || request.isHttpError)
            {
                request.Dispose();
                yield return new WaitForSeconds(0.5f);
                StartCoroutine(LegacyChatRequest(p));
            }
            else
            {
                JObject responseObject = JObject.Parse(request.downloadHandler.text);
                _response = responseObject["choices"][0]["text"].ToString();
                _response = FormatResponse(_response);
                _currentResponseHistory += _response;
                request.Dispose();

                if (onGPTChatResponse != null)
                {
                    onGPTChatResponse(_response, _currentResponseHistory, _npcName);
                }
            }
        }
        
        IEnumerator TestRequest()
        {
            List<Message> messagesList = new List<Message>();
            messagesList.Add(new Message("user", "test"));
            var requestBody = new
            {
                model = _modelName,
                messages = messagesList.ToArray(),
                temperature = 0,
                max_tokens = 4,
                frequency_penalty = 0,
                presence_penalty = 0
            };

            string r = JsonConvert.SerializeObject(requestBody);
            Debug.Log(r);
            byte[] body = Encoding.UTF8.GetBytes(r);

            var request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST");

            _waitingForResponse = true;

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + _apiKey);

            yield return request.SendWebRequest();

            _waitingForResponse = false;

            // Check for errors
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogAssertion($"Connecting to GPT failed: {request.error}");
                request.Dispose();
                _failedRequestWaitTime += 1f;
                yield return new WaitForSeconds(_failedRequestWaitTime);
                StartCoroutine(TestRequest());
            }
            else
            {
                JObject responseObject = JObject.Parse(request.downloadHandler.text);
                _response = responseObject.ToString();
                Debug.Log("Connecting to GPT succeeded!");
                request.Dispose();
            }
        }

        IEnumerator LegacyTestRequest()
        {
            var requestBody = new
            {
                model = _modelName,
                prompt = "a",
                temperature = 0,
                max_tokens = 1,
                frequency_penalty = 0,
                presence_penalty = 0
            };

            string r = JsonConvert.SerializeObject(requestBody);
            byte[] body = Encoding.UTF8.GetBytes(r);

            var request = new UnityWebRequest("https://api.openai.com/v1/completions", "POST");

            _waitingForResponse = true;

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + _apiKey);

            yield return request.SendWebRequest();

            _waitingForResponse = false;

            // Check for errors
            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogAssertion($"Connecting to GPT failed: {request.error}");
                request.Dispose();
                _failedRequestWaitTime += 1f;
                yield return new WaitForSeconds(_failedRequestWaitTime);
                StartCoroutine(LegacyTestRequest());
            }
            else
            {
                Debug.Log("Connecting to GPT succeeded");
                request.Dispose();
            }
        }

        #endregion
    }
}
