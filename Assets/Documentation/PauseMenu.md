# PauseMenu

## Descripción
El script `PauseMenu` se encarga de gestionar el menú de pausa del juego en Unity. Proporciona funcionalidades para pausar y reanudar el juego, así como para salir al menú principal.

## Funcionalidades

### Update
El método `Update` detecta si el jugador presiona la tecla Escape para alternar entre pausar y reanudar el juego.

### PauseGame
El método `PauseGame` pausa el juego, muestra el panel de pausa y gestiona el cursor.

### ResumeGame
El método `ResumeGame` reanuda el juego, oculta el panel de pausa y gestiona el cursor.

### QuitGame
El método `QuitGame` restaura el tiempo y carga la escena del menú principal.

## Uso
Este script está asociado al CanvasPlayer en la escena del juego.
Los botones `ResumeButton` y `QuitButton` están asociados a los métodos `ResumeGame` y `QuitGame` respectivamente mediante el evento `OnClick`.