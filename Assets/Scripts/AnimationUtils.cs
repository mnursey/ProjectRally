using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void AnimCallback();

public class AnimationUtils : MonoBehaviour
{
    public AnimCallback cb;

    public void DisableGameobject()
    {
        this.gameObject.SetActive(false);
    }

    public void OnFinished()
    {
        cb?.Invoke();
    }
}
