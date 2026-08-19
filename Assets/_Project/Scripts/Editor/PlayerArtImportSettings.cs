using UnityEditor;
using UnityEngine;

namespace TapLegends.EditorTools
{
    /// <summary>
    /// Import correcto para todo lo que caiga bajo Art/Player/ (los cuadros de animacion
    /// del personaje, provistos por el usuario, no generados): Sprite unico, pivote
    /// abajo-centro (contrato de la Etapa 0 para "Heroe"), sin mipmaps, sin compresion
    /// agresiva, filtro de punto (pixel art). Point filter + Compression None se fuerzan
    /// tambien por codigo porque son configuracion de plataforma, no de
    /// TextureImporterSettings general.
    /// </summary>
    public class PlayerArtImportSettings : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Art/Player/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;

            // spriteAlignment/spritePivot no son propiedades directas de TextureImporter —
            // viven en TextureImporterSettings, hay que leerlas/escribirlas como bloque.
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            importer.SetTextureSettings(settings);
        }
    }
}
