# Propiedades Serializables Modificables desde Unity

## LevelManager

### Prefabs
- **playerPrefab**: Prefab del jugador humano.
- **zombiePrefab**: Prefab del zombi.

### Team Settings
- **numberOfHumans**: Número de jugadores humanos.
- **numberOfZombies**: Número de zombis.

### Game Mode Settings
- **gameMode**: Selecciona el modo de juego (`Tiempo` o `Monedas`).
- **minutes**: Tiempo de partida en minutos para el modo tiempo.

## LevelBuilder

### Prefabs
- **floorPrefabs**: Array con los prefabs de suelo.
- **obstaclesPrefabs**: Array con los prefabs de ítems decorativos.
- **cornerPrefab**: Prefab para las esquinas.
- **wallPrefab**: Prefab para los muros.
- **doorPrefab**: Prefab para las puertas.
- **doorHolePrefab**: Prefab para el trozo de muro que incluye puerta.
- **exteriorPrefab**: Prefab para el borde exterior.
- **coinPrefab**: Prefab para las monedas.

### Room Settings
- **numberOfRooms**: Número total de salas.
- **roomWidth**: Ancho de cada sala.
- **roomLength**: Largo de cada sala.
- **ítemsDensity**: Densidad de elementos decorativos [%].
- **coinsDensity**: Densidad de monedas [%].

## PlayerController

### Stats
- **CoinsCollected**: Número de monedas recolectadas.

### Character settings
- **isZombie**: Estado del jugador (humano o zombi).
- **uniqueID**: Identificador único del jugador.

### Movement Settings
- **moveSpeed**: Velocidad de movimiento.
- **zombieSpeedModifier**: Modificador de velocidad para zombis.
- **animator**: Referencia al Animator.
- **cameraTransform**: Referencia a la cámara.

## CameraController

### Configuración de la Cámara
- **player**: Referencia al jugador.
- **offset**: Desplazamiento desde el jugador.
- **rotationSpeed**: Velocidad de rotación.
- **pitchSpeed**: Velocidad de inclinación (eje Y).
- **minPitch**: Ángulo mínimo de inclinación.
- **maxPitch**: Ángulo máximo de inclinación.

## CameraSwitcher

### Configuración de la Cámara
- **mainCamera**: La cámara principal.
- **switchKey**: Tecla para alternar la vista.
- **transitionSpeed**: Velocidad de la transición.
- **topDownHeight**: Altura para la vista cenital.
- **topDownRotation**: Rotación para la vista cenital (mirando hacia abajo).

## RotatePrefab

### Configuración de la Rotación
- **rotationSpeed**: Velocidad de rotación en grados por segundo.

## DetectPlayerCollision

### Configuración del Sonido
- **pickupSound**: Sonido al recoger la moneda.

## ZombieCollisionHandler

(No tiene propiedades serializables configurables desde Unity)

## UniqueIdGenerator

(No tiene propiedades serializables configurables desde Unity)


