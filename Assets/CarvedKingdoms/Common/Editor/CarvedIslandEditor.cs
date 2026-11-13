using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OddJelly.CarvedKingdoms.EditorTools
{
    /// <summary>
    /// Adds the island's buttons. Randomize lays another island of the pack's tiles. Lay rebuilds the island from its seed,
    /// and seed 0 is the pack's own. Save the scene to keep an island.
    /// </summary>
    [CustomEditor(typeof(CarvedIsland))]
    public class CarvedIslandEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var island = (CarvedIsland)target;
            EditorGUILayout.HelpBox("Randomize lays another island of this pack's tiles. Seed 0 is the pack's own island. Set the seed to 0 and press Lay to get it back. Save the scene to keep an island.", MessageType.None);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Randomize", GUILayout.Height(28)))
            {
                Undo.RecordObject(island, "Randomize the island");
                island.Randomize();
                Changed(island);
            }
            if (GUILayout.Button("Lay", GUILayout.Height(28)))
            {
                island.Lay();
                Changed(island);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            DrawDefaultInspector();
        }

        static void Changed(CarvedIsland island)
        {
            EditorUtility.SetDirty(island);
            if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(island.gameObject.scene);
        }
    }
}
