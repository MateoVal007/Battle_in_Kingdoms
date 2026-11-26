# Presentación de objetos y movimientos

La poción añadida en Models se usa como salud en Pueblo y Supervivencia, incluidos los premios de cofres. No cambia las cantidades de curación ni concede vidas extra. El modelo de juego conserva su textura, está normalizado y usa 18 000 caras. Los suministros de supervivencia sustituyen su antigua representación al iniciar; el constructor también usa la poción para escenas nuevas.

Arquer reemplaza visualmente a los arqueros del Pueblo. Sigue en su montaña y conserva los ataques hacia abajo. La fuente reparada está en Pruebas blend/Arquer_Reparado/Arquer_Reparado.blend. Se cerraron costuras de la malla, se reconstruyó el estribo y el carril de la ballesta y se redujo la malla de juego a unas 159 000 caras. Tiene reposo, disparo con retroceso, daño y caída. La malla del cuerpo no tiene bordes abiertos ni no manifold tras la reparación.

El guerrero sin equipo usa Punch_Jab para el ataque de pie. Su copia para ese movimiento tiene puños cerrados rígidos; no se cambiaron la caminata, el reposo ni el golpe agachado aprobados. Al agotarse la armadura, se reproduce Idle_Shield_Break durante 0.65 s, se ocultan espada y escudo y se vuelve al modelo sin equipo. La reacción funciona en ambos niveles y se reconstruye también desde los estados replicados. Se conserva el escudo rígido. El animador de estas acciones se evalúa incluso cuando acaba de activarse.

Los prefabs de personajes, enemigos, objetos, peligros y modelos de nivel están en Assets/02_Prefabs/Resources. Se conservaron GUIDs y claves Resources.Load para mantener referencias. Los materiales VFX siguen en su carpeta original. La opción Battle in Kingdoms/Actualizar poción arquero y movimientos permite reconstruir estos prefabs sin reconstruir el nivel.

Fuentes: Punch_Jab de Universal Animation Library 1 ya incluida en el proyecto e Idle_Shield_Break de la versión Standard de [Universal Animation Library 2](https://quaternius.com/packs/universalanimationlibrary2.html). Se conserva su licencia CC0 junto al FBX de la Library 2.

Verificación: pruebas de presentación y recogida en ambos niveles; muestras laterales de ambas animaciones hacia derecha e izquierda; dos procesos Photon comprueban carga de prefabs, reacción de rotura en anfitrión e invitado, T y reinicio de supervivencia sin desconectar. Informes en Docs/Verification/Presentacion-objetos-animaciones-local.txt y Presentacion-objetos-animaciones-online.txt. No se modificó la geometría de las escenas.
