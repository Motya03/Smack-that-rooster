using UnityEngine;
using static UnityEditor.Rendering.ShadowCascadeGUI;

public class CascaraScript : MonoBehaviour
{
    void OnCollisionEnter(Collision col)
    {

         if (col.gameObject.tag == "Ground")
        {

            Destroy(this.gameObject);
        }
    }
    
    
}
