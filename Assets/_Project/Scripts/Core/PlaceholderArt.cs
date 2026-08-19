using System.Collections.Generic;
using UnityEngine;

namespace TapLegends.Core
{
    public enum CreatureRole { CommonEnemy, EliteEnemy, Boss, Hero, Pet }

    public enum WeaponKind { Sword, Bow, Staff, Dagger, Axe }

    /// <summary>
    /// Genera sprites de placeholder por codigo: criaturas simetricas por semilla,
    /// iconos y marcos de UI. Timebox de diseño: 10 h (ver TapLegends-Plan.md).
    ///
    /// La semilla es SIEMPRE el hash del id textual del dato (nunca Random.Range
    /// sin semilla), para que "goblin_01" se vea igual en cada sesion y build.
    ///
    /// Arquitectura: los datos (EnemyData, HeroData...) piden el sprite asi:
    ///     public Sprite Art => art != null ? art : PlaceholderArt.Creature(id, size, role);
    /// Sustituir el placeholder por arte final es solo rellenar el campo `art`
    /// en el ScriptableObject — cero cambios de codigo (ver Etapa 9 del plan).
    /// </summary>
    public static class PlaceholderArt
    {
        private const int PixelsPerUnit = 32;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        // Hue base por rol (0..1). Cada id individual se desplaza dentro de una banda
        // alrededor de este hue (ver RoleTintedColor) para que dos enemigos del mismo
        // rol se noten distintos a simple vista sin perder la lectura por color del rol.
        private static readonly Dictionary<CreatureRole, float> RoleBaseHue = new Dictionary<CreatureRole, float>
        {
            { CreatureRole.CommonEnemy, 0.00f }, // rojo
            { CreatureRole.EliteEnemy,  0.08f }, // naranja
            { CreatureRole.Boss,        0.80f }, // morado
            { CreatureRole.Hero,        0.60f }, // azul
            { CreatureRole.Pet,         0.35f }, // verde
        };

        public static readonly Dictionary<string, Color> RarityColor = new Dictionary<string, Color>
        {
            { "Common",    new Color(0.72f, 0.72f, 0.72f) },
            { "Uncommon",  new Color(0.30f, 0.75f, 0.30f) },
            { "Rare",      new Color(0.25f, 0.45f, 0.90f) },
            { "Epic",      new Color(0.60f, 0.25f, 0.85f) },
            { "Legendary", new Color(0.95f, 0.70f, 0.15f) },
        };

        /// <summary>Genera (o recupera de cache) una criatura simetrica de tamaño size x size.</summary>
        public static Sprite Creature(string id, int size = 32, CreatureRole role = CreatureRole.CommonEnemy)
        {
            string key = $"creature::{id}::{size}::{role}";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            int seed = StableHash(id);
            var rng = new System.Random(seed);
            var baseColor = RoleTintedColor(seed, role);
            var outline = baseColor * 0.45f;
            outline.a = 1f;

            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            int half = size / 2;
            var body = new bool[size, size];

            // 1) Rellenar la mitad izquierda con ruido sesgado hacia el centro (silueta compacta)
            for (int y = 2; y < size - 2; y++)
            {
                for (int x = 2; x < half; x++)
                {
                    float distFromCenter = (float)(half - x) / half; // 0 en el centro, 1 en el borde izq
                    float fillChance = Mathf.Lerp(0.85f, 0.15f, distFromCenter);
                    if (rng.NextDouble() < fillChance)
                        body[x, y] = true;
                }
            }

            // 2) Espejar la mitad izquierda sobre la derecha -> simetria bilateral
            for (int y = 0; y < size; y++)
                for (int x = 0; x < half; x++)
                    body[size - 1 - x, y] = body[x, y];

            // 3) Pintar cuerpo
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    if (body[x, y])
                        pixels[y * size + x] = baseColor;

            // 4) Pasada de contorno: pixel vacio adyacente a cuerpo -> oscuro
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (body[x, y]) continue;
                    if (HasNeighborBody(body, x, y, size))
                        pixels[y * size + x] = outline;
                }
            }

            // 5) Ojos: dos pixeles claros en la mitad superior, simetricos.
            // y=0 es la fila de ABAJO en Unity (Texture2D.SetPixels), asi que "arriba" es un y grande.
            int eyeY = size - size / 3;
            int eyeOffsetX = Mathf.Max(2, size / 6);
            SetPixelSafe(pixels, size, half - eyeOffsetX, eyeY, Color.white);
            SetPixelSafe(pixels, size, half + eyeOffsetX - 1, eyeY, Color.white);
            SetPixelSafe(pixels, size, half - eyeOffsetX, eyeY - 1, Color.black);
            SetPixelSafe(pixels, size, half + eyeOffsetX - 1, eyeY - 1, Color.black);

            var sprite = BuildSprite(pixels, size, size, new Vector2(0.5f, 0f));
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>Icono cuadrado plano de un color solido con borde, para UI (rareza, recursos...).</summary>
        public static Sprite Icon(string id, int size, Color fill)
        {
            string key = $"icon::{id}::{size}::{ColorUtility.ToHtmlStringRGBA(fill)}";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            var pixels = new Color[size * size];
            var border = fill * 0.5f;
            border.a = 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    pixels[y * size + x] = isBorder ? border : fill;
                }
            }

            var sprite = BuildSprite(pixels, size, size, new Vector2(0.5f, 0.5f));
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Icono de arma simple (espada/arco/baston) para diferenciar heroes a simple vista
        /// sin necesitar arte final. El id entra en la clave de cache pero no afecta la forma
        /// (solo hay 3 formas); dos heroes con la misma forma se distinguen por accentColor.
        /// </summary>
        public static Sprite Weapon(string id, WeaponKind kind, Color accentColor, int size = 24)
        {
            string key = $"weapon::{id}::{kind}::{size}::{ColorUtility.ToHtmlStringRGBA(accentColor)}";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            switch (kind)
            {
                case WeaponKind.Bow: DrawBow(pixels, size, accentColor); break;
                case WeaponKind.Staff: DrawStaff(pixels, size, accentColor); break;
                case WeaponKind.Dagger: DrawDagger(pixels, size, accentColor); break;
                case WeaponKind.Axe: DrawAxe(pixels, size, accentColor); break;
                default: DrawSword(pixels, size, accentColor); break;
            }

            var sprite = BuildSprite(pixels, size, size, new Vector2(0.5f, 0f));
            Cache[key] = sprite;
            return sprite;
        }

        private static void DrawSword(Color[] pixels, int size, Color bladeColor)
        {
            int cx = size / 2;
            var handleColor = new Color(0.40f, 0.25f, 0.10f);
            var guardColor = new Color(0.25f, 0.25f, 0.28f);

            int guardY = Mathf.RoundToInt(size * 0.32f);
            FillRect(pixels, size, cx - 1, 1, cx, guardY - 1, handleColor);
            FillRect(pixels, size, cx - 3, guardY, cx + 2, guardY + 1, guardColor);
            FillRect(pixels, size, cx - 1, guardY + 2, cx, size - 2, bladeColor);
        }

        private static void DrawStaff(Color[] pixels, int size, Color orbColor)
        {
            int cx = size / 2;
            var rodColor = new Color(0.35f, 0.22f, 0.10f);

            int rodTop = size - Mathf.RoundToInt(size * 0.22f);
            FillRect(pixels, size, cx - 1, 1, cx, rodTop, rodColor);

            int orbCenterY = size - Mathf.RoundToInt(size * 0.14f);
            int r = Mathf.Max(2, size / 6);
            for (int y = -r; y <= r; y++)
                for (int x = -r; x <= r; x++)
                    if (x * x + y * y <= r * r)
                        SetPixelSafe(pixels, size, cx + x, orbCenterY + y, orbColor);
        }

        private static void DrawDagger(Color[] pixels, int size, Color bladeColor)
        {
            int cx = size / 2;
            var handleColor = new Color(0.35f, 0.20f, 0.10f);

            int gripTop = Mathf.RoundToInt(size * 0.24f);
            FillRect(pixels, size, cx - 1, 1, cx, gripTop, handleColor);
            // Sin guarda, hoja mas corta que la espada: silueta distinta a simple vista.
            FillRect(pixels, size, cx - 1, gripTop + 1, cx, Mathf.RoundToInt(size * 0.72f), bladeColor);
        }

        private static void DrawAxe(Color[] pixels, int size, Color bladeColor)
        {
            int cx = size / 2;
            var handleColor = new Color(0.35f, 0.20f, 0.10f);

            FillRect(pixels, size, cx - 1, 1, cx, size - 2, handleColor); // mango de punta a punta

            // Cabeza asimetrica hacia un lado, cerca de arriba — se distingue de la espada
            // (simetrica) con solo mirar la silueta.
            int headY0 = Mathf.RoundToInt(size * 0.55f);
            int headWidth = Mathf.RoundToInt(size * 0.32f);
            FillRect(pixels, size, cx, headY0, cx + headWidth, size - 3, bladeColor);
        }

        private static void DrawBow(Color[] pixels, int size, Color bowColor)
        {
            var points = new (float xFrac, float yFrac)[]
            {
                (0.62f, 0.10f),
                (0.50f, 0.30f),
                (0.44f, 0.50f),
                (0.50f, 0.70f),
                (0.62f, 0.90f),
            };

            Vector2Int ToPixel(int i) => new Vector2Int(
                Mathf.RoundToInt(points[i].xFrac * size),
                Mathf.RoundToInt(points[i].yFrac * size));

            for (int i = 0; i < points.Length - 1; i++)
            {
                var a = ToPixel(i);
                var b = ToPixel(i + 1);
                DrawLine(pixels, size, a.x, a.y, b.x, b.y, bowColor, 2);
            }

            var top = ToPixel(0);
            var bottom = ToPixel(points.Length - 1);
            var stringColor = new Color(0.9f, 0.9f, 0.85f);
            DrawLine(pixels, size, top.x, top.y, bottom.x, bottom.y, stringColor, 1);
        }

        private static void FillRect(Color[] pixels, int size, int x0, int y0, int x1, int y1, Color color)
        {
            x0 = Mathf.Clamp(x0, 0, size - 1);
            x1 = Mathf.Clamp(x1, 0, size - 1);
            y0 = Mathf.Clamp(y0, 0, size - 1);
            y1 = Mathf.Clamp(y1, 0, size - 1);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    pixels[y * size + x] = color;
        }

        /// <summary>Bresenham con grosor, para siluetas simples (arco, cuerda...) que un rectangulo no puede hacer.</summary>
        private static void DrawLine(Color[] pixels, int size, int x0, int y0, int x1, int y1, Color color, int thickness)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            int half = thickness / 2;

            while (true)
            {
                for (int oy = -half; oy <= half; oy++)
                    for (int ox = -half; ox <= half; ox++)
                        SetPixelSafe(pixels, size, x0 + ox, y0 + oy, color);

                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        /// <summary>Sprite 1x1 blanco solido, util para 9-slice de paneles y barras.</summary>
        public static Sprite SolidPixel()
        {
            const string key = "solid_pixel";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Textura de "tajo" curvo: nucleo brillante que se afina hasta desaparecer en
        /// las puntas, arqueado hacia arriba en el medio — para el efecto de espadazo
        /// (ver PlayerAttackEffect). Blanco con alpha variable, se tinta con Image.color
        /// por skin, igual que SolidPixel. Una barra recta se leia como un disparo/bala
        /// (reportado por el usuario); esta forma se lee como un corte ancho.
        /// </summary>
        public static Sprite SlashArc(int width = 128, int height = 56)
        {
            string key = $"slash_arc::{width}x{height}";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            var pixels = new Color[width * height];
            float midY = height * 0.5f;
            float curve = height * 0.22f;
            float maxThickness = height * 0.34f;

            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                float taper = Mathf.Sin(t * Mathf.PI); // 0 en las puntas, 1 en el medio
                float thickness = Mathf.Max(0.6f, maxThickness * taper);
                float centerY = midY + curve * Mathf.Sin(t * Mathf.PI);

                for (int y = 0; y < height; y++)
                {
                    float dist = Mathf.Abs(y - centerY);
                    float edge = thickness * 0.5f;
                    float alpha = dist >= edge ? 0f : Mathf.Pow(1f - dist / edge, 0.6f);
                    alpha *= taper; // se desvanece del todo en las puntas, no solo se adelgaza
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            var sprite = BuildSprite(pixels, width, height, new Vector2(0.5f, 0.5f));
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Color por id dentro de una banda alrededor del hue del rol: dos enemigos
        /// distintos se notan a simple vista, pero ambos siguen "leyendose" como su rol
        /// (comunes en rojos, jefes en morados...).
        /// </summary>
        private static Color RoleTintedColor(int seed, CreatureRole role)
        {
            float baseHue = RoleBaseHue.TryGetValue(role, out var hue) ? hue : 0f;
            float offset = ((seed & 0xFFFF) / 65535f - 0.5f) * 0.28f; // +/- ~0.14 de hue
            float finalHue = Mathf.Repeat(baseHue + offset, 1f);
            float saturation = role == CreatureRole.Boss ? 0.55f : 0.62f;
            float value = role == CreatureRole.Boss ? 0.55f : 0.75f;
            return Color.HSVToRGB(finalHue, saturation, value);
        }

        /// <summary>
        /// Fondo de paisaje montañoso a pixel art: cielo en degradado + dos capas de
        /// montañas + suelo. Placeholder hasta el arte final de biomas (Etapa 9).
        /// El tamaño por defecto respeta el aspecto 9:16 de ReferenceResolution.
        /// </summary>
        public static Sprite MountainLandscape(string seed, int width = 72, int height = 128)
        {
            string key = $"landscape::{seed}::{width}x{height}";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            var rng = new System.Random(StableHash(seed));
            var pixels = new Color[width * height];

            // Paleta derivada del seed: cada capitulo (biome/jefe vencido) rota el hue base,
            // asi el paisaje se ve distinto en color Y silueta sin dejar de "leerse" como
            // paisaje (mismo esquema sky+montañas+suelo, solo cambia el tono).
            float baseHue = (float)rng.NextDouble();
            float horizonHue = Mathf.Repeat(baseHue + 0.06f, 1f);
            float farHue = Mathf.Repeat(baseHue + 0.5f, 1f); // complementario: sensacion atmosferica
            float groundHue = Mathf.Repeat(baseHue + 0.25f, 1f);

            var skyTop = Color.HSVToRGB(baseHue, 0.55f, 0.22f);
            var skyHorizon = Color.HSVToRGB(horizonHue, 0.40f, 0.80f);
            var farColor = Color.HSVToRGB(farHue, 0.28f, 0.58f);
            var nearColor = Color.HSVToRGB(farHue, 0.35f, 0.28f);
            var groundColor = Color.HSVToRGB(groundHue, 0.45f, 0.20f);

            // 1) Cielo: degradado vertical de horizonte (abajo) a cenit (arriba).
            for (int y = 0; y < height; y++)
            {
                var sky = Color.Lerp(skyHorizon, skyTop, (float)y / height);
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = sky;
            }

            int groundHeight = Mathf.RoundToInt(height * 0.08f);

            // 2) Montañas lejanas (mas claras/frias, "atmosfericas") y 3) cercanas
            // (mas oscuras, tapan a las lejanas donde se solapan porque se pintan despues).
            var farSkyline = GenerateSkyline(rng, width, Mathf.RoundToInt(height * 0.55f), Mathf.RoundToInt(height * 0.10f), 2);
            var nearSkyline = GenerateSkyline(rng, width, Mathf.RoundToInt(height * 0.35f), Mathf.RoundToInt(height * 0.12f), 4);

            PaintSkylineFill(pixels, width, height, farSkyline, groundHeight, farColor);
            PaintSkylineFill(pixels, width, height, nearSkyline, groundHeight, nearColor);

            // 4) Suelo.
            for (int y = 0; y < groundHeight; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = groundColor;

            var sprite = BuildSprite(pixels, width, height, new Vector2(0.5f, 0.5f));
            Cache[key] = sprite;
            return sprite;
        }

        private static void PaintSkylineFill(Color[] pixels, int width, int height, int[] skyline, int groundHeight, Color color)
        {
            for (int x = 0; x < width; x++)
            {
                int top = Mathf.Clamp(skyline[x], groundHeight, height - 1);
                for (int y = groundHeight; y <= top; y++)
                    pixels[y * width + x] = color;
            }
        }

        /// <summary>Perfil de montaña por "paseo aleatorio" acotado: jagged pero deterministico por semilla.</summary>
        private static int[] GenerateSkyline(System.Random rng, int width, int baseline, int amplitude, int roughness)
        {
            var heights = new int[width];
            int current = baseline;
            for (int x = 0; x < width; x++)
            {
                current += rng.Next(-roughness, roughness + 1);
                current = Mathf.Clamp(current, baseline - amplitude, baseline + amplitude);
                heights[x] = current;
            }
            return heights;
        }

        private static bool HasNeighborBody(bool[,] body, int x, int y, int size)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || nx >= size || ny < 0 || ny >= size) continue;
                    if (body[nx, ny]) return true;
                }
            }
            return false;
        }

        private static void SetPixelSafe(Color[] pixels, int size, int x, int y, Color color)
        {
            if (x < 0 || x >= size || y < 0 || y >= size) return;
            pixels[y * size + x] = color;
        }

        private static Sprite BuildSprite(Color[] pixels, int width, int height, Vector2 pivot)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), pivot, PixelsPerUnit);
        }

        /// <summary>Hash estable entre sesiones (GetHashCode de string cambia entre runs de .NET).</summary>
        private static int StableHash(string s)
        {
            unchecked
            {
                int hash = 17;
                foreach (char ch in s) hash = hash * 31 + ch;
                return hash;
            }
        }

        public static void ClearCache() => Cache.Clear();
    }
}
