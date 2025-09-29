using System;
using UnityEngine;

namespace PTL.Framework
{
	public class AudioManager : Singleton<AudioManager>
	{
		public Sound[] Sounds;

		protected override void Awake()
		{
			base.Awake();

			foreach (Sound sound in Sounds)
			{
				sound.Source = gameObject.AddComponent<AudioSource>();
				sound.Source.clip = sound.Clip;

				sound.Source.loop = sound.IsLooping;
				sound.Source.volume = sound.Volume;
				sound.Source.pitch = sound.Pitch;
			}
		}

		public void PlaySound(string name)
		{
			Sound s = Array.Find(Sounds, sound => sound.Name == name); // find the sound with the name that matches to the name of sound to be played
			if (s == null)
			{
				Debug.LogWarning("Sound " + s.Name + " not found");
				return;
			}

			s.Source.Play();
		}

		public void StopSound(string name)
		{
			Sound s = Array.Find(Sounds, sound => sound.Name == name);
			if (s == null)
			{
				Debug.LogWarning("Sound " + s.Name + " not found");
				return;
			}

			if (!s.Source.isPlaying)
			{
				Debug.LogWarning("Sound " + s.Name + " is not being played");
				return;
			}

			s.Source.Stop();
		}

		public Sound GetSound(string name)
		{
			return Array.Find(Sounds, sound => sound.Name == name);
		}
	}

	[System.Serializable]
	public class Sound 
	{
		public string Name;
		public AudioClip Clip;

		[Range(0, 1)] public float Volume;
		[Range(.1f, 3)] public float Pitch;

		public bool IsLooping;
		[HideInInspector] public AudioSource Source;
	}
}