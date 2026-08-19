using UnityEditor;
using UnityEngine;

namespace TapLegends.EditorTools
{
    /// <summary>
    /// Import correcto para Art/Skills/ (iconos reales de habilidades activas, provistos
    /// por el usuario, no generados): Sprite unico, PPU 32, sin mipmaps, filtro de punto,
    /// sin compresion agresiva. Sin pivote especial — son iconos cuadrados de UI, no arte de
    /// personaje que necesite alinearse a un "piso".
    /// </summary>
    public class SkillArtImportSettings : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Art/Skills/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
        }
    }
}
