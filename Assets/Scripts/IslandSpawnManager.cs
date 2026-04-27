using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class IslandSpawnEntry
{
    public GameObject prefab;
    [Min(0f)] public float weight = 1f;
}

public class IslandSpawnManager : MonoBehaviour
{
    [Header("Boat")]
    [SerializeField] private Transform boatAnchor;
    [SerializeField] private BoatDeckAnchor boatDeckAnchor;
    [SerializeField] private Vector3 moveDirection = Vector3.right;
    [SerializeField] private float boatSpeed = 8f;
    [SerializeField] private bool stopBoatOnLeave = true;

    [Header("Island Spawn")]
    [SerializeField] private List<IslandSpawnEntry> islandPrefabs = new List<IslandSpawnEntry>();
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [FormerlySerializedAs("islandSpawnInterval")]
    [SerializeField] private float islandSpawnDistance = 240f;

    [Header("Ocean")]
    [SerializeField] private GameObject oceanPrefab;
    [SerializeField] private float oceanSpawnHeight = 3.81f;
    [SerializeField] private int oceanTilesAhead = 2;
    [SerializeField] private int oceanTilesBehind = 1;
    [SerializeField] private int oceanSideTileCount = 1;

    [Header("Cleanup")]
    [SerializeField] private Transform spawnedWorldParent;
    [SerializeField] private IslandDestroyer destroyer;

    private readonly List<SpawnedWorldObject> spawnedIslands = new List<SpawnedWorldObject>();
    private readonly Dictionary<Vector2Int, SpawnedWorldObject> oceanTiles = new Dictionary<Vector2Int, SpawnedWorldObject>();

    private Vector3 worldForward;
    private Vector3 worldRight;
    private Vector3 oceanOrigin;
    private float oceanTileLength;
    private float oceanTileWidth;
    private float distanceSinceLastIslandSpawn;
    private float virtualDistanceTraveled;
    private bool worldInitialized;

    private void Reset()
    {
        moveDirection = Vector3.right;
        boatSpeed = 8f;
        islandSpawnDistance = 240f;
        oceanSpawnHeight = 3.81f;
        oceanTilesAhead = 2;
        oceanTilesBehind = 1;
        oceanSideTileCount = 1;
        AutoAssignDefaults();
    }

    private void OnValidate()
    {
        boatSpeed = Mathf.Max(0f, boatSpeed);
        islandSpawnDistance = Mathf.Max(1f, islandSpawnDistance);
        oceanTilesAhead = Mathf.Max(1, oceanTilesAhead);
        oceanTilesBehind = Mathf.Max(0, oceanTilesBehind);
        oceanSideTileCount = Mathf.Max(0, oceanSideTileCount);

        AutoAssignDefaults();
        RemoveMissingReferences();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        InitializeWorld();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!worldInitialized)
        {
            InitializeWorld();
        }

        float boatDistanceMoved = MoveBoat();
        EnsureOceanCoverage();
        TrySpawnIslandsByDistance(boatDistanceMoved);
        DespawnPassedObjects();
    }

    private void InitializeWorld()
    {
        AutoAssignDefaults();
        RemoveMissingReferences();

        if (boatAnchor == null)
        {
            return;
        }

        worldForward = GetNormalizedDirection();
        worldRight = Vector3.Cross(Vector3.up, worldForward);
        if (worldRight.sqrMagnitude < 0.0001f)
        {
            worldRight = Vector3.forward;
        }
        else
        {
            worldRight.Normalize();
        }

        if (spawnedWorldParent == null)
        {
            spawnedWorldParent = transform;
        }

        if (boatDeckAnchor == null && boatAnchor != null)
        {
            boatDeckAnchor = boatAnchor.GetComponentInChildren<BoatDeckAnchor>();
        }

        oceanOrigin = GetBoatWaterPosition();
        distanceSinceLastIslandSpawn = 0f;

        EnsureOceanMetrics();
        EnsureOceanCoverage();
        SpawnIsland();
        worldInitialized = true;
    }

    private float MoveBoat()
    {
        if (boatAnchor == null)
        {
            SpawnedWorldObject.GlobalWorldSpeed = 0f;
            return 0f;
        }

        if (stopBoatOnLeave && boatDeckAnchor != null && !boatDeckAnchor.HasPassengers)
        {
            SpawnedWorldObject.GlobalWorldSpeed = 0f;
            return 0f;
        }

        SpawnedWorldObject.GlobalWorldSpeed = boatSpeed;
        float distanceMoved = boatSpeed * Time.deltaTime;
        virtualDistanceTraveled += distanceMoved;
        return distanceMoved;
    }

    public void SpawnIsland()
    {
        GameObject selectedPrefab = GetRandomIslandPrefab();
        Transform spawnPoint = GetRandomSpawnPoint();
        if (selectedPrefab == null || spawnPoint == null)
        {
            return;
        }

        Vector3 spawnPosition = spawnPoint.position + worldForward * virtualDistanceTraveled;
        GameObject spawnedIsland = Instantiate(selectedPrefab, spawnPosition, spawnPoint.rotation, spawnedWorldParent);
        SpawnedWorldObject worldObject = GetOrAddWorldObject(spawnedIsland);
        worldObject.SetWorldMovement(-worldForward);
        spawnedIslands.Add(worldObject);
    }

    private void TrySpawnIslandsByDistance(float boatDistanceMoved)
    {
        if (boatDistanceMoved <= 0f || islandSpawnDistance <= 0f)
        {
            return;
        }

        distanceSinceLastIslandSpawn += boatDistanceMoved;
        while (distanceSinceLastIslandSpawn >= islandSpawnDistance)
        {
            SpawnIsland();
            distanceSinceLastIslandSpawn -= islandSpawnDistance;
        }
    }

    private void EnsureOceanCoverage()
    {
        if (boatAnchor == null || oceanPrefab == null || oceanTileLength <= 0f || oceanTileWidth <= 0f)
        {
            return;
        }

        int currentRow = GetBoatRowIndex();
        for (int row = currentRow - oceanTilesBehind; row <= currentRow + oceanTilesAhead; row++)
        {
            for (int column = -oceanSideTileCount; column <= oceanSideTileCount; column++)
            {
                Vector2Int key = new Vector2Int(row, column);
                if (oceanTiles.ContainsKey(key) && oceanTiles[key] != null)
                {
                    continue;
                }

                SpawnOceanTile(row, column);
            }
        }
    }

    private void SpawnOceanTile(int row, int column)
    {
        Vector3 spawnPosition = oceanOrigin;
        spawnPosition += worldForward * (row * oceanTileLength);
        spawnPosition += worldRight * (column * oceanTileWidth);
        spawnPosition.y = oceanSpawnHeight;

        GameObject oceanTile = Instantiate(oceanPrefab, spawnPosition, Quaternion.identity, spawnedWorldParent);
        SpawnedWorldObject worldObject = GetOrAddWorldObject(oceanTile);
        oceanTiles[new Vector2Int(row, column)] = worldObject;
    }

    private void DespawnPassedObjects()
    {
        if (destroyer == null)
        {
            return;
        }

        for (int i = spawnedIslands.Count - 1; i >= 0; i--)
        {
            SpawnedWorldObject worldObject = spawnedIslands[i];
            if (worldObject == null)
            {
                spawnedIslands.RemoveAt(i);
                continue;
            }

            if (destroyer.Intersects(worldObject.WorldBounds))
            {
                worldObject.DestroySelf();
                spawnedIslands.RemoveAt(i);
            }
        }

        List<Vector2Int> keysToRemove = null;
        foreach (KeyValuePair<Vector2Int, SpawnedWorldObject> pair in oceanTiles)
        {
            SpawnedWorldObject worldObject = pair.Value;
            if (worldObject == null)
            {
                if (keysToRemove == null)
                {
                    keysToRemove = new List<Vector2Int>();
                }

                keysToRemove.Add(pair.Key);
                continue;
            }

            if (!destroyer.Intersects(worldObject.WorldBounds))
            {
                continue;
            }

            worldObject.DestroySelf();
            if (keysToRemove == null)
            {
                keysToRemove = new List<Vector2Int>();
            }

            keysToRemove.Add(pair.Key);
        }

        if (keysToRemove == null)
        {
            return;
        }

        for (int i = 0; i < keysToRemove.Count; i++)
        {
            oceanTiles.Remove(keysToRemove[i]);
        }
    }

    private void AutoAssignDefaults()
    {
        if (boatAnchor == null)
        {
            GameObject boatObject = GameObject.Find("Boat Anchor");
            if (boatObject != null)
            {
                boatAnchor = boatObject.transform;
            }
        }

        if (boatDeckAnchor == null && boatAnchor != null)
        {
            boatDeckAnchor = boatAnchor.GetComponentInChildren<BoatDeckAnchor>();
        }

        if (spawnedWorldParent == null)
        {
            spawnedWorldParent = transform;
        }

        if (destroyer == null)
        {
            destroyer = FindFirstObjectByType<IslandDestroyer>();
        }

        if (spawnPoints.Count == 0)
        {
            Transform spawnPointsRoot = null;
            if (boatAnchor != null)
            {
                spawnPointsRoot = boatAnchor.Find("Island Spawn Points");
            }

            if (spawnPointsRoot == null)
            {
                GameObject sceneSpawnPointsRoot = GameObject.Find("Island Spawn Points");
                if (sceneSpawnPointsRoot != null)
                {
                    spawnPointsRoot = sceneSpawnPointsRoot.transform;
                }
            }

            if (spawnPointsRoot != null)
            {
                spawnPoints.Clear();
                for (int i = 0; i < spawnPointsRoot.childCount; i++)
                {
                    spawnPoints.Add(spawnPointsRoot.GetChild(i));
                }
            }
        }

#if UNITY_EDITOR
        bool hasAssignedIslandPrefab = false;
        for (int i = 0; i < islandPrefabs.Count; i++)
        {
            if (islandPrefabs[i] != null && islandPrefabs[i].prefab != null)
            {
                hasAssignedIslandPrefab = true;
                break;
            }
        }

        if (!hasAssignedIslandPrefab)
        {
            islandPrefabs.Clear();
            TryAddDefaultIslandPrefab("Assets/Prefabs/Island 1.prefab");
            TryAddDefaultIslandPrefab("Assets/Prefabs/Island 2.prefab");
            TryAddDefaultIslandPrefab("Assets/Prefabs/Island 3.prefab");
        }

        if (oceanPrefab == null)
        {
            GameObject defaultOceanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SwimWater.prefab");
            if (defaultOceanPrefab != null)
            {
                oceanPrefab = defaultOceanPrefab;
                oceanSpawnHeight = defaultOceanPrefab.transform.position.y;
            }
        }
#endif
    }

    private void RemoveMissingReferences()
    {
        for (int i = spawnPoints.Count - 1; i >= 0; i--)
        {
            if (spawnPoints[i] == null)
            {
                spawnPoints.RemoveAt(i);
            }
        }

        for (int i = islandPrefabs.Count - 1; i >= 0; i--)
        {
            if (islandPrefabs[i] == null)
            {
                islandPrefabs.RemoveAt(i);
                continue;
            }

            islandPrefabs[i].weight = Mathf.Max(0f, islandPrefabs[i].weight);
        }
    }

    private void EnsureOceanMetrics()
    {
        if (oceanPrefab == null || oceanTileLength > 0f || oceanTileWidth > 0f)
        {
            return;
        }

        GameObject tempOceanTile = Instantiate(oceanPrefab, GetBoatWaterPosition(), Quaternion.identity, spawnedWorldParent);
        SpawnedWorldObject worldObject = GetOrAddWorldObject(tempOceanTile);

        Bounds bounds = worldObject.WorldBounds;
        oceanTileLength = Mathf.Max(1f, GetProjectedSize(bounds.size, worldForward));
        oceanTileWidth = Mathf.Max(1f, GetProjectedSize(bounds.size, worldRight));

        Destroy(tempOceanTile);
    }

    private int GetBoatRowIndex()
    {
        float forwardDistance = Vector3.Dot(GetBoatWaterPosition() - oceanOrigin, worldForward);
        return Mathf.RoundToInt(forwardDistance / oceanTileLength);
    }

    private Vector3 GetBoatWaterPosition()
    {
        if (boatAnchor == null)
        {
            return Vector3.zero;
        }

        Vector3 position = boatAnchor.position;
        position.y = oceanSpawnHeight;
        return position;
    }

    private GameObject GetRandomIslandPrefab()
    {
        float totalWeight = 0f;
        GameObject fallbackPrefab = null;

        for (int i = 0; i < islandPrefabs.Count; i++)
        {
            IslandSpawnEntry entry = islandPrefabs[i];
            if (entry == null || entry.prefab == null || entry.weight <= 0f)
            {
                continue;
            }

            totalWeight += entry.weight;
            fallbackPrefab = entry.prefab;
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float randomWeight = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        for (int i = 0; i < islandPrefabs.Count; i++)
        {
            IslandSpawnEntry entry = islandPrefabs[i];
            if (entry == null || entry.prefab == null || entry.weight <= 0f)
            {
                continue;
            }

            currentWeight += entry.weight;
            if (randomWeight <= currentWeight)
            {
                return entry.prefab;
            }
        }

        return fallbackPrefab;
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints.Count == 0)
        {
            return null;
        }

        return spawnPoints[Random.Range(0, spawnPoints.Count)];
    }

    private SpawnedWorldObject GetOrAddWorldObject(GameObject target)
    {
        SpawnedWorldObject worldObject = target.GetComponent<SpawnedWorldObject>();
        if (worldObject == null)
        {
            worldObject = target.AddComponent<SpawnedWorldObject>();
        }

        return worldObject;
    }

    private Vector3 GetNormalizedDirection()
    {
        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            return Vector3.right;
        }

        return moveDirection.normalized;
    }

    private float GetProjectedSize(Vector3 size, Vector3 axis)
    {
        Vector3 normalizedAxis = axis.normalized;
        return Mathf.Abs(normalizedAxis.x) * size.x
            + Mathf.Abs(normalizedAxis.y) * size.y
            + Mathf.Abs(normalizedAxis.z) * size.z;
    }

#if UNITY_EDITOR
    private void TryAddDefaultIslandPrefab(string assetPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            return;
        }

        IslandSpawnEntry entry = new IslandSpawnEntry();
        entry.prefab = prefab;
        entry.weight = 1f;
        islandPrefabs.Add(entry);
    }
#endif
}
