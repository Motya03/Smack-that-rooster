using UnityEngine.SceneManagement;
using UnityEngine;

public class MusicBox2 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        MusicManager.StopMusic(MusicType.MainMenuBack);
        MusicManager.PlayMusic(MusicType.EnterCharMusic, 0.5f);
    }

  
}
