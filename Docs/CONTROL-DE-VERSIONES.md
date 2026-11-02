# Control de versiones de Battle in Kingdoms

## Estado de esta preparacion
Remoto: https://github.com/MateoVal007/Battle_in_Kingdoms.git. La carpeta ya es repositorio Git, pero su rama local sigue en master, sin commits. origin/main contiene README.md. Esta configuracion no cambia ramas, no prepara archivos, no crea commits y no hace push. La importacion inicial sigue pendiente; no confundir repositorio conectado con proyecto respaldado.

## Reproducibilidad
Usar Unity 6000.5.6f1, confirmado en ProjectSettings/ProjectVersion.txt. Conservar Assets y sus .meta, Packages/manifest.json, Packages/packages-lock.json y ProjectSettings completos. No actualizar paquetes individualmente para intentar arreglar un conflicto. ProjectSettings ya usa Force Text y Visible Meta Files; no fue necesario cambiarlos.

## Reglas y comprobaciones
.gitignore excluye caches, builds y archivos del IDE, incluidos .slnx. .vsconfig se conserva porque puede describir herramientas compartidas. .gitattributes normaliza texto y marca binarios; nunca usa merge=union. .editorconfig alinea formato de edicion sin reformatear archivos existentes.

Ejecutar desde la raiz:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Check-UnityRepository.ps1
```

El hook local de pre-commit revisa lo preparado: caches incluidos por error, marcas de conflicto, archivos >=100 MiB, pares asset/meta, GUID duplicados, colisiones de mayusculas, JSON de paquetes y ajustes de serializacion. Un resultado OK no reemplaza compilar ni jugar la escena. En otra computadora o clon hay que activar el hook con `git config --local core.hooksPath .githooks` y configurar alli la ruta de UnityYAMLMerge.

## Flujo del equipo
Como referencia del laboratorio: main estable, Dev para integrar y feature/<tema> para una tarea. No se crearon ramas automaticamente. Acordar ese flujo con el equipo; para trabajo individual puede bastar main y ramas de tarea. Revisar rama y git status antes de editar. Hacer commits pequenos y coherentes, incluyendo dependencias y metadatos. Integrar por Pull Request y probar en Unity. Nunca hacer force-push sobre ramas compartidas para quitar un error; valorar revert. Para recuperar commits, revisar reflog antes de borrar o resetear.

Antes de cambiar rama, guardar escenas y cerrar Unity si cambiara la estructura de assets/paquetes. Commit autorizado o stash con `-u` incluye trabajo no seguido; un stash no es un respaldo remoto. No ejecutar pull con cambios locales sin revisar. Se configuro pull.ff=only para detener una integracion que requiera decidir entre merge/rebase.

## Conflictos frecuentes y prevencion
| Caso | Prevencion y resolucion |
|---|---|
| Dos personas editan la misma escena/prefab | Repartir escenas, usar prefabs anidados o escenas aditivas. Revisar el diff. UnityYAMLMerge ayuda, pero validar jerarquias y referencias en Unity. |
| Accept Both duplica logica | Combinar intencion manualmente; comprobar inicializacion, eventos y componentes, no solo que compile. |
| Se pierde o cambia un .meta | Mantener GUID original. Mover asset y .meta juntos; no regenerar metas para resolver conflictos. |
| Delete/Modify o Add/Add | Acordar cual archivo debe existir y su GUID; no elegir un lado a ciegas. |
| Cambios de ProjectSettings | Coordinar Input, capas, fisicas y render pipeline. Revisar cada cambio y no pisar configuracion compartida. |
| Distinta version de Unity/paquetes | Respetar ProjectVersion y packages-lock. Migraciones deliberadas en rama propia, no abrir con otra version por comodidad. |
| Modelo/imagen/audio en conflicto | No fusionar binarios. Acordar responsable o elegir version respaldada. LFS facilita almacenamiento, no mezcla modelos. |
| Cache ya versionada | .gitignore no afecta archivos seguidos. Retirar del indice solo despues de revisar y con autorizacion, sin borrar copia local. |
| Rama equivocada o desactualizada | Revisar git status, sincronizar y planificar integracion. No copiar proyectos enteros entre carpetas para simular ramas. |

## UnityYAMLMerge
Configurado solo en este repositorio como herramienta de resolucion manual para Unity 6000.5.6f1. Cuando exista un conflicto real en .unity/.prefab, usar `git mergetool --tool=unityyamlmerge -- ruta/al/archivo`. No se ejecuto ningun merge durante esta preparacion. Revisar el resultado en Unity antes de marcar resuelto. No se activa como union automatica ni garantiza resolver todos los conflictos.

## Archivos grandes
La inspeccion inicial encontro assets pequenos, sin modelos grandes en esta carpeta. Los modelos del otro proyecto todavia no estan aqui. Antes de incorporar GLB/FBX/BLEND grandes, decidir entre assets optimizados y Git LFS; evaluar sus cuotas. No activar LFS para todos los archivos sin necesidad. Un .blend de 180-220 MB requiere tratamiento previo; .gitignore no debe ocultar todos los modelos.

## Respaldo y publicacion
Ningun archivo nuevo esta respaldado por un commit o por GitHub hasta completar esos pasos con autorizacion. Los hooks locales pueden omitirse y no equivalen a protecciones de GitHub. Reglas de rama, revisiones obligatorias y CI quedan para configurar cuando se publique el proyecto; no se cambiaron ajustes remotos ni se agregaron servicios de pago.

Fuentes: https://github.com/github/gitignore/blob/main/Unity.gitignore y https://docs.unity.com/en-us/engine/6000.3/manual/get-started/project-configuration/version-control/smart-merge . Se consulto la practica local como referencia de problemas, no como orden de provocar conflictos en este juego.
