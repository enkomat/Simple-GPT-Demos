using System;
using UnityEngine;
using UnityEditor;

namespace Simple_GPT_Dialog
{
    public class OpenAIApiKey : EditorWindow
    {
        [MenuItem("Window/OpenAI API Key")]
        static void Init()
        {
            OpenAIApiKey window = (OpenAIApiKey)EditorWindow.GetWindow(typeof(OpenAIApiKey));
            window.maxSize = new Vector2(600f, 115f);
            window.minSize = window.maxSize;
            window.Show();
        }

        void OnGUI()
        {
            string key = PlayerPrefs.GetString("KEY_TEXT_FIELD");
            if(key == null) PlayerPrefs.SetString("KEY_TEXT_FIELD", "");
            
            string textField = EditorGUILayout.TextField("OpenAI API Key", key);
            PlayerPrefs.SetString("KEY_TEXT_FIELD", textField);
            
            if (GUILayout.Button("Save"))
            {
                PlayerPrefs.SetString("OPENAI_API_KEY", textField);
                Debug.Log("API key saved!");
                string keyStart = $"{textField[0]}{textField[1]}{textField[2]}";
                string keyEnd = $"{textField[^4]}{textField[^3]}{textField[^2]}{textField[^1]}";
                PlayerPrefs.SetString("KEY_TEXT_FIELD", $"{keyStart}...{keyEnd}");
            }
            if(GUILayout.Button("Clear from PlayerPrefs"))
            {
                Debug.Log("API key cleared from PlayerPrefs!");
                PlayerPrefs.SetString("KEY_TEXT_FIELD", "");
                PlayerPrefs.DeleteKey("OPENAI_API_KEY");
            }
            
            GUILayout.Label(
                "\nNote: API key is stored in PlayerPrefs. This will only work for personal purposes. If you are looking to release your project, a different solution is needed. More information can be found on the OpenAI website.", EditorStyles.wordWrappedLabel);
            
            Repaint();
        }
    }
}
