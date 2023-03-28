using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using TMPro;

namespace Simple_GPT_Dialog
{
    public class SimpleGPTCharacter : MonoBehaviour
    {
        public string CharacterName;
        [TextArea(5, 20)] public string CharacterDescription;
        public string CharacterLocation;
        public bool TurnTowardsPlayer;
        public float ChatDistance = 5f;
        public TMP_Text SpeechBox;
        public GameObject CharacterImage;
        public GameObject DialogBG;
        public List<TopicReactionPair> TopicReactionPairs = new List<TopicReactionPair>();

        [NonSerialized] public string ChatHistory;
        [NonSerialized] public List<SimpleGPT.Message> ChatHistoryList = new List<SimpleGPT.Message>();
        [NonSerialized] public bool ChatStarted = false;
        [NonSerialized] public bool DialogActive;
        [NonSerialized] public TopicReactionPair CurrentTopicReactionPair;

        private readonly List<string> _responses = new();

        [Serializable]
        public class TopicReactionPair
        {
            TopicReactionPair(string topic, string reaction, string id, string parentId = "")
            {
                Topic = topic;
                Reaction = reaction;
                Children = new List<TopicReactionPair>();
                IsRoot = false;
            }
            
            public string Topic;
            public string Reaction;
            public bool IsRoot;
            public List<TopicReactionPair> Children;
        }

        public string GetLastResponse()
        {
            return _responses[^1];
        }

        public List<string> GetResponses()
        {
            return _responses;
        }

        public void ClearResponses()
        {
            _responses.Clear();
        }

        public void AddResponse(string lastResponse)
        {
            _responses.Add(lastResponse);
        }

        public void SetDialogUIActive(bool enabled)
        {
            DialogActive = enabled;
            SpeechBox.text = "";
            if (DialogBG) DialogBG.SetActive(enabled);
            if (CharacterImage) CharacterImage.SetActive(enabled);
        }
    }
}
