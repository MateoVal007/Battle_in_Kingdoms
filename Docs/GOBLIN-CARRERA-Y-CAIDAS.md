# Carrera equipada, caída de bordes y goblin

El guerrero equipado usa `Armature|Jog_Fwd_Loop`, el clip de Universal Animation Library mostrado como `Jog_Fwd` en su visor. Se conserva la vista lateral del plataformas y la dirección libre de supervivencia. La espada y el escudo siguen las manos como objetos rígidos; durante la carrera el escudo conserva su plano vertical.

En supervivencia, el CharacterController resuelve el desplazamiento y la gravedad también sobre el suelo. NavMeshAgent calcula las rutas de clic y las escaleras. Al terminar el soporte físico del piso, el personaje cae sin necesitar pulsar salto. El aterrizaje conserva la posición física y vuelve a conectar la navegación. La reanimación mantiene el punto de muerte, salud completa y consumo de una vida propia.

El modelo `EnemyGoblin.glb` tiene esqueleto y clips de espera, caminata, cuchillazo, impacto y caída. El cuchillo tiene pesos rígidos. El archivo editable está en `D:/discoD/VR/JUEGO_FINAL/Pruebas blend/Goblin_Animado/Goblin_Editable.blend`.

- Plataformas: tres goblins cerca de plaza y molino, en x=124, 139 y 148. Patrullan, persiguen, atacan de cerca y regresan al alejarse. Las dos escenas Pueblo mantienen su geometría anterior.
- Supervivencia: goblins mezclados con los enemigos de las cinco hordas. Persiguen al jugador cercano y atacan con cuchillo; son susceptibles al hielo.
- Prefabs: `Assets/02_Prefabs/Enemies/Goblin_Plataformas.prefab` y `Assets/02_Prefabs/Resources/VillageGoblinVisual.prefab`.
- Menú de regeneración: `Battle in Kingdoms/Actualizar goblin y carrera equipada`.

Las versiones de sesión son `pueblo-v15-bomber` y `kingdoms-survival-v8-bomber`. Para probar online, ambos procesos deben usar esta misma versión del juego.

La carpeta `Docs/Verification` contiene los resultados de presentación, mecánicas, regresión y dos procesos. Las pruebas verifican caídas sin salto, escaleras por clic, combate, protección, hielo, T, estrellas compartidas, reinicio por derrota de ambos y las cinco hordas.
