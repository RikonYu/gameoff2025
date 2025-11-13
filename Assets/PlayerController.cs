using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour, IAbsorbable
{
    public float Speed;
    public float MaxHP;
    public float currentHP;
    public float MagicCD;
    public MagicType CurrentTyp;
    float cooldown;
    Rigidbody2D rb;
    GameObject spr;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spr = transform.Find("Sprite").gameObject;
        currentHP = MaxHP;
        cooldown = 0f;
    }

    public void OnParticleAbsorbed(MagicType type)
    {

    }

    // Update is called once per frame
    void Update()
    {
        currentHP -= dmgCnt;
        cooldown -= Time.deltaTime;
        dmgCnt = 0;
        Vector2 spd = Vector2.zero;
        if (Input.GetKey(KeyCode.A))
            spd += Vector2.left;
        else if (Input.GetKey(KeyCode.D))
            spd += Vector2.right;
        spd *= Speed;
        if (spd.x < 0)
            spr.GetComponent<SpriteRenderer>().flipX = false;
        else if(spd.x>0)
            spr.GetComponent<SpriteRenderer>().flipX = true;
        if (spd != Vector2.zero)
            spr.GetComponent<Animator>().SetBool("isWalking", true);
        else
            spr.GetComponent<Animator>().SetBool("isWalking", false);

        rb.velocity = spd + Vector2.up * rb.velocity.y;
        if(Input.GetMouseButton(0))
        {
            if (cooldown <= 0f)
            {
                cooldown = MagicCD;
                var magic = Instantiate(BattleController.instance.MagicWave);
                var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition, 0f);
                Quaternion direction = Quaternion.Euler(0f, 0f, Mathf.Atan2(pos.y, pos.x) * Mathf.Rad2Deg-90);
                magic.GetComponent<MagicController>().Init(this.transform.position, direction, BattleController.instance.CurrentMagic, 90, true, false, this.gameObject);
            }
        }
    }

    int dmgCnt;
    public void OnHit()
    {
        dmgCnt++;
    }
}
