using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MagicType
{
    WaterWave,
    FireWave,
    MetalWave,
    GrassWave,
    EarthWave
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
        {MagicType.FireWave, new Color(0.647f,0.188f,0.188f) },
        {MagicType.MetalWave, new Color(0.910f, 0.757f, 0.439f) },
        { MagicType.GrassWave, new Color(0.459f, 0.655f, 0.263f)},
        {MagicType.EarthWave, new Color(0.855f, 0.525f, 0.243f) }
    };
}

public interface IAbsorbable
{
    public void OnParticleAbsorbed(MagicType type);
}