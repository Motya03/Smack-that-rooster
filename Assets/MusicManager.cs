using System.Collections.Generic;

using UnityEngine;



public enum MusicType
{
    MainMenuBack,
    FightMusic,
    LastSeconds,
    ClickerGameMusic,
    ChickenMusic,
    EnterCharMusic


}

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    [SerializeField] private AudioClip[] soundList;

    private static MusicManager instance;
    private Dictionary<MusicType, AudioSource> soundSources = new Dictionary<MusicType, AudioSource>();

    private void Awake()
    {
       

        DontDestroyOnLoad(this);
        
      
            instance = this;

            // Crear un AudioSource por cada sonido
            foreach (MusicType type in System.Enum.GetValues(typeof(MusicType)))
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.clip = soundList[(int)type];
                soundSources.Add(type, src);
            }
        
    }

    public static void PlayMusic(MusicType sound, float volume )
    {
        AudioSource src = instance.soundSources[sound];
        src.volume = volume;

        src.Play();
    }

    public static void StopMusic(MusicType sound)
    {
        instance.soundSources[sound].Stop();
    }
}
