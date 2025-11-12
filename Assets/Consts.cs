using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MagicType
{
    WaterWave,
    FireWave,
}
public class Consts
{
    public static float CameraSize = 15f;
    public static float maxHeight = 50f;
    public static float WaveLifetime = 3f;
    public static int ParticlePerDegree = 20;
    public static Dictionary<MagicType, Color> ElementColors = new Dictionary<MagicType, Color>()
    {
        {MagicType.WaterWave, new Color(0.31f, 0.561f, 0.729f) },
        {MagicType.FireWave, new Color(0.647f,0.188f,0.188f) }
    };
}

public interface IAbsorbable
{
    public void OnParticleAbsorbed(MagicType type);
}