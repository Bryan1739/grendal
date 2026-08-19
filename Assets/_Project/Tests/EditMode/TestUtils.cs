using System;
using System.Reflection;
using UnityEngine;

namespace TapLegends.Tests
{
    /// <summary>
    /// Helper compartido para tests EditMode. Los managers del juego son singletons
    /// MonoBehaviour (`static Instance { get; private set; }`) — ese campo estatico
    /// SOBREVIVE entre [Test] dentro de la misma corrida (Unity no recarga el dominio
    /// entre tests EditMode), asi que si un test no limpia su Instance, el SIGUIENTE test
    /// (de la misma clase o de OTRA) puede leer una referencia a un GameObject ya
    /// destruido — exactamente el bug de "?. no respeta el fake-null" que ya mordio a este
    /// proyecto una vez (ver gotcha #10 en CLAUDE.md). Todo [TearDown] que toque un
    /// manager tiene que llamar ResetSingleton&lt;T&gt;() ademas de destruir el GameObject.
    /// </summary>
    public static class TestUtils
    {
        public static void ResetSingleton<T>() where T : Component
        {
            var prop = typeof(T).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            var setMethod = prop?.GetSetMethod(nonPublic: true);
            setMethod?.Invoke(null, new object[] { null });
        }

        /// <summary>
        /// Fuerza `Instance` al objeto dado, sin depender de que `Awake()` haya corrido.
        /// Confirmado con un assert de diagnostico (ver conversacion) que `AddComponent&lt;T&gt;()`
        /// NO garantiza que `Awake()` ya corrio de forma sincronica en este contexto de
        /// EditMode test — el primer run con pocas fixtures lo disimulaba (funcionaba
        /// "por las dudas"), pero con mas fixtures corriendo juntas dejo de ser confiable.
        /// Llamar esto justo despues de `AddComponent&lt;T&gt;()` en vez de confiar en Awake().
        /// </summary>
        public static void ForceSingleton<T>(T instance) where T : Component
        {
            var prop = typeof(T).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            var setMethod = prop?.GetSetMethod(nonPublic: true);
            setMethod?.Invoke(null, new object[] { instance });
        }

        // NO agregar un "ForceOnEnable" que llame OnEnable() por reflection — se probó y
        // rompió el Editor: Unity lleva un contador de version nativo alrededor de cada
        // callback de ciclo de vida (Awake/OnEnable/...) para evitar acceso concurrente;
        // invocar el metodo directo salta esa contabilidad, y cuando el despachador real de
        // Unity intenta entrar al mismo lock mas tarde lo encuentra en un estado invalido —
        // deriva en un loop infinito logueando "Access version should be odd when acquiring
        // lock" (visto en vivo: ~93 millones de repeticiones, 4.4 GB de log, Editor
        // inutilizable). Ver gotcha de testing #4 en CLAUDE.md.

        /// <summary>
        /// Crea un delegate para un metodo privado de `instance` y lo devuelve listo para
        /// pasarle a `EventBus.Subscribe&lt;TEvent&gt;(...)`. Es la forma SEGURA de simular lo que
        /// `OnEnable()` habria suscripto, sin invocar `OnEnable()` en si (ver comentario de
        /// arriba). `Delegate.CreateDelegate` es reflection estandar de .NET sobre un metodo
        /// C# comun — no toca ningun mecanismo interno de Unity, a diferencia de invocar un
        /// metodo de ciclo de vida como OnEnable/Awake.
        /// </summary>
        public static Action<TEvent> PrivateHandler<TOwner, TEvent>(TOwner instance, string methodName)
        {
            var method = typeof(TOwner).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            return (Action<TEvent>)Delegate.CreateDelegate(typeof(Action<TEvent>), instance, method);
        }
    }
}
