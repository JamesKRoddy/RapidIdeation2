using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "New Plant")]
public class PlantInfo : ScriptableObject
{
    public float minSize;
    public float maxSize;
    public float growAmount;
    [Tooltip("Resource type and max ammount")]
    public Resource resource;
}
