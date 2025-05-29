# LevelManager

## Descripción
El script `LevelManager` se encarga de gestionar la lógica del nivel en Unity, incluyendo la generación de equipos, el manejo de modos de juego y la actualización de la interfaz de usuario. Proporciona métodos para cambiar entre humanos y zombis, manejar el estado del juego y actualizar la interfaz de usuario.

## Funcionalidades

### Awake
Inicializa las referencias necesarias y asegura que el tiempo del juego no esté detenido.

### Start
Configura la interfaz de usuario, obtiene los puntos de aparición y el número de monedas generadas desde `LevelBuilder`, y genera los equipos.

### Update
Maneja la lógica del juego basada en el modo seleccionado (tiempo o monedas) y permite cambiar entre humano y zombi.

### ChangeToZombie
Cambia el jugador actual a un zombi, manteniendo su posición, rotación y identificador único.

### ChangeToHuman
Cambia el jugador actual a un humano, manteniendo su posición y rotación.

### SpawnPlayer
Crea una instancia del jugador en un punto de aparición especificado y asigna la cámara principal.

### SpawnTeams
Genera los equipos de humanos y zombis en los puntos de aparición correspondientes.

### SpawnNonPlayableCharacter
Crea una instancia de un personaje no jugable en un punto de aparición especificado.

### UpdateTeamUI
Actualiza la interfaz de usuario con el número de humanos y zombis.

### HandleTimeLimitedGameMode
Maneja la lógica del modo de juego basado en tiempo, actualizando el temporizador y verificando si el tiempo ha llegado a cero.

### HandleCoinBasedGameMode
Maneja la lógica del modo de juego basado en monedas, actualizando el contador de monedas y verificando si se han recolectado todas las monedas.

### ShowGameOverPanel
Muestra el panel de fin de juego y gestiona el cursor.

### ReturnToMainMenu
Carga la escena del menú principal y gestiona el cursor.

## Uso
Este script está asociado al objeto LevelManager de la escena de juego.
