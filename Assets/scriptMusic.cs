using UnityEngine;

public class MusicBox : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MusicManager.StopMusic(MusicType.EnterCharMusic);
        
        MusicManager.PlayMusic(MusicType.MainMenuBack, 0.5f);
    }

 
}
