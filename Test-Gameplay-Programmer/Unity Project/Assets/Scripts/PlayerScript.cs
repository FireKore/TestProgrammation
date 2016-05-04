using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerScript : MonoBehaviour {

    enum InputEnum { Nothing, Attack, Targetting, Dodge };

    Animator anim;
    Rigidbody playerRigidBody;
    Transform target = null;
    Slider healthSlider;
    Slider comboSlider;
    Text healthText;
    Text comboText;
    Image hitSplash;
    Color flashColor = new Color(1f, 0f, 0f, 0.3f);
    GameObject targettingCone;

    public float speed = 6f;
    public float swipeTresholdSqr = 500f;
    public float longRangeMax = 6f;
    public int attackPower = 15;
    public int startingHealth = 150;
    public float rayMaxLength = 100f;
    public float targettingConeAngle = 15f;
    public float comboResetTime = 2f;

    bool wasTouching = false;
    Touch firstTouch;
    Touch lastTouchSwipe;
    bool isBufferOpenned = false;
    bool isAttacking = false;
    InputEnum lastInput = InputEnum.Nothing;
    public int currentHealth;
    bool arrow = false;
    bool damaged = false;
    bool isDodgeWindowOpen = false;
    bool dodge = false;
    int floorMask;
    int combo;
    float comboResetTimer;
    //Test on pc
    Vector3 lastPos;

	// Use this for initialization
	void Awake () {
        anim = GetComponent<Animator>();
        playerRigidBody = GetComponent<Rigidbody>();

        Slider[] sliders = GameObject.FindGameObjectWithTag("UI").GetComponentsInChildren<Slider>();
        foreach (Slider slider in sliders)
        {
            if(slider.name.Contains("Health"))
            {
                healthSlider = slider;
            }
            else if(slider.name.Contains("Combo"))
            {
                comboSlider = slider;
            }
        }
        Text[] texts = GameObject.FindGameObjectWithTag("UI").GetComponentsInChildren<Text>();
        foreach (Text text in texts)
        {
            if(text.name.Contains("Health"))
            {
                healthText = text;
            }
            else if(text.name.Contains("Combo"))
            {
                comboText = text;
            }
        }

        GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>().transform.LookAt(transform.position);

        Component[] images = GameObject.FindGameObjectWithTag("UI").GetComponentsInChildren<Image>();
        foreach(Image image in images)
        {
            if(image.name.Contains("HitFlash"))
            {
                hitSplash = image;
            }
        }

        currentHealth = startingHealth;
        healthSlider.maxValue = startingHealth;
        healthSlider.value = currentHealth;
        healthText.text = currentHealth + "/" + startingHealth;
        floorMask = LayerMask.GetMask("Floor");
        combo = 0;
        comboResetTimer = 0;
        comboSlider.maxValue = comboResetTime;
        targettingCone = GameObject.FindGameObjectWithTag("Targetting");
    }
	
    void FixedUpdate()
    {
        if (anim.GetBool("Dash"))
        {
            Vector3 toTarget = target.position - transform.position;
            if (toTarget.magnitude >= longRangeMax)
            {
                Debug.Log("DASHING");
                playerRigidBody.MovePosition(transform.position + toTarget.normalized * speed * Time.deltaTime);
            }
            else
            {
                Debug.Log("END DASH");
                anim.SetBool("Dash", false);
            }
        }

        comboResetTimer += Time.deltaTime;
        if(comboResetTimer > comboResetTime)
        {
            combo = 0;
        }
        comboText.text = "Combo:\n" + combo;
        comboSlider.value = comboResetTime - comboResetTimer;
    }

	// Update is called once per frame
	void Update ()
    {
        InputEnum currentInput = InputEnum.Nothing;
        if (Input.touchSupported)
        {
            if (Input.touchCount == 1)
            {
                if (!wasTouching)
                {
                    wasTouching = true;
                    firstTouch = Input.GetTouch(0);
                }
                foreach (Touch touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Ended)
                    {
                        wasTouching = false;
                        if ((firstTouch.position - touch.position).SqrMagnitude() >= swipeTresholdSqr)
                        {
                            lastTouchSwipe = touch;
                            currentInput = InputEnum.Targetting;
                        }
                        else
                        {
                            if (touch.position.x > Screen.width / 2)
                            {
                                currentInput = InputEnum.Attack;
                            }
                            else
                            {
                                currentInput = InputEnum.Dodge;
                            }
                        }
                    }
                }
            }
        }
        //Test on pc
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (!wasTouching)
                {
                    wasTouching = true;
                    lastPos = Input.mousePosition;
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                if (wasTouching)
                {
                    wasTouching = false;

                    if (Vector3.Distance(lastPos, Input.mousePosition) > 2f)
                    {
                        //currentInput = InputEnum.Targetting;
                        Targetting(lastPos, Input.mousePosition);
                    }
                    else
                    {
                        if (lastPos.x <= Screen.width / 2)
                        {
                            currentInput = InputEnum.Dodge;
                        }
                        else
                        {
                            currentInput = InputEnum.Attack;
                        }
                    }
                }
            } 
        }

        if(currentInput != InputEnum.Nothing)
        {
            if(currentInput == InputEnum.Dodge)
            {
                Dodge();
            }
            else
            {
                if(!isBufferOpenned && !isAttacking)
                {
                    Debug.Log("FIRST INPUT");
                    if(currentInput == InputEnum.Attack)
                    {
                        Attack();
                    }
                    else if(currentInput == InputEnum.Targetting)
                    {
                        Targetting(firstTouch.position, lastTouchSwipe.position);
                    }
                }
                else if(isBufferOpenned && isAttacking)
                {
                    Debug.Log("INPUT REGISTERED");
                    lastInput = currentInput;
                }
            }
        }

        if (damaged)
        {
            hitSplash.color = flashColor;
        }
        else
        {
            hitSplash.color = Color.Lerp(hitSplash.color, Color.clear, 5f * Time.deltaTime);
        }
        damaged = false;
	}
    
    void Attack()
    {
        if(target == null)
        {
            Targetting();
        }
        if(target != null)
        {
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            Quaternion newRotation = Quaternion.LookRotation(toTarget);
            playerRigidBody.MoveRotation(newRotation);

            if (toTarget.magnitude >= longRangeMax)
            {
                Debug.Log("START DASH");
                anim.SetBool("Dash", true);
            }
            else
            {
                Debug.Log("START ATTACK");
                anim.SetFloat("DistToTarget", toTarget.magnitude);
                anim.SetTrigger("Attack");
            }
        }
    }

    void Dodge()
    {
        if(isDodgeWindowOpen)
        {
            Debug.Log("FK : DODGE");
            dodge = true;
        }
        else
        {
            combo = 0;
        }
    }

    void Targetting()
    {
        Targetting(GameObject.FindGameObjectsWithTag("Enemy"));
    }

    void Targetting(GameObject[] enemies)
    {
        if(target != null)
            target.FindChild("Target").gameObject.SetActive(false);

        float minDist = float.MaxValue;
        int maxThreatValue = 0;
        GameObject go = null;
        foreach (GameObject g in enemies)
        {
            if (!g.GetComponent<EnnemyScript>().isDead)
            {
                if (target == null || g.transform.position != target.position)
                {
                    //if there is a bigger threat
                    if(maxThreatValue < g.GetComponent<EnnemyScript>().threatValue)
                    {
                        go = g;
                        maxThreatValue = g.GetComponent<EnnemyScript>().threatValue;
                    }
                    //or if their threat values are the same...
                    else if(maxThreatValue == g.GetComponent<EnnemyScript>().threatValue)
                    {
                        //if there is a closest one
                        if (Vector3.Distance(transform.position, g.transform.position) < minDist)
                        {
                            go = g;
                            minDist = Vector3.Distance(transform.position, g.transform.position);
                        }
                    }
                }
            }
        }
        if (go != null)
        {
            target = go.transform;
            target.FindChild("Target").gameObject.SetActive(true);
        }
        else
            target = null;
    }

    void Targetting(Vector3 startPos, Vector3 endPos)
    {
        Debug.Log("TARGETTING");
        Ray startRay = Camera.main.ScreenPointToRay(startPos);
        Ray endRay = Camera.main.ScreenPointToRay(endPos);

        RaycastHit startHit;
        RaycastHit endHit;
        if (Physics.Raycast(startRay, out startHit, rayMaxLength, floorMask) && Physics.Raycast(endRay, out endHit, rayMaxLength, floorMask))
        {
            Debug.Log("DOUBLE HIT");
            Vector3 a, b, swipeDirection;
            swipeDirection = (endHit.point - startHit.point).normalized;
            a = endHit.point + new Vector3(swipeDirection.z, 0f, -swipeDirection.x) * Vector3.Distance(startHit.point, endHit.point) * Mathf.Tan(targettingConeAngle / 2 * Mathf.Deg2Rad);
            a.y = 0.1f;
            b = endHit.point + new Vector3(-swipeDirection.z, 0f, swipeDirection.x) * Vector3.Distance(startHit.point, endHit.point) * Mathf.Tan(targettingConeAngle / 2 * Mathf.Deg2Rad);
            b.y = 0.1f;

            List<GameObject> enemiesInCone = new List<GameObject>();
            foreach(GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
            {
                if(isInSwipe(enemy.transform.position, startHit.point, a, b))
                {
                    enemiesInCone.Add(enemy);
                }
            }
            Targetting(enemiesInCone.ToArray());
            Attack();
            
            //Display swipe cone for test
            Mesh mesh = targettingCone.GetComponent<MeshFilter>().mesh;
            mesh.Clear();

            Vector3[] vertices = new Vector3[3];
            vertices[0] = startHit.point;
            vertices[1] = b;
            vertices[2] = a;
            mesh.vertices = vertices;

            Vector2[] uvs = new Vector2[mesh.vertices.Length];
            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i] = new Vector2(mesh.vertices[i].x, mesh.vertices[i].z);
            }

            int[] triangles = new int[3];
            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;
            mesh.triangles = triangles;
        }
    }

    bool isInSwipe(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
    {
        bool b1, b2, b3;

        b1 = sign(point, a, b) < 0.0f;
        b2 = sign(point, b, c) < 0.0f;
        b3 = sign(point, c, a) < 0.0f;

        return ((b1 == b2) && (b2 == b3));
    }

    float sign(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return (p1.x - p3.x) * (p2.z - p3.z) - (p2.x - p3.x) * (p1.z - p3.z);
    }

    public void GetHit(int attackPower)
    {
        combo = 0;
        if (currentHealth > 0)
        {
            if (!dodge)
            {
                damaged = true;
                currentHealth -= attackPower;
                healthSlider.value = currentHealth;
                healthText.text = currentHealth + "/" + startingHealth;
                if (currentHealth <= 0)
                {
                    Die();
                    return;
                }
                anim.SetTrigger("Hit");
            }
            else
            {
                anim.SetTrigger("Dodge");
                dodge = false;
            }
        }
    }

    void Die()
    {
        anim.SetTrigger("Die");
    }

    public void OpenBuffer()
    {
        Debug.Log("FK : OPEN BUFFER");
        isBufferOpenned = true;
    }

    public void StrikePoint()
    {
        Debug.Log("FK : STRIKE POINT");
        if (target != null)
        {
            EnnemyScript ennemy = target.GetComponentInParent<EnnemyScript>();
            if (ennemy != null)
            {
                combo++;
                comboResetTimer = 0f;
                if (ennemy.getHit(attackPower, arrow))
                {
                    target.FindChild("Target").gameObject.SetActive(false);
                    target = null;
                }
            }
        }
    }

    public void LaunchArrow()
    {
        Debug.Log("LAUNCH ARROW");
        arrow = true;
    }

    public void CloseBuffer()
    {
        Debug.Log("FK : CLOSE BUFFER");
        isBufferOpenned = false;
    }

    public void EndAttack()
    {
        Debug.Log("FK : END ATTACK");
        if (lastInput == InputEnum.Attack)
        {
            Debug.Log("CONTINUE COMBO");
            Attack();
        }
        else if (lastInput == InputEnum.Targetting)
        {
            Debug.Log("SWITCH TARGET");
            Targetting();
        }
        else if(lastInput == InputEnum.Nothing)
        {
            Debug.Log("END ATTACK");
            isAttacking = false;
        }

        lastInput = InputEnum.Nothing;
        arrow = false;
    }

    public void KnockDownStrikePoint()
    {
        Debug.Log("FK : KNOCKDOWN STRIKE POINT");
        if(target != null)
        {
            EnnemyScript ennemy = target.GetComponentInParent<EnnemyScript>();
            if (ennemy != null)
            {
                combo++;
                comboResetTimer = 0f;
                if (ennemy.getHit(attackPower, arrow))
                {
                    target.FindChild("Target").gameObject.SetActive(false);
                    target = null;
                }
                ennemy.getKnockedDown();
            }
        }
    }

    public void EndAnim()
    {

    }

    public void setDodgeWindow(bool isOpen)
    {
        isDodgeWindowOpen = isOpen;
    }
}
