using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRLab.EditorTools
{
    /// <summary>
    /// CLI entry point: Unity.exe -batchmode -projectPath <proj>
    ///   -executeMethod VRLab.EditorTools.ConfigureURP.Apply -quit
    /// Removes the "Built-in Render Pipeline is deprecated" warning by creating
    /// a URP asset + renderer and assigning them to Graphics/Quality settings.
    /// Marker: "[ConfigureURP] DONE".
    /// </summary>
    public static class ConfigureURP
    {
        private const string AssetDir = "Assets/Settings";
        private const string PipelinePath = AssetDir + "/VRLabURP.asset";
        private const string RendererPath = AssetDir + "/VRLabURP_Renderer.asset";

        public static void Apply()
        {
            try
            {
                Directory.CreateDirectory(AssetDir);

                var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath)
                               ?? CreateAsset(ScriptableObject.CreateInstance<UniversalRendererData>(), RendererPath);

                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
                if (pipeline == null)
                {
                    pipeline = UniversalRenderPipelineAsset.Create(renderer);
                    CreateAsset(pipeline, PipelinePath);
                }
                else
                {
                    AssetDatabase.SaveAssetIfDirty(pipeline);
                }

                GraphicsSettings.defaultRenderPipeline = pipeline;

                // Assign to every quality level so the active pipeline is never null.
                int levels = QualitySettings.names.Length;
                int current = QualitySettings.GetQualityLevel();
                for (int i = 0; i < levels; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = pipeline;
                }
                QualitySettings.SetQualityLevel(current, false);

                AssetDatabase.SaveAssets();
                Debug.Log($"[ConfigureURP] DONE — active pipeline: {GraphicsSettings.defaultRenderPipeline}");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[ConfigureURP] FAILED: " + e);
                EditorApplication.Exit(5);
            }
        }

        private static T CreateAsset<T>(T asset, string path) where T : UnityEngine.Object
        {
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
