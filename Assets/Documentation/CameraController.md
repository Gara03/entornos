# CameraController

## Descripción
El script `CameraController` se encarga de controlar la cámara en Unity, permitiendo que siga al jugador y ajuste su rotación e inclinación basándose en la entrada del ratón. Proporciona una vista en tercera persona con un desplazamiento configurable desde el jugador.

## Funcionalidades

### LateUpdate
Comprueba si la referencia al jugador está asignada y, si no, muestra una advertencia. Llama a los métodos para manejar la rotación de la cámara y actualizar su posición.

### HandleCameraRotation
Obtiene la entrada del ratón para la rotación de la cámara, modifica los ángulos de rotación (yaw y pitch) y limita la inclinación de la cámara dentro de los valores mínimos y máximos configurados.

### UpdateCameraPosition
Calcula la nueva dirección de la cámara basándose en la rotación y el desplazamiento configurados, posiciona la cámara en función del jugador y el nuevo desplazamiento, y asegura que la cámara siempre mire al jugador.

## Uso
Este script está asociado a la MainCamera de la escena de juego.
