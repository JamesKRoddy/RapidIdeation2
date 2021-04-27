using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Carnivore : Animal
{
    public static List<Herbivore> prey = new List<Herbivore>();

    public Animal targetAnimal;

    public override void Start()
    {
        AnimalManager.Instance.carnivores.Add(this);
        base.Start();
    }

    private void OnDestroy()
    {
        AnimalManager.Instance.carnivores.Remove(this);
    }

    public override bool FindFood(out Vector3 target)
    {
        if (logChanges)
        {
            Debug.Log(gameObject + " looking for food");
        }

        List<Herbivore> herbivores = AnimalManager.Instance.herbivores;

        if(herbivores.Count <= 0)
        {
            target = Vector3.zero;
            return base.FindFood(out target);
        }

        for (int i = 0; i < herbivores.Count; i++)
        {
            if (herbivores[i] == null)
            {
                continue;
            }

            if (Vector3.Distance(transform.position, herbivores[i].transform.position) > awareness)
            {
                continue;
            }

            if (herbivores[i].dead)
            {
                continue;
            }

            //if (logChanges)
            //{
            //    Debug.Log(string.Format("{0}: Found plant ({1}), moving towards it.", gameObject.name, plants[i].gameObject.name));
            //}

            targetAnimal = herbivores[i];

            target = herbivores[i].transform.position;

            return true;
        }

        target = Vector3.zero;

        return base.FindFood(out target);
    }

    public override void Eat()
    {
        if (targetAnimal == null)
        {
            DecideNextState();
            return;
        }

        if (logChanges)
        {
            Debug.Log(gameObject + " ate " + targetAnimal.gameObject.name);
        }

        Attack();

        targetAnimal.Die();

        health = stats.toughness;

        resource.resourceCount = Mathf.RoundToInt(health);

        targetAnimal = null;
    }

    void Attack()
    {
        if (logChanges)
        {
            Debug.Log(gameObject + " attacked " + targetAnimal.gameObject.name);
        }

        UpdateState(attackStates[0], transform.position);
        animator.SetBool(attackStates[0].animationParameterName, true);
        currentStateRoutine = StartCoroutine(AttackState());
    }

    public IEnumerator AttackState()
    {
        yield return new WaitForSeconds(currentState.maxTimeInState);
        animator.SetBool(attackStates[0].animationParameterName, false);
        Debug.Log("!!!!!!!!!!!!!!!!!HIT");
        DecideNextState(true);
    }
}
