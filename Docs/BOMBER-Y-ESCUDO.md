# Duende, Bomber y congelación del lanzadagas

El duende usa una escala cercana al guerrero en ambos niveles. Su colisión en plataformas crece con el cuerpo y conserva los pies apoyados en el suelo.

El Bomber usa `BomberEnemy.glb`, preparado con esqueleto, espera, caminata, retirada, lanzamiento, daño y caída. Mantiene distancia del jugador cercano y continúa lanzando mientras retrocede. Las bombas siguen una trayectoria con gravedad, rebotan al tocar el escenario y explotan después de la mecha. La explosión daña una vez y respeta la protección del mago y la recuperación tras daño.

En Pueblo hay dos Bomber (x=36 y x=176). En supervivencia aparecen desde la tercera horda. La explosión incluye un shader de fuego y humo, brasas, destello y onda expansiva; conserva el daño único y su alcance. Los prefabs están en `Assets/02_Prefabs/Enemies/Bomber_Plataformas.prefab` y `Assets/02_Prefabs/Resources/VillageBomberVisual.prefab`. El archivo editable está en `D:/discoD/VR/JUEGO_FINAL/Pruebas blend/Bomber_Animado/Bomber_Editable.blend`.

El lanzadagas resiste el hielo mientras conserva escudo. El proyectil que destruye el escudo queda absorbido; un siguiente impacto de hielo ya lo congela durante tres segundos, con color celeste y animación detenida. Los enemigos grandes conservan su inmunidad.

Las sesiones usan `pueblo-v15-bomber` y `kingdoms-survival-v8-bomber`. Ambos jugadores deben ejecutar la misma versión para compartir las bombas y sus explosiones. Los resultados de pruebas locales, regresión y dos procesos están en `Docs/Verification` con el prefijo `bomber-`.
