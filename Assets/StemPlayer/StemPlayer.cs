using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


namespace User670.StemPlayer{
    public class StemPlayer: MonoBehaviour {
        const string versionInfo="Unity Stem Player by User670";
        public Dictionary<string, AudioSource> introSources = new();
        public Dictionary<string, AudioSource> loopSources = new();
        /// <summary>
        /// Active profile is the profile currently applied to the AudioSources.
        /// It may differ from any other profiles during a fade in/out.
        /// </summary>
        public Dictionary<string, float> activeProfile = new();
        /// <summary>
        /// Target profile is the target of the fade in/out.
        /// </summary>
        public Dictionary<string, float> targetProfile = new();
        /// <summary>
        /// Previous profile is the profile before a fade in/out started.
        /// </summary>
        public Dictionary<string, float> previousProfile = new();
        public Dictionary<string, Dictionary<string, float>> profiles = new();

        /// <summary>
        /// global volume is intended to be set to whatever the game's volume setting is.
        /// </summary>
        public float globalVolume = 1.0f;
        /// <summary>
        /// volume is intended to be set per-StemPlayer (i.e. per-song).
        /// </summary>
        public float volume = 1.0f;
        /// <summary>
        /// This volume is used during fade in and fade out (so that profiles don't need to be changed).
        /// </summary>
        float fadeInOutVolume = 1.0f;

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
        /// `preloadAudioData` = true.
        /// </summary>
        public bool preloadAudioClips = true;
        /// <summary>
        /// Before playing the audio, wait for all audio to be loaded.
        /// Can cause a few frames of delay from calling Play() to the audio actually starts.
        /// If not wait for the audio, there could be click sounds at the transition between intro and loop,
        /// or potentially desync between instruments.
        /// </summary>
        public bool waitForAudioToLoad = true;

        /// <summary>
        /// This value will be used if a duration is not passed to fading-related coroutines.
        /// </summary>
        public float defaultFadeVolumeTime = 1.0f;
        public Coroutine fadeProfileCoroutine;
        public Coroutine fadeVolumeCoroutine;
        public Coroutine waitAudioToLoadCoroutine;


        enum State {
            stopped,
            waiting, // Play() called, but waiting for audio to load
            loopNotScheduled, // Intro playing, loop not scheduled
            loopScheduled, // Loop scheduled or is already playing
            stopping // During the fade out of a fade-out-and-stop
        }

        State state = State.stopped;

        #region Unity callbacks
        // Start is called before the first frame update
        void Start() {
            Debug.Log(versionInfo);
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
        #endregion

        #region AudioSource and profile management
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
        /// Add or update a profile by name.
        /// </summary>
        /// <param name="name">Name of the profile</param>
        /// <param name="profile">The dictionary containing the profile</param>
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
        #endregion

        #region Functions that set properties
        

        /// <summary>
        /// Set the global volume and update volume of all AudioSources.
        /// </summary>
        /// <remark>
        /// You may set the gloablVolume property directly, but the effects will not be applied to the
        /// AudioSources until `UpdateVolume()` is called.
        /// </remark>
        /// <param name="vol">value to set to.</param>
        public void SetGlobalVolume(float vol) {
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
        public void SetVolume(float vol) {
            volume = vol;
            UpdateVolume();
        }

        /// <summary>
        /// Update volume of audio sources to `activeProfile`. Affected by `globalVolume` and `volume`.
        /// </summary>
        public void UpdateVolume() {
            foreach (var pair in introSources) {
                if (activeProfile.ContainsKey(pair.Key)) {
                    pair.Value.volume = globalVolume * volume * fadeInOutVolume * activeProfile[pair.Key];
                } else {
                    pair.Value.volume = 0f;
                }
            }
            foreach (var pair in loopSources) {
                if (activeProfile.ContainsKey(pair.Key)) {
                    pair.Value.volume = globalVolume * volume * fadeInOutVolume * activeProfile[pair.Key];
                } else {
                    pair.Value.volume = 0f;
                }
            }
        }

        #endregion

        #region Main actions
        /// <summary>
        /// Apply the specified profile by name.
        /// </summary>
        /// <param name="name">The name of the profile to apply.</param>
        public void ApplyProfile(string name) {
            // Apply the profile by setting the activeProfile to the specified profile and update the volume.
            activeProfile = profiles[name];
            UpdateVolume();
        }

        /// <summary>
        /// Start a crossfade to a profile.
        /// </summary>
        /// <param name="name">Name of the profile to cross fade to</param>
        /// <param name="duration">Duration of the cross fade</param>
        public void StartFadeToProfile(string name, float duration) {
            if (ProfilesEquivalent(targetProfile, profiles[name])) {
                // Target profile is already the profile being requested, don't repeat fade
                return;
            }
            previousProfile = activeProfile;
            targetProfile = profiles[name];
            StartFadeProfileCoroutine(DoFadeProfile(duration));
        }

        /// <summary>
        /// Start a crossfade to a profile.
        /// Uses default fade time.
        /// </summary>
        /// <param name="name">Name of the profile to fade to</param>
        public void StartFadeToProfile(string name) {
            StartFadeToProfile(name, defaultFadeVolumeTime);
        }

        /// <summary>
        /// Plays the audio.
        /// </summary>
        public void Play() {
            if (waitForAudioToLoad) {
                state = State.waiting;
                waitAudioToLoadCoroutine = StartCoroutine(DoWaitAndStartAudio());
            } else {
                StartAudio();
            }
        }

        /// <summary>
        /// Sets the volume to 0, starts playing the audio, and start a fade in.
        /// </summary>
        /// <param name="duration">Duration of the fade in</param>
        public void PlayAndFadeIn(float duration){
            if(state==State.stopped){
                fadeInOutVolume = 0;
                UpdateVolume();
            }
            if (waitForAudioToLoad) {
                state = State.waiting;
                waitAudioToLoadCoroutine = StartCoroutine(DoWaitAndFadeIn(duration));
            } else {
                StartAudio();
                StartFadeVolumeCoroutine(DoFadeIn(duration));
            }
        }

        /// <summary>
        /// Sets the volume to 0, starts playing the audio, and start a fade in.
        /// </summary>
        public void PlayAndFadeIn(){ 
            PlayAndFadeIn(defaultFadeVolumeTime);
        }


        /// <summary>
        /// Stops all audio sources.
        /// </summary>
        public void Stop() {
            foreach (var introSource in introSources.Values) {
                introSource.Stop();
            }
            foreach (var loopSource in loopSources.Values) {
                loopSource.Stop();
            }
            state = State.stopped;
        }

        /// <summary>
        /// Start a fade out. After the fade out completes, stop the audio.
        /// </summary>
        /// <param name="duration">Duration of the fade out</param>
        public void FadeOutAndStop(float duration){
            StartFadeVolumeCoroutine(DoFadeOutAndStop(duration));
        }

        /// <summary>
        /// Start a fade out. After the fade out completes, stop the audio.
        /// </summary>
        public void FadeOutAndStop() {
            FadeOutAndStop(defaultFadeVolumeTime);
        }
        #endregion

        #region Secondary actions
        
        /// <summary>
        /// Start playing the audio.
        /// This is mainly used by other methods.
        /// </summary>
        public void StartAudio() {
            UpdateVolume();

            // if there are no intro
            if (introSources.Count == 0) {
                // directly play loop
                foreach (var loopSource in loopSources.Values) {
                    loopSource.Play();
                }
                state = State.loopScheduled;
                return;
            }

            // if there are intro
            bool flagHasSetLoopStartDspTime = false;
            foreach (var introSource in introSources.Values) {
                introSource.Play();
                if (flagHasSetLoopStartDspTime == false) {
                    loopStartDspTime = AudioSettings.dspTime + GetDoubleClipLength(introSource);
                    flagHasSetLoopStartDspTime = true;
                }
            }
            if (transitionLookAhead <= 0) {
                // schedule loop immediately
                foreach (var loopSource in loopSources.Values) {
                    loopSource.PlayScheduled(loopStartDspTime);
                }
                state = State.loopScheduled;
            } else {
                state = State.loopNotScheduled;
            }
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// COROUTINE. Waits for all audio to be loaded before calling `StartAudio()`.
        /// This CAN get stuck if an audio failed to load entirely.
        /// </summary>
        /// <returns></returns>
        public IEnumerator DoWaitAndStartAudio() {
            while (AllAudioLoaded() == false) {
                yield return null;
            }
            StartAudio();
        }

        /// <summary>
        /// COROUTINE. Waits for all audio to be loaded, calls `StartAudio()`, and starts a fade in.
        /// </summary>
        /// <param name="duration">duration of the fade in</param>
        /// <returns></returns>
        public IEnumerator DoWaitAndFadeIn(float duration){
            while (AllAudioLoaded() == false) {
                yield return null;
            }
            StartAudio();
            StartFadeVolumeCoroutine(DoFadeIn(duration));
        }

        /// <summary>
        /// COROUTINE. Waits for all audio to be loaded, calls `StartAudio()`, and starts a fade in.
        /// Uses default fade time.
        /// </summary>
        /// <returns></returns>
        public IEnumerator DoWaitAndFadeIn(){
            return DoWaitAndFadeIn(defaultFadeVolumeTime);
        }

        /// <summary>
        /// COROUTINE. Fades volume from `previousProfile` to `targetProfile` across `duration` seconds.
        /// Before calling this coroutine, you need to set `previousProfile` and `targetProfile` beforehand,
        /// and do not modify them while the coroutine is active.
        /// </summary>
        /// <param name="duration">How long the fade should be, in seconds.</param>
        /// <returns></returns>
        public IEnumerator DoFadeProfile(float duration) {
            float timeElapsed=0;
            float timerProgress;
            while (true) {
                timerProgress = timeElapsed / duration;
                if (timerProgress > 1) {
                    timerProgress = 1;
                }
                SetFadeVolumeProfile(timerProgress);
                UpdateVolume();
                timeElapsed += Time.deltaTime;
                if (timerProgress == 1) {
                    yield break;
                }
                yield return null;
            }
        }

        /// <summary>
        /// COROUTINE. Fades volume from `previousProfile` to `targetProfile` across `defaultFadeVolumeTime` seconds.
        /// Before calling this coroutine, you need to set `previousProfile` and `targetProfile` beforehand,
        /// and do not modify them while the coroutine is active.
        /// </summary>
        /// <returns></returns>
        public IEnumerator DoFadeProfile() {
            return DoFadeProfile(defaultFadeVolumeTime);
        }

        /// <summary>
        /// COROUTINE. Fades in over some period of time.
        /// </summary>
        /// <param name="duration">Duration of the fade in</param>
        /// <returns></returns>
        public IEnumerator DoFadeIn(float duration) {
            while(fadeInOutVolume < 1) {
                fadeInOutVolume += Time.deltaTime / duration;
                if(fadeInOutVolume > 1) {
                    fadeInOutVolume = 1;
                }
                UpdateVolume();
                yield return null;
            }
            yield break;
        }

        /// <summary>
        /// COROUTINE. Fades in over some period of time.
        /// Uses default fade time.
        /// </summary>
        /// <returns></returns>
        public IEnumerator DoFadeIn(){ 
            return DoFadeIn(defaultFadeVolumeTime);
        }

        /// <summary>
        /// COROUTINE. Fades out over some period of time.
        /// </summary>
        /// <param name="duration">Duration of the fade out.</param>
        /// <returns></returns>
        public IEnumerator DoFadeOut(float duration) {
            while (fadeInOutVolume > 0) {
                fadeInOutVolume -= Time.deltaTime / duration;
                if (fadeInOutVolume < 0) {
                    fadeInOutVolume = 0;
                }
                UpdateVolume();
                yield return null;
            }
            yield break;
        }
        
        /// <summary>
        /// COROUTINE. Fades out over some period of time.
        /// Uses default fade time.
        /// </summary>
        /// <returns></returns>
        public IEnumerator DoFadeOut() {
            return DoFadeOut(defaultFadeVolumeTime);
        }

        // Does not call `DoFadeOut()` because nested coroutine makes things harder.
        // Yes this DOES violate DRY.
        /// <summary>
        /// COROUTINE. Fades out over some period of time, and then stops the audio.
        /// </summary>
        /// <param name="duration">Duration of the fade out.</param>
        /// <returns></returns>
        public IEnumerator DoFadeOutAndStop(float duration) {
            while (fadeInOutVolume > 0) {
                fadeInOutVolume -= Time.deltaTime / duration;
                if (fadeInOutVolume < 0) {
                    fadeInOutVolume = 0;
                }
                UpdateVolume();
                yield return null;
            }
            Stop();
            yield break;
        }

        /// <summary>
        /// COROUTINE. Fades out over some period of time, and then stops the audio.
        /// Uses default fade time.
        /// </summary>
        /// <returns></returns>
        public IEnumerator DoFadeOutAndStop(){ 
            return DoFadeOutAndStop(defaultFadeVolumeTime);
        }

        #endregion

        #region Coroutine management
        /// <summary>
        /// Stops an existing fade profile coroutine (if any), and starts a new one.
        /// </summary>
        /// <param name="coroutine">The new coroutine IEnumerator to start.</param>
        public void StartFadeProfileCoroutine(IEnumerator coroutine) {
            try {
                StopCoroutine(fadeProfileCoroutine);
            }catch (NullReferenceException) {
                // do nothing
                // catches only NullReferenceException. Let the rest of the errors throw normally.
            }
            fadeProfileCoroutine = StartCoroutine(coroutine);
        }

        /// <summary>
        /// Stops an existing fade volume coroutine (if any), and starts a new one.
        /// </summary>
        /// <param name="coroutine">The new coroutine IEnumerator to start.</param>
        public void StartFadeVolumeCoroutine(IEnumerator coroutine) {
            try {
                StopCoroutine(fadeVolumeCoroutine);
            } catch (NullReferenceException) {
                // do nothing
                // catches only NullReferenceException. Let the rest of the errors throw normally.
            }
            fadeVolumeCoroutine = StartCoroutine(coroutine);
        }
        #endregion

        #region Helper functions
        /// <summary>
        /// Lerps `previousProfile` and `targetProfile` and stores the result in `activeProfile`.
        /// </summary>
        /// <param name="progress">Percentage progress of the fade, between 0 and 1.</param>
        public void SetFadeVolumeProfile(float progress) {
            var keys = previousProfile.Keys.Union(targetProfile.Keys);
            activeProfile = new Dictionary<string, float>();
            foreach (var key in keys) {
                float a = previousProfile.TryGetValue(key, out float value) ? value : 0f;
                float b = targetProfile.TryGetValue(key, out value) ? value : 0f;
                activeProfile[key] = Mathf.Lerp(a, b, progress);
            }
        }

        /// <summary>
        /// Check whether all audio sources are loaded.
        /// </summary>
        /// <returns>True if all audio sources are loaded, false if any one isn't.</returns>
        public bool AllAudioLoaded() {
            foreach (var introSource in introSources.Values) {
                if (introSource.clip.loadState != AudioDataLoadState.Loaded) {
                    return false;
                }
            }
            foreach (var loopSource in loopSources.Values) {
                if (loopSource.clip.loadState != AudioDataLoadState.Loaded) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// check whether all intro audio are of the same length and sample rate.
        /// </summary>
        /// <returns>true if same, false if different.</returns>
        public bool ValidateIntroClipLength() {
            int length=-1;
            int sr=-1; //sample rate
            foreach (AudioSource source in introSources.Values) {
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

        /// <summary>
        /// Determine whether two profiles are equivalent - that is, all the volume values are the same.
        /// A missing key is treated the same as a value of zero, so {bass: 0} and {} are equivalent.
        /// </summary>
        /// <param name="profileA"></param>
        /// <param name="profileB"></param>
        /// <returns></returns>
        static bool ProfilesEquivalent(Dictionary<string, float> profileA, Dictionary<string, float> profileB) {
            var keys = profileA.Keys.Union(profileB.Keys);
            foreach (var key in keys) {
                float a = profileA.TryGetValue(key, out float value) ? value : 0f;
                float b = profileB.TryGetValue(key, out value) ? value : 0f;
                if (a != b) {
                    return false;
                }
            }
            return true;
        }
        #endregion


    }
}
