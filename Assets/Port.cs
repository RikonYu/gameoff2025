using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Port : MonoBehaviour
{
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.transform.parent.gameObject.GetComponent<PlayerController>()!=null)
        {
            BattleController.instance.SwitchScene();
        }
    }
}
