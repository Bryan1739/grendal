using UnityEditor;
using UnityEngine;

namespace TapLegends.EditorTools
{
    /// <summary>
    /// Import correcto para Art/HeroBases/ (pedestales/bases decorativas bajo los heroes
    /// acompañantes, recortadas de una hoja de props de biomas provista por el usuario, no
    /// generadas): Sprite unico, PPU 32, sin mipmaps, filtro de punto, sin compresion
    /// agresiva. Pivote centro-abajo igual que Heroes/Pets — se posicionan por codigo, no
    /// dependen del pivote del sprite, pero mantiene el mismo criterio del resto del arte de
    /// personajes por consistencia.
    /// </summary>
    public class HeroBaseArtImportSettings : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Art/HeroBases/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            importer.SetTextureSettings(settings);
        }
    }
}
