using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems; 

public class MenuButtonMainNewInput : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] MenuButtonController menuButtonController;
    [SerializeField] Animator animator;
    [SerializeField] AnimatorFunctions animatorFunctions;
    [SerializeField] int thisIndex;
   

    void Update()
    {
        if (menuButtonController.index == thisIndex)
        {
            animator.SetBool("selected", true);

            if (Input.GetAxis("Submit") == 1 ) 
            {
                animator.SetBool("pressed", true);
                Debug.Log("a1");
            }
            else if (animator.GetBool("pressed"))
            {
                animator.SetBool("pressed", false);
                animatorFunctions.disableOnce = true;
                Debug.Log("a2");
            }
        }
        else  
        {
            animator.SetBool("selected", false);
            Debug.Log("a3");
            
        }
       
    }

    // 👇 SECCIÓN NUEVA 👇

    // Cuando el ratón entra sobre este botón
    public void OnPointerEnter(PointerEventData eventData)
    {
        menuButtonController.index = thisIndex; // mueve la selección a este botón
        animator.SetBool("selected", true);
    }

    // Cuando se hace clic en este botón
    public void OnPointerClick(PointerEventData eventData)
    {
        animator.Play("press");
        animatorFunctions.disableOnce = true;
        Debug.Log("aaaaaaaaaaaaaaaaaaa");
         

    }
    public void OnPointerExit(PointerEventData eventData)
    {
        animator.SetBool("selected", false);
    }
}
