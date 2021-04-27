using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "New Resource")]

public class ResourceObject : ScriptableObject
{
    public Sprite resourceImage;
    public string resourceName;
}
