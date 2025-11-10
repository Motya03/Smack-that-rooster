using UnityEngine;

public class CageScript : MonoBehaviour
{
    private Animator myAnimator;

    private void Start()
    {
        // busca el Animator en el mismo objeto o en hijos
        myAnimator = GetComponent<Animator>();
    }

    public void ClickBattleEnd()
    {
        if (myAnimator == null)
        {
            Debug.LogWarning("⚠️ No se encontró Animator en la jaula.");
            return;
        }

        Debug.Log("LALAL");
        myAnimator.Play("cAGEbACK");
    }
    private void OnDestroy()
    { 
        Destroy(this.gameObject); 
    }
}
