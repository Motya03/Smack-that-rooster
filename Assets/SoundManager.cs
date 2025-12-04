using System;
using UnityEngine;
using UnityEngine.Audio;


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
    private static SoundManager instance = null;
    private AudioSource audioSource;

    private void Awake()
    {
        if (!instance)
        {
            instance = this;
            audioSource = GetComponent<AudioSource>();
        }
    }

    public static void PlaySound(SoundType sound, AudioSource source = null, float volume = 1)
    {
        instance.audioSource.PlayOneShot(instance.soundList[(int)sound], volume);
    }
}


