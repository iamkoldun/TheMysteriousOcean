using UnityEngine;

[CreateAssetMenu(menuName = "Crafting/Recipe", fileName = "CraftingRecipe")]
public class CraftingRecipe : ScriptableObject
{
    [SerializeField] private string recipeName;
    [SerializeField] private string[] ingredientItemIds;
    [SerializeField] private Sprite[] ingredientIcons;
    [SerializeField] private Item[] ingredientPrefabs;
    [SerializeField] private Item outputPrefab;
    [SerializeField] private Sprite outputIcon;

    public string RecipeName => recipeName;
    public string[] IngredientItemIds => ingredientItemIds;
    public Sprite[] IngredientIcons => ingredientIcons;
    public Item[] IngredientPrefabs => ingredientPrefabs;
    public Item OutputPrefab => outputPrefab;
    public Sprite OutputIcon => outputIcon;
}
