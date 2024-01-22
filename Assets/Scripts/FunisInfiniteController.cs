using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using User670.StemPlayer;

[RequireComponent(typeof(StemPlayer))]
public class FunisInfiniteController : MonoBehaviour
{
    StemPlayer stemPlayer;
    public string[] instrumentNames;
    public AudioClip[] introClips;
    public AudioClip[] loopClips;
    // Start is called before the first frame update
    void Start()
    {
        stemPlayer=GetComponent<StemPlayer>();

        for(int i = 0; i < instrumentNames.Length; i++){ 
            string n=instrumentNames[i];
            AudioSource introSource = gameObject.AddComponent<AudioSource>();
            introSource.clip = introClips[i];
            stemPlayer.AddIntroSource(n, introSource);
            AudioSource loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.clip = loopClips[i];
            stemPlayer.AddLoopSource(n, loopSource);
        }

        // profiles here are hardcoded for this example
        // don't change instrumentNames in the editor
        Dictionary<string, float> profileFull=new Dictionary<string, float>{
            { "Lead1", 1.0f},
            { "Lead2", 1.0f},
            { "Percussion", 1.0f},
            { "Piano", 1.0f},
            { "TremoloStrings", 1.0f},
            { "TubularBells", 1.0f}
        };
        Dictionary<string, float> profileMuted=new Dictionary<string, float>{
            { "Percussion-lowpass", 1.0f},
            { "Piano-lowpass", 1.0f},
            { "TremoloStrings", 1.0f},
            { "TubularBells", 1.0f}
        };

        stemPlayer.AddProfile("full", profileFull);
        stemPlayer.AddProfile("muted", profileMuted);

        // without applying a profile, the audio is muted by default.
        // applying the full profile so that users are not confused
        // (it definitely confused me)
        stemPlayer.ApplyProfile("full");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void useProfileFull(){
        stemPlayer.ApplyProfile("full");
    }

    public void useProfileMuted() {
        stemPlayer.ApplyProfile("muted");
    }

    public void play(){
        stemPlayer.Play();
    }

    public void stop(){ 
        stemPlayer.Stop();
    }
}
