using System.Collections.Generic;
using UnityEngine;


namespace User670.StemPlayer{
    public class StemPlayer: MonoBehaviour {
        public Dictionary<string, AudioSource> introSources = new Dictionary<string, AudioSource>();
        public Dictionary<string, AudioSource> loopSources = new Dictionary<string, AudioSource>();
        /// <summary>
        /// Active profile is the profile currently applied to the AudioSources.
        /// It may differ from any other profiles during a fade in/out.
        /// </summary>
        public Dictionary<string, float> activeProfile = new Dictionary<string, float>();
        /// <summary>
        /// Target profile is the target of the fade in/out.
        /// </summary>
        public Dictionary<string, float> targetProfile = new Dictionary<string, float>();
        /// <summary>
        /// Previous profile is the profile before a fade in/out started.
        /// </summary>
        public Dictionary<string, float> previousProfile = new Dictionary<string, float>();
        public Dictionary<string, Dictionary<string, float>> profiles = new Dictionary<string, Dictionary<string, float>>();

        /// <summary>
        /// global volume is intended to be set to whatever the game's volume setting is.
        /// </summary>
        public float globalVolume = 1.0f;
        /// <summary>
        /// volume is intended to be set per-StemPlayer (i.e. per-song).
        /// </summary>
        public float volume = 1.0f;

        /// <summary>
        /// this many seconds before intro ends, the loop will be scheduled.
        /// If set to 0 or negative, then scheduling will happen as soon as the intro starts playing.
        /// </summary>
        public float transitionLookAhead = 1.0f;

        double loopStartDspTime;

        /// <summary>
        /// Whether to call LoadAudioData() on audio clips when added to this class.
        /// If this is not needed, this should be set to False before adding AudioSources.
        /// This effectively does nothing if the audio clips are configured with
        /// `preloadAudioData` - true.
        /// </summary>
        public bool preloadAudioClips = true;


        enum State {
            stopped,
            loopNotScheduled,
            loopScheduled
        }

        State state = State.stopped;

        // Start is called before the first frame update
        void Start() {

        }

        // Update is called once per frame
        void Update() {
            // schedule loop, if time is close
            if (state != State.loopNotScheduled) {
                // no need to schedule
                return;
            }
            if (AudioSettings.dspTime > loopStartDspTime - transitionLookAhead){
                foreach (var loopSource in loopSources.Values) {
                    loopSource.PlayScheduled(loopStartDspTime);
                }
                state = State.loopScheduled;
            }
        }

        /// <summary>
        /// Add or replace a intro AudioSource by instrument name.
        /// </summary>
        /// <remarks>
        /// A new key will be added if it doesn't exist; otherwise the new AudioSource will replace the old one.
        /// The AudioSource's `loop` property will then be set to false.
        /// </remarks>
        /// <param name="instrument">Name of the instrument.</param>
        /// <param name="source">The new AudioSource.</param>
        public void AddIntroSource(string instrument, AudioSource source) {
            if (introSources.ContainsKey(instrument)) {
                introSources[instrument] = source;
            } else {
                introSources.Add(instrument, source);
            }
            introSources[instrument].loop = false;
            if(preloadAudioClips){
                introSources[instrument].clip.LoadAudioData();
            }
        }

        /// <summary>
        /// Add or replace a loop AudioSource by instrument name.
        /// </summary>
        /// <remarks>
        /// A new key will be added if it doesn't exist; otherwise the new AudioSource will replace the old one.
        /// The AudioSource's `loop` property will then be set to true.
        /// </remarks>
        /// <param name="instrument">Name of the instrument.</param>
        /// <param name="source">The new AudioSource.</param>
        public void AddLoopSource(string instrument, AudioSource source) {
            if (loopSources.ContainsKey(instrument)) {
                loopSources[instrument] = source;
            } else {
                loopSources.Add(instrument, source);
            }
            loopSources[instrument].loop = true;
            if (preloadAudioClips) {
                loopSources[instrument].clip.LoadAudioData();
            }
        }

        /// <summary>
        /// Removes the intro source for the specified instrument.
        /// </summary>
        /// <param name="instrument">The instrument to remove the intro source for.</param>
        public void RemoveIntroSource(string instrument) {
            introSources.Remove(instrument);
        }

        /// <summary>
        /// Removes the loop source for the specified instrument.
        /// </summary>
        /// <param name="instrument">The instrument to remove the loop source for.</param>
        public void RemoveLoopSource(string instrument) {
            loopSources.Remove(instrument);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="profile"></param>
        public void AddProfile(string name, Dictionary<string, float> profile) {
            if (profiles.ContainsKey(name)) {
                profiles[name] = profile;
            } else {
                profiles.Add(name, profile);
            }
        }

        /// <summary>
        /// Remove a profile by name
        /// </summary>
        /// <param name="name">The name of the profile to remove</param>
        public void RemoveProfile(string name) {
            profiles.Remove(name);
        }

        /// <summary>
        /// Apply the specified profile by name.
        /// </summary>
        /// <param name="name">The name of the profile to apply.</param>
        public void ApplyProfile(string name){ 
            // Apply the profile by setting the activeProfile to the specified profile and update the volume.
            activeProfile = profiles[name];
            UpdateVolume();
        }

        /// <summary>
        /// Plays the audio.
        /// </summary>
        public void Play() {
            UpdateVolume(); // Update the volume

            // if there are no intro
            if(introSources.Count == 0) {
                // directly play loop
                foreach(var loopSource in loopSources.Values) {
                    loopSource.Play();
                }
                state = State.loopScheduled;
                return;
            }

            // if there are intro
            bool flagHasSetLoopStartDspTime = false;
            foreach(var introSource in introSources.Values) {
                introSource.Play();
                if(flagHasSetLoopStartDspTime == false) {
                    loopStartDspTime = AudioSettings.dspTime + GetDoubleClipLength(introSource);
                    flagHasSetLoopStartDspTime = true;
                }
            }
            if(transitionLookAhead <= 0) {
                // schedule loop immediately
                foreach(var loopSource in loopSources.Values) {
                    loopSource.PlayScheduled(loopStartDspTime);
                }
                state = State.loopScheduled;
            } else {
                state = State.loopNotScheduled;
            }
        }

        /// <summary>
        /// Stops all audio sources.
        /// </summary>
        public void Stop(){ 
            foreach(var introSource in introSources.Values){
                introSource.Stop();
            }
            foreach(var loopSource in loopSources.Values){
                loopSource.Stop();
            }
            state = State.stopped;
        }

        /// <summary>
        /// check whether all intro audio are of the same length and sample rate.
        /// </summary>
        /// <returns>true if same, false if different.</returns>
        public bool ValidateIntroClipLength() {
            int length=-1;
            int sr=-1; //sample rate
            foreach (AudioSource source in introSources.Values) {
                if (length<0) {
                    length = source.clip.samples;
                    sr = source.clip.frequency;
                } else {
                    if (source.clip.samples != length || source.clip.frequency != sr) {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// check whether all intro audio are of the same length and sample rate.
        /// </summary>
        /// <returns>true if same, false if different.</returns>
        public bool ValidateLoopClipLength() {
            int length=-1;
            int sr=-1; //sample rate
            foreach (AudioSource source in loopSources.Values) {
                if (length < 0) {
                    length = source.clip.samples;
                    sr = source.clip.frequency;
                } else {
                    if (source.clip.samples != length || source.clip.frequency != sr) {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Set the global volume and update volume of all AudioSources.
        /// </summary>
        /// <remark>
        /// You may set the gloablVolume property directly, but the effects will not be applied to the
        /// AudioSources until `UpdateVolume()` is called.
        /// </remark>
        /// <param name="vol">value to set to.</param>
        public void SetGlobalVolume(float vol){
            globalVolume = vol;
            UpdateVolume();
        }

        /// <summary>
        /// Set the volume and update volume of all AudioSources.
        /// </summary>
        /// <remark>
        /// You may set the volume property directly, but the effects will not be applied to the
        /// AudioSources until `UpdateVolume()` is called.
        /// </remark>
        /// <param name="vol">value to set to.</param>
        public void SetVolume(float vol){
            volume = vol;
            UpdateVolume();
        }

        public void UpdateVolume(){ 
            foreach(var pair in introSources){ 
                if(activeProfile.ContainsKey(pair.Key)){ 
                    pair.Value.volume = globalVolume * volume * activeProfile[pair.Key];
                }else{
                    pair.Value.volume = 0f;
                }
            }
            foreach (var pair in loopSources) {
                if (activeProfile.ContainsKey(pair.Key)) {
                    pair.Value.volume = globalVolume * volume * activeProfile[pair.Key];
                } else {
                    pair.Value.volume = 0f;
                }
            }
        }

        /// <summary>
        /// Obtain the length of the clip in seconds, using the clip's sample rate and length in samples.
        /// This returns a `double` instead of a `float`, which helps with sample-accurate stitching of audio.
        /// </summary>
        /// <param name="clip">The audio clip for which to obtain the length</param>
        /// <returns>The length of the audio clip in seconds as a double</returns>
        static double GetDoubleClipLength(AudioClip clip) {
            return (double)clip.samples / clip.frequency;
        }

        /// <summary>
        /// Obtain the length of the clip in seconds, using the clip's sample rate and length in samples.
        /// This returns a `double` instead of a `float`, which helps with sample-accurate stitching of audio.
        /// </summary>
        /// <param name="source">The audio source for which to obtain the length</param>
        /// <returns>The length of the audio clip in seconds as a double</returns>
        static double GetDoubleClipLength(AudioSource source) {
            return GetDoubleClipLength(source.clip);
        }
    }
}
