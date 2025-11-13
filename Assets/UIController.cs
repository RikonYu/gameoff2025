using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public GameObject wheel, pin;
    public float WheelSpeed;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        wheel.transform.position = Input.mousePosition;
        pin.transform.position = Input.mousePosition + Vector3.up*50;
        wheel.SetActive(Input.GetMouseButton(2));
        pin.SetActive(Input.GetMouseButton(2));
        if(Input.GetMouseButton(2))
        {
            print(Input.GetAxis("Mouse ScrollWheel"));
            wheel.transform.Rotate(Vector3.forward * WheelSpeed * Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime);
            
        }
        if (Input.GetMouseButtonUp(2))
        {
            Color clr = GetOverlapColor(pin.transform.Find("target").gameObject, wheel);
            if (Utils.SameColor(clr, Consts.ElementColors[MagicType.WaterWave]))
                BattleController.instance.CurrentMagic = MagicType.WaterWave;
            if (Utils.SameColor(clr, Consts.ElementColors[MagicType.FireWave]))
                BattleController.instance.CurrentMagic = MagicType.FireWave;
            if (Utils.SameColor(clr, Consts.ElementColors[MagicType.EarthWave]))
                BattleController.instance.CurrentMagic = MagicType.EarthWave;
            if (Utils.SameColor(clr, Consts.ElementColors[MagicType.GrassWave]))
                BattleController.instance.CurrentMagic = MagicType.GrassWave;
            if (Utils.SameColor(clr, Consts.ElementColors[MagicType.MetalWave]))
                BattleController.instance.CurrentMagic = MagicType.MetalWave;


        }

    }
    public Color GetOverlapColor(GameObject objA, GameObject objB)
    {
        var rectA = objA.GetComponent<RectTransform>();
        var rectB = objB.GetComponent<RectTransform>();
        var imgB = objB.GetComponent<Image>();

        if (!rectA || !rectB || !imgB || !imgB.sprite) return Color.clear;

        Vector2 localPos = rectB.InverseTransformPoint(rectA.position);
        Rect bRect = rectB.rect;

        if (!bRect.Contains(localPos)) return Color.clear;

        float normX = (localPos.x - bRect.x) / bRect.width;
        float normY = (localPos.y - bRect.y) / bRect.height;

        var tr = imgB.sprite.textureRect;
        float pixelX = tr.x + (normX * tr.width);
        float pixelY = tr.y + (normY * tr.height);

        return imgB.sprite.texture.GetPixel((int)pixelX, (int)pixelY);
    }
}
