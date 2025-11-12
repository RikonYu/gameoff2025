using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SconchObj : EnvObj
{
    int HP;
    public int MaxHP;
    public bool IsStartLighted;
    public Sprite OffSprite;
    GameObject Light;
    Animator anim;

    public override void Start()
    {
        base.Start();
        Light = transform.Find("Light").gameObject;
        anim = GetComponent<Animator>();
        if (IsStartLighted)
            HP = MaxHP;
        else
            HP = 0;
    }
    private void Update()
    {
        if (HP <= 0)
        {
            this.cnt = 0;
            anim.enabled = false;
            Light.SetActive(false);
            spr.sprite = OffSprite;
        }
        else
        {
            anim.enabled = true;
            Light.SetActive(true);
        }
    }
    public override bool CanAbsorb(MagicType typ)
    {
        if (typ == MagicType.WaterWave || typ == MagicType.FireWave)
            return true;
        return false;
    }
    public override void OnBlast(Vector2 pos, Quaternion direction, bool isCircle)
    {
        HP = 0;
    }
    int cnt = 0;
    public override void Absorb(MagicType typ, int cnt)
    {
        if(typ == MagicType.FireWave)
        {
            this.cnt += cnt;
            if (this.cnt >= MaxHP)
                HP = MaxHP;
        }
        else
        {
            HP -= cnt;
        }
    }
}
