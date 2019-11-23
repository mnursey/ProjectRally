using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void AnimCallback();

public class AnimationUtils : MonoBehaviour
{
    public AnimCallback cb;
    public bool randomStart = false;
    public string rndAnimName = "";

    void Awake()
    {
        if(randomStart)
        {
            Animator anim = GetComponent<Animator>();
            AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
            anim.Play(rndAnimName, -1, Random.Range(0f, 1f));
        }
    }

    public void DisableGameobject()
    {
        this.gameObject.SetActive(false);
    }

    public void OnFinished()
    {
        cb?.Invoke();
    }
}
