using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils
{
    public static IEnumerator ChainEnums(List<IEnumerator> ienumList)
    {
        foreach (var ienum in ienumList)
        {
            while (ienum.MoveNext())
            {
                yield return ienum.Current;
            }
        }
    }

    public static IEnumerator WaitForKSeconds(float k)
    {
        yield return new WaitForSeconds(k);
    }

    public static IEnumerator WaitUntilCondition(System.Func<bool> condition)
    {
        while (!condition())
        {
            yield return null;
        }

    }

    public static IEnumerator RunOnce(System.Action func)
    {
        func?.Invoke();
        yield return null;
    }
    public static bool CanAbsorb(MagicType typ, GameObject src, GameObject des)
    {
        if (des.GetComponent<EnvObj>() != null && des.GetComponent<EnvObj>().CanAbsorb(typ))
            return true;
        return false;

    }
    public static bool IsCounter(MagicType A, MagicType B)
    {
        return true;
    }

    public static bool SameColor(Color color1, Color color2)
    {
        float rDiff = color1.r - color2.r;
        float gDiff = color1.g - color2.g;
        float bDiff = color1.b - color2.b;
        return Mathf.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff)<=0.1f;
    }

}
