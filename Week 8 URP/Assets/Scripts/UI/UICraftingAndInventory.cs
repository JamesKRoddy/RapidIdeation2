using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UICraftingAndInventory : UIMenu
{
    [SerializeField] GameObject inventoryDisplayPrefab;

    [SerializeField] Transform inventoryPanel;

    public void PopulateInventory()
    {
        foreach (Transform child in inventoryPanel)
        {
            GameObject.Destroy(child.gameObject);
        }

        Dictionary<string, Resource> resources = InventoryManager.Instance.resources;

        foreach (KeyValuePair<string, Resource> item in resources)
        {
            Resource res = item.Value;

            GameObject inventoryDisplay = Instantiate(inventoryDisplayPrefab, inventoryPanel);

            inventoryDisplay.GetComponent<UIInventoryAndCraftingInfo>().PopulateInfo(res.resourceObject.resourceImage, res.resourceCount.ToString());
        }
    }
}

