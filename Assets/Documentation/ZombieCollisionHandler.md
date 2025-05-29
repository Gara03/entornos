# ZombieCollisionHandler

## Descripción
El script `ZombieCollisionHandler` se encarga de manejar las colisiones entre un zombi y otros objetos en Unity. Si el objeto con el que colisiona es un jugador humano, lo convierte en zombi y actualiza su estado en el `LevelManager`.

## Funcionalidades

### OnCollisionEnter
Detecta colisiones con otros objetos. Si el objeto colisionado es un jugador humano, lo convierte en zombi y actualiza su estado en el `LevelManager`.

## Uso
Este script está asociado al prefab Characer-orc (zombi).