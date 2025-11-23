using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    public List<SconchObj> Links;
    Animator anim;
    BoxCollider2D bc;
    void Start()
    {
        anim = GetComponent<Animator>();
        bc = GetComponent<BoxCollider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        foreach(var i in Links)
        {
            if (!i.IsLightUp)
                return;
        }
        anim.enabled = true;
        bc.enabled = false;
    }
}
