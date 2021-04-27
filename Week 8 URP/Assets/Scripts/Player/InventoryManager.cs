using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [SerializeField] GameObject inventoryUI;

    public Dictionary<string, Resource> resources = new Dictionary<string, Resource>();

    private Animator anim;

    [SerializeField] Transform cameraTransform;

    [SerializeField] GameObject[] items;
    private GameObject currentItem;

    private static InventoryManager _instance;
    public static InventoryManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            //anim.SetTrigger("Use");
            anim.SetTrigger("UseItem");
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            UIManager.Instance.OpenMenu(inventoryUI);
        }

            if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            anim.SetBool("EquipAxe", !anim.GetBool("EquipAxe"));
        }
    }

    public void TryToHarvest()
    {
        Vector3 target = (transform.position - cameraTransform.position).normalized;

        Ray ray = new Ray(transform.position + Vector3.up, target);

        Debug.DrawRay(transform.position + Vector3.up, target, Color.red, 1.0f);

        RaycastHit hit;

        if(Physics.Raycast(ray, out hit, 3.0f))
        {
            IHarvestable har = hit.collider.gameObject.GetComponent<IHarvestable>();

            if (har != null)
            {
                AddResource(har.Harvest());
            }
        }
    }

    public void EnableItem(int index)
    {
        if(index == -1)//unequip item
        {
            currentItem.SetActive(false);
            currentItem = null;
            return;
        }

        if(currentItem != null) //disable current item
        {
            currentItem.SetActive(false);
        }
        currentItem = items[index];
        currentItem.SetActive(true);
    }

    void AddResource(Resource resource)
    {
        if (!resources.ContainsKey(resource.resourceObject.resourceName))
        {
            resources.Add(resource.resourceObject.resourceName, resource);
        }

        Resource aResource = resources[resource.resourceObject.resourceName];

        aResource.resourceCount += resource.resourceCount;

        resources[resource.resourceObject.resourceName] = aResource;

        
    }
}

[System.Serializable]
public struct Resource
{
    public ResourceObject resourceObject;
    public int resourceCount;
}

public interface IHarvestable
{
    Resource Harvest();
}
