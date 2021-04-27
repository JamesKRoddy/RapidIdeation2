using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PlantManager : MonoBehaviour
{
    private static PlantManager _instance;
    public static PlantManager Instance { get { return _instance; } }

    public List<GameObject> plantPrefabs;
    public List<Plant> plantsInScene;

    public delegate void GrowPlants();
    public GrowPlants onGrowPlants;

    Vector3 spawnPoint;

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
        InvokeRepeating("GrowAllPlants", 2.0f, 10f);
        InvokeRepeating("SpawnNewPlants", 200.0f, 200.0f);
        Invoke("EnableRain", 30.0f);
        Invoke("DisableRain", 60.0f);
    }

    [SerializeField] ParticleSystem rain;

    void EnableRain()
    {
        rain.Play();
    }

    void DisableRain()
    {
        rain.Stop();
    }

    public void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;
        
        Gizmos.DrawSphere(spawnPoint + new Vector3(0f, 0.1f, 0f), 0.2f);        
    }

    public void GrowAllPlants()
    {
        onGrowPlants?.Invoke();
    }

    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.I))
    //    {
    //        spawnPoint = NavMeshUtil.GetRandomPoint(Vector3.zero, 100.0f);
    //        Debug.Log(spawnPoint);
    //    }
    //}

    void SpawnNewPlants()
    {
        if (plantsInScene.Count <= 0) //all plants are dead dont respawn
            return;

        int numberOfSpawned = plantsInScene.Count/10;

        if (numberOfSpawned <= 0)
            numberOfSpawned = 1;

        for (int i = 0; i < numberOfSpawned; i++)
        {
            int index = Random.Range(0, plantPrefabs.Count);

            GameObject newPlant = Instantiate(plantPrefabs[i], NavMeshUtil.GetRandomPoint(Vector3.zero, 100.0f), Quaternion.identity);

            newPlant.transform.parent = this.transform;

            newPlant.GetComponent<Plant>().randomStartSize = false;
        }
    }
}

public static class NavMeshUtil
{

    // Get Random Point on a Navmesh surface
    public static Vector3 GetRandomPoint(Vector3 center, float maxDistance)
    {
        // Get Random Point inside Sphere which position is center, radius is maxDistance
        Vector3 randomPos = Random.insideUnitSphere * maxDistance + center;

        NavMeshHit hit; // NavMesh Sampling Info Container

        // from randomPos find a nearest point on NavMesh surface in range of maxDistance
        NavMesh.SamplePosition(randomPos, out hit, maxDistance, NavMesh.AllAreas);

        return hit.position;
    }
}
