using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvObj : MonoBehaviour, IAbsorbable
{
    public MagicType Typ;
    void Start()
    {
        
    }

    public void OnParticleAbsorbed(MagicType type)
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnBlast(Vector2 pos, Quaternion direction, bool isCircle)
    {
        var magic = Instantiate(BattleController.instance.MagicWave);
        magic.GetComponent<MagicController>().Init(pos, direction, Typ, isCircle ? 360 : 90, false, false, this.gameObject);
    }
    public bool CanAbsorb(MagicType typ)
    {
        return true;
    }
}
