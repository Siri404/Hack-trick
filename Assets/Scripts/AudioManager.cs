using UnityEngine.Audio;
using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{

	public static AudioManager instance;

	public AudioMixerGroup mixerGroup;

	public Sound[] sounds;

	void Awake()
	{
		if (instance != null)
		{
			Destroy(gameObject);
		}
		else
		{
			instance = this;
			DontDestroyOnLoad(gameObject);
		}

		foreach (Sound sound in sounds)
		{
			sound.source = gameObject.AddComponent<AudioSource>();
			sound.source.clip = sound.clip;
			sound.source.loop = sound.loop;

			sound.source.outputAudioMixerGroup = mixerGroup;
		}
	}

	public void Play(string soundName)
	{
		return;
		Sound sound = Array.Find(sounds, item => item.name == soundName);
		if (sound == null)
		{
			Debug.LogWarning("Sound: " + name + " not found!");
			return;
		}

		sound.source.volume = sound.volume * (1f + UnityEngine.Random.Range(-sound.volumeVariance / 2f, sound.volumeVariance / 2f));
		sound.source.pitch = sound.pitch * (1f + UnityEngine.Random.Range(-sound.pitchVariance / 2f, sound.pitchVariance / 2f));

		sound.source.Play();
	}

	public void Start()
	{
		//Play("Theme");
	}
}
