using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuneObj : EnvObj
{
    public float Cooldown;
    float cd;
    int hitCount;
    public int MaxHit;

    
    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        cd = 0f;
        hitCount = 0;
    }
    private void Update()
    {
        cd -= Time.deltaTime;
    }
    public override void Absorb(MagicType typ, int cnt, bool isEnemy)
    {
        if (cd > 0)
            return;
        if (Utils.IsCounter(typ, this.Typ))
        {
            hitCount -= cnt * 2;
            if (hitCount <= 0)
                hitCount = 0;
        }
        else if (typ == this.Typ)
        {
            hitCount += cnt;
            if (hitCount >= MaxHit)
                OnBlast(transform.position, Quaternion.identity, true, isEnemy);
        }
        else if (Utils.IsGenerate(typ, this.Typ))
        {
            hitCount += cnt*2;
            if (hitCount >= MaxHit)
                OnBlast(transform.position, Quaternion.identity, true, isEnemy);
        }
    }
    public override bool CanAbsorb(MagicType typ)
    {
        if (cd > 0) return false;
        if (Utils.IsCounter(typ, this.Typ))
            return true;
        if (typ == this.Typ)
            return true;
        if (Utils.IsGenerate(typ, this.Typ))
            return true;
        return false;
    }
    public override void OnBlast(Vector2 pos, Quaternion direction, bool isCircle, bool isEnemy)
    {
        cd = Cooldown;
        hitCount = 0;
        base.OnBlast(pos, direction, isCircle, isEnemy);
    }
}
