# MenuManager

## Descripción
El script `MenuManager` se encarga de gestionar el menú principal del juego en Unity. Está asociado al canvas de la escena `MenuScene` y proporciona funcionalidades para iniciar el juego y salir de la aplicación. El menú principal consta de dos opciones: "Jugar" y "Salir".

## Funcionalidades

### Awake
El método `Awake` se asegura de que el tiempo del juego esté restaurado al valor normal (`1f`) cuando se carga la escena del menú. Esto es útil para reiniciar el tiempo en caso de que haya sido pausado en una escena anterior.


### StartGame
El método `StartGame` carga la escena principal del juego.


### QuitGame
El método `QuitGame` cierra la aplicación. Si se está ejecutando en el editor de Unity, detiene la reproducción. Si se está ejecutando en una build, cierra la aplicación.


## Uso
Este script está asociado al Canvas de la escena `MenuScene`.
Los botones `StartButton` y `QuitButton` están asociados a los métodos `StartGame` y `QuitGame` respectivamente mediante el evento `OnClick`.
