using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    [Header("Islands")]
    [SerializeField] private List<IslandSpawnEntry> islandPrefabs = new List<IslandSpawnEntry>();
    [SerializeField] private Transform spawnedIslandsParent;

    [Header("Spawn Points")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [Header("Movement")]
    [SerializeField] private Vector3 moveDirection = Vector3.left;
    [SerializeField] private float moveSpeed = 8f;

    [Header("Spawn Timing")]
    [SerializeField] private float spawnInterval = 6f;

    [Header("Cleanup")]
    [SerializeField] private IslandDestroyer destroyer;

    private Coroutine spawnRoutine;

    private void Reset()
    {
        moveDirection = Vector3.left;
        moveSpeed = 8f;
        spawnInterval = 6f;
        AutoAssignDefaults();
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        spawnInterval = Mathf.Max(0.1f, spawnInterval);
        AutoAssignDefaults();
        RemoveMissingReferences();
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            spawnRoutine = StartCoroutine(SpawnLoop());
        }
    }

    private void OnDisable()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        SpawnIsland();

        WaitForSeconds wait = new WaitForSeconds(spawnInterval);
        while (true)
        {
            yield return wait;
            SpawnIsland();
            wait = new WaitForSeconds(spawnInterval);
        }
    }

    public void SpawnIsland()
    {
        GameObject selectedPrefab = GetRandomIslandPrefab();
        Transform spawnPoint = GetRandomSpawnPoint();
        if (selectedPrefab == null || spawnPoint == null)
        {
            return;
        }

        Transform parent = spawnedIslandsParent != null ? spawnedIslandsParent : transform;
        GameObject spawnedIsland = Instantiate(selectedPrefab, spawnPoint.position, spawnPoint.rotation, parent);

        MovingIsland movingIsland = spawnedIsland.GetComponent<MovingIsland>();
        if (movingIsland == null)
        {
            movingIsland = spawnedIsland.AddComponent<MovingIsland>();
        }

        movingIsland.Initialize(GetNormalizedDirection(), moveSpeed, destroyer);
    }

    private void AutoAssignDefaults()
    {
        if (spawnedIslandsParent == null)
        {
            spawnedIslandsParent = transform;
        }

        if (destroyer == null)
        {
            destroyer = FindFirstObjectByType<IslandDestroyer>();
        }

        if (spawnPoints.Count == 0)
        {
            GameObject spawnPointsRoot = GameObject.Find("Island Spawn Points");
            if (spawnPointsRoot != null)
            {
                spawnPoints.Clear();
                for (int i = 0; i < spawnPointsRoot.transform.childCount; i++)
                {
                    spawnPoints.Add(spawnPointsRoot.transform.GetChild(i));
                }
            }
        }

#if UNITY_EDITOR
        bool hasAssignedPrefab = false;
        for (int i = 0; i < islandPrefabs.Count; i++)
        {
            if (islandPrefabs[i] != null && islandPrefabs[i].prefab != null)
            {
                hasAssignedPrefab = true;
                break;
            }
        }

        if (!hasAssignedPrefab)
        {
            islandPrefabs.Clear();
            TryAddDefaultPrefab("Assets/Prefabs/Island 1.prefab");
            TryAddDefaultPrefab("Assets/Prefabs/Island 2.prefab");
            TryAddDefaultPrefab("Assets/Prefabs/Island 3.prefab");
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

    private GameObject GetRandomIslandPrefab()
    {
        float totalWeight = 0f;
        for (int i = 0; i < islandPrefabs.Count; i++)
        {
            IslandSpawnEntry entry = islandPrefabs[i];
            if (entry == null || entry.prefab == null || entry.weight <= 0f)
            {
                continue;
            }

            totalWeight += entry.weight;
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

        return islandPrefabs[islandPrefabs.Count - 1].prefab;
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints.Count == 0)
        {
            return null;
        }

        return spawnPoints[Random.Range(0, spawnPoints.Count)];
    }

    private Vector3 GetNormalizedDirection()
    {
        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            return Vector3.left;
        }

        return moveDirection.normalized;
    }

#if UNITY_EDITOR
    private void TryAddDefaultPrefab(string assetPath)
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
