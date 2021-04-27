using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plant : MonoBehaviour, IHarvestable
{
    [SerializeField] PlantInfo plantInfo;

    [Tooltip("Whether the plant can be harvested by the player")]
    public bool harvestable = true;
    public bool edible = false;
    public bool randomStartSize = true;
    public Resource resource;



    // Start is called before the first frame update
    void Start()
    {
        PlantManager.Instance.plantsInScene.Add(this);
        PlantManager.Instance.onGrowPlants += Grow;
        if(randomStartSize == true)
        {
            float size = Random.Range(plantInfo.minSize, plantInfo.maxSize);
            transform.localScale = Vector3.one * size;
            resource.resourceCount = Mathf.RoundToInt(size * 10);
        }
        else
        {
            transform.localScale = Vector3.one * plantInfo.minSize;
        }
    }

    private void OnDestroy()
    {
        PlantManager.Instance.plantsInScene.Remove(this);
        PlantManager.Instance.onGrowPlants -= Grow;
    }

    void Harvent()
    {
        DecreasePlantSize();

        if (!harvestable)
            return;


    }

    public float GetEaten()
    {
        return DecreasePlantSize();
    }

    float DecreasePlantSize()
    {
        int ammount = 1;// Mathf.RoundToInt(plantInfo.resource.resourceCount * plantInfo.growAmount);        

        if (transform.localScale.x > plantInfo.minSize)
        {
            Vector3 scaleChange = Vector3.one * 0.1f;

            resource.resourceCount -= ammount;

            transform.localScale -= scaleChange;
        }
        else
        {
            resource.resourceCount = -1;
            Destroy(gameObject);
        }

        return (float)ammount;
    }

    void Grow()
    {
        if (transform.localScale.x < plantInfo.maxSize)
        {
            Vector3 scaleChange = Vector3.one * plantInfo.growAmount;

            resource.resourceCount += Mathf.RoundToInt(plantInfo.resource.resourceCount * plantInfo.growAmount);

            transform.localScale += scaleChange;
        }
    }

    public Resource Harvest()
    {
        Destroy(gameObject);
        return resource;
    }
}
