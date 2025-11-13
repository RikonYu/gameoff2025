using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleController : MonoBehaviour
{
    public static BattleController instance;
    public GameObject MagicWave;
    public GameObject LeftWall, RightWall;
    public MagicType CurrentMagic;
    // Start is called before the first frame update
    void Start()
    {
        CurrentMagic = MagicType.WaterWave;
        Camera.main.orthographicSize = Consts.CameraSize;
        instance = this;
        LeftWall.transform.position = Vector3.left * (5f+Consts.CameraSize);
        RightWall.transform.position = Vector3.right * (5f + Consts.CameraSize);
        LeftWall.GetComponent<SpriteRenderer>().size = new Vector2(1f, Consts.maxHeight);
        RightWall.GetComponent<SpriteRenderer>().size = new Vector2(1f, Consts.maxHeight);
        LeftWall.GetComponent<BoxCollider2D>().size = new Vector2(1f, Consts.maxHeight);
        RightWall.GetComponent<BoxCollider2D>().size = new Vector2(1f, Consts.maxHeight);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
