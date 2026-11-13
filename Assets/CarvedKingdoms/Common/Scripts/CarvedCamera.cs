using UnityEngine;
#if ENABLE_INPUT_SYSTEM && CARVED_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace OddJelly.CarvedKingdoms
{
    /// <summary>
    /// An orbit camera for the demo scenes. Drag or use WASD to move, the mouse wheel zooms, and Q and E turn the view.
    /// It works with the Input System package and with the old Input Manager.
    /// </summary>
    public class CarvedCamera : MonoBehaviour
    {
        public Vector3 focus = Vector3.zero;
        public float metres = 14f;
        public float yaw = 20f;
        public float pitch = 48f;

        void Start() { Place(); }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM && CARVED_INPUT_SYSTEM
            var k = Keyboard.current;
            var mouse = Mouse.current;
            var move = Vector3.zero;
            if (k != null)
            {
                if (k.wKey.isPressed) move.z += 1f;
                if (k.sKey.isPressed) move.z -= 1f;
                if (k.aKey.isPressed) move.x -= 1f;
                if (k.dKey.isPressed) move.x += 1f;
                if (k.qKey.isPressed) yaw += 60f * Time.deltaTime;
                if (k.eKey.isPressed) yaw -= 60f * Time.deltaTime;
            }
            if (mouse != null)
            {
                float wheel = mouse.scroll.ReadValue().y;
                if (wheel != 0f) metres = Mathf.Clamp(metres * (wheel > 0f ? 0.9f : 1.1f), 2f, 120f);
                if (mouse.leftButton.isPressed)
                {
                    var d = mouse.delta.ReadValue();
                    move -= new Vector3(d.x, 0f, d.y) * 0.1f;
                }
            }
            focus += Quaternion.Euler(0f, yaw, 0f) * move * metres * 0.6f * Time.deltaTime;
#elif ENABLE_LEGACY_INPUT_MANAGER
            var move = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (Input.GetKey(KeyCode.Q)) yaw += 60f * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) yaw -= 60f * Time.deltaTime;
            float wheel = Input.mouseScrollDelta.y;
            if (wheel != 0f) metres = Mathf.Clamp(metres * (wheel > 0f ? 0.9f : 1.1f), 2f, 120f);
            if (Input.GetMouseButton(0))
                move -= new Vector3(Input.GetAxis("Mouse X"), 0f, Input.GetAxis("Mouse Y")) * 4f;
            focus += Quaternion.Euler(0f, yaw, 0f) * move * metres * 0.6f * Time.deltaTime;
#endif
            Place();
        }

        void Place()
        {
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = focus + rot * new Vector3(0f, 0f, -metres);
            transform.rotation = rot;
        }
    }
}
