# LevelBuilder

## Descripción
El script `LevelBuilder` se encarga de generar el nivel del juego en Unity, incluyendo suelos, paredes, ítems decorativos, monedas y el borde exterior. Proporciona métodos para construir el nivel y obtener información sobre los puntos de aparición y las monedas generadas.

## Funcionalidades

### Awake
Inicializa el objeto `RoomsParent` que agrupa todas las salas en la jerarquía.

### Build
Inicia la creación de las habitaciones y el nivel completo.

### CreateRooms
Crea una matriz de habitaciones y calcula los puntos de aparición.

### CreateRoom
Crea una habitación con suelos y paredes.

### CreateFloor
Crea una cuadrícula de baldosas para el suelo de una habitación.

### CreateWalls
Coloca las esquinas, muros y puertas en los bordes de una habitación.

### PlaceElement
Instancia un prefab en una posición específica con una rotación específica.

### CreateExterior
Crea el muro exterior alrededor de las salas.

### CreateDecorativeItem
Coloca ítems decorativos en las baldosas del suelo.

### CreateCoin
Coloca monedas en las baldosas del suelo.

### ShouldPlaceItem
Determina si se debe colocar un ítem decorativo basado en la densidad configurada.

### ShouldPlaceCoin
Determina si se debe colocar una moneda basada en la densidad configurada.

### GetHumanSpawnPoints
Obtiene la lista de puntos de aparición de humanos.

### GetZombieSpawnPoints
Obtiene la lista de puntos de aparición de zombies.

### GetCoinsGenerated
Obtiene el número de monedas generadas.

## Uso
El script `LevelBuilder` está asociado al objeto LevelManager de la escena de juego.
el script LevelManager invoca al método `Build` para construir el nivel.
