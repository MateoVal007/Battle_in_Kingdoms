# Pueblo online — prueba de dos jugadores

Escena: Assets/00_Scenes/Pueblo_Online.unity. La escena Pueblo_01_Blockout conserva la prueba local.

## Conectar

1. Abrir la escena online y pulsar Play, o ejecutar Pueblo.exe desde la carpeta completa de la compilación.
2. En una instancia escribir un nombre de sala y pulsar Crear partida (guerrero).
3. En la otra escribir exactamente el mismo nombre y pulsar Entrar a partida (mago).
4. Ambas instancias usan la región US. La simulación espera a los dos jugadores.

Controles en cada equipo: A/D o flechas para moverse; Espacio, W o flecha arriba para saltar; F o K para atacar; G o L para escudo/canalización. La cámara sigue al personaje propio. El anfitrión puede reiniciar con R si ambos caen. Salir de sala vuelve al menú.

Se sincronizan personajes, salud, armadura, encantamientos, postura de escudo/canalización, golpes, enemigos, cofres y recompensas, objetos recogidos, proyectiles, checkpoint, puerta cooperativa y salida. El anfitrión aplica las reglas; el invitado envía entradas y muestra el estado recibido. La recogida de salud reanima al compañero con las reglas del prototipo local.

## Alcance de esta primera integración

Host/Client con Photon Fusion, máximo dos participantes y sala por nombre. No hay migración de anfitrión: si este sale, hay que crear otra partida. El movimiento del invitado todavía espera la respuesta del anfitrión (sin predicción/reconciliación); habrá que mejorar esa respuesta y evaluar latencia con dos equipos. No es una arquitectura final para publicación. Enemigos y otros sistemas heredados todavía se simulan por fotograma en el anfitrión; los personajes reciben entradas en ticks de Fusion. Proyectiles visuales limitados a 32 simultáneos en el invitado.

## Repetir la compilación y la prueba

Menú Battle in Kingdoms > Preparar y compilar prueba online. Guarda el ejecutable en C:/Users/MAQ/Documents/ChatGPT/Juego final/outputs/Pueblo_online. No volver a generar la escena local para actualizar la online: son escenas separadas y los cambios de trazado deben trasladarse deliberadamente.

El ejecutable acepta -village-role host|client y -village-room NOMBRE. Solo para la prueba automatizada: -village-test 1 -village-report RUTA_ABSOLUTA. La prueba mueve cada personaje desde su proceso, salta con el mago, abre un cofre, provoca un derribo, recoge su salud para reanimar y mantiene las dos habilidades en el molino. Los traslados entre casos y el daño de preparación son instrumentación de prueba, no acciones normales de juego.

Verificación realizada el 2 de noviembre de 2026: compilación Windows correcta (0 errores); dos procesos del ejecutable conectados a Photon, región US. Ambos informes aprueban movimiento independiente y salto, apertura de cofre, derribo/reanimación y puerta cooperativa. Informes en Docs/Verification/online-host-results.txt y online-client-results.txt. Se probó en un mismo equipo con dos procesos; falta comprobar dos equipos y condiciones de latencia. SDK instalado reporta Fusion 2.1.4 en ejecución.

Regresión local posterior a la integración: 44/44 comprobaciones aprobadas en Play Mode. La comprobación de metadatos, GUID y archivos del repositorio también pasó.

## Nuevas habilidades y recorrido

Guerrero: S/abajo agacha; G cubre al frente; G+W/arriba cubre arriba; F ataca incluso cubriéndose. Espacio salta, mientras W/arriba solo salta sin el escudo. Mago: Q/E selecciona fuego, hielo o protección; R activa/guarda el objeto. La carga solo se consume mientras esté activo y se muestra con un círculo y segundos restantes. Cambiar de objeto conserva la carga. G/L sigue siendo canalización para la puerta, independiente de los objetos.

Postura, orientación del escudo, inventario, selección, activación y carga restante se replican desde el anfitrión. Al cambiar de ventana se deja de enviar entrada física desde la ventana sin foco. No mezclar el nuevo editor con un ejecutable anterior: cambió la estructura de estado de red; usar la compilación actual en ambos procesos.

El recorrido ahora incluye pendientes y caminos altos separados de la calle baja en 02, 03, 07 y 08. Copia de las escenas anteriores en outputs/Respaldo_diseno_20261102.

Corrección v3: equipo inicial en cofre x=6, cofre de salud adicional x=20. Ataque sin equipo: puñetazo F. El escudo solo funciona después de equiparse. Se retiró la plataforma baja sobre la orilla izquierda del arroyo; la ruta inferior salta desde la orilla x=78 a x=83 y x=88. Piedras de rescate x=80 y x=86.

La conexión usa AppVersion pueblo-v3-solid-equipment para separar versiones incompatibles. Iniciar ambas instancias con esta versión (incluido el nuevo ejecutable). El nombre de sala debe coincidir.

Verificación v3 completada el 3 de noviembre de 2026: Unity Editor como anfitrión y ejecutable Windows como cliente, dos jugadores conectados a Photon. Ambos informes aprueban las 8 comprobaciones online, incluido equipo inicial, postura, escudo superior e inventario. 73 comprobaciones locales aprobadas. Compilación Windows con cero errores.


## Revisión 4: enemigos
Usar la nueva compilación Pueblo.exe en ambas instancias. El protocolo ahora es pueblo-v4-enemies y no comparte partidas con la compilación v3. Incluye persecución terrestre, arquero fijo en la colina del huerto y guardián con látigo en la plaza. El anfitrión resuelve impactos y bloqueos; el invitado recibe las posiciones, avisos, flechas y derrotas. Se conserva el prototipo sin predicción local del cliente.

## Revisión 5
Usar Pueblo.exe actualizado en ambas instancias: protocolo pueblo-v5-layout-gear. Se replican la resistencia del equipo y la pérdida de espada/escudo; cambia el trazado de plataformas y el arquero solo dispara hacia abajo. Las versiones anteriores no comparten sala con esta.

Validación online final v5: 13 comprobaciones aprobadas en anfitrión y 13 en invitado, dos procesos conectados por Photon Fusion. Incluye desgaste y pérdida de equipo replicados. Compilación Windows correcta, 0 errores. La comprobación de pendiente se trasladó al tramo transitable x=40,5 a x=42, porque la posición anterior quedó ocupada por una plataforma del nuevo trazado. Informes v5-final-online-host/client.txt.

## Revisión 6
Usar el ejecutable actualizado en ambas instancias; protocolo pueblo-v6-patrol-spells. Se sincronizan las cargas por disparo del mago, la protección por tiempo, las nuevas patrullas y los objetos y enemigos añadidos a las secciones 9 y 10.

Validación online definitiva v6: 14 comprobaciones aprobadas en cada proceso, anfitrión e invitado conectados a Photon Fusion. Se verifica también la persecución por un tramo libre de otros enemigos estacionarios, manteniendo las colisiones normales. Informes v6-verified-online-host/client.txt. Compilación correcta con 0 errores.

## Revisión 7
Usar el ejecutable actualizado en ambas instancias: protocolo pueblo-v7-personal-stars. El anfitrión valida la recogida y sincroniza las marcas personales de cada estrella, los contadores y vidas extra de ambos jugadores y el tiempo de recuperación. T gasta una vida extra para reanimar al jugador local derribado. La esfera del mago se muestra también en el invitado.

Validación online v7: 19 comprobaciones aprobadas en anfitrión y 19 en invitado con dos procesos conectados a Photon Fusion. Incluye contadores personales, recogida independiente de las mismas estrellas, uso de vida extra mediante entrada de red, recuperación y esfera del mago. Compilación correcta con 0 errores. Informes: v7-online-host.txt y v7-online-client.txt.

## Revisión 8
Protocolo pueblo-v8-shared-stars. Las estrellas son globales y solo su primer recolector suma al contador propio; la recogida desaparece en ambos clientes. Se mantiene la sincronización de contadores individuales, recuperación y vidas extra. Protección anula el daño. El mago se agacha y dispara con S/abajo. El molino abre por estar sobre ambos pisos, sin mantener botones.

Validación definitiva v8: 17 comprobaciones locales y 20 comprobaciones por proceso online aprobadas con dos jugadores conectados por Photon Fusion. Verifica recogida única compartida, contadores propios, protección total, agacharse y disparar y apertura de pisos sin botones. Ambas escenas contienen 108 estrellas en grupos. Compilación Windows correcta con 0 errores. Informes v8-play-results.txt y v8-online-host/client.txt.


Hielo: congelación de 3 segundos para enemigos normales y arqueros; cancela ataques, detiene movimiento y animación, y muestra color celeste. Los enemigos grandes de látigo y los del tipo de escudo son inmunes a congelarse. El daño del proyectil se mantiene. La duración restante se replica en Photon; ambas instancias deben usar la misma compilación actualizada.
