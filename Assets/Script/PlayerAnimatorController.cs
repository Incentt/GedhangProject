using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    private Animator anim;
    void Start()
    {
        anim = GetComponent<Animator>();
        PlayIdleAnimation();
    }
    public void PlayRunAnimation()
    {
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.SetBool("isRunning", true);
        }
    }
    public void PlayIdleAnimation()
    {
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.SetBool("isRunning", false);
        }
    }
    public void PlayJumpAnimation()
    {
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.SetTrigger("jump");
        }
    }
    public void PlayLandAnimation()
    {
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.Play("Land");
        }
    }
}
