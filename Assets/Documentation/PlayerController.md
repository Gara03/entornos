# PlayerController

## Descripción
El script `PlayerController` se encarga de controlar el movimiento y las animaciones del jugador en Unity. También maneja la recolección de monedas y actualiza la interfaz de usuario correspondiente. Proporciona funcionalidades para mover al jugador, manejar animaciones y recolectar monedas.

## Funcionalidades

### Start
Inicializa las referencias necesarias, como el texto de la interfaz de usuario para las monedas, y actualiza la interfaz de usuario al inicio.

### Update
Lee la entrada del teclado para el movimiento del jugador, llama a los métodos para mover al jugador y manejar las animaciones.

### MovePlayer
Calcula la dirección de movimiento en relación a la cámara, ajusta la velocidad si el jugador es un zombi, y mueve al jugador en la dirección deseada.

### HandleAnimations
Controla las animaciones del jugador basadas en la dirección del movimiento.

### CoinCollected
Incrementa el contador de monedas recolectadas si el jugador es humano y actualiza la interfaz de usuario.

### UpdateCoinUI
Actualiza el texto de la interfaz de usuario con el número de monedas recolectadas.

## Uso
Este script está asociado a los Prefab Character-Human y Character-Orc (Zombi).