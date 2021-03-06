﻿using UnityEngine;
using System.Collections;

public delegate void PlayerDeath();
public delegate void StandingEnter(bool standing);

public class MountScript : MonoBehaviour
{
    public bool isSecondary = false;
    public float horizontalInput = 0;
    public bool upInput = false;
    public event PlayerDeath OnBeeDeath;
    public event StandingEnter OnStandingEnter;

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.GetComponent<Rigidbody2D>().velocity.x > 0)
        {
            gameObject.GetComponent<SpriteRenderer>().flipX = false;
            transform.Find("Rider").GetComponent<SpriteRenderer>().flipX = false;
        }
        if (gameObject.GetComponent<Rigidbody2D>().velocity.x < 0)
        {
            gameObject.GetComponent<SpriteRenderer>().flipX = true;
            transform.Find("Rider").GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    void Jump(bool up)
    {
        upInput = up;
    }

    void MoveHorizontal(float input)
    {
        horizontalInput = input;
    }

    void StandingAnimation(bool standing)
    {
        gameObject.GetComponent<Animator>().SetBool("OnPlatform", standing);
    }

    // FixedUpdate is called about once per frame at a regular interval
    void FixedUpdate()
    {
        //Animations
        if (upInput == true)
        {
            gameObject.GetComponent<Animator>().SetTrigger("UpButtonPushed");
        }
        gameObject.GetComponent<Animator>().SetBool("SideButtonPushed", (horizontalInput != 0));

        gameObject.GetComponent<Animator>().SetFloat("RunningSpeed", Mathf.Abs(gameObject.GetComponent<Rigidbody2D>().velocity.x));

        if (isSecondary == false)
        {
            if ((gameObject.GetComponent<Rigidbody2D>().velocity.x < 2 && gameObject.GetComponent<Rigidbody2D>().velocity.x > -2) ||
                (horizontalInput > 0 && gameObject.GetComponent<Rigidbody2D>().velocity.x < 0) ||
                (horizontalInput < 0 && gameObject.GetComponent<Rigidbody2D>().velocity.x > 0)
                )
            {
                gameObject.GetComponent<Rigidbody2D>().AddForce(Vector2.right * horizontalInput * 5);
            }

            if (upInput == true)
            {
                gameObject.GetComponent<Rigidbody2D>().AddForce(Vector2.up * 50);
                upInput = false;
            }
        }

    }
    void OnCollisionEnter2D(Collision2D coll)
    {
        if (coll.gameObject.tag == "Warrior")
        {
            Debug.Log("Collision");
            if (coll.gameObject.transform.position.y > gameObject.transform.position.y)
            {
                if (OnBeeDeath != null)
                {
                    OnBeeDeath();
                }
            }
        }
        if (coll.gameObject.tag == "Terrain" && coll.contacts[0].normal == Vector2.up && isSecondary == false)
        {
            OnStandingEnter(true);
        }
    }

    void OnCollisionExit2D(Collision2D coll)
    {
        if (coll.gameObject.tag == "Terrain" && coll.contacts[0].normal == Vector2.up && isSecondary == false)
        {
            OnStandingEnter(false);
        }
    }
}
