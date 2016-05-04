using UnityEngine;
using System.Collections;

public class EnnemyScript : MonoBehaviour {

    enum Direction { VOID, FORWARD, LEFT, RIGHT, BACKWARD };

    public int startingHealth = 120;
    public int attackPower = 10;
    public float knockDownEndTime = 1.5f;
    public float attackMaxRange = 2f;
    public float attackMinRange = 1f;
    public int threatValue = 0;
    public int waitBetweenAttack = 3;
    public bool isDead;
    public Material preparingAttackMaterial;
    public Material defaultMat;
    Vector2 smoothDeltaPosition = Vector2.zero;
    Vector2 velocity = Vector2.zero;

    int currentHealth;
    float knockDownTimer;
    int currentTurnToWait;
    bool isAttacking = false;
    //float attackTimer;

    Transform player;
    NavMeshAgent nav;
    Animator anim;
    Rigidbody ennemyRigidBody;

	// Use this for initialization
	void Start () {
	
	}

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        anim = GetComponent<Animator>();
        nav = GetComponent<NavMeshAgent>();
        nav.updatePosition = false;
        ennemyRigidBody = GetComponent<Rigidbody>();
        currentHealth = startingHealth;
        knockDownTimer = 0f;
        isDead = false;
        currentTurnToWait = 0;
    }
	
	// Update is called once per frame
	void Update () {
        //attackTimer += Time.deltaTime;
        Vector3 toTarget = player.position - transform.position;
        toTarget.y = 0f;
        Quaternion newRotation = Quaternion.LookRotation(toTarget);
        ennemyRigidBody.MoveRotation(newRotation);
        if (Vector3.Distance(player.position, transform.position) > attackMaxRange)
        {
            //Debug.Log("EN : MOVING INTO POSITION : (" + player.position.x + ", " + player.position.z +")");
            nav.SetDestination(player.position);
            WalkingAnimation();
        }
        else if(Vector3.Distance(player.position, transform.position) < attackMinRange)
        {
            //The ennemy should move backward in the direction opposite to the player (player.position + 4 * (transform.position - player.position))
            nav.SetDestination(3 * transform.position - 2 * player.position);
        }
        else
        {
            nav.SetDestination(transform.position);
            WalkingAnimation();
        }

        if (anim.GetBool("KnockDown"))
        {
            knockDownTimer += Time.deltaTime;
            if(knockDownTimer >= knockDownEndTime)
            {
                anim.SetBool("KnockDown", false);
                knockDownTimer = 0f;
            }
        }
	}

    public bool CanAttack()
    {
        if(!isDead && !isAttacking)
        {
            if(currentTurnToWait <= 0 && Vector3.Distance(player.position, transform.position) <= attackMaxRange)
            {
                return true;
            }
            currentTurnToWait--;
        }
        return false;
    }

    void WalkingAnimation()
    {
        Vector3 worldDeltaPosition = nav.nextPosition - transform.position;
        float dx = Vector3.Dot(transform.right, worldDeltaPosition);
        float dy = Vector3.Dot(transform.forward, worldDeltaPosition);
        Vector2 deltaPosition = new Vector2(dx, dy);

        // Low-pass filter the deltaMove
        float smooth = Mathf.Min(1.0f, Time.deltaTime / 0.15f);
        smoothDeltaPosition = Vector2.Lerp(smoothDeltaPosition, deltaPosition, smooth);

        // Update velocity if time advances
        if (Time.deltaTime > 1e-5f)
            velocity = smoothDeltaPosition / Time.deltaTime;

        bool shouldMove = velocity.magnitude > 0.5f && nav.remainingDistance > nav.radius;

        // Update animation parameters
        anim.SetBool("Move", shouldMove);
        anim.SetFloat("velx", velocity.x);
        anim.SetFloat("vely", velocity.y);

        transform.LookAt(player.position);
    }

    void OnAnimatorMove()
    {
        // Update position to agent position
        transform.position = nav.nextPosition;
    }


    public void Attack()
    {
        Debug.Log("ATTACKING");
        isAttacking = true;
        threatValue++;
        anim.SetTrigger("Attack");
    }

    public bool getHit(int attackPower, bool ranged)
    {
        if(isAttacking)
        {
            threatValue--;
            isAttacking = false;
            GameObject.FindGameObjectWithTag("AttackManager").GetComponent<EnemyAttack>().isAttacking = false;
            GameObject.FindGameObjectWithTag("AttackManager").GetComponent<EnemyAttack>().attackTimer = 0f;
        }
        currentHealth -= attackPower;
        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            if (!rend.gameObject.name.Contains("Target"))
            {
                rend.sharedMaterial = defaultMat;
            }
        }
        if (currentHealth <= 0)
        {
            Die();
            return true;
        }
        if (ranged)
        {
            anim.SetTrigger("HitRanged");
        }
        else
        {
            anim.SetTrigger("HitMelee");
        }
        return false;
    }

    public void getKnockedDown()
    {
        anim.SetBool("KnockDown", true);
        anim.SetTrigger("KnockDownTrigger");
    }

    void Die()
    {
        isDead = true;
        anim.SetTrigger("Dead");
        Destroy(gameObject, 1f);
    }

    public void OpenParry()
    {
        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            if (!rend.gameObject.name.Contains("Target"))
            {
                rend.sharedMaterial = preparingAttackMaterial;
            }
        }   
        GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerScript>().setDodgeWindow(true);
    }

    public void CloseParry()
    {
        GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerScript>().setDodgeWindow(false);
    }

    public void OpenPerfectParry()
    {

    }

    public void ClosePerfectParry()
    {

    }

    public void StrikePoint()
    {
        GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerScript>().GetHit(attackPower);
    }

    public void EndAttack()
    {
        currentTurnToWait = waitBetweenAttack;
        isAttacking = false;
        threatValue--;
        GameObject.FindGameObjectWithTag("AttackManager").GetComponent<EnemyAttack>().isAttacking = false;
        GameObject.FindGameObjectWithTag("AttackManager").GetComponent<EnemyAttack>().attackTimer = 0f;
        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            if(!rend.gameObject.name.Contains("Target"))
            {
                rend.sharedMaterial = defaultMat;
            }
        }
    }

    public void KnockDownOver()
    {
            
    }
}
