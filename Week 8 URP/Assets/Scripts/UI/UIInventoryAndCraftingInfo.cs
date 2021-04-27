using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIInventoryAndCraftingInfo : MonoBehaviour
{
    [SerializeField] Image image;
    [SerializeField] Button button;
    [SerializeField] TMP_Text buttonText;
    [SerializeField] TMP_Text resourceCount;

    public void PopulateInfo(Sprite resSprite, string text)
    {
        image.sprite = resSprite;
        resourceCount.text = text;
        button.gameObject.SetActive(false);
    }
}
