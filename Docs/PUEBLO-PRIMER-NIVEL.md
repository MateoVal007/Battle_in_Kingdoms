# Pueblo del Roble — primer nivel jugable de revisión

Abrir Assets/00_Scenes/Pueblo_01_Blockout.unity y pulsar Play. La escena está guardada y se puede editar directamente. Es una prueba LOCAL de dos personajes; Photon Fusion aún no está instalado ni configurado. No hay conexión online simulada.

## Controles

| Acción | Guerrero | Mago |
|---|---|---|
| Movimiento | A / D | Flechas izquierda / derecha |
| Salto | Espacio / W (sin escudo) | Flecha arriba |
| Agacharse | S | — |
| Defensa superior | G + W / flecha arriba | — |
| Cambiar objeto | — | Q / E |
| Activar o guardar objeto | — | R |
| Ataque | F, secuencia de tres golpes | K, proyectil |
| Cooperación | G, mantener escudo | L, mantener canalización |

TAB cambia el personaje seguido por la cámara. Los personajes pueden cruzarse. F1 muestra controles y revisión. R permite reintentar desde el checkpoint únicamente cuando ambos están derribados. Los botones 01–08 son herramientas de revisión que teletransportan a ambos a un sector; no son parte del menú final del juego.

## Sectores editables

01 Llegada: presentación, plataformas suaves, libro de fuego.
02 Huertos: ruta elevada, espinos, armadura y enemigo.
03 Tejados: recorrido superior con libro de hielo o calle inferior con combate.
04 Arroyo: veneno, plataformas bajas y alternativa superior con salud.
05 Plaza: checkpoint, salud, protección y fuente.
06 Molino: guerrero en sello azul x=132 manteniendo G; mago en sello morado x=137 manteniendo L durante 3 segundos. Se abre el muro x=142.
07 Bosque: espinos, ruta elevada, fuego y tirador.
08 Puerta: enemigos y salida que exige a ambos jugadores más allá de x=184.

## Reglas del prototipo

Salud inicial 100; el guerrero comienza sin equipo y con armadura 0. El primer cofre se abre a puñetazos y contiene espada, escudo y armadura 60; solo el guerrero puede recogerlo. Armadura recogida repone a 60. Salud recogida cura 30. Espinos dañan 20 y veneno 10, con un segundo de protección entre impactos. Al quedarse sin salud, el personaje queda derribado: no se mueve, ataca ni recoge objetos y no reaparece automáticamente. El compañero vivo debe recoger salud (del escenario o de un cofre); esto reanima al caído cerca del recolector con 50 HP y 2 segundos de protección, consumiendo el objeto sin curar además al recolector. Si ambos están vivos, la salud cura 30. Si ambos caen, R o el botón de reintento los devuelve al checkpoint con salud completa y repone cofres y objetos. Caer al veneno permite salir saltando por las piedras de rescate y plataformas; permanecer dentro sigue haciendo daño.

Los libros solo los recoge el mago y se almacenan en tres espacios: fuego, hielo y protección. Cada recogida repone hasta 25 segundos de carga de ese tipo. Q/E selecciona; R activa o guarda. Cambiar de objeto lo desactiva y conserva la carga restante. Solo el objeto activo consume carga; el círculo se vacía, y al agotarse vuelve el disparo normal. No se pueden activar objetos vacíos. Efectos: fuego aumenta daño de 20 a 35; hielo ralentiza al enemigo durante 3 segundos; protección reduce daño a la mitad. G mantiene defensa frontal; S agacha con una colisión más baja; G+W orienta el escudo hacia arriba sin saltar. Se puede atacar con F mientras se mantiene cualquiera de esas defensas, también agachado. La defensa superior deja expuesto el frente y la frontal no detiene impactos desde arriba. No evita veneno o espinos. No se puede volver a erguir dentro de un techo bajo. Espada alcanza 2.5 unidades y alterna daños 20/20/35 con arco visual. No usa animaciones ni recursos de Mega Man.

Enemigos sencillos con patrulla limitada y tiradores. No tienen navegación avanzada. La fauna es decorativa. Personajes y escenario usan primitivas; modelos Blender y animaciones finales no se importaron. Las imágenes de sectores son capturas de cámara de Unity, no arte final.

## Verificación

Se ejecutó una prueba automatizada en Play Mode con eventos de teclado del Input System: movimiento, salto, libros, daño por contacto, checkpoint, derribo, reanimación mediante salud, apertura de cofres por espada y disparos, escape del veneno, reinicio conjunto, espada, proyectiles, puerta con uno y dos participantes, salida. El informe actualizado en Docs/Verification/play-mode-results.txt incluye los resultados por mecánica y por salto. Además se prueban secuencias de saltos en huertos, tejados y las dos rutas del arroyo usando entradas de teclado y colisiones reales. Estas pruebas no equivalen a un recorrido manual completo ni a pruebas de red o latencia.

Menú Battle in Kingdoms > Verificar pueblo en Play Mode vuelve a ejecutar la prueba y guarda capturas/resultados en C:/Users/MAQ/Documents/ChatGPT/Juego final/outputs/Pueblo_verificacion. Se debe usar la escena del pueblo fuera de Play Mode.

El generador del menú reconstruye la escena base: usar solo si se desea reemplazar el trazado generado. Para ajustar sectores, editar los objetos de la escena guardada. No regenerar después de cambios manuales sin guardar una copia.

## Siguiente integración de red

Crear aplicación Photon Fusion y obtener SDK oficial y App ID. Integrar Host/Client, selección de guerrero/mago, entradas por jugador, estado autoritativo para daño/pickups/puerta/checkpoints y cámara local. Probar con dos instancias y luego dos equipos. El controlador actual es la base local de pruebas y aún necesita adaptación a los ticks y autoridad de Fusion.

Actualización de presentación: huertos con cercas y carreta, mercado, bosque, nubes, fauna con patas, molino animado y burbujas de veneno. La armadura del guerrero desaparece al agotarse. La espada gira durante el ataque y el cuerpo muestra impactos. Los proyectiles enemigos chocan con plataformas y paredes. Los botones de revisión buscan una superficie libre para colocar a cada personaje.

Cofres: llegada (salud), tejados (hielo), orilla del arroyo (salud), plaza (salud) y bosque (salud). Se abren una sola vez al golpearlos o dispararles; la recompensa aparece físicamente para recogerla. Todas las plataformas son sólidas por arriba, por los lados y por debajo. Las plataformas bajas tienen base hasta el suelo y deben superarse saltando.

Cámara de perspectiva lateral con ligera inclinación y anticipación del movimiento, para mostrar profundidad del escenario como referencia de composición de Mega Man. Al caer el personaje seguido, pasa al compañero vivo. Sigue siendo un blockout de primitivas, sin arte ni animaciones finales.

Última verificación: 44 comprobaciones aprobadas en Play Mode, incluyendo el recorrido de salida del veneno y recogida única de recompensas.


Revisión de diseño: sectores 02, 03, 07 y 08 con perfiles de suelo inclinados, colinas y hondonadas. Plataformas superiores más altas y una pasarela diagonal entre tejados. Las modificaciones están aplicadas tanto a Pueblo_01_Blockout como a Pueblo_Online. Se mantiene el desplazamiento lateral X/Y. Hay un tirador elevado en el huerto para usar la defensa superior.

Corrección v3: equipo inicial en cofre x=6, cofre de salud adicional x=20. Ataque sin equipo: puñetazo F. El escudo solo funciona después de equiparse. Se retiró la plataforma baja sobre la orilla izquierda del arroyo; la ruta inferior salta desde la orilla x=78 a x=83 y x=88. Piedras de rescate x=80 y x=86.

La conexión usa AppVersion pueblo-v3-solid-equipment para separar versiones incompatibles. Iniciar ambas instancias con esta versión (incluido el nuevo ejecutable). El nombre de sala debe coincidir.

## Enemigos de la revisión 4

- 02 Huertos: arquero fijo en el borde de la colina, x=41,8. Avisa en amarillo antes de lanzar una flecha hacia la posición observada del jugador. Se queda en su puesto y puede recibir disparos o golpes al alcanzarlo por la ruta alta. G + W protege de flechas que llegan desde arriba.
- Bandidos terrestres: persiguen al jugador vivo que detectan y mantienen el objetivo mientras siga al alcance. Si queda derribado, buscan al compañero. Ya no están limitados a cuatro metros de su punto inicial. Respetan colisiones y se detienen antes de caídas: no saltan ni atraviesan obstáculos para alcanzar una ruta superior.
- 05 Plaza, x=114: guardián grande con látigo, después del checkpoint de x=99 y antes del molino cooperativo. 240 HP, 25 de daño y 3,8 m de alcance. Amarillo y línea de alcance durante 0,9 s de preparación; golpe único y recuperación de 1,8 s. Ambos jugadores pueden atacarlo, esquivarlo o cubrirse de frente. Es un enemigo fuerte del recorrido, sin fases de jefe ni cierre nuevo de la plaza.
- Los avisos, flechas, posiciones, salud y derrotas se replican desde el anfitrión. Las figuras siguen siendo primitivas del prototipo.

El GDD define cuerpo a cuerpo, ataques a distancia, persecución, avisos y respeto de bordes/paredes. La ubicación del arquero y el guardián con látigo corresponden a la ampliación solicitada por el usuario.

Validación de la revisión 4: 16 comprobaciones específicas de enemigos aprobadas, 73 comprobaciones generales del nivel aprobadas, compilación Windows correcta con 0 errores y 12 comprobaciones online aprobadas en cada proceso (anfitrión e invitado, Photon Fusion, 2 jugadores). Informes en Docs/Verification/enemy-play-results.txt y enemy-online-host/client.txt.

## Revisión 5: saltos, equipo y arquero
Las plataformas se reorganizaron con desniveles habituales de 1,4 m frente al salto máximo aproximado de 2,3 m. El arroyo tiene pasarela alta, piedras bajas y una piedra central que permite salir del veneno. Las plataformas siguen siendo sólidas por todos sus lados. Se añadieron terraplenes, puntales de madera y piedras estrechas bajo las superficies; se corrigió la altura de los objetos y sus etiquetas.
Bloquear un impacto consume la mitad del daño entrante, redondeada hacia arriba, de los 60 puntos de resistencia del equipo. Mantener el botón sin recibir golpes no consume resistencia. El impacto que agota la resistencia queda bloqueado, desaparecen espada/escudo y el guerrero vuelve al puñetazo. Romper la armadura por daño normal también retira el equipo. Recoger armadura o equipo vuelve a equiparlo.
El arquero fijo está en x=43, y=7,1. Solo elige jugadores vivos al menos un metro por debajo, a menos de ocho metros horizontales, y dispara una flecha descendente hacia la posición vista durante la preparación. Las plataformas sólidas pueden servir de cobertura.

Validación v5: 47 comprobaciones específicas aprobadas más resultado global aprobado (48 líneas). Incluyen recorrido de plataformas con el CharacterController real, rutas alta y baja del arroyo, escape del veneno, desgaste y rotura del equipo, y rechazo de disparos ascendentes. La prueba abre los cofres antes de recorrer sus plataformas y utiliza las mismas velocidades y gravedad del jugador. Los informes v4 y los antiguos recorridos conservan sus resultados históricos; el trazado vigente se verifica en v5-play-results.txt.

## Revisión 6
El nivel pasa a 240 m y 10 secciones. Se añaden Camino Real y Patio de la Torre, 10 enemigos y 6 cofres de armadura. La salida se traslada a x>232 y hay un checkpoint nuevo en x=193.
Los enemigos terrestres patrullan con pausas, persiguen dentro de una zona y regresan al puesto cuando el jugador se aleja. El arquero permanece fijo en y=8,8 y mantiene sus disparos descendentes.
En el arroyo la pasarela superior alcanza y=6,4. Sobre la piedra de y=0,6 quedan 5,4 m hasta la cara inferior de la pasarela: espacio para los 2 m del personaje y aproximadamente 2,3 m de salto, con margen. Los escalones de acceso conservan subidas de 1,4 m.
Mago: K (local) o F/K (online) dispara magia básica sin gastar cargas. Q/E elige la magia especial y R la utiliza. Fuego/hielo tienen 12 usos por libro y gastan uno por lanzamiento, sin consumo por tiempo. Protección dispone de 25 s; R la activa o detiene, y cambiar de selección detiene el consumo conservando lo restante. El círculo indica usos restantes o segundos según el tipo.
La cámara se aleja suavemente en el arroyo para mostrar la pasarela elevada y las piedras inferiores. La visibilidad del arquero respeta plataformas y cofres sólidos: no dispara atravesando una cobertura.

Validación local v6: 74 líneas de comprobación aprobadas, incluidos los saltos de las diez secciones, el salto completo bajo la pasarela del arroyo, las cargas por uso del mago, la pausa de protección y la patrulla/persecución/regreso del guardián. Los cofres nuevos de las secciones 9 y 10 están en las plataformas de x=204 y x=228; el checkpoint de x=193 queda despejado.

## Revisión 7: estrellas y vidas personales
Se añadieron 274 estrellas por personaje para guiar el camino principal y los saltos de rutas altas. Los arcos se calculan con la velocidad de salto y la gravedad del guerrero. Cada estrella tiene una copia independiente para guerrero y mago: recogerla no se la quita al compañero. El contador convierte cada 100 estrellas en una vida extra y conserva el resto. Las estrellas recogidas no reaparecen al reiniciar el checkpoint.
Las vidas extra empiezan en cero y se ganan con estrellas. Un jugador derribado puede gastar una para volver con 100 HP al checkpoint: T en online; T para el guerrero e Y para el mago en local. Sin gastar una vida, se mantiene la reanimación por salud recogida por el compañero.
Tras daño de salud o armadura hay 3 segundos de recuperación sin acumular daño. Los bloqueos conservan su pausa breve y el desgaste del equipo. La reanimación concede también 3 segundos de protección. El parpadeo y el indicador REC muestran la recuperación.
El mago muestra una esfera azul translúcida alrededor del cuerpo mientras está activa Protección; se oculta al cambiar, desactivarla, agotarse o quedar derribado. Caer sobre enemigos produce daño de contacto y no daña al enemigo como un pisotón.
Validación local v7: 25 líneas aprobadas, incluidos recogida física, 100 estrellas, cuentas independientes, vida extra, reanimación cooperativa, recuperación de 3 s, caída sobre enemigos y esfera de protección.

## Revisión 8: corrección de estrellas, protección y paso cooperativo
Esta revisión sustituye el comportamiento de la revisión 7: ahora hay 108 estrellas en total para toda la partida, en grupos cortos con tramos vacíos. Cada estrella solo se recoge una vez. Desaparece para ambos jugadores y suma exclusivamente al contador del primer jugador que la toma. Las 100 estrellas y la vida extra siguen siendo de cada personaje, sin duplicar los coleccionables del mapa. Los contadores ESTRELLAS se muestran en amarillo y negrita bajo la salud de cada jugador.
Protección del mago bloquea totalmente el daño de ataques, contacto y peligros ambientales mientras esté activa. No reduce el daño a la mitad. Al cambiar de magia o agotarse vuelve a ser vulnerable.
El mago puede agacharse con abajo en local, o S/abajo online, disparar su ataque básico y usar fuego/hielo agachado. La colisión se reduce y el proyectil sale a menor altura.
El paso del molino abre cuando el guerrero está de pie sobre el piso azul y el mago sobre el morado. No exige mantener habilidades ni esperar tres segundos. La lógica anterior permanece comentada en VillageGame.cs.
Validación local: 17 líneas de comprobación aprobadas, incluida recogida única, contadores individuales, inmunidad de Protección, disparos agachado y apertura por presencia en los pisos.

Validación definitiva v8: 17 comprobaciones locales y 20 comprobaciones por proceso online aprobadas con dos jugadores conectados por Photon Fusion. Verifica recogida única compartida, contadores propios, protección total, agacharse y disparar y apertura de pisos sin botones. Ambas escenas contienen 108 estrellas en grupos. Compilación Windows correcta con 0 errores. Informes v8-play-results.txt y v8-online-host/client.txt.


Hielo: congelación de 3 segundos para enemigos normales y arqueros; cancela ataques, detiene movimiento y animación, y muestra color celeste. Los enemigos grandes de látigo y los del tipo de escudo son inmunes a congelarse. El daño del proyectil se mantiene. La duración restante se replica en Photon; ambas instancias deben usar la misma compilación actualizada.
