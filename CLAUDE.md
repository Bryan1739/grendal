# CLAUDE.md — Tap Legends

Guía de trabajo para este proyecto Unity. El **plan de diseño completo** (etapas, GDD,
iteraciones ludológicas) vive en `../TapLegends-Plan.md`, un nivel arriba de esta carpeta —
leelo primero para contexto de producto. Este archivo es sobre **cómo** trabajar en el
código, no **qué** construir.

**Regla permanente**: actualizar este archivo cada vez que se hagan cambios significativos
(sistema nuevo, cambio de arquitectura, bug de Unity descubierto, convención nueva).

---

## Estado actual (ver también docs/GDD.md)

Etapas del plan completadas: 0-8 y parte de la 9 (misiones + logros; falta arte final).
Sistemas funcionando: tap, oleadas con jefe (10 en 10, primer jefe automático hasta la
primera derrota, después opcional via botón), héroes, equipo (5 slots, rareza sube con
botín), mascotas (1 activa + 20% las demás), 5 habilidades activas, prestigio (reliquias +
4 mejoras permanentes), guardado/offline, misiones diarias, 30 logros.

---

## Control de versiones

Repo local conectado a GitHub: `https://github.com/Bryan1739/grendal.git` (remoto
`origin`, rama `master`). Autenticación vía Git Credential Manager (ya viene con Git for
Windows), sin PAT manual. Primer commit (`cce1f03`, "Primer commit: Tap Legends v0.1-v0.8")
incluye todo el proyecto hasta Etapa 8 + parte de la 9. `.gitignore` excluye `Library/`,
`Temp/`, `Logs/`, `UserSettings/`, builds (`*.apk`/`*.aab`), keystores.

Nota: `TapLegends-Plan.md` vive un nivel arriba de este repo (`../TapLegends-Plan.md`), por
lo tanto **no** está versionado en GitHub — solo existe localmente.

---

## Setup después de cambiar datos

1. `Tap Legends → Setup → Create Initial Data (Bosque Quebrado)` — crea/actualiza los
   ScriptableObject assets (enemigos, héroes, skills, equipo, mascotas, prestigio,
   misiones, logros). Seguro de re-correr: actualiza valores en vez de duplicar assets.
2. `Tap Legends → Setup → Build Boot + Game Scenes (v0.1)` — reconstruye Boot.unity y
   Game.unity **desde cero** por código (`SceneBuilder.cs`). No hay prefabs.
3. Play.

**Cuándo hace falta el paso 2**: solo si se agregó/cambió ESTRUCTURA en `SceneBuilder.cs`
(GameObjects nuevos, componentes nuevos, wiring nuevo). Si el cambio es lógica pura en un
script existente (por ejemplo ajustar una fórmula en `GameConstants.cs` o el flujo de
`EnemySpawner.cs`), alcanza con dejar que Unity recompile — no hace falta reconstruir la
escena. Aclarar esto en cada respuesta evita que el usuario reconstruya sin necesidad.

---

## Arquitectura: el patrón que se repite en cada sistema de progresión

Cada sistema (Héroes, Equipo, Mascotas, Habilidades, Prestigio, Misiones, Logros) sigue
las mismas 4 piezas:

1. **`XxxData`** (ScriptableObject, `Scripts/Progression/`) — la "receta": id, displayName,
   curvas de crecimiento. Nunca contiene estado de partida.
2. **`XxxManager`** (MonoBehaviour singleton, `Instance` estático) — dueño del ESTADO en
   tiempo de ejecución (niveles, progreso, activo/inactivo). Expone `GetXxx()`/`TryXxx()`/
   `GetSaveEntries()`/`LoadState()`. Se suscribe a `EventBus` para reaccionar a gameplay.
3. **`XxxRow`/`XxxDisplay`** (UI, `Scripts/UI/`) — un componente por fila/casillero, se
   refresca solo escuchando su propio evento de cambio (`XxxChangedEvent`).
4. **`SceneBuilder.cs`** arma la UI por código; **`CreateInitialData.cs`** genera los
   assets. Ningún prefab en el proyecto todavía.

Multiplicadores de cada sistema (daño, oro, DPS de héroes, cooldown) se consultan en la
**fuente** (`PlayerStats.TapDamage`, `HeroRoster.GetDps`, `Enemy.Die()`,
`SkillManager.GetCooldownDuration`) — ningún sistema le "empuja" un valor a otro.

---

## Gotchas de Unity ya descubiertos (no perder tiempo re-descubriéndolos)

1. **`AddListener` desde un script de Editor no sobrevive a Play mode.**
   `SceneBuilder.cs` corre en modo Edit. `button.onClick.AddListener(...)` ahí crea un
   listener "no persistente" que no se serializa y desaparece al entrar en Play (recarga
   de dominio). **Siempre conectar el onClick dentro del propio componente, en su
   `Awake()`**. `SceneBuilder` solo asigna referencias (`AssignSerialized`,
   `AssignSerializedInt`, `AssignSerializedEnum`, `AssignSerializedArray`), nunca
   registra listeners.

2. **`Image.Type.Filled` necesita un sprite.** Sin uno, Unity ni siquiera muestra el
   campo "Fill Amount" en el Inspector y el relleno no se ve bien. Usar
   `PlaceholderArt.SolidPixel()` para cualquier barra.

3. **Un campo no marcado `[SerializeField]` pierde su valor al entrar en Play** si se
   asignó desde un script de Editor (mismo problema que el #1, pero para datos en vez de
   listeners). Cualquier campo que `SceneBuilder` necesite poblar tiene que ser
   `[SerializeField]` (o público).

4. **RectTransform por código: fijar `anchorMin`/`anchorMax`/`pivot` siempre explícito.**
   Los defaults no son confiables para construir UI por código.

5. **Un solo Unity Editor puede tener el proyecto abierto a la vez.** Un batch-mode que
   intenta abrirlo mientras ya está abierto en GUI falla en silencio (exit code 1, log
   casi vacío). Antes de asumir que es un error real, chequear procesos:
   `Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'"`.

6. **Canvas Screen Space Overlay ignora la cámara.** Mover `Camera.transform` no hace
   nada visible — el Canvas Overlay se dibuja independiente de ella. Para "shake" de
   impacto usamos un flash de pantalla completa (`SkillImpactFlash.cs`), no un shake real.

7. **minSdkVersion 24 no compiló** en la versión de Unity instalada — quedó en 26. Google
   sube el piso con el tiempo; si vuelve a fallar, leer el mensaje de error exacto.

---

## Convenciones de balance (ver `GameConstants.cs`)

- Todo valor de juego (oro, daño, vida) es `double`, nunca `int`/`float`.
- Todo crecimiento es **multiplicativo** (`base * growth^nivel`), nunca aditivo — un +1
  fijo no compone contra curvas exponenciales (bug real encontrado y corregido en L1).
- Cuando se agrega un sistema nuevo que aporta un multiplicador (Prestigio, Equipo,
  Mascotas...), reconsiderar si `EnemyHealthGrowth`/`EnemyGoldGrowth` siguen calibrados —
  cada multiplicador nuevo que se apila hace que la dificultad/inflación real sentida sea
  mayor que lo que esas dos constantes solas sugieren.

---

## Placeholder art

Todo el arte es procedural (`PlaceholderArt.cs`) hasta la Etapa 9 (arte final, todavía sin
definir de dónde sale). El patrón `art ?? PlaceholderArt.Xxx(...)` en cada `XxxData` permite
reemplazar por arte final después rellenando el campo `art`/`icon` en el asset — cero
cambios de código.

---

## Testing

No hay tests automatizados. Verificación manual: Play en el Editor. El menú de debug
(triple tap arriba a la derecha, solo en builds de desarrollo/editor — ver
`DebugMenuController.cs`) tiene trucos para saltar el grind: oro, daño de tap, borrar
guardado, simular progreso offline.
