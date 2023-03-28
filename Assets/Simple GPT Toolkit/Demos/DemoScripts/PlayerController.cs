using UnityEngine;

namespace Simple_GPT_Dialog.Demos
{
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 5.0f;
        public float mouseSensitivity = 2.0f;
        public float upDownRange = 80.0f;

        private Vector2 mouseLook;
        private Vector2 smoothV;
        public float smoothing = 2.0f;

        public GameObject character;
        public bool PlayerCanMove;

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            // Mouse input
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            smoothV.x = Mathf.Lerp(smoothV.x, mouseX, 1f / smoothing);
            smoothV.y = Mathf.Lerp(smoothV.y, mouseY, 1f / smoothing);
            mouseLook += smoothV;

            mouseLook.y = Mathf.Clamp(mouseLook.y, -upDownRange, upDownRange);

            // Apply rotation to camera and character
            transform.localRotation = Quaternion.AngleAxis(-mouseLook.y, Vector3.right);
            character.transform.rotation = Quaternion.AngleAxis(mouseLook.x, character.transform.up);
            
            if (PlayerCanMove)
            {
                // Character movement
                float forwardMove = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
                float sideMove = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
                character.transform.Translate(sideMove, 0, forwardMove);
            }

            // Toggle cursor lock
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
    }
}
