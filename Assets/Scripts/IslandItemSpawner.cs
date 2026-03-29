using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

    [Header("Spawn Settings")]
    [SerializeField] private int minItemCount = 3;
    [SerializeField] private int maxItemCount = 5;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    private void Awake()
    {
        AutoFindSpawnPoints();
        AutoAssignDefaultTiers();
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

    private void AutoAssignDefaultTiers()
    {
        if (rarityTiers != null && rarityTiers.Length > 0) return;

#if UNITY_EDITOR
        rarityTiers = new ItemRarityTier[3];

        rarityTiers[0] = new ItemRarityTier
        {
            label = "Common",
            weight = 70f,
            prefabs = new GameObject[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/items/apple.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/items/Log.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/items/Rock2.prefab")
            }
        };

        rarityTiers[1] = new ItemRarityTier
        {
            label = "Rare",
            weight = 25f,
            prefabs = new GameObject[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/items/Tool_Axe_Improvised.prefab")
            }
        };

        rarityTiers[2] = new ItemRarityTier
        {
            label = "Epic",
            weight = 5f,
            prefabs = new GameObject[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/items/Tool_Flashlight_01.prefab")
            }
        };
#endif
    }

    private void SpawnItems()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;
        if (rarityTiers == null || rarityTiers.Length == 0) return;

        int count = Random.Range(minItemCount, maxItemCount + 1);
        count = Mathf.Min(count, spawnPoints.Length);

        int[] indices = new int[spawnPoints.Length];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;

        for (int i = 0; i < count; i++)
        {
            int j = Random.Range(i, indices.Length);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        for (int i = 0; i < count; i++)
        {
            Transform point = spawnPoints[indices[i]];
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

        Instantiate(prefab, spawnPoint.position, spawnPoint.rotation, transform);
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
