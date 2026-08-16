using TapLegends.Core;
using UnityEditor;
using UnityEngine;

namespace TapLegends.EditorTools
{
    /// <summary>
    /// Borra el guardado sin tener que entrar a Play mode y usar el truco del menu de
    /// debug. Usa el mismo SaveSystem.DeleteSave() que ese truco — un solo lugar que
    /// sabe donde vive el archivo (ver TapLegends-Plan.md, Etapa 4).
    /// </summary>
    public static class SaveTools
    {
        [MenuItem("Tap Legends/Delete Save Data")]
        public static void DeleteSaveData()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Borrar guardado",
                "Esto borra el archivo de guardado de Tap Legends (oro, heroes, nivel, habilidades...). No se puede deshacer.",
                "Borrar",
                "Cancelar");

            if (!confirmed) return;

            SaveSystem.DeleteSave();
            Debug.Log("[SaveTools] Guardado borrado. La proxima vez que entres a Play, arranca una partida nueva.");
        }
    }
}
