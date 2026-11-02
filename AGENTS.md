# Battle in Kingdoms

- Proyecto Unity. Leer Docs/CONTROL-DE-VERSIONES.md antes de modificar Git o integrar cambios.
- Editor acordado: el de ProjectSettings/ProjectVersion.txt (actualmente 6000.5.6f1). No actualizar Unity ni paquetes sin una solicitud concreta.
- Mantener los .meta y sus GUID; mover/renombrar assets con sus .meta, preferentemente desde Unity.
- No editar Library, Temp, Logs, Obj ni UserSettings. No reserializar todo el proyecto por un cambio pequeño.
- No hacer push, commits, force-push, reset --hard ni limpiar archivos sin autorización del usuario. No cambiar de rama con trabajo sin guardar.
- Inspeccionar git status y rama actual antes de trabajar. El estado inicial local es master sin commits; origin/main contiene README. No asumir que la importacion inicial se ha completado.
- Escenas/prefabs: limitar modificaciones, preferir prefabs y repartir escenas. No aceptar ambos lados de un conflicto automaticamente.
- Ejecutar tools/Check-UnityRepository.ps1 antes de preparar una entrega; comprobar en Unity los cambios de juego cuando corresponda.
- No incorporar modelos de mas de 100 MiB con Git normal; evaluar LFS primero. Los binarios no se fusionan como texto.
