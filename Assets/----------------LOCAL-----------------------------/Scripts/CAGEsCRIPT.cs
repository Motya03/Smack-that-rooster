using UnityEngine;

public class CageScript : MonoBehaviour
{
    public Animator myAnimator;


    private void Start()
    {
         SoundManager.PlaySound(SoundType.BoxGoingDown);
    }
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
    public void CageImpactSound()
    {
        SoundManager.PlaySound(SoundType.CageImpact);
    }

    private void OnDestroy()
    {
        Destroy(this.gameObject);
    }

}