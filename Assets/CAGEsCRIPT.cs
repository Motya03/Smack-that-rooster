using UnityEngine;

public class CageScript : MonoBehaviour
{
    public Animator myAnimator;



    public void ClickBattleEnd()
    {
        Debug.Log("LALAL");

        // Si el objeto está inactivo, lo activamos
        if (!myAnimator.gameObject.activeInHierarchy)
        {
            myAnimator.gameObject.SetActive(true);
        }

        // Ahora sí, reproducimos la animación
        myAnimator.Play("CageBack");
    }


}