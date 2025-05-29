# DetectPlayerCollision

## Descripción
El script `DetectPlayerCollision` se encarga de detectar colisiones entre el jugador y un objeto, como una moneda, en Unity. Si el jugador colisiona con el objeto y no es un zombi, se reproduce un sonido de recogida, se incrementa el contador de monedas del jugador y se destruye el objeto.

## Funcionalidades

### OnTriggerEnter
Detecta colisiones con otros objetos utilizando un trigger. Si el objeto colisionado es el jugador y no es un zombi, incrementa el contador de monedas del jugador, reproduce un sonido de recogida y destruye el objeto.

## Uso
Este script está asociado al prefab `coin`.