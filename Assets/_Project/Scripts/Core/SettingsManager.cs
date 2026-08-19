using UnityEngine;

namespace TapLegends.Core
{
    /// <summary>
    /// Preferencias del dispositivo (no de la partida): vibracion, etc. Vive en
    /// PlayerPrefs a proposito, separado de SaveData — son ajustes de la instalacion,
    /// sobreviven a "Borrar partida" y no tienen que ver con oro/nivel/heroes.
    /// </summary>
    public static class SettingsManager
    {
        private const string VibrationKey = "settings_vibration_enabled";

        public static bool VibrationEnabled
        {
            get => PlayerPrefs.GetInt(VibrationKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(VibrationKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
