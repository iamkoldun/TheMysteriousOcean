using System.Collections.Generic;
using UnityEngine;

public class Workbench : MonoBehaviour
{
    [SerializeField] private List<CraftingRecipe> recipes = new List<CraftingRecipe>();

    public IReadOnlyList<CraftingRecipe> Recipes => recipes;
}
