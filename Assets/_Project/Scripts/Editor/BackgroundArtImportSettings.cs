using UnityEditor;
using UnityEngine;

namespace TapLegends.EditorTools
{
    /// <summary>
    /// Import correcto para Art/Backgrounds/ (los 20 fondos reales, provistos por el
    /// usuario, no generados): Sprite unico, sin compresion agresiva, filtro de punto
    /// (mismo criterio pixel-art que Art/Player/). Pivote centro (default): el fondo se
    /// ESTIRA para llenar la pantalla completa (BackgroundController, preserveAspect=false,
    /// anclas 0-1), asi que el pivote no afecta como se ve — a diferencia de Art/Player/,
    /// que si necesita pivote abajo-centro porque el personaje NO se estira.
    /// </summary>
    public class BackgroundArtImportSettings : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Art/Backgrounds/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
        }
    }
}
