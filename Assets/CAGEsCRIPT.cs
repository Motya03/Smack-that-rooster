using UnityEngine;

public class CageScript : MonoBehaviour
{
    

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Collider mainCol = GetComponent<BoxCollider>();
            Physics.IgnoreCollision(mainCol, other.collider, true);
        }
    }

    void OnCollisionExit(Collision col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            Collider mainCol = GetComponent<BoxCollider>();
            Physics.IgnoreCollision(mainCol, col.collider, false);
        }
    }
}
