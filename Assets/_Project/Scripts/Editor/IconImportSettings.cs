using UnityEditor;
using UnityEngine;

namespace TapLegends.EditorTools
{
    /// <summary>
    /// Fuerza el import correcto de todo lo que caiga bajo Art/Icons/: Texture2D plano
    /// (no Sprite — el proyecto es 2D y el default de importacion suele ser Sprite, que
    /// PlayerSettings.SetIconsForTargetGroup no acepta directamente), sin mipmaps (un
    /// icono no se ve nunca a distintas distancias) y sin compresion agresiva (se ve en
    /// el launcher del telefono a tamaño fijo, vale la pena que quede nitido).
    /// </summary>
    public class IconImportSettings : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Art/Icons/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 1024;
        }
    }
}
