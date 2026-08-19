using System.Collections.Generic;
using TapLegends.Core;
using UnityEngine;

namespace TapLegends.Progression
{
    /// <summary>
    /// Perfil del jugador: nombre, presentacion y skins elegidas (personaje, cada heroe,
    /// cada mascota). Todo local por ahora — la presentacion ya esta pensada como algo que
    /// otros jugadores leerian en un futuro modo online (ver TapLegends-Plan.md), pero hoy
    /// no sale de este dispositivo.
    ///
    /// Las "skins" son variantes proceduraless de PlaceholderArt (mismo criterio que el
    /// resto del arte del proyecto): skin 0 = aspecto de siempre (sin cambios), skins 1+
    /// le agregan un sufijo determinístico al id antes de pedirselo a PlaceholderArt, asi
    /// que cada skin se ve siempre igual entre sesiones. Sustituir esto por arte final es
    /// el mismo patron de siempre — un campo `art` por skin en una futura SkinData — pero
    /// no hace falta para tener el mecanismo funcionando ahora.
    /// </summary>
    public class ProfileManager : MonoBehaviour
    {
        public static ProfileManager Instance { get; private set; }

        // Mismo id que se usaba antes de que existiera el sistema de skins — asi la skin 0
        // (default) sigue viendose EXACTAMENTE igual que siempre, sin sorpresas visuales.
        public const string PlayerCharacterId = "player_hero";
        public const int SkinCount = 4;
        public static readonly string[] SkinNames = { "Clásico", "Bronce", "Plata", "Oro" };

        public string Username { get; private set; } = "";
        public string Bio { get; private set; } = "";

        private readonly Dictionary<string, int> skins = new Dictionary<string, int>();

        private void Awake() => Instance = this;

        public void SetUsername(string value)
        {
            Username = value ?? "";
            EventBus.Publish(new ProfileChangedEvent());
        }

        public void SetBio(string value)
        {
            Bio = value ?? "";
            EventBus.Publish(new ProfileChangedEvent());
        }

        public int GetSkin(string characterId) =>
            !string.IsNullOrEmpty(characterId) && skins.TryGetValue(characterId, out var v) ? v : 0;

        public void SetSkin(string characterId, int skinIndex)
        {
            if (string.IsNullOrEmpty(characterId)) return;

            skinIndex = Mathf.Clamp(skinIndex, 0, SkinCount - 1);
            skins[characterId] = skinIndex;
            EventBus.Publish(new SkinChangedEvent { CharacterId = characterId });
        }

        /// <summary>Id que hay que pedirle a PlaceholderArt para que refleje la skin
        /// elegida — skin 0 devuelve el id tal cual (aspecto de siempre, cero cambios).</summary>
        public string ApplySkin(string characterId) => SkinnedId(characterId, GetSkin(characterId));

        /// <summary>Version estatica de ApplySkin, para generar previews de skin en
        /// SceneBuilder (modo Editor, sin ProfileManager.Instance disponible todavia).</summary>
        public static string SkinnedId(string characterId, int skinIndex) =>
            skinIndex <= 0 || string.IsNullOrEmpty(characterId) ? characterId : $"{characterId}#skin{skinIndex}";

        public List<SkinSaveEntry> GetSaveEntries()
        {
            var list = new List<SkinSaveEntry>();
            foreach (var kvp in skins)
                if (kvp.Value != 0) list.Add(new SkinSaveEntry { characterId = kvp.Key, skinIndex = kvp.Value });
            return list;
        }

        public void LoadState(string username, string bio, List<SkinSaveEntry> savedSkins)
        {
            Username = username ?? "";
            Bio = bio ?? "";

            skins.Clear();
            if (savedSkins != null)
                foreach (var entry in savedSkins)
                    if (!string.IsNullOrEmpty(entry.characterId)) skins[entry.characterId] = entry.skinIndex;

            EventBus.Publish(new ProfileChangedEvent());
            // CharacterId vacio = "revisa todo": mas simple que republicar un evento por
            // cada skin guardada, y los listeners ya saben tratar ese caso como refresh total.
            EventBus.Publish(new SkinChangedEvent { CharacterId = "" });
        }
    }
}
