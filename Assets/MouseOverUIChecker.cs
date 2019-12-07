using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseOverUIChecker : MonoBehaviour
{
    private UIController uIController;

    void Awake()
    {
        uIController = GetComponentInParent<UIController>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && EventSystem.current.IsPointerOverGameObject())
        {
            uIController.clickedUIThisFrame = true;
        }
    }
}
