using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OddJelly.CarvedKingdoms.EditorTools
{
    /// <summary>
    /// Opens once, the first time a Carved Kingdoms package is in a project. It lists each pack's scenes to open,
    /// shows how the pieces go together, and offers the URP look. Open it again from Tools > Carved Kingdoms > Welcome.
    /// </summary>
    public class CarvedWelcome : EditorWindow
    {
        const string Root = "Assets/CarvedKingdoms";
        Vector2 _scroll;

        [MenuItem("Tools/Carved Kingdoms/Welcome", priority = 0)]
        public static void Open()
        {
            var w = GetWindow<CarvedWelcome>(true, "Carved Kingdoms", true);
            w.minSize = new Vector2(480f, 460f);
            w.Show();
        }

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            var title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 };
            var wrap = new GUIStyle(EditorStyles.label) { wordWrap = true };
            GUILayout.Space(8);
            GUILayout.Label("Carved Kingdoms", title);
            GUILayout.Label("Hex tiles, buildings and figures carved from wood. Every pack shares one grid and one scale, so they fit together.", wrap);

            GUILayout.Space(12);
            GUILayout.Label("Your packs", EditorStyles.boldLabel);
            var packs = Directory.Exists(Root)
                ? Directory.GetDirectories(Root).Select(Path.GetFileName).Where(p => p != "Common" && Directory.Exists($"{Root}/{p}/Scenes")).OrderBy(p => p).ToList()
                : new System.Collections.Generic.List<string>();
            if (packs.Count == 0) GUILayout.Label("No pack found under " + Root + ".", wrap);
            foreach (var pack in packs)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(pack, GUILayout.Width(110));
                foreach (var kind in new[] { "Demo", "Overview", "Board" })
                {
                    var path = $"{Root}/{pack}/Scenes/{pack}_{kind}.unity";
                    if (!File.Exists(path)) continue;
                    if (GUILayout.Button(kind, GUILayout.Width(90))) OpenScene(path);
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Label("Demo is an island to play with, and Randomize lays another. Overview shows every piece and every clip with its name. Board is open ground to lay your own.", EditorStyles.miniLabel);

            GUILayout.Space(12);
            GUILayout.Label("Putting pieces together", EditorStyles.boldLabel);
            GUILayout.Label("1. Drag a tile prefab (Prefabs/Tiles) under a Board object.\n" +
                "2. Tools > Carved Kingdoms > Snap selection to the hex grid (Alt+Shift+G) sets it on a cell.\n" +
                "3. Alt+Shift+R turns it 60 degrees, so rivers, roads and coasts meet their neighbours.\n" +
                "4. A piece's Carved Team component colours it red, blue, green or yellow.\n" +
                "5. Figures play their clips with CarvedUnit.Play(\"Attack\"). The Overview names every clip.", wrap);

            GUILayout.Space(12);
            GUILayout.Label("The look", EditorStyles.boldLabel);
            bool urp = GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset;
            if (!urp) GUILayout.Label("This project does not use URP. The pieces still show, but the edge lines need URP.", wrap);
            else if (CarvedSetup.HasLook()) GUILayout.Label("Edge lines and ambient occlusion are on your URP renderer.", wrap);
            else if (GUILayout.Button("Add edge lines and ambient occlusion to my URP renderer", GUILayout.Height(26))) CarvedSetup.AddToRenderer();

            GUILayout.FlexibleSpace();
            GUILayout.Space(12);
            GUILayout.Label("Odd Jelly Games · assets@oddjellygames.com · Open this window again from Tools > Carved Kingdoms > Welcome", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndScrollView();
        }

        static void OpenScene(string path)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path);
        }
    }
}
