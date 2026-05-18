using System.Collections.Generic;
using UnityEngine;

public static class CraftingService
{
    public static bool CanCraft(Inventory inventory, CraftingRecipe recipe)
    {
        if (inventory == null || recipe == null) return false;
        var ingredients = recipe.IngredientItemIds;
        if (ingredients == null || ingredients.Length == 0) return false;
        var counts = new Dictionary<string, int>();
        foreach (var id in ingredients)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (counts.ContainsKey(id)) counts[id]++; else counts[id] = 1;
        }
        foreach (var kv in counts)
        {
            if (inventory.CountItemsById(kv.Key) < kv.Value) return false;
        }
        return true;
    }

    public static bool TryCraft(Inventory inventory, PlayerInventory playerInventory, CraftingRecipe recipe)
    {
        if (!CanCraft(inventory, recipe)) return false;
        if (recipe.OutputPrefab == null) return false;

        foreach (var id in recipe.IngredientItemIds)
        {
            Item consumed = inventory.RemoveOneItemById(id);
            if (consumed != null) Object.Destroy(consumed.gameObject);
        }

        Item output = Object.Instantiate(recipe.OutputPrefab);
        output.name = recipe.OutputPrefab.name;

        if (!inventory.TryAddToHands(output))
        {
            if (!inventory.AddToExpansion(output))
            {
                Vector3 dropPos = playerInventory != null && playerInventory.transform != null
                    ? playerInventory.transform.position + playerInventory.transform.forward * 1.5f + Vector3.up * 0.5f
                    : Vector3.zero;
                output.Drop(dropPos, Quaternion.identity);
            }
        }
        return true;
    }
}
