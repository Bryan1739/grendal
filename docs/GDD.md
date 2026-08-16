# Tap Legends — GDD (v0.1)

## Supuestos tomados por defecto (modo auto — cámbialos cuando quieras)

- **Nombre del héroe protagonista:** "El Errante" (sin nombre propio fijo, para que el jugador lo sienta suyo). Puedes ponerle nombre después.
- **Ambientación:** reino de **Aldemark**, caído en ruinas por una corrupción llamada "el Óxido" que convierte todo en no-muertos y bestias corruptas.
- **Tono:** medieval fantástico ligero, no oscuro/gore. Apto para todas las edades (facilita clasificación IARC y ampliar audiencia).
- **5 biomas** para los primeros ~250 niveles (50 niveles c/u), cada uno con su jefe de bioma cada 10 niveles y un jefe final de bioma en el nivel 50:
  1. **Bosque Quebrado** (niveles 1–50) — lobos corruptos, bandidos, arañas. Jefe final: **Grendal, el Lobo Ceniza**.
  2. **Cripta Olvidada** (51–100) — esqueletos, espectros, gules. Jefe final: **Morwen, la Dama Pálida**.
  3. **Castillo Caído** (101–150) — caballeros caídos, gárgolas, verdugos. Jefe final: **Sir Aldric, el Perjuro**.
  4. **Picos Helados** (151–200) — trolls de hielo, yetis, arqueros de nieve. Jefe final: **Ymira, Reina de Escarcha**.
  5. **Fisura Infernal** (201–250) — demonios menores, íncubos, hellhounds. Jefe final: **Vorkath, el Óxido Encarnado** (antagonista final de la v1.0).

- **10 héroes** (DPS pasivo), progresión de coste creciente, temática de compañeros de armas:
  1. Escudero Tomas (barato, inicial)
  2. Arquera Lyra
  3. Monje Berrin
  4. Mercenario Kael
  5. Hechicera Ysolde
  6. Caballero Dorn
  7. Cazadora Wren
  8. Alquimista Fenn
  9. Paladín Aurelia
  10. Dragón Ancestral Zaltheus (el más caro, tope de la primera pasada)

- **5 slots de equipo:** Arma, Casco, Armadura, Botas, Amuleto — 5 rarezas (Común/Poco Común/Raro/Épico/Legendario).
- **10 mascotas**, temática de bestias medievales/fantásticas: Lobezno, Gatuno Feérico, Dracónido Bebé, Búho Sabio, Grifo Menor, Rana Alquímica, Hada Luminosa, Jabalí Blindado, Fénix Joven, Slime Real.
- **6 habilidades activas:** Furia del Guerrero, Lluvia de Flechas, Bendición Dorada, Invocar Aliado, Golpe Certero, Escudo de Piedra.
- **Moneda de prestigio:** "Fragmentos de Corona".

Estos nombres son placeholder de diseño — fáciles de cambiar sin tocar código (todo vive en ScriptableObjects, ver `Assets/_Project/Data/`). Dilo si quieres otro tono (más oscuro, más cómico, nombres en otro idioma, etc.) y se regeneran los assets.

## Bucle central (una frase)
Tocas para matar enemigos → ganas oro → subes héroes y mejoras → avanzas de nivel → un jefe con temporizador te frena → equipas botín y mascotas → cuando te estancas, prestigias por Fragmentos de Corona y vuelves más fuerte.

## Primeros 60 segundos
1. Pantalla de un enemigo (Lobo Corrupto) con barra de vida.
2. Tap → daño, número flotante, el enemigo se sacude.
3. Enemigo muere → suelta oro → aparece el siguiente, un poco más fuerte.
4. Botón "Mejorar daño de tap" siempre visible en la parte inferior.

*(La oleada de 10 + jefe con temporizador es Etapa 3 — la v0.1 implementada ahora es solo el bucle de tap continuo, ver estado de implementación abajo.)*

## Tesis de diferenciación (pendiente — L0)
Placeholder hasta hacer la deconstrucción comparada de la Etapa 0: *"Tap Legends se diferencia de Tap Titans/Tap Souls en ___."* Esto se define jugando la competencia con cronómetro (ver `TapLegends-Plan.md`, iteración L0), no antes.

---

## Estado de implementación

**v0.1 "Tap" — construida.** Ver `../TapLegends-Plan.md` para el plan completo por etapas.

Lo que hay ahora mismo, jugable:
- Tap sobre el enemigo → daño → número flotante → muerte → oro → siguiente enemigo (un poco más fuerte, fórmulas en `GameConstants.cs`).
- Botón de mejora de daño de tap con coste creciente.
- Arte 100% procedural (`PlaceholderArt.cs`) — criaturas simétricas generadas por semilla del id, sin arte final todavía (llega en la Etapa 9).
- Menú de debug (triple tap arriba a la derecha, solo en builds de desarrollo): cheats de oro y de daño de tap.

Lo que falta para completar v0.1 según el plan y requiere tu decisión o una sesión de Unity Editor (ver sección "Próximos pasos" en la respuesta):
- Abrir el proyecto en el Editor y darle Play para sentirlo por primera vez.
- Ajustar visualmente el layout (anchors) a ojo — se construyó por código con valores razonables, no pixel-perfect.
- L1 (playtest del verbo primario) una vez lo hayas probado tú mismo unos minutos.
