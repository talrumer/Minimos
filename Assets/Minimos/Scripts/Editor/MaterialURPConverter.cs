using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Minimos.Editor
{
    /// <summary>
    /// Converts Built-in Render Pipeline materials to URP (Universal Render Pipeline).
    /// Fixes pink/magenta materials from asset packs designed for Built-in.
    ///
    /// Access via: Minimos → Utils → 🔄 Convert All Materials to URP
    /// </summary>
    public static class MaterialURPConverter
    {
        private static readonly string[] AssetPackPaths =
        {
            "Assets/LowPolyLivingRoomPack",
            "Assets/SimpleNaturePack",
            "Assets/Aquaset",
        };

        [MenuItem("Minimos/Utils/🔄 Convert All Materials to URP", false, 500)]
        public static void ConvertAllToURP()
        {
            var urpLit = Shader.Find("Universal Render Pipeline/Lit");
            var urpSimpleLit = Shader.Find("Universal Render Pipeline/Simple Lit");

            if (urpLit == null)
            {
                Debug.LogError("🚨 [Material Converter] URP Lit shader not found! Is URP installed?");
                return;
            }

            // Prefer Simple Lit for low-poly assets (better performance, fits cartoon style)
            Shader targetShader = urpSimpleLit != null ? urpSimpleLit : urpLit;

            int converted = 0;
            int skipped = 0;

            foreach (var folder in AssetPackPaths)
            {
                var guids = AssetDatabase.FindAssets("t:Material", new[] { folder });

                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    // Skip materials already in URP subfolder
                    if (path.Contains("/URP/")) { skipped++; continue; }

                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null) continue;

                    // Check if it's using a Built-in shader (Standard, Diffuse, etc.)
                    string shaderName = mat.shader.name;
                    bool needsConversion = shaderName == "Standard" ||
                                           shaderName == "Mobile/Diffuse" ||
                                           shaderName == "Legacy Shaders/Diffuse" ||
                                           shaderName == "Hidden/InternalErrorShader" ||
                                           shaderName.StartsWith("Standard");

                    if (!needsConversion) { skipped++; continue; }

                    // Save the main color/texture before switching shader
                    Color baseColor = Color.white;
                    Texture mainTex = null;

                    if (mat.HasProperty("_Color"))
                        baseColor = mat.GetColor("_Color");
                    if (mat.HasProperty("_MainTex"))
                        mainTex = mat.GetTexture("_MainTex");

                    // Switch to URP shader
                    mat.shader = targetShader;

                    // Re-apply color and texture with URP property names
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", baseColor);
                    if (mat.HasProperty("_BaseMap") && mainTex != null)
                        mat.SetTexture("_BaseMap", mainTex);

                    // Set to opaque
                    mat.SetFloat("_Surface", 0); // 0 = Opaque
                    mat.renderQueue = (int)RenderQueue.Geometry;

                    EditorUtility.SetDirty(mat);
                    converted++;

                    Debug.Log($"✅ Converted: {path} ({shaderName} → {targetShader.name})");
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"✅ [Material Converter] Done! Converted: {converted}, Skipped: {skipped}");

            if (converted > 0)
            {
                EditorUtility.DisplayDialog("Material Conversion Complete",
                    $"Converted {converted} material(s) to URP.\nSkipped {skipped} (already URP or in URP subfolder).\n\nPink materials should now render correctly!",
                    "Nice!");
            }
        }
    }
}
