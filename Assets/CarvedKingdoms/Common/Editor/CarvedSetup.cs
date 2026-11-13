using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OddJelly.CarvedKingdoms.EditorTools
{
    /// <summary>
    /// Adds the Carved Kingdoms edge lines and ambient occlusion to the project's URP renderer, so the pieces look the
    /// way they do in the demo scenes: from the welcome window, or Tools > Carved Kingdoms > Add the look to my URP
    /// renderer. Running it again changes nothing.
    /// </summary>
    [InitializeOnLoad]
    public static class CarvedSetup
    {
        static CarvedSetup()
        {
            EditorApplication.delayCall += WelcomeOnce;
        }

        static void WelcomeOnce()
        {
            if (Application.isBatchMode) return;
            var key = "CarvedKingdoms.Asked." + Application.dataPath;
            if (EditorPrefs.GetBool(key)) return;
            EditorPrefs.SetBool(key, true);
            CarvedWelcome.Open();
        }

        /// <summary>Tells whether the project's URP renderer already has the edge lines.</summary>
        public static bool HasLook()
        {
            var renderer = Renderer();
            return renderer != null && renderer.rendererFeatures.Any(f => f != null && f.name == EdgesName);
        }

        static ScriptableRendererData Renderer()
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null) return null;
            var so = new SerializedObject(pipeline);
            int index = so.FindProperty("m_DefaultRendererIndex").intValue;
            return so.FindProperty("m_RendererDataList").GetArrayElementAtIndex(index).objectReferenceValue as ScriptableRendererData;
        }

        const string EdgesName = "Carved Kingdoms edges";
        const string EdgesMaterial = "Assets/CarvedKingdoms/Common/Materials/CarvedKingdoms_Edges.mat";

        [MenuItem("Tools/Carved Kingdoms/Add the look to my URP renderer")]
        public static void AddToRenderer()
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null)
            {
                Debug.LogWarning("Carved Kingdoms: this project does not use URP, and the edge lines need it.");
                return;
            }
            var pipelineObject = new SerializedObject(pipeline);
            var list = pipelineObject.FindProperty("m_RendererDataList");
            int index = pipelineObject.FindProperty("m_DefaultRendererIndex").intValue;
            var renderer = list.GetArrayElementAtIndex(index).objectReferenceValue as ScriptableRendererData;
            if (renderer == null)
            {
                Debug.LogWarning("Carved Kingdoms: no renderer found in the URP asset.");
                return;
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(EdgesMaterial);
            if (material == null)
            {
                Debug.LogWarning("Carved Kingdoms: " + EdgesMaterial + " is missing.");
                return;
            }
            if (!renderer.rendererFeatures.Any(f => f != null && f.name == EdgesName))
            {
                var edges = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                edges.name = EdgesName;
                edges.passMaterial = material;
                edges.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
                edges.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
                Add(renderer, edges);
            }
            // Ambient occlusion where pieces meet the ground. This adds URP's own, if the renderer has none yet.
            var ssaoType = Type.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion, Unity.RenderPipelines.Universal.Runtime");
            if (ssaoType != null && !renderer.rendererFeatures.Any(f => f != null && f.GetType() == ssaoType))
            {
                var ssao = (ScriptableRendererFeature)ScriptableObject.CreateInstance(ssaoType);
                ssao.name = "Screen Space Ambient Occlusion";
                // URP's defaults are made for life-size scenes, but the pieces range from centimetres to a metre. So
                // the radius is wider, the touch is lighter, and there is none on the sunlit side.
                var so = new SerializedObject(ssao);
                foreach (var (field, value) in new[] { ("Intensity", 1.2f), ("Radius", 0.12f), ("DirectLightingStrength", 0.1f) })
                {
                    var prop = so.FindProperty("m_Settings." + field);
                    if (prop != null) prop.floatValue = value;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                Add(renderer, ssao);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Carved Kingdoms: edge lines and ambient occlusion are on " + renderer.name);
        }

        static void Add(ScriptableRendererData renderer, ScriptableRendererFeature feature)
        {
            AssetDatabase.AddObjectToAsset(feature, renderer);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long id);
            var so = new SerializedObject(renderer);
            var features = so.FindProperty("m_RendererFeatures");
            var map = so.FindProperty("m_RendererFeatureMap");
            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(renderer);
        }
    }
}
