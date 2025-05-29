# Resumen del Juego y sus Mecánicas

## Descripción General
El juego es un juego de supervivencia y recolección en el que los jugadores pueden ser humanos o zombis. Los humanos deben recolectar monedas mientras evitan ser convertidos en zombis. Los zombis intentan convertir a los humanos al tocarlos. El juego tiene dos modos principales: basado en tiempo y basado en la recolección de monedas.

## Mecánicas del Juego

1. **Modos de Juego:**
   - **Modo Tiempo:** Los jugadores deben sobrevivir durante un tiempo determinado. El juego termina cuando el tiempo se agota.
   - **Modo Monedas:** Los jugadores deben recolectar todas las monedas en el nivel. El juego termina cuando se recolectan todas las monedas.

2. **Control del Jugador:**
   - Los jugadores pueden moverse utilizando las teclas de dirección (W, A, S, D) o las flechas del teclado.
   - [Funcionalidad para depuración] Los jugadores pueden cambiar entre ser humano y zombi presionando las teclas `H` y `Z`.

3. **Interacción con el Entorno:**
   - **Recolección de Monedas:** Los humanos pueden recolectar monedas al tocarlas. Cada moneda recolectada incrementa el contador de monedas del jugador.
   - **Conversión a Zombi:** Si un zombi colisiona con un humano, el humano se convierte en zombi.

4. **Cámara:**
   - La cámara sigue al jugador en tercera persona.
   - [Funcionalidad para depuración] Existe un mecanismo que permite alternar entre la vista en tercera persona y una vista cenital presionando la tecla `C`.

5. **Interfaz de Usuario:**
   - La interfaz de usuario muestra el número de humanos y zombis, el tiempo restante (en el modo basado en tiempo) y el número de monedas recolectadas (en el modo basado en monedas).
   - Un panel de fin de juego se muestra cuando el juego termina, permitiendo al jugador regresar al menú principal.

## Scripts Principales

1. **LevelManager:**
   - Gestiona la lógica del nivel, incluyendo la generación de equipos, el manejo de modos de juego y la actualización de la interfaz de usuario.
   - Controla el cambio de estado entre humano y zombi y maneja el fin del juego.

2. **PlayerController:**
   - Controla el movimiento y las animaciones del jugador.
   - Maneja la recolección de monedas y actualiza la interfaz de usuario correspondiente.

3. **CameraController:**
   - Controla la cámara, permitiendo que siga al jugador y ajuste su rotación e inclinación basándose en la entrada del ratón.

4. **CameraSwitcher:**
   - Alterna la vista de la cámara entre una vista en tercera persona y una vista cenital.

5. **LevelBuilder:**
   - Genera el nivel del juego, incluyendo suelos, paredes, ítems decorativos, monedas y el borde exterior.

6. **UniqueIdGenerator:**
   - Genera identificadores únicos basados en combinaciones de adjetivos, monstruos y obras.

7. **RotatePrefab:**
   - Rota un GameObject a una velocidad constante.

8. **ZombieCollisionHandler:**
   - Maneja las colisiones entre un zombi y otros objetos, convirtiendo a los humanos en zombis al tocarlos.

9. **DetectPlayerCollision:**
   - Detecta colisiones entre el jugador y un objeto, como una moneda, y maneja la recolección de monedas.

## Flujo del Juego

1. **Inicio del Juego:**
   - El jugador selecciona el modo de juego desde el menú principal.
   - El nivel se genera y los equipos de humanos y zombis se instancian en sus puntos de aparición.

2. **Durante el Juego:**
   - Los jugadores humanos recolectan monedas y evitan a los zombis.
   - Los zombis intentan convertir a los humanos al tocarlos.
   - La cámara sigue al jugador y puede alternar entre vistas.

3. **Fin del Juego:**
   - En el modo basado en tiempo, el juego termina cuando el tiempo se agota.
   - En el modo basado en monedas, el juego termina cuando se recolectan todas las monedas.
   - Se muestra un panel de fin de juego y el jugador puede regresar al menú principal.




