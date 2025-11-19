using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public MagicType AtkType, Weakness;
    public float MaxHP;
    public float currentHP;
    public float MagicCD;
    float cooldown;
    Rigidbody2D rb;
    public virtual void Start()
    {
        currentHP = MaxHP;
        cooldown = MagicCD;
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    public virtual void Update()
    {
        
    }
}
