# UniqueIdGenerator

## Descripción
El script `UniqueIdGenerator` se encarga de generar identificadores únicos basados en combinaciones de adjetivos, monstruos y obras. Proporciona métodos para generar estos identificadores y obtener la lista de IDs generados.

## Funcionalidades

### GenerateUniqueID
Genera un identificador único combinando un adjetivo, un monstruo y una obra. Si no se puede generar un identificador único después de 3 intentos, utiliza `System.Guid.NewGuid().ToString()` para generar un identificador único.

### GenerateRandomID
Genera un identificador aleatorio basado en combinaciones de adjetivos, monstruos y obras.

### GetGeneratedIDs
Obtiene la lista de identificadores únicos generados.

## Uso
Este script está asociado al objeto LevelManager permite ser usado desde el script LevelManager para generar los identificadores únicos para cada personaje del juego, tanto humano como zombi.

