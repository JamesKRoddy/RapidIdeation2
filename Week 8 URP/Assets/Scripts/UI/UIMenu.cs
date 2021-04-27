using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMenu : MonoBehaviour
{
    public GameObject[] uiMenus;

    public void OpenMenu(GameObject menuObject)
    {
        foreach (GameObject item in uiMenus)
        {
            item.SetActive(false);
        }

        menuObject.SetActive(true);
    }
}
