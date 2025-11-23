using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrunkObj : EnvObj
{
    bool isIgnite;
    public int MaxHP, HpDownRate;
    int hp;
    public Sprite fireSpr, normalSpr;
    GameObject LightObj;
    public override void Start()
    {
        LightObj = transform.Find("Light").gameObject;
        base.Start();
        isIgnite = false;
        normalSpr = spr.sprite;
        LightObj.SetActive(false);
    }
    private void FixedUpdate()
    {
        hp -= HpDownRate;
        if (hp < 0)
        {
            OnBlast(Vector2.zero, Quaternion.identity, false, false);
        }
    }
    public override bool CanAbsorb(MagicType typ)
    {
        if (!isIgnite && typ == MagicType.FireWave)
            return true;
        if (isIgnite && typ == MagicType.WaterWave)
            return true;
        return false;
    }
    public override void Absorb(MagicType typ, int cnt, bool isEnemy)
    {
        if(!isIgnite)
        {
            hp += cnt;
            if(hp>=MaxHP)
            {
                OnBlast(Vector2.zero, Quaternion.identity, false, false);
            }
        }
        else
        {
            hp -= cnt;
            if(hp<=0)
            {
                OnBlast(Vector2.zero, Quaternion.identity,false,false);
            }
        }
    }
    public override void OnBlast(Vector2 pos, Quaternion direction, bool isCircle, bool isEnemy)
    {
        if (hp >= MaxHP)
        {
            isIgnite = true;
            hp = MaxHP;
            spr.sprite = fireSpr;
            LightObj.SetActive(true);
        }
        else
        {
            isIgnite = false;
            hp = 0;
            spr.sprite = normalSpr;
            LightObj.SetActive(false);
        }
    }

}
