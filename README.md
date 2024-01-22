# Stem Player

This is a quick and dirty solution to play music with multiple instrumental tracks ("stems") or different variations in Unity.

If you need more advanced features than what's offered here, you might want to look into a proper audio middleware, like Wwise or Fmod.

## Currently implemented features

- Play, and seamlessly transition, between multiple variations of a piece of music, or adjust the volume of different instruments to create a different feeling.
- - Fading in/out or crossfading is not implemented yet, but it's on the bucket list.
- Have an intro section seamlessly transition into a loop section.

## Known limitations

- Like mentioned above, fading in/out or crossfading is currently not implemented. Audio can only abruptly start, stop, or cut to another variation.
- Audio has to be preloaded into memory for timing to be accurate. The StemPlayer class attempts to address this by calling `loadAudioData` on all added audio clips by default.
- Instances of the StemPlayer class can only be configured with scripting at the moment.

## Usage

This project is made in Unity 2022.3.4f1.

Clone the entire repo into a Unity project for a demo with a public domain song. You may inspect `Assets/Scripts/FurisInfiniteController.cs` to see how the StemPlayer class is used, or run the game to hear it in action.

To use the class, simply take `Assets/StemPlayer/StemPlayer.cs` and add it to your project. While there are currently no web documentation, you may read the docstring to see learn more about each method.

## Legal stuff?

I'm not sure how I would license this project. Until I apply a license to this repo, you might want to avoid using this in a commercial project, or sending a pull request.

Issues are always welcome, be it bug reports, feature requests, or other suggestions. I especially appreciate suggestions on what license to use.

Pull requests, however, are not welcome until a license is chosen.

I'll be on a trip soon after this commit, so I might not be able to respond to your issues very quickly. Sorry if that bothers you!

The song used in the demo is [Fun is Infinite at AGM](https://opengameart.org/content/fun-is-infinite-at-agm) by northivanastan. It's been released to the public domain by the author. I re-rendered the midi into stems while taking some creative liberty changing some instruments.