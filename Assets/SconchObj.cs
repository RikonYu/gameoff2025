using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SconchObj : EnvObj
{
    int HP;
    public int MaxHP;
    public bool IsStartLighted;
    public bool IsLightUp;
    public Sprite OffSprite;
    GameObject Light;
    Animator anim;

    public override void Start()
    {
        base.Start();
        Light = transform.Find("Light").gameObject;
        anim = GetComponent<Animator>();
        IsLightUp = IsStartLighted;
        if (IsStartLighted)
            HP = MaxHP;
        else
            HP = 0;
    }
    private void Update()
    {
        if (HP <= 0)
        {
            anim.enabled = false;
            IsLightUp = false;
            Light.SetActive(false);
            spr.sprite = OffSprite;
        }
        else
        {
            IsLightUp = true;
            anim.enabled = true;
            Light.SetActive(true);
        }
    }
    public override bool CanAbsorb(MagicType typ)
    {
        if (typ == MagicType.WaterWave && IsLightUp)
            return true;
        else if (typ == MagicType.FireWave && !IsLightUp)
            return true;
        return false;
    }
    public override void OnBlast(Vector2 pos, Quaternion direction, bool isCircle, bool isEnemy)
    {
        HP = 0;
    }
    public override void Absorb(MagicType typ, int cnt, bool isEnemy)
    {
        if(typ == MagicType.FireWave)
        {
            HP += cnt;
            if (HP >= MaxHP)
                HP = MaxHP;
        }
        else
        {
            HP -= cnt;
            if (HP <= 0)
                HP = 0;
        }
    }
}
