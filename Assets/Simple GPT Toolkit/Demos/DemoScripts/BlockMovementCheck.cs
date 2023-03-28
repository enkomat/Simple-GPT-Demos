using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Simple_GPT_Dialog.Demos
{
    public class BlockMovementCheck : MonoBehaviour
    {
        public SimpleGPTAutoDialog GPT;
        public PlayerController FPVController;

        void Update()
        {
            if (GPT.MovementBlocked())
            {
                FPVController.PlayerCanMove = false;
            }
            else
            {
                FPVController.PlayerCanMove = true;
            }
        }
    }
}
