# CLAUDE.md — Tap Legends

Guía de trabajo para este proyecto Unity. El **plan de diseño completo** (etapas, GDD,
iteraciones ludológicas) vive en `../TapLegends-Plan.md`, un nivel arriba de esta carpeta —
leelo primero para contexto de producto. Este archivo es sobre **cómo** trabajar en el
código, no **qué** construir.

**Regla permanente**: actualizar este archivo cada vez que se hagan cambios significativos
(sistema nuevo, cambio de arquitectura, bug de Unity descubierto, convención nueva).

**Regla permanente (nueva)**: agregar tests EditMode para la lógica pura que toque cada
cambio (fórmulas de balance, `TryXxx`/`GetXxx` de un Manager, formateo). Ver sección
"Tests" más abajo — incluye qué se testea, qué no, y por qué.

---

## Estado actual (ver también docs/GDD.md)

Etapas del plan completadas: 0-8, parte de la 9 (misiones + logros; falta arte final) y
parte de la 10 (desbloqueo progresivo, tutorial, boton de retroceso, ajustes, safe area —
falta el resto del pulido UX y los playtests de L5).

**Iteración ludológica hecha:** [docs/ludo/L4-economia-offline.md](../docs/ludo/L4-economia-offline.md)
— el oro offline no aplicaba los multiplicadores de Prestigio/Equipo/Mascotas que el oro
activo sí aplica (`GameManager.GoldPerDamageAtLevel`, ahora factorizado y corregido); de
paso, `OfflineDpsFactor` 0.5→0.65 y `MaxOfflineSeconds` 8h→16h, pedido explícito para que
dejar el juego se sienta mejor recompensado.

Sistemas funcionando: tap, oleadas con jefe (10 en 10; CADA jefe arranca automático la
primera vez, tras perderlo se vuelve opcional via botón, y vencerlo reinicia el ciclo para
que el próximo jefe — 10 niveles después — vuelva a ser automático), héroes, equipo (5
slots, rareza sube con botín), mascotas (1 activa + 20% las demás), 5 habilidades activas,
prestigio (reliquias + 4 mejoras permanentes), guardado/offline, misiones diarias, 30 logros.

**Etapa 10 (parcial) — UX y onboarding:**
- Desbloqueo progresivo: las pestañas Heroes/Equipo/Mascotas/Prestigio quedan ocultas
  hasta nivel 3/8/15/30 (`GameConstants`, `BottomTabController`), con un toast
  (`SystemUnlockedEvent` → `LootToast`) la primera vez que se desbloquean en la sesión
  (nunca al cargar una partida que ya las tenia).
- Tutorial de los primeros 3 minutos (`TutorialManager`): 3 pasos — tocar, comprar mejora
  de daño, contratar un heroe — cada uno avanza con el evento real, no con un timer. Corre
  una vez por partida (`SaveData.tutorialCompleted`), con boton "Saltar".
- Boton de retroceso de Android → menu de pausa, no cierra la app (`PauseMenuController`,
  ver gotcha #8 mas abajo). Boton ☰ en la esquina superior izquierda abre lo mismo.
  Ajustes: vibracion (en la muerte del enemigo, no en cada tap), borrar partida (con
  confirmacion), creditos. Musica/SFX quedan para cuando exista un sistema de audio.
- Safe area: `SafeAreaFitter` recorta TopBar (borde superior) y BottomPanel (borde
  inferior) al `Screen.safeArea`, sin tocar el resto de sus anclas.
- Mantener pulsado para comprar varias veces seguidas (`HoldToRepeatButton`, agregado a
  los botones de mejora de tap/heroes/habilidades/equipo/mascotas/prestigio): no sabe nada
  del sistema que compra, solo re-invoca el `onClick` del boton cada ~90ms tras un delay
  inicial de 0.45s — cada repeticion pasa por el mismo chequeo de "me alcanza" de siempre,
  asi que se frena sola al quedarse sin oro/fragmentos/reliquias.
- Pendiente de la Etapa 10: auditoria de tamaño minimo de botones (48dp), playtests L5
  con novatos.

**Ciclo automático/opcional del jefe** (ver `EnemySpawner`): `hasLostBossOnce` describe
"¿ya se perdió EL JEFE ACTUAL alguna vez?", no "¿algún jefe alguna vez en toda la
partida?". Se pone en `true` cuando el jefe escapa por tiempo (`HandleBossEscaped`) —desde
ahí, los reintentos de *ese* jefe son opcionales vía botón— y se reinicia solo a `false`
en el momento en que se lo vence (`HandleEnemyDied`, rama `wasBoss`), así que el próximo
jefe (10 niveles después) siempre vuelve a arrancar automático, gane o pierda el jugador
cuantas veces quiera antes de vencerlo. También se reinicia en cada Renacer (mismo motivo).
`EnemySpawner.DebugResetBossAutoFight()` (truco "Reset auto-jefe" en el menú de debug)
fuerza el modo automático sin tener que vencer al jefe de verdad — útil para probar el
mecanismo rápido y repetidas veces.

**Tap en toda la pantalla** (estilo Tap Titans): `TapCatcher` es una Image invisible
(`Color.clear`, `raycastTarget=true`) que cubre TODO el Canvas y vive muy temprano en la
jerarquía — justo después del fondo, antes de absolutamente todo lo demás (`TopBar`,
`BottomPanel`, botones, paneles). Reenvía el tap al enemigo actual llamando directo a
`Enemy.OnPointerDown` (cero lógica de daño duplicada). Funciona sin pisar ningún botón
porque Unity solo entrega el evento de puntero al elemento MÁS AL FRENTE bajo el dedo
(`GraphicRaycaster.FindFirstRaycast`): cualquier UI agregada DESPUÉS del `TapCatcher` en la
jerarquía se queda con su propio click automáticamente, sin coordinar nada a mano. Un
`CanvasGroup.blocksRaycasts=false` (botón oculto, ej. `ChallengeBossButton` cuando no está
visible) también deja pasar el tap hacia el `TapCatcher` gratis. **Regla para UI nueva que
reciba taps propios**: agregarla en el Canvas DESPUÉS de este bloque (que está justo
después del fondo) — si se agrega antes, `TapCatcher` le robaría los clicks.

**Perfil del jugador** (base para un futuro modo online): botón 👤 junto al ☰, abre
`ProfilePanel` con 3 sub-pestañas (`ProfileTabController`):
- **Info**: nombre y presentación de texto libre (`ProfileInfoPanel`, `InputField` legacy
  uGUI vía el nuevo helper `CreateInputField` en `SceneBuilder`), pensados para cuando
  exista modo online — hoy solo viven en el guardado local. Preview del personaje con la
  skin actual.
- **Estadísticas** (`ProfileStatsPanel`): lee directo los contadores de
  `AchievementManager.GetProgress(...)` (kills, jefes, nivel máximo, oro de por vida,
  Renaceres, héroes/equipo/mascotas conseguidos) — **no hay un sistema de tracking nuevo**,
  esos contadores ya existían y son de por vida (nunca se reinician, ni con Renacer).
- **Skins** (`SkinSwatchButton`, 4 por personaje): cada héroe + cada mascota (el personaje
  principal se saco de aca, ver "Animación del personaje" mas abajo — usa arte real, no
  esto). Las skins son variantes de `PlaceholderArt` (mismo criterio que todo el arte
  placeholder del proyecto): skin 0 = aspecto de siempre, skins 1-3 =
  `PlaceholderArt.Creature(id + "#skinN", ...)` — un id distinto genera una silueta Y
  color distintos (no es un simple recolor), asi que hoy cada skin se ve como "una
  criatura distinta" en vez de "el mismo personaje con otra ropa". Cuando haya arte final,
  sustituir esto por un campo `art` por skin (mismo patrón `art ?? Placeholder` de
  siempre) es un cambio acotado, no una reescritura. `ProfileManager.SkinnedId(id, index)`
  es la fuente de verdad (estática, usable desde SceneBuilder en modo Editor sin
  `ProfileManager.Instance`). `HeroData.Art`/`PetData.Art` ya aplican la skin elegida
  automáticamente — cualquier componente que muestre `hero.Art`/`pet.Art` la respeta sin
  cambios propios, pero **para que un sprite ya en pantalla se actualice cuando el
  jugador CAMBIA de skin en vivo, ese componente tiene que escuchar `SkinChangedEvent` y
  volver a pedir `.Art`** (ver `HeroFieldSprite`/`PetFieldSprite`/`PetRow` — la asignación
  inicial en `SceneBuilder` sola no alcanza, solo cubre el primer frame).

**Animación del personaje principal (arte real, no placeholder):** `Assets/_Project/Art/Player/`
tiene 3 skins de espada (Madera/Hierro/Hierro Negra), cada una con Idle + Ataque1/2/3 (4
cuadros cada animación = 16 por skin) + icono — recortados de una hoja de sprites que dio el
usuario (no generados por PlaceholderArt). `PlayerArtImportSettings` (AssetPostprocessor)
fuerza Sprite/pivote abajo-centro/PPU 32/sin compresión para todo lo que caiga en esa carpeta.
- `WeaponSkinData` (ScriptableObject): icono + los 4 arreglos de sprites. `WeaponSkinManager`
  guarda cual esta elegida (persistido por id, ver `SaveData.weaponSkinId`).
  `WeaponSkinSwatchButton` es el selector en la pestaña Skins del Perfil (fila "Espada"),
  con el icono real del asset — no un preview `PlaceholderArt`.
- `PlayerAnimator` reproduce Idle en loop (10fps) y alterna Ataque1→2→3→1... en cada
  `PlayerTappedEvent`, sin reiniciar si ya hay un ataque en curso (el daño del tap ya se
  aplicó vía `Enemy.OnPointerDown` de cualquier forma — esto es solo el dibujo). Se usa en
  DOS lugares: el `PlayerCharacter` real en juego, y el preview del Perfil (pestaña Info)
  — mismo componente, no una copia, para que el Perfil muestre EXACTAMENTE lo que se ve
  jugando.
- **Fuente actual: hoja de sprites v2** (reemplazó una v1 con recortes imprecisos). Trae
  Idle(4)/Ataque1-3(4 c/u) — lo único que el juego usa — más Walk/Jump/Hit/Death y un panel
  de iconos extra que NO se extrajeron (nada los usa todavía; agregarlos cuando haya un
  gancho real de gameplay, ej. una escena de derrota).
- **Pipeline de corte** (si hay que repetirlo con otra hoja): la v2 SÍ trae canal alpha
  real (a diferencia de la v1, que tenía fondo sólido y hubo que quitarlo por distancia de
  color) — mejor pedirle siempre al usuario que el PNG tenga transparencia real, ahorra
  todo ese paso. Aun así hicieron falta dos vueltas:
  1. Las etiquetas de texto ("ATAQUE 1 (RÁPIDO)"...) viven DENTRO de la banda de fila
     detectada por alpha — hay que arrancar el recorte unos px despues de esa etiqueta, si
     no se cuela en el cuadro (recortarla por altura de blob <40px no alcanza si el corte
     ya arranca ahí).
  2. Las skins con efectos muy conectados (estelas de Hierro/Hierro Negra) hacen que
     `scipy.ndimage.label` fusione cuadros vecinos de verdad (los pixeles se tocan, no es
     un artefacto de dilatación). El primer intento (reusar las columnas X de Madera como
     plantilla para Hierro/Hierro Negra) parecía razonable pero causó un bug real reportado
     por el usuario: se veían "pedazos" de un personaje fantasma en el borde de varios
     cuadros de Ataque1/Ataque2, porque el personaje de Hierro/Hierro Negra no cae exacto
     en la misma columna que el de Madera (pose/espada distinta desplaza un poco el
     contenido) — la ventana de recorte, aunque fija, terminaba agarrando el borde del
     cuadro vecino. **Arreglo real**: cortar cada skin por SUS PROPIOS blobs (no una
     plantilla ajena), y — mas importante — recortar por **mascara de label**, no por bbox:
     dos labels distintos pueden tener bounding boxes que se superponen (un espadazo en
     forma de media luna cruza en X el rango del cuadro vecino sin tocarlo en pixeles), así
     que agarrar "todo lo no-transparente dentro de la ventana" sigue coloandose contenido
     ajeno — agarrar "solo los pixeles de ESTE label dentro de la ventana" no. Para blobs
     que de verdad son 2+ personajes fusionados en un solo label (Hierro Negra especialmente
     — su estela conecta hasta 5 cuadros en un blob), no queda otra que partir en partes
     iguales (recortando cada parte a su propio contenido sin cruzar el reparto); Madera no
     necesita esto (sus blobs anchos son un personaje + una estela grande, partirlos cortaría
     al personaje al medio) — hay que verificar a ojo cuál caso es cada blob, no asumir.
  3. **"Partir en partes iguales" todavía puede fallar si el CONTEO de cuántas partes
     tiene cada blob se estima mal** — bug real reportado por el usuario ("los sprites de
     Hierro Negra se ven cortados, la animación se ve mal"). El conteo se estimaba con
     `round(ancho_del_blob / 95)` (95 = ancho promedio de un cuadro) — para Hierro Negra
     específicamente, un blob de Ataque3 se sobre-estimó en cuadros, lo que corrió el
     límite de "tomar los primeros 12" y dejó el contenido REAL de Ataque3 afuera (un
     cuadro terminaba mostrando solo un fragmento negro suelto, no el personaje).
     **Arreglo**: en vez de contar por blob, usar 3 ZONAS X FIJAS (medidas de Madera, la
     única sección que separa limpio) — Ataque1/2/3 son siempre 4 cuadros cada uno sin
     importar cuántos blobs originales crucen esa zona. Esto solo hizo falta para Hierro
     Negra (Hierro, con el mismo método de conteo por blob, daba justo 12 — verificado
     antes de aplicar el mismo parche a los tres, que hubiera sido innecesario).
  4. **Los 4 tipos de animación (Idle/Ataque1/2/3) tienen que compartir UN SOLO lienzo
     final, no uno por tipo.** La primera vuelta de este bug (Idle se veía diminuto) se
     "arregló" dándole a cada tipo su propio lienzo ajustado a su propio contenido — pero
     eso rompe `Image.preserveAspect`: como cada tipo terminaba con una relación de aspecto
     distinta (Idle ~0.87, Ataque3 ~1.9), Unity escala cada uno con un factor DISTINTO para
     entrar en el mismo cuadro fijo, y el personaje cambiaba de tamaño/se "alejaba" al
     atacar (segundo bug real reportado, esta vez por verse mas chico en vez de mas grande).
     El arreglo correcto es al revés: TODOS los cuadros (Idle + los 3 ataques, de las 3
     skins) van al mismo lienzo, tan ancho como el espadazo mas amplio lo necesite, con el
     personaje anclado abajo-izquierda (nunca centrado — el personaje mira a la derecha y
     el espadazo se extiende hacia ese lado, así que centrar movería al personaje de lugar
     entre cuadros). Con un solo lienzo, el factor de escala de `preserveAspect` es SIEMPRE
     el mismo, así que el personaje se ve exactamente igual de grande en reposo y en pleno
     swing. El contenedor (`PlayerCharacter`/`CharacterPreview` en `SceneBuilder.cs`) tiene
     que dejar de ser cuadrado y adoptar la proporción real del lienzo (quedó en 210x110,
     no 110x110, para un lienzo de ~185x97) — una caja cuadrada aplasta un lienzo mucho mas
     ancho que alto.
  5. **El arreglo del punto 3 (3 zonas X fijas, medidas de Madera) seguía roto — bug real
     reportado por el usuario otra vez ("la espada oscura sigue cortada, todo feo") después
     de darlo por resuelto.** Causa raíz: Madera y Hierro Negra NO tienen el mismo espaciado
     de personajes en su fila de ataque — una zona fija calcada de Madera no cae en los
     mismos límites reales de Hierro Negra, así que seguía colando texturas de un personaje
     vecino en varios cuadros (Ataque1/2/3) y, en un caso, la zona ni siquiera caía sobre
     contenido real (`ataque3_02` salía casi vacío, solo un fragmento del espadazo, sin
     personaje). **Arreglo real (esta vez sí, verificado a ojo cuadro por cuadro)**: medir
     los límites de los 12 cuadros DIRECTO sobre la fila de Hierro Negra (nunca reusar
     medidas de otra skin) — la fila real trae 12 personajes bien definidos más una estela
     enorme (el "espadazo giratorio" de Ataque3, cuadro 3) que no es un cuadro aparte, es
     el efecto de ESE cuadro. Los límites se ubicaron como mínimos locales de contenido
     (alpha) entre cabezas detectadas — no por conteo de blob ni por ancho promedio (ambos
     métodos ya habían fallado dos veces). El cuadro con el espadazo grande (Ataque3 #3)
     terminó midiendo 210px de ancho, más que el lienzo compartido de las otras 2 skins
     (185px) — **agrandar el lienzo de Hierro Negra a 210x98 sin agrandar la caja de
     `PlayerCharacter` hubiera vuelto a achicar el personaje ~11% al equiparla** (mismo bug
     del punto 4, esta vez entre skins distintas en vez de entre cuadros de la misma skin) —
     por eso `playerRect.sizeDelta` subió de 210 a 238 (`210 × 210/185`), para que el alto
     siga siendo el límite de escala en las 3 skins por igual. Los 4 cuadros de Idle de
     Hierro Negra se re-generaron también (mismo contenido, solo repadeados al lienzo nuevo
     de 210x98) — un skin no puede tener un lienzo distinto entre su Idle y sus Ataques, eso
     reintroduciría el bug del punto 4 pero DENTRO de la misma skin.
  Herramientas: Python 3.13 + Pillow + numpy + scipy, instalados con pip
  (`/c/Python313/python.exe` — `python`/`python3` no resuelven directo en este entorno).
- `PlayerAnimator`: Idle a 10fps, ataques a ~17fps (`AttackFrameDuration`) — a 10fps el
  swing se sentía lento/pastoso para lo seguido que se dispara en un tapper.
- **Posición en pantalla — `PlayerCharacter` centrado en X, parado en `BottomPanelHeightFraction`
  (constante local en `SceneBuilder.BuildGameScene()`, hoy 0.28)**: pedido del usuario,
  varias vueltas. Primero se paró en X=0.18 (izquierda) — el usuario pidió centrarlo (X=0.5,
  mismo eje que `EnemyArea`, que ya estaba centrado — "protagonista abajo, monstruo arriba").
  El Y tampoco es fijo a ojo: es la MISMA constante que define el alto de `BottomPanel` (el
  "menú de mejoras"), así el personaje siempre queda parado justo en el borde superior del
  panel, cualquiera sea su altura. Esa altura bajó de 0.34 a 0.28 porque cada fondo
  (`Art/Backgrounds/`) tiene una franja MÁS PLANA (sin pasto/rocas, más simple) que la franja
  de "piso" completa — el usuario señaló que quería los menús ahí específicamente, no en
  toda la franja de piso.
- **`BottomTabController.expandedAnchorY` — vueltas sucesivas hasta dejar aire real entre el
  panel expandido y el personaje.** Empezó en 0.34, desincronizado de
  `BottomPanelHeightFraction` (0.28 en `SceneBuilder`, la misma constante que fija donde
  paran los pies de `PlayerCharacter`) — el borde del panel quedaba por ENCIMA de los pies,
  tapándolo (reportado: "la pestaña tapa al héroe"). Primer arreglo: igualar a 0.28 (borde
  del panel = pies exactos). El usuario reportó que **seguía sin alcanzar**: no basta con que
  el borde del panel toque los pies, hay que dejar los CUADROS de mejora (adentro del panel,
  con su propio margen para el tab bar — ver `upgradesPanelRect`/`skillRowRect` en
  `SceneBuilder`) claramente por debajo del personaje COMPLETO, no solo de sus pies. Valor
  final: **0.22** — dos ajustes en la misma dirección para el mismo pedido, ninguno de los
  dos "un solo punto de verdad" (`expandedAnchorY` vive en el componente runtime,
  `BottomPanelHeightFraction` en el Editor) así que si alguno de los dos vuelve a cambiar,
  revisar el otro a mano y probar en el Editor antes de darlo por bueno — la geometría en
  papel no bastó para predecir el resultado real esta vez.
- **`PlayerAttackEffect`**: efecto que aparece sobre el enemigo en cada `PlayerTappedEvent`
  (pedido del usuario: "que parezca que si le estamos dando" — antes el personaje atacaba
  sin ningún efecto conectando con el enemigo). Canvas Screen Space Overlay, así que
  `RectTransform.position` YA es posición de pantalla — no hace falta convertir desde
  anclas. Color por skin de espada: morado para `espada_hierro_negra` (a tono con su estela
  de ataque, pedido explícito), **blanco** para el resto (antes dorado — "blanco" pedido
  explícito).
  - **v1 (reemplazada): una sola barra recta (`SolidPixel` estirado) viajando de extremo a
    extremo.** El usuario reportó que "parece más un disparo que un espadazo" — una barra
    recta que viaja en línea perfecta se lee como proyectil/bala, no como un corte de espada.
  - **v2 (reemplazada): barra + `SlashHit` curvo.** Se agregaron 2 `WindStreak` finas
    viajando de origen a destino ANTES del tajo, para sostener la sensación de que el golpe
    "recorre" esa distancia. El usuario reportó que esa línea "se ve rara" y que el tajo
    solo ya alcanza para dar a entender el golpe — se sacó por completo, no hacía falta.
  - **v3 (actual): solo `SlashHit`, sin viaje.** Un breve delay (`SlashDelay`, sincroniza
    con el swing del arte) y aparece DIRECTO sobre el objetivo — usa
    `PlaceholderArt.SlashArc()` (textura procedural: núcleo brillante en forma de tajo
    curvo, afinado hasta desaparecer en las puntas), gira rápido (ease-out) mientras crece
    de escala 0.55→1.15, y se desvanece. Más grande que v2 (`distance*0.65`, tope 190 en vez
    de `distance*0.55`/150). **Alterna entre 3 variantes** (`Variants[]`, un índice de
    instancia que avanza en cada tap) de ángulo de inicio/fin y espejado horizontal — pedido
    explícito ("variaciones intercaladas... para que se vea más genial") para que no sea
    siempre el mismo corte.

**Fondos de escenario (arte real, no placeholder):** `Assets/_Project/Art/Backgrounds/`
tiene 20 fondos (`bg_01.png`..`bg_20.png`), uno por jefe vencido. Si la carpeta está vacía
(proyecto sin ese arte todavía), cae automáticamente al paisaje procedural de siempre
(`PlaceholderArt.MountainLandscape`) — mismo patrón `?? PlaceholderArt...` que el resto del
proyecto. `BackgroundArtImportSettings` (AssetPostprocessor) fuerza Sprite/PPU 32/filtro de
punto/sin compresión para esa carpeta (pivote se deja en el default — el fondo se ESTIRA
para llenar la pantalla, `preserveAspect=false`, así que el pivote no afecta cómo se ve, a
diferencia de Art/Player/).
- **Orden de aparición — `ShuffleBag` (`Core/ShuffleBag.cs`), no módulo simple.** Pedido
  explícito del usuario: "que uno no vuelva a salir hasta que ya hayan salido todos, para
  mantener variedad". Es una clase pura (sin `UnityEngine.Random` ni `MonoBehaviour`) a
  propósito, para poder testearla sin instanciar nada de Unity — ver
  `ShuffleBagTests.cs`. Devuelve los 20 índices en orden aleatorio sin repetir ninguno
  hasta agotarlos, ahí barajea de nuevo. `BackgroundController` la usa en vez del módulo
  simple que tenía antes.
- **El progreso se guarda** (`SaveData.backgroundChapter/backgroundOrder/backgroundPosition`,
  cargado/guardado en `GameManager`) — bug real encontrado mientras se investigaba el
  reporte del usuario: antes NO se guardaba nada, así que cerrar y reabrir la app siempre
  volvía a arrancar en el fondo 1. Con partidas cortas (típico en un idle game que se abre
  y cierra seguido) casi nunca se llegaba a ver variedad real — esa era la causa de fondo
  del reporte, más que el orden secuencial en sí. `BackgroundController.LoadState(...)`
  también aplica de una el fondo correspondiente al progreso restaurado (sin fade) — sin
  esto la pantalla se quedaba mostrando el fondo 1 (el default de `SceneBuilder`) hasta el
  PRÓXIMO jefe vencido, aunque el progreso real ya fuera mucho más alto.
- **Proporción real vs. resolución de referencia del juego**: los 20 fondos vienen recortados
  a 176x404 (relación ~0.436 — más angostos/altos que los 720x1280, relación 0.5625, del
  juego). Se decidió a propósito NO cambiar `GameConstants.ReferenceWidth/Height` ni el
  `CanvasScaler` para esto — el fondo ya se estiraba para llenar pantalla desde antes
  (`preserveAspect=false`), así que usarlos tal cual solo significa que se ven un poco más
  anchos de lo que el arte original pretendía, sin ningún riesgo. La alternativa (recalcular
  la resolución de referencia real del juego) hubiera obligado a re-revisar a mano el tamaño
  en píxeles de prácticamente cada elemento de `SceneBuilder.cs` sin poder verificarlo en
  vivo — se le preguntó al usuario y eligió la opción de cero riesgo.
- **Pipeline de corte**: la hoja original (`ChatGPT Image 17 ago 2026, 02_15_23 p.m..png`,
  941x1672) es una grilla de 5x4 paneles con líneas negras separadoras y una etiqueta de
  número ("01".."20") en la esquina superior izquierda de cada uno — límites de grilla
  ubicados por mínimos de brillo fila/columna (`row.mean(axis=(1,2))`), no por un tamaño de
  celda fijo (las celdas variaban 174-177 x 396-409px, quedaron normalizadas a 176x404 al
  final). La etiqueta se tapa con un parche clonado de la imagen misma (no hay librería de
  inpainting instalada) — **clonar HORIZONTAL (mismo Y, X vecino) anda mucho mejor que
  clonar hacia abajo** para cielos con degradado vertical (clonar hacia abajo agarra un tono
  de un punto distinto del degradado y deja una costura visible; horizontal preserva la
  altura exacta del degradado). Un panel con luz radial simétrica (rayos convergiendo al
  centro) necesitó un tercer método — espejar la esquina opuesta (`ImageOps.mirror`) — porque
  tanto clonar hacia abajo como hacia el costado agarraban un tramo de rayo con brillo
  distinto al de la esquina tapada; verificar la hoja de contacto completa a ojo antes de dar
  por buena cualquier técnica de parche, no asumir que la misma sirve para las 20.

**Retratos de héroes (arte real, no placeholder):** `Assets/_Project/Art/Heroes/` tiene 4
retratos (uno por héroe, id de archivo = nombre del personaje). Los 4 héroes actuales,
reemplazando a los 3 placeholder originales (Escudero Tomas/Arquera Lyra/Monje Berrin —
esos 3 `.asset` se borraron, no quedan huérfanos en `Data/Heroes/`):
- `alice_veloz` — Alice, "La Veloz" (tier 1, la más barata)
- `callu_bueno` — Callu, "El Bueno" (tier 2)
- `velaryon_justo` — Velaryon, "El Justo" (tier 3)
- `ruijerd_dragon` — Ruijerd, "El Dragón" (tier 4, la más cara/fuerte)

`HeroData.art`/`attackArt` pasaron de `[SerializeField] private` a **públicos** (mismo
criterio que `WeaponSkinData`): `CreateInitialData.CreateHero(...)` tiene parámetros
opcionales `artPath`/`attackArtPath` que asignan el sprite directo por
`AssetDatabase.LoadAssetAtPath<Sprite>` sin reflection — no hacía falta agregar un helper
tipo `AssignSerialized` (ese vive en `SceneBuilder.cs`, en modo Play/build de escena) solo
para esto. `HeroArtImportSettings` (AssetPostprocessor) fuerza Sprite/pivote abajo-centro/
PPU 32/filtro de punto — mismo criterio que `PlayerArtImportSettings`, los héroes tampoco se
estiran (a diferencia de los fondos). La hoja original (`ChatGPT Image 17 ago 2026,
02_53_59 p.m..png`, 1024x1536, alpha real) traía 5 personajes con 2-3 poses cada uno (13
blobs en total, separados limpio por `scipy.ndimage.label` — sin bleeding esta vez, los
personajes no se tocan entre si en la hoja); de cada uno se usaron 2 poses: una "de pie"
(`_idle.png`) y una dinámica (`_attack.png`, la más "en acción" — Ruijerd escupiendo fuego,
Velaryon/Alice en pleno ataque, Callu con la espada desenvainada). Las poses restantes de
cada personaje (agachado, otra variante de ataque) no se usaron; quedan en la hoja original
en Downloads.
- **`ruijerd_attack.png` traía huecos blancos en el aliento de fuego (reportado por el
  usuario)** — verificado que NO es un bug de mi recorte: la hoja original ya tenía esos
  huecos (pixeles con alpha=0 genuino, muestreados a mano en la fuente), más un segundo
  problema encontrado al zoomear el primer intento de arreglo: parches de color OPACO
  casi-blanco (no transparente) dentro de la llama, un defecto de generación distinto que
  tampoco se veía a simple vista hasta hacer zoom. Arreglo en 2 pasadas: (1) detectar
  huecos por `binary_closing` (kernel 10x10, para juntar muescas que tocan el borde de la
  llama y que `binary_fill_holes` solo no agarra) + `binary_fill_holes`; (2) detectar los
  parches casi-blancos por COLOR (`alpha>100 and R>170 and G>150 and B>150` — verificado que
  esto no toca al dragón en sí, cero falsos positivos con `x<300`). Ambos grupos de pixeles
  "malos" se rellenan por vecino-mas-cercano-de-color-bueno
  (`scipy.ndimage.distance_transform_edt(~good, return_indices=True)`), no por vecino más
  cercano sin filtrar — si no, un hueco cerca de un parche blanco copiaba blanco de vuelta.
- **Pedestales (`Assets/_Project/Art/HeroBases/`)**: pedido del usuario ("que los héroes
  ayudantes estén parados sobre bases"), recortados de una hoja de props de biomas
  (`Gemini_Generated_Image_bqvhobqvhobqvhob.jpg`, 768x1365, grilla 5x4 de 20 celdas
  numeradas en el MISMO orden que los 20 fondos). No es transparencia real (JPEG, RGB puro)
  — el "tablero" de fondo está literalmente pintado en dos grises neutros; se les quitó por
  color (`abs(R-G)<10 and abs(G-B)<10 and abs(R-B)<10`, dos rangos de brillo) en vez de por
  alpha, y se filtró por **blob conexo más grande** (8-conectado) para descartar fragmentos
  de íconos vecinos que asoman en el recorte. Se eligieron 6 (uno por `heroFieldSlot`, fijo
  por posición, no por bioma — los héroes no están atados a un fondo en particular) de
  material variado y silueta plana ("que interfieran menos con el ambiente" — se
  descartaron los íconos de escena completa, picos de cristal muy altos, y rocas
  redondeadas sin tapa plana): tocón (`base_stump`), bloque de hielo (`base_ice`), bloque de
  arena (`base_sand`), roca flotante con lianas (`base_float`), cubo de piedra oscura
  (`base_stone`), roca agrietada del desierto (`base_crack`). `HeroBaseArtImportSettings`
  (AssetPostprocessor) — mismo criterio que Heroes/Pets. En `SceneBuilder.cs`, cada
  `HeroBase_i` es un HERMANO de `HeroField_` (no un hijo) creado ANTES en el mismo padre
  para dibujarse DETRÁS — un hijo se dibujaría ENCIMA porque el sprite del héroe vive en el
  root de `HeroField_`, no en un hijo suyo. Posición: mismo ancla que el héroe, pivote
  arriba-centro con `anchoredPosition.y = -fieldHeight/2` (la mitad de la caja del héroe,
  centrada verticalmente en su ancla) para que el borde superior de la base quede justo a la
  altura de los pies. Visibilidad ligada al héroe: `HeroFieldSprite` tiene un
  `CanvasGroup baseGroup` opcional que alterna junto con el del héroe (invisible hasta
  comprarlo).
- **Animación**: `HeroFieldSprite` (el sprite del héroe alrededor del monstruo en batalla —
  NO el retrato estático de la lista en la pestaña Héroes, ese se queda quieto en `Art`)
  alterna `Art`/`AttackArt` en loop continuo mientras el héroe esté comprado, con un offset
  inicial aleatorio por héroe para que no ataquen los 4 en perfecta sincronía. A propósito
  NO está sincronizado al tick de daño real de `HeroRoster` (ese aplica el DPS COMBINADO de
  todos los héroes una vez por segundo, no hay un evento por-héroe-individual) — sincronizarlo
  hubiera significado agregar un evento nuevo a `HeroRoster`/`EventBus` solo para esto; un
  loop propio por héroe logra el mismo efecto visual pedido ("ataca, vuelve a pie, repite")
  sin tocar la lógica de combate real.
- **Mismo bug de "se aleja" que ya mordió al personaje principal, esta vez en los héroes**:
  idle y ataque de cada héroe NO comparten canvas de origen (ataque se extiende mucho más
  ancho — ej. Ruijerd: idle 261x207, ataque 572x195) — con `Image.preserveAspect` y una caja
  fija, alternar entre ambos hace que el personaje cambie de tamaño en pantalla. Mismo
  arreglo que la vez anterior: normalizar idle+ataque de cada héroe a un lienzo compartido
  (ancla abajo-izquierda, el personaje no se mueve, el canvas se extiende a la derecha) ANTES
  de que el bug llegara a reportarse.
- **Tamaño inconsistente ENTRE héroes (bug real, reportado después)**: con una caja fija
  igual para los 4 (probado primero con 100x64), el héroe con el lienzo compartido más
  ANCHO (Ruijerd — su aliento de fuego estira el lienzo mucho más que a los demás) quedaba
  limitado por ancho en vez de por alto, y se renderizaba más CHICO que el resto pese a ser
  conceptualmente el más grande. Arreglo: en `SceneBuilder.cs`, cada héroe recibe un
  `sizeDelta` calculado individualmente (alto fijo por héroe — 100 para Ruijerd, 64 para el
  resto — × la proporción real de `hero.Art.rect`), así el ALTO es siempre el límite para
  los 4, y se ven consistentes entre si sin importar cuánto se estire cada lienzo hacia los
  costados.
- **Los héroes miran/atacan siempre hacia la DERECHA** (mismo criterio que el personaje
  principal) — los slots de `heroFieldSlots` con x>0.5 (lado derecho del monstruo) quedaban
  de espaldas a él sin corrección. `SceneBuilder.cs` espeja el `RectTransform` completo
  (`localScale.x = -1`, no un sprite duplicado) para esos slots — de paso espeja el ícono de
  arma hijo también (aceptable, es un detalle menor).
- **Bug real, reportado después del fix de tamaño anterior: Ruijerd quedaba con "la mitad
  del cuerpo afuera de la pantalla"**. Causa: su caja quedó mucho más ancha que las de los
  demás (`sizeDelta≈276x100`, ver punto anterior) pero seguía centrada (pivote default
  0.5,0.5) sobre un slot pegado al borde izquierdo (`heroFieldSlots[3]=(0.10, 0.50)`, donde
  cae por ser el héroe más caro tras el orden por costo) — la mitad de esa caja ancha caía
  en X negativo, fuera de pantalla. Arreglo: `heroFieldRect.pivot = (0, 0.5)` para TODOS los
  héroes (la caja crece hacia la derecha desde su ancla) — combinado con el flip existente
  (`localScale.x=-1` para slots x>0.5), el flip espeja la caja alrededor de ese mismo
  pivote, así que para CUALQUIER héroe, en cualquier lado, la caja siempre crece hacia el
  CENTRO de la pantalla, nunca hacia el borde más cercano — se resuelve para el dragón y
  queda a prueba de que algún héroe futuro tenga una caja ancha en un slot de borde.
- **`heroFieldSlots` reordenado — alturas dispares entre lados (bug real, reportado: "hay
  unos más abajo y otros más arriba")**. Los 6 slots son 2 parejas simétricas
  izquierda/derecha (misma Y cada una) más 2 sueltos de altura única — pensados para cuando
  haya un 5to/6to héroe. Con exactamente 4 héroes (el roster actual), la asignación por
  índice (0,1,2,3) agarraba 1 slot suelto + una pareja completa + LA MITAD de la otra pareja
  (sin su contraparte), así que los 4 quedaban a alturas distintas en vez de emparejados.
  Arreglo: reordenar el array para que los primeros 4 slots sean las 2 parejas completas —
  los sueltos quedan al final (índices 4/5). También se subió `fieldHeight` de los 3 héroes
  que no son el dragón (64→76, "un poquitín más grandes", pedido explícito, dejando al
  dragón como el más grande de los 4 igual que antes).

**Retratos de mascotas (arte real, no placeholder):** `Assets/_Project/Art/Pets/` tiene 5
retratos, reemplazando el roster placeholder original (Lobezno/Dracónido Bebé/Búho Sabio/
Slime Real/Hada Luminosa/Grifo Menor — esos 6 `.asset` se borraron):
- `fenrir` — Fenrir (lobo, TapDamage, ataca sola)
- `kitsune` — Kitsune (zorro, Gold)
- `thing` — Thing (golem, HeroDps, ataca sola)
- `valeryon_pet` — Valeryon (dragón — id con sufijo `_pet` a propósito: el héroe
  `velaryon_justo` ya usa un id parecido, hay que evitar que coincidan)
- `khaos` — Khaos (guerrero oscuro, HeroDps, ataca sola)

Mismo criterio que `HeroData`: `PetData.art`/`attackArt` son **públicos** (no
`[SerializeField] private`), `CreateInitialData.CreatePet(...)` tiene parámetros opcionales
`artPath`/`attackArtPath`, y `PetArtImportSettings` (AssetPostprocessor) fuerza Sprite/pivote
abajo-centro/PPU 32/filtro de punto. La hoja original (`ChatGPT Image 17 ago 2026,
11_00_07 p.m..png`, 1536x1024, alpha real) trae 5 personajes en grilla 5x5 (5 poses cada
uno: reposo, alerta, caminata, acción, y acción+efecto grande). La detección de blobs
globales fallaba para esta hoja (los efectos de la última columna —aliento, explosión, tajo
de viento, brillo de arma— se fusionan con celdas vecinas y hacen desaparecer los huecos
entre columnas en la suma global); se resolvió recortando primero BANDAS de fila fijas
(medidas a ojo de los huecos reales) y recién ahí separando blobs por columna dentro de cada
banda — mismo problema de fondo que el zoneo de Hierro Negra, solución análoga.
- **Animación (pedido posterior del usuario: "que estén animadas... para que se vean
  vivas")**: se agregó una segunda pose por mascota (`attackArt`, columna 4 de la hoja — la
  pose "en acción" SIN el efecto grande de la columna 5, mismo criterio que se usó para
  elegir la pose de ataque de los héroes). Columna 1 (reposo) sigue siendo `art`. Cada
  mascota normaliza su propio idle+attack a un lienzo compartido (ancla abajo-izquierda,
  mismo arreglo del bug de "se aleja" ya aplicado a jugador y héroes). `PetFieldSprite`
  alterna `Art`/`AttackArt` en loop continuo con offset inicial aleatorio, igual que
  `HeroFieldSprite.AttackLoop()`. **Ya NO tiene vaivén en Y** — la primera versión sumaba un
  seno en `Update()` (mismo patrón que el `PetRow` de antes de tener animación real) para
  que "se viera viva"; una vez que el cambio de sprites idle/ataque hizo ese trabajo, el
  usuario pidió sacar el vaivén ("que ya no se muevan de arriba a abajo... así no parece que
  levitan") — la animación de sprites sola ya alcanza, el bob de más se veía como si
  flotara.
- **Posición: a los lados del personaje, en el piso — hasta 2 mascotas a la vez** (pedido
  posterior: primero "que estén abajo a un lado del héroe" — antes flotaba sola en
  `(0.30, 0.53)`, lejos de donde quedó el personaje tras bajarlo al piso — y después
  "que puedas agregar hasta 2 mascotas... una a cada lado"). `SceneBuilder` crea DOS
  `PetField_0`/`PetField_1` (antes uno solo), mismo ancla Y que `PlayerCharacter`
  (`BottomPanelHeightFraction`, pivote abajo) con offset fijo en X vía `anchoredPosition`
  (`-180`/`+180` — subido desde `-150`/`+150` porque `PlayerCharacter` se ensanchó a 238 para
  el arreglo de Hierro Negra y la mascota izquierda quedó invadiendo su borde, reportado por
  el usuario; no fracción de ancla, para no depender de la resolución de referencia).
  Cada `PetFieldSprite` tiene un campo `slot` (0 o 1, asignado por `SceneBuilder` vía
  `AssignSerializedInt`) y le pregunta a `PetManager.GetActiveSlot(slot)` qué mascota le
  toca mostrar — ya no existe un único "ActivePet" global.
  - **`PetManager`**: `PetData ActivePet` (1 slot) pasó a `PetData[] activeSlots` (2 slots) +
    `float[] attackTimers` (cada mascota con ataque propio tiene su propio cooldown
    independiente, antes uno solo compartido). `SetActive(pet)` ahora ALTERNA: si la
    mascota ya está activa la desequipa (libera su lado); si no, la equipa en el primer
    lado libre, o reemplaza el lado 0 si los 2 están ocupados — no hay UI para elegir lado a
    mano, el usuario solo pidió poder tener 2 a la vez. `IsActive(pet)`/`GetActiveSlot(i)`
    reemplazan al viejo getter `ActivePet`. `SaveData`: `PetSaveEntry.isActive` (bool) pasó a
    `activeSlot` (int, -1/0/1) — cambio de esquema sin migración (proyecto en desarrollo,
    sin usuarios reales todavía; ver el resto de este archivo, no hay sistema de versionado
    de saves en ningún otro lado tampoco).
  - **Mirando al jefe según el lado** (pedido explícito: "que dependiendo de en que espacio
    se ponga la mascota se voltee la imagen para quedar viendo al jefe"): mismo criterio que
    `heroFieldSlots` — el arte de mascota mira a la derecha por defecto, así que el slot 1
    (derecha) se espeja (`localScale.x=-1`) para que ambas mascotas queden mirando hacia el
    CENTRO (donde está el héroe/jefe) en vez de una de ellas mirando hacia afuera.

**Iconos de habilidades (arte real, no `PlaceholderArt.Weapon`):** `Assets/_Project/Art/Skills/`
tiene 5 iconos (uno por `SkillData`, id de archivo = id del skill), recortados de una hoja
de 5 cartas (`Gemini_Generated_Image_pw8clgpw8clgpw8c (1).jpg`, 784x1367, JPEG sin alpha
real — mismo tratamiento por color que `HeroBases`: quitar el tablero de fondo por color,
quedarse con el blob conexo más grande). A diferencia de `HeroBases`, cada carta ya trae su
propio marco/fondo diseñado (no hace falta lienzo transparente completo) — el recorte es
básicamente "encontrar el borde de la tarjeta cuadrada", más simple que los props sin marco.
Dos cartas tienen texto en español HORNEADO en la imagen (`golpe_certero`=
"Espadazo Super-Potente", `lluvia_flechas`="Ataque de Compañeros") — asignadas por lo que
MUESTRAN (efecto), no por el nombre viejo del skill (`lluvia_flechas` ya no tiene nada que
ver con flechas, pero el ícono real es "ataque de todos los compañeros a la vez" que es
justo lo que hace `SkillKind.InstantHeroBurst`). `SkillData.icon` pasó de
`[SerializeField] private` a **público** (mismo criterio que Hero/Pet/WeaponSkinData),
`CreateInitialData.CreateSkill(...)` tiene un parámetro opcional `iconPath`.
`SkillArtImportSettings` (AssetPostprocessor) — mismo criterio que el resto, sin pivote
especial (son íconos cuadrados de UI). `SkillUpgradeButton` ganó un campo `icon` (antes solo
mostraba texto) — en `SceneBuilder.cs`, cada fila de habilidad ahora tiene un `Image` de
28x28 a la izquierda con `skill.Icon`, y el label le deja hueco (`offsetMin.x=36`).

**Iconos de pestañas y botones (arte real):** `Assets/_Project/Art/TabIcons/` tiene 8
iconos, recortados de una segunda hoja de cartas (`Gemini_Generated_Image_48pn1648pn1648pn
(1).jpg`, 784x1368, JPEG sin alpha real — mismo tratamiento por color que Skills/HeroBases):
`quests` (pergamino+poción, sin texto horneado), `achievements` ("LOGROS"),
`settings` (engranajes, sin texto horneado), `upgrades` ("MEJORAS"), `heroes` ("HEROES"),
`equipment` ("EQUIPO"), `pets` ("MASCOTAS"), `prestige` ("PRESTIGIO"). Rutas explícitas en
`SceneBuilder.cs` (no `FindAssets`+orden) — cada archivo va a un botón específico, no una
lista intercambiable.
- **Bug de extracción propio de esta hoja**: a diferencia de Skills (donde el texto vive
  ADENTRO del marco, tocando los mismos píxeles), las 6 cartas de pestañas tienen su label
  ("MEJORAS"...) FUERA del marco, sin tocarlo — filtrar por "blob conexo más grande" (la
  técnica que venía usándose desde HeroBases) descartaba el label ENTERO por estar
  desconectado del marco (visto en vivo: el ícono aparecía sin su palabra). Arreglo: esas 6
  no se filtran por blob — se quedan con TODO el contenido no-tablero de su ventana (ya
  ajustada fila/columna a mano, así que el riesgo de colar algo del vecino es bajo);
  `quests`/`achievements`/`settings` sí siguen filtrándose por blob más grande (su contenido
  es un solo blob conectado, filtrar solo limpia artefactos sueltos del borde).
- **No todos los botones se tratan igual** — depende de la forma real del botón:
  - Pestañas de `BottomTabController` (`CreateTabButton`, parámetro `icon` nuevo, opcional):
    cuadradas, y las 5 cartas ya traen su nombre horneado — el ícono REEMPLAZA fondo de
    color Y el `Text` (mostrar los dos repetiría la palabra). Sigue siendo `null` para las
    sub-pestañas del Perfil (Info/Estadísticas/Skins) — sin ícono nuevo para esas, quedan
    como texto plano de siempre.
  - `QuestsButton`/`AchievementsButton` (TopBar, ~58x82, cuadrado-ish): mismo criterio,
    ícono reemplaza fondo+texto — salvo Logros, que necesita mostrar un contador DINÁMICO
    (no se puede hornear un número). Se achicó a solo "X/Y" (antes "Logros: X/Y", ahora
    redundante con el ícono) en una franja angosta abajo del ícono, no superpuesto a todo el
    botón.
  - `SettingsButton` (pausa, barra ANCHA 80%x26%, no cuadrada): el ícono NO reemplaza el
    botón entero — con `preserveAspect` en una barra tan ancha quedaría diminuto y centrado
    en un mar de espacio vacío. Se agregó chico (36x36) a la izquierda, acompañando el texto
    "Ajustes" (que se mantiene) — mismo patrón visual que `PetRow`/`EquipmentRow`.
  `TabIconArtImportSettings` (AssetPostprocessor) — mismo criterio que el resto.

**Segunda vuelta de iconos — listones anchos (reemplazan a los cuadrados de arriba):** el
usuario pidió reemplazar los 8 iconos cuadrados recién agregados por una nueva hoja
(`ChatGPT Image 18 ago 2026, 11_22_45 p.m..png`, 1536x1024, **PNG con alpha real** — a
diferencia de las hojas anteriores no hizo falta quitar tablero por color, mucho más simple)
con formato "listón" horizontal (ícono a la izquierda + banner de texto a la derecha,
aspecto ~2.7:1) en vez de tarjeta cuadrada. Mismos 8 archivos, mismos nombres
(`Art/TabIcons/quests.png`, etc. — se sobrescribieron, no quedan los cuadrados viejos) más
uno nuevo: `exit.png` ("Salir", pedido explícito: "usa la de salir que no teníamos" — antes
NO existía un botón para cerrar la app desde el menú, solo el botón de atrás de Android que
abre pausa).
- **La hoja trae 18 iconos en total (grilla 3x6); solo se usaron 9** (los 8 que ya existían
  + Salir). Los otros 9 (Tienda, Eventos, Inventario, Clasificación, Mapa, Guía, Amigos,
  Correo, Arena) son para funciones que todavía no existen en el juego — el usuario dijo
  "los guardaremos para el futuro", así que quedan sin recortar; el archivo original sigue
  en Downloads para cuando haga falta alguno.
- **El aspecto ancho (~2.7:1) casualmente coincide con la proporción de las pestañas del
  panel inferior** (144x52 = 2.77:1) — no hizo falta ajustarles la caja, encajan solas.
  TopBar (`QuestsButton`/`AchievementsButton`) SÍ se ajustó: antes eran cajas portrait
  (0.18-0.82 de alto, ~0.7:1) pensadas para las tarjetas cuadradas viejas — con un listón
  horizontal ahí quedaban diminutos (limitados por ancho). Se acortaron a 0.34-0.66 de alto
  y se ensancharon (Misiones 0.16-0.30, Logros 0.70-0.98) para que el listón entre casi
  completo.
- **`AchievementCounterDisplay` — el contador "X/Y" se movió AFUERA del botón** (antes
  superpuesto en una franja abajo del ícono cuadrado). Con el listón usando casi toda la
  altura del botón, superponer texto ahí se veía amontonado; ahora es un `Text` hermano en
  `topBar`, en la franja libre justo debajo del botón.
- **`CreateMenuButton` ganó un parámetro `icon` opcional** (mismo patrón que
  `CreateTabButton`): con ícono, reemplaza fondo+texto; sin él (el default — casi todos los
  demás botones de pausa/ajustes/créditos/confirmaciones que usan este helper), sigue
  exactamente igual que antes. El panel de Pausa pasó de 2 franjas (Continuar/Ajustes) a 3
  iguales (+ Salir) para hacerle lugar.
- **`PauseMenuController.QuitButton`** — nuevo. `Application.Quit()` no hace nada en el
  Editor (limitación de Unity), así que el handler tiene un `#if UNITY_EDITOR` que para el
  Play Mode en su lugar, para poder probarlo sin hacer un build. No llama a `SaveNow()` a
  mano — `GameManager.OnApplicationQuit()` ya guarda solo.

**Iconos de habilidades — 2 tamaños (reemplaza a la vuelta anterior, que usaba 1 solo
tamaño para las 2 pantallas):** el usuario proveyó una tercera hoja
(`18 ago 2026, 11_35_44 p.m..png`, 1774x887, PNG con alpha real, sin necesidad de quitar
tablero) con 2 versiones de cada uno de los 5 iconos — una GRANDE y detallada (marco ornado,
más "epica") y una CHICA y simple — pidiendo explícitamente usar cada una donde corresponde:
"uno para apachar en la pantalla" (la barra de habilidades que se toca durante la partida)
"y otro para la pantalla de mejora" (la fila en la pestaña Mejoras).
- `SkillData` ganó `hotbarIcon`/`HotbarIcon` (con fallback a `Icon` si no está asignado,
  mismo patrón `?? PlaceholderArt` que el resto del proyecto) — `icon`/`Icon` (ya existía)
  sigue siendo la versión chica. `CreateInitialData.CreateSkill(...)` ganó un parámetro
  `hotbarIconPath` opcional, en paralelo a `iconPath`.
- **Dos consumidores distintos, cada uno con SU tamaño**: `SkillSlotButton` (el casillero
  tocable de `SkillBar`, en `SceneBuilder.cs`) ahora usa `skillData.HotbarIcon`;
  `SkillUpgradeButton` (la fila de la pestaña Mejoras) sigue usando `skillItem.Icon`, sin
  cambios — ya estaba cableado a la versión chica desde la vuelta anterior.
- Mismo criterio de mapeo por efecto (no por nombre viejo) que la vuelta anterior: saco de
  oro→`bendicion_dorada`, espada azul→`espadazo`, puño rojo ardiente→`furia_combate`, tajo
  oscuro con calavera con cuernos→`golpe_certero`, grupo de aliados atacando juntos
  (arquero+maga+lobo)→`lluvia_flechas`.
- **`skillBarHeight` 64→84** (pedido: "se ven un poco chiquitos en pantalla") y el
  `CooldownOverlay` de `SkillSlotButton` dejó de estirarse sobre TODO el casillero — antes
  `anchorMin=(0,0)/anchorMax=(1,1)` cubría el casillero entero, pero el ícono (con
  `preserveAspect`, aspecto ~0.8 — más angosto que alto) queda más chico que esa caja
  (bastante más ancha que alta), así que el círculo radial de cooldown se dibujaba
  desalineado del ícono real, con margen vacío incluido (reportado: "que la parte donde
  carga el cooldown sea del mismo tamaño de la imagen"). Ahora el overlay usa
  `sizeDelta = (skillBarHeight × aspecto_del_HotbarIcon, skillBarHeight)`, centrado — el
  mismo tamaño que `preserveAspect` calcula para el ícono, no un valor aparte inventado a
  mano.

**Misiones/Logros: más grandes, Misiones cambió de lugar** (pedido: "se ven muy pequeñas...
la de misiones ponla en fila a un lado del menú que saca los ajustes"):
- `AchievementsButton` se agrandó en el lugar (dentro de `TopBar`, 32%→60% del alto de la
  barra) — el contador "X/Y" que vive debajo se corrió para seguir teniendo hueco.
- `QuestsButton` se sacó de `TopBar` por completo. Ahora es hermano de `MenuButton`/
  `ProfileButton` (el botón ☰ que abre el menú de pausa, donde está Ajustes — "el menú que
  saca los ajustes") bajo `canvasGo` directo, anclado en PÍXELES al estilo de esos dos
  (esquina superior izquierda, pivote (0,1), `anchoredPosition=(120,-2)`, justo después del
  botón de perfil) en vez de fracciones de `TopBar` — para quedar en la MISMA fila que
  ellos, no dentro de la barra de oro. También más grande (alto fijo 60, ancho derivado de
  la proporción real del ícono, igual criterio que el resto de los íconos con aspecto
  variable en este proyecto).

**Audio (arranque de la Etapa 9C):** clips provistos por el usuario (no generados), en
`Assets/_Project/Audio/`:
- `Music/BaseTheme.mp3` + `Music/BossBattle.mp3` — `MusicManager` (namespace
  `TapLegends.Audio`) escucha `StageStatusEvent.BossActive`: tema de jefe mientras dura la
  pelea, vuelve solo al tema base al salir (ganada, perdida o escapada — las tres terminan
  en `BossActive=false`, no hace falta distinguirlas).
- `SFX/Tap.wav` — `SfxManager` lo dispara en cada `PlayerTappedEvent` (publicado por
  `Enemy.OnPointerDown`, solo tap real del jugador — el DPS pasivo de heroes no lo
  publica), a volumen mas bajo (0.45x) porque suena decenas de veces por minuto.
- `SFX/PowerUp.wav` — `SfxManager` lo dispara en `UpgradePurchasedEvent`: daño de tap,
  habilidades, heroes, equipo, mascotas y mejoras de prestigio, publicado junto al evento
  propio de cada sistema **solo en el metodo de compra real** (`PlayerStats.TryUpgrade`,
  `SkillManager.TryUpgrade`, `HeroRoster.TryBuyOrUpgrade`, `EquipmentManager.TryUpgrade`,
  `PetManager.TryUpgrade`, `PrestigeManager.TryBuyUpgrade`). A proposito NO en
  `LoadState`/`ResetAll`/`SetActive`/loot de jefe de esos mismos sistemas, aunque reusen el
  mismo evento de "cambio" — si se hubiera enganchado ahi, sonaria una vez por cada heroe/
  skill/equipo/mascota poseido en CADA carga de partida. Ver `UpgradePurchasedEvent` en
  `EventBus.cs` si se agrega un sistema de progresion nuevo — hay que acordarse de publicarlo
  a mano en el punto de compra, no viene gratis del evento de "cambio" de cada sistema.
- `SFX/BossDefeat.wav` — `SfxManager` escucha `EnemyKilledEvent` y lo dispara con
  `evt.WasBoss == true` (vencer al jefe; perderlo via `BossEscapedEvent` no cuenta).
  `AudioSource.PlayOneShot` deja que tap, mejora y explosion se superpongan sin cortarse.
- `AudioImportSettings` (AssetPostprocessor, `Scripts/Editor/`) fuerza el Load Type
  correcto al importar segun la carpeta: Streaming bajo `Audio/Music/`, Decompress On Load
  bajo `Audio/SFX/` — cualquier clip nuevo en esas carpetas queda bien configurado solo.
- Musica y SFX usan `AudioSource` separados (mismo GameObject "MusicManager"/"SfxManager"
  cada uno) para que el volumen de uno no dependa del otro. Sin ajuste de volumen/mute en
  el menu de Ajustes todavia — pendiente.

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

## Icono de la app

`Assets/_Project/Art/Icons/AppIcon.png` (arte provisto por el usuario, no generado) es el
icono de Android. `IconImportSettings` (AssetPostprocessor) fuerza Texture2D plano sin
mipmaps para cualquier imagen bajo `Art/Icons/`. Para aplicarlo a Player Settings:
`Tap Legends → Setup → Set App Icon (Android)` (en `ProjectSetup.cs`) — usa el slot
"Legacy" (`SetIconsForTargetGroup`), no Adaptive Icon (foreground/background separados),
porque el arte es una imagen plana ya recortada, no capas. Alcanza para tener icono real
en el dispositivo; descomponerlo en capas de Adaptive Icon queda para el arte final de
tienda (Etapa 13). Volver a correr este mismo item si se reemplaza `AppIcon.png`.

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

8. **`Text.verticalOverflow` por defecto es `Truncate`, no `Overflow` — un label con mas
   líneas de las que entran a ese fontSize en esa altura de fila pierde la ULTIMA línea
   sin ningún error ni warning.** Este fue el bug real detrás de "no me dice el precio de
   nada": varios labels de esta escena tienen 3 líneas (nombre, stat, "Coste: X") y el
   precio, al ser la última línea, era la que desaparecía. `CreateText()` ahora fuerza
   `VerticalWrapMode.Overflow` siempre — un label que no entra se sale un poco de su fila
   en vez de ocultar texto. Igual se ajustó el fontSize de los 3 labels mas apretados
   (mejora de tap, habilidades, prestigio) para que el overflow sea minimo. Si un label
   nuevo de varias líneas "no muestra" su última línea, este es el primer sospechoso.

9. **El botón de retroceso de Android llega como `Input.GetKeyDown(KeyCode.Escape)`**
   (Input Manager viejo, no el Input System nuevo — el proyecto ya usaba
   `StandaloneInputModule`, no `InputSystemUIInputModule`). Es la misma tecla que Esc en
   el Editor, así que `PauseMenuController` se prueba con teclado sin dispositivo. Por
   defecto Android cierra la app con este botón; para que abra un menú en cambio hay que
   interceptarlo explícitamente (no hay "cancelar" el comportamiento default, solo
   consumirlo con tu propia lógica cada frame).

10. **El operador `?.` en un `UnityEngine.Object` NO respeta el "fake-null" de Unity.**
    Cuando un GameObject se destruye (`Destroy(...)`), Unity sobrecarga `==`/`!=` para que
    esa referencia se compare como `null` — pero `?.` se compila como una comparación de
    referencia C# cruda (`(object)x != null`), que NO pasa por esa sobrecarga. Resultado:
    `referenciaDestruida?.Metodo()` **SI llama** al método sobre un objeto destruido →
    `MissingReferenceException`. Bug real: `EnemySpawner.currentEnemy` no se limpiaba a
    `null` en `HandleEnemyDied` hasta que el próximo `SpawnNext()` lo reemplazaba (~0.25s
    después); en esa ventana, cualquier `CurrentEnemyInstance?.TakeDamage(...)` (DPS
    pasivo de héroes/mascotas, `TapCatcher`) SI se ejecutaba sobre el enemigo ya destruido.
    Arreglado poniendo `currentEnemy = null` apenas muere. **Regla:** para chequear si una
    referencia a un `UnityEngine.Object` sigue viva, usar `if (x != null)` explícito (o
    limpiar el campo a `null` en el momento exacto en que se destruye), nunca confiar en
    que `x?.Metodo()` lo va a frenar solo.

11. **Una corrutina de fade/juice que usa `Time.deltaTime` se congela si el jugador pausa
    a mitad de camino** (`PauseMenuController` pone `Time.timeScale=0`). Bug real:
    `BackgroundController.Fade` (el cambio de paisaje al vencer un jefe) usaba
    `Time.deltaTime` — pausar justo durante ese fade lo dejaba pegado en alpha bajo,
    mostrando fondo blanco (el default de una `Image` sin nada resuelto) hasta despausar.
    Arreglado con `Time.unscaledDeltaTime`. **Regla:** cualquier corrutina de transición
    visual pura (fades, popups, "juice") va con tiempo NO escalado — solo la simulación de
    juego de verdad (timers de jefe, cooldowns) tiene que respetar la pausa.

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

Verificación manual (Play en el Editor) sigue siendo la única forma de probar UI,
animación, y flujos completos (nada de esto cambia). El menú de debug (triple tap arriba
a la derecha, solo en builds de desarrollo/editor — ver `DebugMenuController.cs`) tiene
trucos para saltar el grind: oro, daño de tap, borrar guardado, simular progreso offline.

**Tests automatizados (EditMode, NUnit)** — nuevo, cubre la lógica pura por ahora, no todo
el proyecto. Correrlos: `Window → General → Test Runner` en Unity, pestaña **EditMode**,
"Run All". No hay CI configurado — correrlos a mano antes de tocar balance es la única
red de seguridad hoy.

**Estructura de ensamblados** (nueva, antes todo compilaba en el Assembly-CSharp implícito):
- `Assets/_Project/Scripts/TapLegends.Runtime.asmdef` — todo el código de juego (Core,
  Combat, Progression, UI, Debug).
- `Assets/_Project/Scripts/Editor/TapLegends.Editor.asmdef` — herramientas de Editor
  (`SceneBuilder`, `CreateInitialData`, los `*ImportSettings`...), `includePlatforms:
  ["Editor"]`, referencia a `TapLegends.Runtime`.
- `Assets/_Project/Tests/EditMode/TapLegends.Tests.EditMode.asmdef` — los tests,
  `includePlatforms: ["Editor"]`, `autoReferenced: false` (no se cuela en builds ni en
  otros ensamblados), referencia a `TapLegends.Runtime`.
- Si se agrega un ensamblado nuevo (ej. PlayMode tests en el futuro), seguir el mismo
  patrón: nombre explícito, referencias explícitas, nunca depender del auto-reference
  implícito entre asmdefs custom (solo los ensamblados implícitos de Unity —
  Assembly-CSharp/-Editor— auto-referencian paquetes con `autoReferenced: true`; un asmdef
  propio SIEMPRE tiene que listar sus dependencias a mano).

**Gotcha de testing #1 — los singletons `static Instance` sobreviven entre `[Test]`.**
Unity no recarga el dominio entre tests EditMode de la misma corrida, así que si un test
no limpia el campo estático `Instance` de un Manager, el SIGUIENTE test (de esa clase o de
OTRA) puede leer una referencia a un GameObject ya destruido — el mismo bug de "`?.` no
respeta el fake-null" del gotcha #10 de Unity más abajo, pero disparado por tests en vez
de gameplay. Por eso **todo `[TearDown]` que toque un Manager tiene que**:
1. Llamar `TestUtils.ResetSingleton<T>()` (reflection sobre el setter privado de
   `Instance` — está en `Assets/_Project/Tests/EditMode/TestUtils.cs`).
2. `Object.DestroyImmediate(gameObject)` — no `Destroy()`, que en EditMode queda diferido
   sin un loop de frames corriendo que lo procese.
3. `EventBus.ClearAll()` si el test suscribió algo — mismo problema de estado estático
   compartido entre tests.

**Gotcha de testing #2 — `ScriptableObject.CreateInstance<T>()` en vez de assets reales.**
Para testear código que usa `HeroData`/`PetData`/`PrestigeUpgradeData`/etc., se crea una
instancia en memoria (`ScriptableObject.CreateInstance<HeroData>()`), se le asignan los
campos públicos a mano, y se destruye con `Object.DestroyImmediate()` en el `[TearDown]`
— no hace falta (ni conviene) guardarlo como asset en disco para un test.

**Gotcha de testing #3 — `Destroy()` fuera de Play mode loguea un error que hace fallar
el test.** Código de producción que llama `Object.Destroy(...)` (no `DestroyImmediate`) —
ej. `EnemySpawner.HandleEnemyDied` — corre igual en EditMode, pero Unity loguea "Destroy
may not be called from edit mode!" como error, y el Test Framework marca cualquier log de
error no esperado como fallo por defecto. Si un test ejercita un camino de producción que
llama `Destroy()` (no algo que el test mismo controla), poner
`UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;` en el `[SetUp]` y volver a
`false` en el `[TearDown]`.

**Gotcha de testing #4 — `AddComponent<T>()` en EditMode NO garantiza que `Awake()`/
`OnEnable()` ya corrieron cuando la llamada retorna.** Esto es lo que de verdad rompió el
backfill de esta sesión (no la guarda anti-duplicados de `GoldWallet` que se sospechó al
principio — esa teoría se probó y no cambió nada). Confirmado con un assert de diagnóstico
directo: justo después de `wallet = walletGo.AddComponent<GoldWallet>();`,
`GoldWallet.Instance` daba `null` — es decir, `Awake()` (que hace `Instance = this;`)
simplemente no había corrido todavía. Con pocas fixtures (el primer run, 50 tests) esto no
se manifestaba nunca; con más fixtures corriendo juntas (148 tests) empezó a fallar
consistentemente. No se pudo aislar la causa raíz exacta sin poder correr Unity en vivo,
así que en vez de seguir adivinando, la solución es **no depender de Awake()/OnEnable()
en absoluto**:
- Para el singleton (`static Instance`): en vez de esperar a que `Awake()` lo asigne, forzarlo
  a mano con `TestUtils.ForceSingleton(instance)` justo después de `AddComponent<T>()`.
- Para managers que se suscriben a eventos en `OnEnable()` (`AchievementManager`,
  `QuestManager`): `ForceSingleton` no alcanza, porque `OnEnable()` tampoco corrió y por lo
  tanto `EventBus.Subscribe(...)` nunca se llamó. Se probó forzarlo con
  `go.SetActive(false); go.SetActive(true);` — no funcionó, resultado idéntico. **Después se
  probó invocar `OnEnable()` directo por reflection y ROMPIÓ EL EDITOR de verdad**: Unity
  lleva un contador de versión nativo alrededor de cada callback de ciclo de vida
  (Awake/OnEnable/...) para evitar acceso concurrente; llamar el método por reflection salta
  esa contabilidad nativa, y cuando el despachador real de Unity intentó entrar al mismo lock
  más tarde lo encontró en un estado inválido — derivó en un loop infinito logueando "Access
  version should be odd when acquiring lock" (~93 millones de repeticiones, 4.4 GB de log,
  Editor totalmente inutilizable, hubo que matar el proceso a mano). **NUNCA invocar un
  método de ciclo de vida de Unity (`Awake`/`OnEnable`/`OnDisable`/`Start`/`Update`/...) por
  reflection — ni por curiosidad.**

  La solución segura que sí funciona: no tocar el ciclo de vida en absoluto, solo replicar
  a mano lo que `OnEnable()` habría hecho — suscribir el MISMO delegate a `EventBus`
  directamente. `TestUtils.PrivateHandler<TOwner, TEvent>(instance, "NombreDelMetodo")` usa
  `Delegate.CreateDelegate` (reflection estándar de .NET sobre un método C# común, sin
  ningún gancho especial de Unity) para crear el delegate y devolverlo listo para
  `EventBus.Subscribe(...)`. Ver `AchievementManagerTests.cs`/`QuestManagerTests.cs` para el
  patrón completo.

Este mismo mecanismo es casi seguro la causa de que `Enemy.Setup()` tirara
`NullReferenceException` en `EnemySpawnerBossCycleTests` (`bodyImage`, asignado en
`Enemy.Awake()`, era null cuando `Setup()` lo usaba) — ese archivo se acotó en vez de
arreglarse porque `EnemySpawner.CreateEnemyGameObject()` es código de producción que no
conviene tocar solo para un test sin poder verificarlo en vivo.

**Qué está cubierto hoy** (`Assets/_Project/Tests/EditMode/`):
`GameConstants` (fórmulas de balance), `NumberFormatter`, `EventBus`, `GoldWallet`,
`PlayerStats`, `HeroRoster`, `PrestigeManager`, `EquipmentManager`, `PetManager`,
`SkillManager`, `AchievementManager`, `QuestManager`, `WeaponSkinManager`,
`ProfileManager`, `ShuffleBag` (algoritmo puro, sin dependencias de Unity — ver "Fondos de
escenario" más abajo), `GameManager.DebugSimulateOfflineSeconds` (la única puerta pública a la
fórmula de oro offline — `ComputeOfflineGold`/`GoldPerDamageAtLevel` son privados y
`LoadGame()` depende de leer disco, no se testean directamente). El ciclo de jefe de
`EnemySpawner` está cubierto solo parcialmente (`EnemySpawnerBossCycleTests` — ver gotcha
#5 arriba: el estado vía `LoadState`/`ChallengeBoss` sin jefe disponible sí, el flujo
completo de vencer a un jefe real no).

**Qué falta** (pendiente, ir agregando de a poco): los componentes de UI puramente
event-driven quedan a propósito sin unit-testear — ver el párrafo siguiente.

**A propósito NO se van a unit-testear**: `SceneBuilder`/`CreateInitialData` (construyen
la escena a mano en modo Editor, su "prueba" real es que el juego levante y se juegue —
ver el flujo de Setup más abajo) ni los componentes de UI puramente event-driven
(`XxxRow`/`XxxDisplay`, la mayoría son un `Refresh()` de dos líneas que engancha texto a
un getter ya testeado por el Manager correspondiente — el costo de un test PlayMode ahí
no paga su propio mantenimiento).
