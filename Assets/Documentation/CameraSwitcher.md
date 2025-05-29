# CameraSwitcher

## Descripción
El script `CameraSwitcher` se encarga de alternar la vista de la cámara entre una vista de tercera persona y una vista cenital en Unity. Proporciona una transición suave entre ambas vistas y permite activar o desactivar el control de la cámara.
Esta funcionalidad está pensada para tener una vista del mapa generado, por lo que no es necesaria en el juego nominal.

## Funcionalidades

### Start
Inicializa la referencia al componente `CameraController` de la cámara principal.

### LateUpdate
Detecta la tecla de alternancia y realiza la transición entre las vistas de la cámara.

### ToggleCameraView
Alterna entre la vista de tercera persona y la vista cenital, calculando las posiciones y rotaciones objetivo para la cámara.

### PerformTransition
Realiza la transición suave de la cámara hacia la posición y rotación objetivo utilizando interpolación.

### EnableCameraController
Activa o desactiva el componente `CameraController` de la cámara principal.

## Uso
Este script está asociado al objeto LevelManager de la escena de juego. La funcionalidad se activa pulsando la tecla C.
