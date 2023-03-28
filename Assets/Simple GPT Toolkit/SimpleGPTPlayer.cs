using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Simple_GPT_Dialog
{
    public class SimpleGPTPlayer : MonoBehaviour
    {
        public string PlayerName;
        [TextArea(5, 20)] public string PlayerDescription;
    }
}
