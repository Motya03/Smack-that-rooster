using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuButtonController : MonoBehaviour
{
    public int index;
    [SerializeField] bool keyDown;
    [SerializeField] int maxIndex;
    public AudioSource audioSource;

    // Bandera para saber si estamos usando ratón o teclado
    public bool mouseActive = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // Si se usa el ratón, no mover el índice
        if (mouseActive)
            return;

        // Control con teclado o mando
        float vertical = Input.GetAxis("Vertical");
        if (vertical != 0)
        {
            if (!keyDown)
            {
                if (vertical < 0)
                {
                    if (index < maxIndex)
                        index++;
                    else
                        index = 0;
                }
                else if (vertical > 0)
                {
                    if (index > 0)
                        index--;
                    else
                        index = maxIndex;
                }

                keyDown = true;
            }
        }
        else
        {
            keyDown = false;
        }
    }
}
