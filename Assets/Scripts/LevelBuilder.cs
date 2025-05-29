using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Clase para generar el nivel del juego, incluyendo suelos, paredes, ítems decorativos, monedas y el borde exterior.
/// </summary>
public class LevelBuilder : MonoBehaviour
{
    #region Properties

    [Header("Prefabs")]
    [Tooltip("Array con los prefabs de suelo")]
    [SerializeField] private GameObject[] floorPrefabs;

    [Tooltip("Array con los prefabs de ítems decorativos")]
    [SerializeField] private GameObject[] obstaclesPrefabs;

    [Tooltip("Prefab para las esquinas")]
    [SerializeField] private GameObject cornerPrefab;

    [Tooltip("Prefab para los muros")]
    [SerializeField] private GameObject wallPrefab;

    [Tooltip("Prefab para las puertas")]
    [SerializeField] private GameObject doorPrefab;

    [Tooltip("Prefab para el trozo de muro que incluye puerta")]
    [SerializeField] private GameObject doorHolePrefab;

    [Tooltip("Prefab para el borde exterior")]
    [SerializeField] private GameObject exteriorPrefab;

    [Tooltip("Prefab para las monedas")]
    [SerializeField] private GameObject coinPrefab;

    [Header("Room Settings")]
    [Tooltip("Número total de salas")]
    [SerializeField] private int numberOfRooms = 1;

    [Tooltip("Ancho de cada sala")]
    [SerializeField] private int roomWidth = 5;

    [Tooltip("Largo de cada sala")]
    [SerializeField] private int roomLength = 5;

    [Tooltip("Densidad de elementos decorativos [%]")]
    [SerializeField] private float ítemsDensity = 20f;

    [Tooltip("Densidad de monedas [%]")]
    [SerializeField] private float coinsDensity = 20f;

    private readonly float tileSize = 1.0f;
    private Transform roomParent;

    private int CoinsGenerated = 0;
    private HashSet<Vector3> humanSpawnPoints = new HashSet<Vector3>();
    private HashSet<Vector3> zombieSpawnPoints = new HashSet<Vector3>();

    #endregion

    #region Unity game loop methods

    private void Awake()
    {
        GameObject parentObject = new GameObject("RoomsParent");
        roomParent = parentObject.transform;
    }

    #endregion

    #region World building methods

    public void Build()
    {
        CreateRooms(roomWidth, roomLength, numberOfRooms);
    }

    /// <summary>
    /// Crea una matriz de habitaciones y calcula los puntos de aparición.
    /// </summary>
    private void CreateRooms(int roomWidth, int roomLength, int numberOfRooms)
    {
        int rows = Mathf.CeilToInt(Mathf.Sqrt(numberOfRooms)); // Filas
        int cols = Mathf.CeilToInt((float)numberOfRooms / rows); // Columnas

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                float x = j * roomWidth;  // Posición X
                float z = i * roomLength; // Posición Z

                CreateRoom(roomWidth, roomLength, x, z);

                Vector3 spawnPoint = new Vector3(x + roomWidth / 2, 2, z + roomLength / 2);

                Debug.Log($"[LevelBuilder] Spawn point calculado: {spawnPoint}");
                if (i % 2 == 0 && j % 2 == 0)
                {
                    humanSpawnPoints.Add(spawnPoint);
                }
                else
                {
                    zombieSpawnPoints.Add(spawnPoint);
                }
            }
        }

        CreateExterior(rows, cols, roomWidth, roomLength);
    }

    /// <summary>
    /// Crea una habitación con suelos y paredes.
    /// </summary>
    private void CreateRoom(int width, int length, float offsetX, float offsetZ)
    {
        if (floorPrefabs == null || floorPrefabs.Length == 0)
        {
            Debug.LogError("No se han asignado prefabs de suelo.");
            return;
        }

        Debug.Log($"Creando habitación en ({offsetX}, {offsetZ}) con dimensiones {width}x{length}...");
        CreateFloor(width, length, offsetX, offsetZ);
        Debug.Log($"Habitación generada en ({offsetX}, {offsetZ}).");

        Debug.Log($"Creando paredes en ({offsetX}, {offsetZ}) con dimensiones {width}x{length}...");
        CreateWalls(width, length, offsetX, offsetZ);
        Debug.Log($"Paredes generadas en ({offsetX}, {offsetZ}).");
    }

    /// <summary>
    /// Crea una cuadrícula de baldosas para el suelo de una habitación.
    /// </summary>
    private void CreateFloor(int width, int length, float offsetX, float offsetZ)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                int randomIndex = Random.Range(0, floorPrefabs.Length);
                GameObject selectedFloorPrefab = floorPrefabs[randomIndex];

                Vector3 tilePosition = new Vector3(x * tileSize + offsetX, 0, z * tileSize + offsetZ);
                GameObject tile = Instantiate(selectedFloorPrefab, tilePosition, Quaternion.identity, roomParent);
                tile.name = $"Tile_{x}_{z}";

                CreateDecorativeItem(x, z, width, length, tilePosition);
                CreateCoin(x, z, width, length, tilePosition);
            }
        }

        Debug.Log($"Suelo generado con dimensiones {width}x{length} usando {floorPrefabs.Length} prefabs diferentes.");
    }

    /// <summary>
    /// Coloca las esquinas, muros y puertas en los bordes de una habitación.
    /// </summary>
    private void CreateWalls(int width, int length, float offsetX, float offsetZ)
    {
        PlaceElement(cornerPrefab, 0 + offsetX, 0 + offsetZ, Quaternion.identity);                        // Esquina inferior izquierda
        PlaceElement(cornerPrefab, (width - 1) * tileSize + offsetX, 0 + offsetZ, Quaternion.identity);   // Esquina inferior derecha
        PlaceElement(cornerPrefab, 0 + offsetX, (length - 1) * tileSize + offsetZ, Quaternion.identity);  // Esquina superior izquierda
        PlaceElement(cornerPrefab, (width - 1) * tileSize + offsetX, (length - 1) * tileSize + offsetZ, Quaternion.identity); // Esquina superior derecha

        int doorPositionBottom = (width - 1) / 2;  // Puerta en pared inferior
        int doorPositionTop = (width - 1) / 2;     // Puerta en pared superior
        int doorPositionLeft = (length - 1) / 2;   // Puerta en lateral izquierdo
        int doorPositionRight = (length - 1) / 2;  // Puerta en lateral derecho

        for (int x = 1; x < width - 1; x++) // Borde inferior y superior
        {
            if (x == doorPositionBottom)
            {
                PlaceElement(doorPrefab, x * tileSize + offsetX, 0 + offsetZ, Quaternion.identity); // Puerta en borde inferior
                PlaceElement(doorHolePrefab, x * tileSize + offsetX, 0 + offsetZ, Quaternion.identity); // Hueco de la puerta
            }
            else
                PlaceElement(wallPrefab, x * tileSize + offsetX, 0 + offsetZ, Quaternion.identity); // Muro en borde inferior

            if (x == doorPositionTop)
            {
                PlaceElement(doorPrefab, x * tileSize + offsetX, (length - 1) * tileSize + offsetZ, Quaternion.Euler(0, 180, 0)); // Puerta en borde superior
                PlaceElement(doorHolePrefab, x * tileSize + offsetX, (length - 1) * tileSize + offsetZ, Quaternion.identity); // Hueco de la puerta
            }
            else
                PlaceElement(wallPrefab, x * tileSize + offsetX, (length - 1) * tileSize + offsetZ, Quaternion.identity); // Muro en borde superior
        }

        for (int z = 1; z < length - 1; z++) // Laterales izquierdo y derecho
        {
            if (z == doorPositionLeft)
            {
                PlaceElement(doorPrefab, 0 + offsetX, z * tileSize + offsetZ, Quaternion.Euler(0, 90, 0)); // Puerta en lateral izquierdo (rotada 90 grados)
                PlaceElement(doorHolePrefab, 0 + offsetX, z * tileSize + offsetZ, Quaternion.Euler(0, 90, 0)); // hueco de la puerta
            }
            else
                PlaceElement(wallPrefab, 0 + offsetX, z * tileSize + offsetZ, Quaternion.Euler(0, 90, 0)); // Muro en lateral izquierdo (rotado 90 grados)

            if (z == doorPositionRight)
            {
                PlaceElement(doorPrefab, (width - 1) * tileSize + offsetX, z * tileSize + offsetZ, Quaternion.Euler(0, 270, 0)); // Puerta en lateral derecho (rotada 270 grados)
                PlaceElement(doorHolePrefab, (width - 1) * tileSize + offsetX, z * tileSize + offsetZ, Quaternion.Euler(0, 90, 0)); //hueco de la puerta
            }
            else
                PlaceElement(wallPrefab, (width - 1) * tileSize + offsetX, z * tileSize + offsetZ, Quaternion.Euler(0, 90, 0)); // Muro en lateral derecho (rotado 90 grados)
        }
    }

    /// <summary>
    /// Instancia un prefab en una posición específica con una rotación específica.
    /// </summary>
    private void PlaceElement(GameObject prefab, float x, float z, Quaternion rotation)
    {
        Vector3 position = new Vector3(x, 0, z);
        Instantiate(prefab, position, rotation, roomParent);
    }

    /// <summary>
    /// Crea el muro exterior alrededor de las salas.
    /// </summary>
    private void CreateExterior(int rows, int cols, int roomWidth, int roomLength)
    {
        float minX = -tileSize;                        // Borde izquierdo
        float maxX = cols * roomWidth;         // Borde derecho
        float minZ = -tileSize;                        // Borde inferior
        float maxZ = rows * roomLength;        // Borde superior

        for (float x = minX; x <= maxX; x += tileSize)
        {
            PlaceElement(exteriorPrefab, x, minZ, Quaternion.identity); // Fila inferior
            PlaceElement(exteriorPrefab, x, maxZ, Quaternion.identity); // Fila superior
        }

        for (float z = minZ; z < maxZ; z += tileSize)
        {
            PlaceElement(exteriorPrefab, minX, z, Quaternion.identity); // Columna izquierda
            PlaceElement(exteriorPrefab, maxX, z, Quaternion.identity); // Columna derecha
        }
    }

    /// <summary>
    /// Coloca ítems decorativos en las baldosas del suelo.
    /// </summary>
    private void CreateDecorativeItem(int x, int z, int width, int length, Vector3 tilePosition)
    {
        bool widthBorderCondition = x > 0 && x < (width - 1);
        bool lengthBorderCondition = z > 0 && z < (length - 1);

        int doorPositionBottom = (width - 1) / 2;  // Puerta en pared inferior
        int doorPositionTop = (width - 1) / 2;     // Puerta en pared superior
        int doorPositionLeft = (length - 1) / 2;   // Puerta en lateral izquierdo
        int doorPositionRight = (length - 1) / 2;  // Puerta en lateral derecho

        bool blockingDoorConditionX = x != doorPositionBottom && x != doorPositionTop;
        bool blockingDoorConditionZ = z != doorPositionLeft && z != doorPositionRight;

        bool totalCondition = widthBorderCondition && lengthBorderCondition && blockingDoorConditionX && blockingDoorConditionZ;

        if (totalCondition && ShouldPlaceItem())
        {
            int randomIndex = Random.Range(0, obstaclesPrefabs.Length);
            GameObject obstaclePrefab = obstaclesPrefabs[randomIndex];
            obstaclePrefab.tag = "Obstacle";
            PlaceElement(obstaclePrefab, tilePosition.x, tilePosition.z, Quaternion.identity);
        }
    }

    /// <summary>
    /// Coloca monedas en las baldosas del suelo.
    /// </summary>
    private void CreateCoin(int x, int z, int width, int length, Vector3 tilePosition)
    {
        bool widthBorderCondition = x > 0 && x < (width - 1);
        bool lengthBorderCondition = z > 0 && z < (length - 1);

        bool totalCondition = widthBorderCondition && lengthBorderCondition;

        if (totalCondition && ShouldPlaceCoin())
        {
            float checkRadius = 0.5f; // Ajusta según el tamaño de los prefabs

            Collider[] colliders = Physics.OverlapSphere(tilePosition, checkRadius);
            bool isPositionOccupied = colliders.Any(collider => collider.CompareTag("Obstacle"));

            if (!isPositionOccupied) // Si no hay obstáculos, colocar la moneda
            {
                PlaceElement(coinPrefab, tilePosition.x, tilePosition.z, Quaternion.identity);
                CoinsGenerated++;
            }
        }
    }

    /// <summary>
    /// Determina si se debe colocar un ítem decorativo basado en la densidad configurada.
    /// </summary>
    private bool ShouldPlaceItem()
    {
        float randomValue = Random.Range(0, 100);
        return randomValue < ítemsDensity;
    }

    /// <summary>
    /// Determina si se debe colocar una moneda basada en la densidad configurada.
    /// </summary>
    private bool ShouldPlaceCoin()
    {
        float randomValue = Random.Range(0, 100);
        return randomValue < coinsDensity;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Obtiene la lista de puntos de aparición de humanos.
    /// </summary>
    /// <returns>Lista de puntos de aparición de humanos.</returns>
    public List<Vector3> GetHumanSpawnPoints()
    {
        return humanSpawnPoints.ToList();
    }

    /// <summary>
    /// Obtiene la lista de puntos de aparición de zombies.
    /// </summary>
    /// <returns>Lista de puntos de aparición de zombies.</returns>
    public List<Vector3> GetZombieSpawnPoints()
    {
        return zombieSpawnPoints.ToList();
    }

    /// <summary>
    /// Obtiene el número de monedas generadas.
    /// </summary>
    /// <returns>Número de monedas generadas.</returns>
    public int GetCoinsGenerated()
    {
        return CoinsGenerated;
    }

    #endregion
}







