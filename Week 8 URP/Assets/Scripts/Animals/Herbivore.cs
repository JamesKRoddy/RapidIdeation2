using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Herbivore : Animal
{
    public static List<Plant> plants = new List<Plant>();

    private Plant targetPlant;

    public override void Start()
    {
        AnimalManager.Instance.herbivores.Add(this);
        base.Start();
    }

    private void OnDestroy()
    {
        AnimalManager.Instance.herbivores.Remove(this);
    }

    public override bool FindFood(out Vector3 target)
    {
        if (logChanges)
        {
            Debug.Log(gameObject + " looking for food");
        }

        List<Plant> plants = PlantManager.Instance.plantsInScene;

        for (int i = 0; i < plants.Count; i++)
        {
            if (plants[i] == null)
            {
                continue;
            }

            if (!plants[i].edible)
            {
                continue;
            }

            if (Vector3.Distance(transform.position, plants[i].transform.position) > awareness)
            {
                continue;
            }

            if (plants[i].resource.resourceCount < 0)
            {
                continue;
            }

            //if (logChanges)
            //{
            //    Debug.Log(string.Format("{0}: Found plant ({1}), moving towards it.", gameObject.name, plants[i].gameObject.name));
            //}

            targetPlant = plants[i];

            target = plants[i].transform.position;
            return true;
        }

        target = Vector3.zero;
        return base.FindFood(out target);
    }

    public override void Eat()
    {
        if (targetPlant == null) //Incase the plant gets destroyed
        { 
            DecideNextState();
            return;
        }

        if (logChanges)
        {
            Debug.Log(gameObject + " ate " + targetPlant.gameObject.name);
        }

        targetPlant.GetEaten();

        health = stats.toughness;

        resource.resourceCount = Mathf.RoundToInt(health);

        targetPlant = null;

        DecideNextState();
    }

    public override bool AwarenessCheck(out Vector3 target)
    {
        Transform player = GameObject.FindGameObjectWithTag("Player").transform;

        if(Vector3.Distance(transform.position, player.transform.position) < awareness)
        {
            Vector3 targetDes = (transform.position - player.transform.position).normalized;

            target = transform.position + (targetDes * 2);

            //target = Vector3.zero;

            return true;
        }

        List<Carnivore> carnivores = AnimalManager.Instance.carnivores;

        target = Vector3.zero;

        foreach (Carnivore item in carnivores)
        {
            if (item.dead || Vector3.Distance(transform.position, item.transform.position) > awareness)
                continue;

            Vector3 targetDes = (transform.position - item.transform.position).normalized;

            target = transform.position + (targetDes * 2);

            //target = Vector3.zero;

            return true;
        }

        return false;
    }
}
