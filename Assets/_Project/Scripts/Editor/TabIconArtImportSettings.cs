using UnityEditor;
using UnityEngine;

namespace TapLegends.EditorTools
{
    /// <summary>
    /// Import correcto para Art/TabIcons/ (iconos reales de las pestañas del panel inferior
    /// y de los botones de Misiones/Logros/Ajustes, provistos por el usuario, no generados):
    /// Sprite unico, PPU 32, sin mipmaps, filtro de punto, sin compresion agresiva.
    /// </summary>
    public class TabIconArtImportSettings : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Art/TabIcons/")) return;

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
