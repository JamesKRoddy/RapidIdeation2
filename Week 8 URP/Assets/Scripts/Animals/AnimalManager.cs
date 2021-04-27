using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimalManager : MonoBehaviour
{
    private static AnimalManager _instance;
    public static AnimalManager Instance { get { return _instance; } }

    public List<Carnivore> carnivores = new List<Carnivore>();
    public List<Herbivore> herbivores = new List<Herbivore>();

    public GameObject carnivorePrefab;
    public GameObject herbivorePrefab;

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
        InvokeRepeating("SpawnNewAnimals", 200.0f, 200.0f);
    }

    void SpawnNewAnimals()
    {
        SpawnCarnivore();
        SpawnHerbivore();
    }

    void SpawnHerbivore()
    {
        if(herbivores.Count <= 0)
        {
            return;
        }

        int numberOfSpawned = herbivores.Count / 10;

        if (numberOfSpawned <= 0)
            numberOfSpawned = 1;

        for (int i = 0; i < numberOfSpawned; i++)
        {
            GameObject newnewAnimal = Instantiate(herbivorePrefab, NavMeshUtil.GetRandomPoint(Vector3.zero, 100.0f), Quaternion.identity);

            newnewAnimal.transform.parent = this.transform;
        }
    }

    void SpawnCarnivore()
    {
        if (carnivores.Count <= 0)
        {
            return;
        }

        int numberOfSpawned = carnivores.Count / 10;

        if (numberOfSpawned <= 0)
            numberOfSpawned = 1;

        for (int i = 0; i < numberOfSpawned; i++)
        {
            GameObject newnewAnimal = Instantiate(carnivorePrefab, NavMeshUtil.GetRandomPoint(Vector3.zero, 100.0f), Quaternion.identity);

            newnewAnimal.transform.parent = this.transform;
        }
    }
}
