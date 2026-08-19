using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace TapLegends.EditorTools
{
    /// <summary>
    /// Configuracion de Player Settings para Android que es mas segura de aplicar
    /// via API que a mano en el YAML (evita corromper el diccionario serializado
    /// de applicationIdentifier). Ejecutar una vez desde el menu o via -executeMethod.
    /// </summary>
    public static class ProjectSetup
    {
        private const string DevApplicationId = "com.taplegendsstudio.taplegends.dev";
        private const string AppIconPath = "Assets/_Project/Art/Icons/AppIcon.png";

        [MenuItem("Tap Legends/Setup/Configure Android Player Settings")]
        public static void ConfigureAndroid()
        {
            var target = NamedBuildTarget.Android;

            PlayerSettings.SetApplicationIdentifier(target, DevApplicationId);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            // IL2CPP es obligatorio para arm64 (exigido por Google Play desde 2019).
            // La idea original del plan de usar Mono para iterar rapido no es viable en
            // dispositivos modernos (todos arm64) — se corrige aqui: IL2CPP + arm64 only
            // para desarrollo (compila mas rapido que arm64+armv7 combinados), y ambas
            // arquitecturas se añaden solo para la build de release (Etapa 12).
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            PlayerSettings.colorSpace = ColorSpace.Linear;

            Debug.Log($"[ProjectSetup] Android configurado: {DevApplicationId}, IL2CPP, ARM64, minSdk 26.");
        }

        /// <summary>
        /// Asigna Art/Icons/AppIcon.png como icono de Android. Usa la API "Legacy"
        /// (SetIconsForTargetGroup), no los slots de Adaptive Icon (foreground/background
        /// separados): el arte que tenemos es una sola imagen plana con las esquinas ya
        /// redondeadas, no capas separadas para que Android las recorte solo. Unity usa el
        /// Legacy icon como base para generar el ic_launcher igual, asi que alcanza para
        /// tener icono real en el telefono ya — descomponerlo en capas de Adaptive Icon
        /// queda para la Etapa 13 (arte final de tienda), si hace falta.
        /// </summary>
        [MenuItem("Tap Legends/Setup/Set App Icon (Android)")]
        public static void SetAppIcon()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (icon == null)
            {
                Debug.LogWarning($"[ProjectSetup] No se encontro {AppIconPath}.");
                return;
            }

            var target = BuildTargetGroup.Android;
            int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(target);
            var icons = new Texture2D[sizes.Length];
            for (int i = 0; i < icons.Length; i++) icons[i] = icon;
            PlayerSettings.SetIconsForTargetGroup(target, icons);

            Debug.Log($"[ProjectSetup] Icono de Android asignado desde {AppIconPath} ({sizes.Length} tamaños).");
        }
    }
}
