using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RandomAudioPlayer : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<AudioClip> clips = new();
    [SerializeField] private bool playOnAwake = true;

    [SerializeField] private bool doRandomDelay = true;
    [SerializeField] private float minDelay = 0.1f;
    [SerializeField] private float maxDelay = 2f;

    private void Awake()
    {
        if(audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    IEnumerator PlayRandom()
    {

        int index = Random.Range(0, clips.Count);
        audioSource.PlayOneShot(clips[index]);
        yield return new WaitForSeconds(audioSource.clip.length);
    }

    IEnumerator PlayRandomCoroutine()
    {
        if(playOnAwake)
        {
            StartCoroutine(PlayRandomCoroutine());
        }
        
        while(true)
        {
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
            StartCoroutine(PlayRandomCoroutine());
        }
    }
}
