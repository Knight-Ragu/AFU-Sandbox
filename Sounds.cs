using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Il2CppQuantum;
using NAudio.Wave;
using UnityEngine;

namespace AfuSandbox;

internal static class Sounds
{
    internal const int SAMPLERATE = 48000;
    internal static string SoundsDir => Sandbox.Assets + "\\sfx";

    internal static List<float[]> sounds = [];

    internal static (string fileName, float gain)[] files = [
        ("spawn_gun_fire-001", 0.8f),
        ("spawn_gun_fire-002", 0.8f),
        ("spawn_gun_fire-003", 0.8f),
        ("spawn_gun_fire-004", 0.8f),
    ];

    internal static void LoadSounds()
    {
        foreach (var (fileName, gain) in files)
            using (var reader = new WaveFileReader($"{SoundsDir}\\{fileName}.wav"))
                sounds.Add(reader.CollectSamples(gain));

                // var samples = reader.CollectSamples();
                // Sandbox .Log.Msg($"Count: {(int)reader.SampleCount}, Rate: {SAMPLERATE}");

                // AudioClip clip = AudioClip.Create(file, samples.Length, 2, SAMPLERATE * 2, true, false);
                // clip.SetData(samples, 0);
    }

    internal static float[] CollectSamples(this WaveFileReader reader, float gain = 1.0f)
    {
        List<float> samples = [];

        float[] currSample = [0.0f];

        while(currSample is not null)
        {
            currSample = reader.ReadNextSampleFrame();

            if (currSample is null) break;

            foreach (var s in currSample)
                samples.Add(s * gain);
        }

        return [.. samples];
    }

    public static AudioClip GetClip(int clipID)
    {
        var samps = sounds[clipID];

        AudioClip clip = AudioClip.Create("soundEffect", samps.Length, 2, Sounds.SAMPLERATE, true, false);
        clip.SetData(samps, 0);

        return clip;
    }
    
    public static AudioClip GetClip(string clipName)
    {
        for (int i = 0; i < files.Length; i++)
            if (files[i].fileName == clipName)
                return GetClip(i);

        Sandbox .Log.Msg($"Could not find sound '{clipName}', returning default sound effect");
        return GetClip(0);
    }

    internal static List<AudioSource> Sources = [];

    public static void PlaySound(int soundID, int randOffset, float volume, float pitch, Vector3 position)
    {
        int ID = soundID + Random.RandomRangeInt(0, randOffset);
        AudioSource aS = Sources.FirstOrDefault(s => !s.isPlaying);
        var samps = sounds[ID];

        if (aS == default)
        {
            GameObject sound = new($"Source");
            sound.transform.position = position;
            
            aS = sound.AddComponent<AudioSource>();
            aS.mute = false;
            aS.enabled = true;
            aS.maxDistance = 15;
            aS.minDistance = 1;

            AudioClip clip = AudioClip.Create("soundEffect", samps.Length, 2, Sounds.SAMPLERATE, true, false);
            aS.clip = clip;

            Sources.Add(aS);
        }
        
        aS.clip.SetData(samps, 0);

        aS.volume = volume;
        aS.pitch = pitch;
        aS.clip = Sounds.GetClip(ID);

        aS.Play();
    }
}

[HarmonyPatch(typeof(SessionRunner), "Shutdown")]
class Shutdown
{
    public static void Postfix() // Shutting down runner
    {
        Sounds.Sources.Clear();
    }
}