using UnityEngine;

[System.Serializable]
public class ItemRarityTier
{
    public string label;
    public GameObject[] prefabs;
    [Min(0f)] public float weight = 1f;
}

public class IslandItemSpawner : MonoBehaviour
{
    [Header("Rarity Tiers")]
    [SerializeField] private ItemRarityTier[] rarityTiers;

    [Header("Guaranteed Items")]
    [Tooltip("One of each prefab here is guaranteed to spawn on its own spawn point before random items roll.")]
    [SerializeField] private GameObject[] guaranteedItems;

    [Header("Spawn Settings")]
    [SerializeField] private int minItemCount = 3;
    [SerializeField] private int maxItemCount = 5;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    private void Awake()
    {
        AutoFindSpawnPoints();
        SpawnItems();
    }

    private void OnValidate()
    {
        if (minItemCount < 0) minItemCount = 0;
        if (maxItemCount < minItemCount) maxItemCount = minItemCount;
    }

    private void AutoFindSpawnPoints()
    {
        if (spawnPoints != null && spawnPoints.Length > 0) return;

        int count = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).name.StartsWith("ItemSpawnPoint"))
                count++;
        }

        if (count == 0) return;

        spawnPoints = new Transform[count];
        int index = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("ItemSpawnPoint"))
                spawnPoints[index++] = child;
        }
    }

    private void SpawnItems()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        int[] indices = new int[spawnPoints.Length];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        for (int i = 0; i < indices.Length; i++)
        {
            int j = Random.Range(i, indices.Length);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        int cursor = 0;

        if (guaranteedItems != null)
        {
            for (int g = 0; g < guaranteedItems.Length && cursor < indices.Length; g++)
            {
                GameObject prefab = guaranteedItems[g];
                if (prefab == null) continue;
                Transform point = spawnPoints[indices[cursor++]];
                if (point == null) continue;
                SpawnPrefab(prefab, point);
            }
        }

        if (rarityTiers == null || rarityTiers.Length == 0) return;

        int remaining = indices.Length - cursor;
        int count = Random.Range(minItemCount, maxItemCount + 1);
        count = Mathf.Min(count, remaining);

        for (int i = 0; i < count; i++)
        {
            Transform point = spawnPoints[indices[cursor++]];
            if (point == null) continue;
            SpawnSingleItem(point);
        }
    }

    private void SpawnSingleItem(Transform spawnPoint)
    {
        ItemRarityTier tier = SelectRarityTier();
        if (tier == null || tier.prefabs == null || tier.prefabs.Length == 0) return;

        GameObject prefab = tier.prefabs[Random.Range(0, tier.prefabs.Length)];
        if (prefab == null) return;

        SpawnPrefab(prefab, spawnPoint);
    }

    private void SpawnPrefab(GameObject prefab, Transform spawnPoint)
    {
        GameObject spawned = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, transform);
        Rigidbody rb = spawned.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    private ItemRarityTier SelectRarityTier()
    {
        float totalWeight = 0f;
        ItemRarityTier fallback = null;

        for (int i = 0; i < rarityTiers.Length; i++)
        {
            ItemRarityTier tier = rarityTiers[i];
            if (tier == null || tier.prefabs == null || tier.prefabs.Length == 0 || tier.weight <= 0f)
                continue;

            totalWeight += tier.weight;
            fallback = tier;
        }

        if (totalWeight <= 0f) return null;

        float roll = Random.Range(0f, totalWeight);
        float current = 0f;

        for (int i = 0; i < rarityTiers.Length; i++)
        {
            ItemRarityTier tier = rarityTiers[i];
            if (tier == null || tier.prefabs == null || tier.prefabs.Length == 0 || tier.weight <= 0f)
                continue;

            current += tier.weight;
            if (roll <= current) return tier;
        }

        return fallback;
    }
}
