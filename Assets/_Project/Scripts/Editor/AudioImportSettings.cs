using UnityEditor;
using UnityEngine;

namespace TapLegends.EditorTools
{
    /// <summary>
    /// Fuerza el Load Type correcto segun la carpeta (ver TapLegends-Plan.md: "musica en
    /// Streaming, SFX en Decompress on Load") — Streaming para pistas largas bajo
    /// Audio/Music/, Decompress On Load para sonidos cortos bajo Audio/SFX/. Corre solo al
    /// importar, asi que cualquier clip nuevo que se agregue a esas carpetas queda bien
    /// configurado sin tocar nada a mano en el Inspector.
    /// </summary>
    public class AudioImportSettings : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            AudioClipLoadType? loadType = null;
            if (assetPath.Contains("/Audio/Music/")) loadType = AudioClipLoadType.Streaming;
            else if (assetPath.Contains("/Audio/SFX/")) loadType = AudioClipLoadType.DecompressOnLoad;

            if (loadType == null) return;

            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            settings.loadType = loadType.Value;
            importer.defaultSampleSettings = settings;
        }
    }
}
