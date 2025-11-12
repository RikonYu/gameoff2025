using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvObj : MonoBehaviour, IAbsorbable
{
    public MagicType Typ;
    public SpriteRenderer spr;
    public virtual void Start()
    {
        spr = GetComponent<SpriteRenderer>();
    }

    public virtual void OnParticleAbsorbed(MagicType type)
    {

    }

    public virtual void Absorb(MagicType typ, int cnt)
    {

    }
    public virtual void OnBlast(Vector2 pos, Quaternion direction, bool isCircle)
    {
        var magic = Instantiate(BattleController.instance.MagicWave);
        magic.GetComponent<MagicController>().Init(pos, direction, Typ, isCircle ? 360 : 90, false, false, this.gameObject);
    }
    public virtual bool CanAbsorb(MagicType typ)
    {
        return true;
    }
}
