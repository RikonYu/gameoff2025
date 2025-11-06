using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Rigidbody2D rb;
    public float HSpeed;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.A))
            rb.velocity = new Vector2(-HSpeed, rb.velocity.y);
        else if (Input.GetKey(KeyCode.D))
            rb.velocity = new Vector2(HSpeed, rb.velocity.y);

    }
}
