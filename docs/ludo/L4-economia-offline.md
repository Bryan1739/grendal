# L4 (ad-hoc) — Economía offline y percepción de velocidad de progresión

**Motivo:** feedback directo del usuario ("siento que la progresión se vuelve muy rápida y
pierde sentido el modo offline; además hay que subir esas recompensas para que el jugador
sienta que puede dejar el juego e igual avanzar"). Se trata como iteración L4 (Economía,
refuerzo y auditoría ética) porque el síntoma es económico, no de dificultad de combate.

---

## 1. Objeto de estudio

La fórmula de recompensa offline (`GameManager.ComputeOfflineGold`) comparada contra la
tasa de generación de oro en juego activo (`Enemy.Die()`), y cómo esa comparación cambia a
medida que el jugador invierte en Prestigio, Equipo y Mascotas.

## 2. Marco teórico

Grifos y sumideros + el modelo Hooked leído críticamente (ver TapLegends-Plan.md, sección
L4). El diseño ya declarado del proyecto es explícito: la recompensa offline existe para
**respetar el tiempo del jugador**, no para generar ansiedad de "tengo que volver ya". Si
la recompensa offline se degrada relativa al juego activo a medida que el jugador progresa,
el sistema empieza a operar en la dirección contraria a su propia tesis de diseño — sin
necesidad de ningún patrón oscuro deliberado, un simple gap de fórmula alcanza para lograr
el mismo efecto no deseado.

## 3. Preguntas

1. ¿La tasa de oro/segundo offline escala con los mismos multiplicadores que la tasa de
   oro/segundo activa, o se queda atrás a medida que el jugador invierte en Prestigio,
   Equipo o Mascotas?
2. ¿El tope de horas offline (`MaxOfflineSeconds`) cubre las ausencias reales que el
   sistema dice premiar ("dormir", "un día de trabajo")?
3. ¿`OfflineDpsFactor` (fracción de eficiencia offline vs. activo) sigue siendo razonable
   una vez resuelto lo anterior, o corresponde subirlo también?

## 4. Instrumento

Lectura directa de código (no hace falta playtest para esto: es una comparación de dos
fórmulas del mismo sistema) — `Enemy.Die()` vs. `GameManager.ComputeOfflineGold()` /
`DebugSimulateOfflineSeconds()`, más los valores reales de `CreateInitialData.cs`.

## 5. Datos crudos

**Oro por kill activo** (`Enemy.Die()`, antes del fix):
```
gold = EnemyGoldForLevel(nivel)
     * BossGoldMultiplier          (si es jefe)
     * PrestigeManager.GoldMultiplier    (Codicia: +5%/nivel)
     * EquipmentManager.GoldMultiplier   (Coraza: +3%/nivel dentro de la rareza actual)
     * PetManager.GoldMultiplier         (Búho Sabio +6%·1.10^n, Slime Real +5%·1.10^n)
     * SkillManager.GoldBuffMultiplier   (Bendición Dorada: x3, temporal, 20s)
```

**Oro por segundo offline** (`GameManager.ComputeOfflineGold()`, ANTES del fix):
```
goldPerDamage = EnemyGoldForLevel(nivel) / EnemyHealthForLevel(nivel)   ← SIN NINGÚN multiplicador
oroPorSegundo = TotalDps * OfflineDpsFactor(0.5) * goldPerDamage
```

`TotalDps` (el lado del DAÑO) sí incluye Prestigio/Equipo/Mascotas correctamente — se
verificó en `HeroRoster.TotalDps`. El problema está exclusivamente del lado del ORO: la
fórmula offline usaba `EnemyGoldForLevel` en crudo, sin `PrestigeManager.GoldMultiplier`,
sin `EquipmentManager.GoldMultiplier`, sin `PetManager.GoldMultiplier`.

**Ejemplo ilustrativo** (jugador de progreso medio: Codicia nivel 10 → +50% oro; Coraza
nivel 10 de rareza intermedia → +30% oro aprox.; un pet de oro activo nivel 10 → +20% oro
aprox.): el oro activo por kill ya viene multiplicado por `1.5 × 1.3 × 1.2 ≈ 2.34×` sobre
la fórmula base. El oro offline, con el bug, seguía usando exactamente esa fórmula base sin
multiplicar — es decir, un jugador así de invertido ganaba offline **menos de la mitad**
de lo que la coherencia del propio sistema económico diría que debería ganar. Y esa brecha
**crece con la inversión**: cuanto más tiempo lleva jugando (más reliquias, mejor equipo,
más mascotas), peor se pone la proporción offline/activo — exactamente al revés de lo
deseable para un sistema pensado para el jugador de más horas acumuladas.

`MaxOfflineSeconds` estaba en 8h: cubre una siesta o una jornada corta, pero no "dormí 8h
+ trabajé 8h" (16h reales sin abrir el teléfono), el caso de uso más común de un jugador
que de verdad "deja el juego y sigue con su vida".

## 6. Interpretación

La sensación de "la progresión se vuelve muy rápida" no requiere necesariamente retocar
`EnemyHealthGrowth`/`EnemyGoldGrowth` otra vez (ya se ajustaron en la iteración anterior
para compensar los 4 multiplicadores acumulados de Prestigio/Equipo/Mascotas/Habilidades).
Alcanza con esta lectura, más simple y verificable: el juego **activo** sí se beneficia de
esos 4 multiplicadores acumulados, así que la sensación de progreso rápido cuando jugás es
correcta y esperada — pero el modo **offline**, al no beneficiarse de 3 de esos 4
multiplicadores, se queda comparativamente cada vez más atrás. El jugador no siente "la
progresión es rápida" en abstracto: siente específicamente que **jugar activo rinde cada
vez más, mientras que dejar el juego cerrado rinde relativamente cada vez menos** — el
patrón exacto que el juego dice no querer reforzar (ver Etapa 0/L4: "recompensa offline
respeta el tiempo del jugador").

## 7. DECISIONES

1. **`GameManager.ComputeOfflineGold` y `DebugSimulateOfflineSeconds` ahora aplican
   `PrestigeManager.GoldMultiplier`, `EquipmentManager.GoldMultiplier` y
   `PetManager.GoldMultiplier`** (factorizado en `GoldPerDamageAtLevel`, un solo punto de
   verdad para ambos métodos). El oro offline ahora escala con la MISMA inversión del
   jugador que el oro activo — la brecha deja de crecer con el progreso.
2. **`OfflineDpsFactor` sube de 0.5 a 0.65.** Con la Decisión 1 ya puesta, subir esto
   además es una decisión de diseño explícita (no solo un arreglo de bug): hacer el
   offline más generoso a propósito, pedido directo del usuario. Activo sigue rindiendo
   más (mantiene la escalera de agencia de L3 — jugar de verdad sigue valiendo la pena),
   pero la diferencia ya no es tan marcada.
3. **`MaxOfflineSeconds` sube de 8h a 16h.** Cubre "dormir + un día de trabajo" sin que el
   jugador sienta que tiene que volver a un horario fijo para no perder progreso — el caso
   de uso que este sistema dice premiar en primer lugar.

## 8. Qué NO se cambia

- **`EnemyHealthGrowth`/`EnemyGoldGrowth`** (curvas base de dificultad/inflación): no hay
  evidencia nueva de que estén mal calibradas — el síntoma reportado se explica
  completamente por el gap de fórmula de la Decisión 1. Retocarlas de nuevo sin evidencia
  sería resolver el mismo síntoma dos veces con causas distintas.
- **`SkillManager.GoldBuffMultiplier` (Bendición Dorada) NO se aplica al oro offline.** Es
  un buff temporal de sesión activa (20s de duración, con cooldown); no tiene sentido como
  tasa permanente para una app cerrada, y aplicarlo infleria el oro offline muy por encima
  de lo que el balance activo sostiene.
- **No se saca el tope de horas.** `MaxOfflineSeconds` sigue existiendo (ahora en 16h) a
  propósito: un tope, aunque generoso, evita que "dejar el juego abierto en un cajón por
  una semana" sea una estrategia dominante que vuelva irrelevante jugar de verdad — eso sí
  rompería la escalera de agencia de L3.
