using UnityEngine;
using System.Collections;

public class EnemyAttack : MonoBehaviour {

    public float attackTimer = 0f;
    public float newAttackTime = 2f;
    public bool isAttacking = false;

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update ()
    {
        attackTimer += Time.deltaTime;
        GameObject attacker = null;
        if (!isAttacking && attackTimer > newAttackTime)
        {
            Debug.Log("TIME TO ATTACK");
            if (GameObject.FindGameObjectsWithTag("Enemy").Length > 1)
            {
                int maxThreat = -1;
                foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
                {
                    if (enemy.GetComponent<EnnemyScript>().threatValue > maxThreat && enemy.GetComponent<EnnemyScript>().CanAttack())
                    {
                        attacker = enemy;
                        maxThreat = enemy.GetComponent<EnnemyScript>().threatValue;
                    }
                }
            }
            if (attacker != null)
            {
                isAttacking = true;
                attacker.GetComponent<EnnemyScript>().Attack();
            }
            else
            {
                Debug.Log("TURN LOST");
            }
        }
    }
}
