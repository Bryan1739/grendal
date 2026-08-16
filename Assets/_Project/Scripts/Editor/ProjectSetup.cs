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
    }
}
