# Visión del producto

## A futuro (más allá de este repo)

La idea de largo plazo del dueño de este proyecto **no es una app de finanzas
aislada**, sino una **plataforma personal de gestión de recursos importantes**
(tiempo, finanzas, y otros por definir). No un monolito: **herramientas
independientes que puedan interactuar entre sí**. Este repositorio
(`SistemaFinanzasPres`) es la primera de esas herramientas — finanzas.

Cuando se proponga arquitectura nueva (auth compartida, un "hub" que las
conecte, etc.), tenerlo en cuenta — pero no hay que resolverlo ahora mismo:
hoy la prioridad es que la herramienta de finanzas sea sólida y simple por
sí sola.

## Principios para la herramienta de finanzas (este repo)

Explícitos del dueño del proyecto (2026-09-05) — son el filtro para decidir
qué construir, qué mantener y qué cortar:

1. **Utilidad real, no exhaustividad.** Implementar funcionalidades y
   **mantener solo las que en la práctica el usuario realmente usa**. Una
   feature "completa" que nadie necesita revisar cada mes no vale lo que
   cuesta en complejidad. Si una pantalla no se visita, es candidata a
   fusionarse o desaparecer — no a "mejorarse".
2. **Se debe sentir como un Excel simple.** Diligencias tus gastos e
   ingresos, y la herramienta te da todo lo que necesitas saber sobre cómo
   van tus finanzas. Sin fricción, sin pasos de más.
3. **Dashboards/resúmenes prácticos, no exhaustivos.** Deben ayudar a
   **tomar decisiones**, no exhibir todos los datos disponibles. Si un
   número en el dashboard no cambia lo que el usuario haría, sobra.
4. **Pocas pantallas, las más útiles.** Preferir consolidar sobre añadir.
   Antes de crear una pantalla nueva, preguntar si puede vivir dentro de
   una existente.
5. **Poco tiempo invertido, resultado claro.** El usuario no debe tener que
   dedicarle tiempo a la herramienta para que le sea útil — registrar un
   gasto debe tomar segundos.
6. **Debe sugerir, no solo mostrar.** El objetivo final no es solo reportar
   el estado de las finanzas, sino **sugerir ajustes concretos** para
   seguir mejorando (ej. las alertas automáticas del Excel de referencia:
   "vas a exceder Deseos", "te faltan $X para tu meta de ahorro").
7. **Intuitivo por encima de todo.** Si hay que explicar cómo usar una
   pantalla, algo está mal diseñado.

## Cómo aplicar esto en la práctica

Antes de proponer o construir una feature nueva, pasarla por este filtro:
- ¿Es algo que el usuario necesita ver/hacer **seguido** (a diario o cada
  mes), o es un reporte que se consulta una vez y no vuelve?
- ¿Puede vivir como una sección dentro de una pantalla existente en vez de
  una pantalla nueva?
- ¿El dashboard resultante sigue siendo legible en unos segundos, o ya
  empieza a competir por atención con lo que sí importa?
- ¿Ayuda a decidir algo, o es solo información de fondo?

Esto aplica igual para features nuevas que para las que ya existen: al
tocar una pantalla existente por otro motivo, vale la pena preguntarse si
sigue ganándose su lugar bajo estos criterios.
