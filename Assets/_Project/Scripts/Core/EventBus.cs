using System;
using System.Collections.Generic;

namespace TapLegends.Core
{
    /// <summary>
    /// Bus de eventos minimo desacoplado. Los sistemas (misiones, logros, UI)
    /// escuchan eventos de juego sin conocerse entre si.
    /// Uso: EventBus.Subscribe&lt;EnemyKilledEvent&gt;(OnEnemyKilled);
    ///      EventBus.Publish(new EnemyKilledEvent { EnemyId = "goblin_01" });
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> Listeners = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> listener)
        {
            var type = typeof(T);
            if (Listeners.TryGetValue(type, out var existing))
            {
                Listeners[type] = Delegate.Combine(existing, listener);
            }
            else
            {
                Listeners[type] = listener;
            }
        }

        public static void Unsubscribe<T>(Action<T> listener)
        {
            var type = typeof(T);
            if (!Listeners.TryGetValue(type, out var existing)) return;

            var remaining = Delegate.Remove(existing, listener);
            if (remaining == null) Listeners.Remove(type);
            else Listeners[type] = remaining;
        }

        public static void Publish<T>(T evt)
        {
            var type = typeof(T);
            if (Listeners.TryGetValue(type, out var existing))
            {
                (existing as Action<T>)?.Invoke(evt);
            }
        }

        /// <summary>Limpia todos los listeners. Llamar solo al cambiar de escena/reiniciar en el editor.</summary>
        public static void ClearAll() => Listeners.Clear();
    }

    // --- Eventos de juego (se amplia por etapa) ---

    public struct EnemyKilledEvent
    {
        public string EnemyId;
        public double GoldDropped;
        public bool WasBoss;
    }

    public struct GoldChangedEvent
    {
        public double NewTotal;
        public double Delta;
    }

    public struct TapDamageUpgradedEvent
    {
        public double NewTapDamage;
        public int NewLevel;
    }

    /// <summary>Estado de la oleada actual, para que la UI muestre nivel/progreso, el
    /// temporizador de jefe, o el boton de "volver a desafiar al jefe".</summary>
    public struct StageStatusEvent
    {
        public int Level;
        public int KillsInStage;
        public int EnemiesPerStage;
        public bool BossActive;
        public float BossSecondsRemaining;
        public bool BossAvailable;
        public bool HasLostBossOnce;
    }

    /// <summary>Se compro o mejoro un heroe. HeroId en vez de la referencia al ScriptableObject
    /// para que Core no dependa de TapLegends.Progression.</summary>
    public struct HeroChangedEvent
    {
        public string HeroId;
    }

    /// <summary>Se cargo una partida con tiempo fuera de la app: cuanto oro se gano offline.</summary>
    public struct OfflineProgressEvent
    {
        public double GoldEarned;
        public float SecondsAway;
    }

    /// <summary>El jefe no murio a tiempo y escapo: la oleada se repite. Ver analisis L2 —
    /// el fracaso tiene que leerse como informativo, no como un reseteo silencioso.</summary>
    public struct BossEscapedEvent { }

    /// <summary>Se mejoro una habilidad. SkillId en vez de la referencia al ScriptableObject,
    /// mismo criterio que HeroChangedEvent.</summary>
    public struct SkillChangedEvent
    {
        public string SkillId;
    }

    /// <summary>Se disparo una habilidad (golpe o buff) — dispara el flash de impacto en pantalla.</summary>
    public struct SkillActivatedEvent
    {
        public string SkillId;
    }

    /// <summary>Cambio el total de reliquias (compra de mejora permanente o un Renacer).</summary>
    public struct RelicsChangedEvent
    {
        public double NewTotal;
    }

    /// <summary>Se compro una mejora permanente de prestigio.</summary>
    public struct PrestigeUpgradeChangedEvent
    {
        public string UpgradeId;
    }

    /// <summary>Se completo un Renacer: oro/heroes/nivel se reiniciaron, se ganaron reliquias.</summary>
    public struct PrestigeCompletedEvent
    {
        public double RelicsGained;
    }

    /// <summary>Cambio de nivel, rareza o fragmentos de una pieza de equipo.</summary>
    public struct EquipmentChangedEvent
    {
        public string EquipmentId;
    }

    /// <summary>Un jefe solto botin — para el toast de "conseguiste X" en la UI.</summary>
    public struct LootObtainedEvent
    {
        public string EquipmentId;
        public string DisplayName;
        public string Rarity;
        public bool WasUpgrade; // true = subio de rareza, false = se convirtio en fragmentos
        public int FragmentsGained;
    }

    /// <summary>Cambio de nivel de una mascota o cambio de cual esta activa.</summary>
    public struct PetChangedEvent
    {
        public string PetId;
    }

    /// <summary>Un jefe solto un huevo de mascota — mismo toast que LootObtainedEvent, evento
    /// separado porque no encaja en los campos de "rareza" del equipo.</summary>
    public struct PetObtainedEvent
    {
        public string PetId;
        public string DisplayName;
        public bool WasNew; // true = mascota nueva, false = se convirtio en fragmentos
        public int FragmentsGained;
    }

    /// <summary>Progreso de las 3 misiones diarias cambio (o se regeneraron para un dia nuevo).</summary>
    public struct QuestsChangedEvent { }

    /// <summary>Se desbloqueo un logro — para el toast y el contador de la UI.</summary>
    public struct AchievementUnlockedEvent
    {
        public string AchievementId;
        public string DisplayName;
        public double RewardGold;
    }
}
