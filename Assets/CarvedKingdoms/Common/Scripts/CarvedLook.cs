using UnityEngine;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM && CARVED_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace OddJelly.CarvedKingdoms
{
    /// <summary>
    /// Picks one of the three looks for the whole scene. Wooden is the default. Outlined adds an outline round every
    /// piece. Plain shows only the palette's colours and patterns, with no grain, no lines, no table and no vignette,
    /// for games with their own shading and for phones. In the demo scenes, O switches the outline and P the plain
    /// look while playing.
    /// It sets every Carved Kingdoms material at once, so you don't have to touch them. To have plain without this
    /// component, tick Plain on the materials and switch off the edge lines in your URP renderer.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Carved Kingdoms/Carved Look")]
    public class CarvedLook : MonoBehaviour
    {
        [Tooltip("An outline round every piece")]
        public bool outlined;
        [Tooltip("Only the palette's colours and patterns: no grain, no lines, no table")]
        public bool plain;
        [Tooltip("O and P switch the outline and the plain look while playing")]
        public bool keys = true;
        [Tooltip("The sun. Its colour and angle follow the look.")]
        public Light sun;
        [Tooltip("The walnut table under the board. It is hidden in the plain look.")]
        public GameObject table;
        [Tooltip("The warm grade and vignette. They are off in the plain look.")]
        public Volume grade;
        [Tooltip("The camera whose background follows the look. Empty uses the main camera.")]
        public Camera view;

        static readonly int Plain = Shader.PropertyToID("_CarvedPlain");
        static readonly int Outline = Shader.PropertyToID("_CarvedOutline");

        void OnEnable() { Apply(); }
        void OnValidate() { Apply(); }

        void OnDisable()
        {
            // the settings are global, so leave nothing switched on behind us
            Shader.SetGlobalFloat(Plain, 0f);
            Shader.SetGlobalFloat(Outline, 0f);
        }

        void Update()
        {
            if (!Application.isPlaying || !keys) return;
            bool o = false, p = false;
#if ENABLE_INPUT_SYSTEM && CARVED_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                o = k.oKey.wasPressedThisFrame;
                p = k.pKey.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            o = Input.GetKeyDown(KeyCode.O);
            p = Input.GetKeyDown(KeyCode.P);
#endif
            if (o) outlined = !outlined;
            if (p) plain = !plain;
            if (o || p) Apply();
        }

        /// <summary>Puts the chosen look on the scene. Call it after changing outlined or plain from a script.</summary>
        public void Apply()
        {
            Shader.SetGlobalFloat(Plain, plain ? 1f : 0f);
            Shader.SetGlobalFloat(Outline, outlined ? 1f : 0f);
            if (sun != null)
            {
                sun.color = plain ? new Color32(0xff, 0xfa, 0xf0, 0xff) : new Color32(0xff, 0xe2, 0xbd, 0xff);
                sun.intensity = plain ? 1.05f : 1.2f;
                sun.transform.rotation = plain ? Quaternion.Euler(55f, 35f, 0f) : Quaternion.Euler(46f, 40f, 0f);
            }
            if (table != null) table.SetActive(!plain);
            if (grade != null) grade.weight = plain ? 0f : 1f;
            var cam = view != null ? view : Camera.main;
            if (cam != null) cam.backgroundColor = plain ? new Color32(0xcf, 0xd6, 0xdc, 0xff) : new Color32(0x2a, 0x1d, 0x16, 0xff);
            RenderSettings.ambientLight = plain ? new Color32(0xa7, 0xae, 0xb8, 0xff) : new Color32(0x7c, 0x74, 0x86, 0xff);
            var ambient = new SphericalHarmonicsL2();
            ambient.AddAmbientLight(RenderSettings.ambientLight.linear);
            RenderSettings.ambientProbe = ambient;
        }
    }
}
