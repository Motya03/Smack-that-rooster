using System.Collections.Generic;
using UnityEngine;

public enum SoundType
{
    AttackPrime,
    AttackSecond,
    Crouch,
    Dance,
    Dash,
    Dead,
    Jump,
    StunStars,
    Throw,
    BoxDestroyed,
    Run,
    CageImpact,
    BoxGoingDown,
    FullRun,
    EggCrack
}

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioClip[] soundList;

    private static SoundManager instance;
    private Dictionary<SoundType, AudioSource> soundSources = new Dictionary<SoundType, AudioSource>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            // Crear un AudioSource por cada sonido
            foreach (SoundType type in System.Enum.GetValues(typeof(SoundType)))
            {
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.clip = soundList[(int)type];
                soundSources.Add(type, src);
            }
        }
    }

    public static void PlaySound(SoundType sound, float volume = 1f)
    {
        AudioSource src = instance.soundSources[sound];
        src.volume = volume;
       
        src.Play();
    }

    public static void StopSound(SoundType sound)
    {
        instance.soundSources[sound].Stop();
    }
}
