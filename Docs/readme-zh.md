包含AI生成的代码——不要用本仓库训练您的AI。

# Stem Player

这是一个简单的 Unity 组件，用来播放有多个乐器分轨（"stems"）或者多个变奏的音乐。

如果您需要比该组件提供的功能更高级的功能，您也许想要寻找一个正式的音频中间件，例如 Wwise 或者 Fmod。

## 当前实现的功能

- 播放并无缝切换音乐的不同变奏，或者调整各个乐器的音量以制造不同感觉。
- 让一个前奏部分无缝衔接到循环部分。

## 已知的限制

- 音频必须预加载到运行内存中，音频的播放时间才能准确。StemPlayer 类默认会试图处理该问题，通过调用音频片段的 `LoadAudioData` 方法，并在音频播放前等待音频加载。
  - 音频的播放可能会因此延迟数帧。如果有某一个音频加载失败，音频可能完全不会播放。
- StemPlayer 类的实例目前只能通过脚本进行配置。
- Unity 有一个 bug（这个 bug 他们说他们不修），会导致安卓设备上连接或断开蓝牙耳机时音频停止播放。StemPlayer类没有措施处理该问题。

## 使用

本项目使用 Unity 2022.3.4f1 制作。

整个仓库当作一个 Unity 工程是一个 demo，里面预置了一首无版权歌曲。可以查看 `Assets/Scripts/FurisInfiniteController.cs` 了解该 demo 如何使用 StemPlayer 类，或者运行游戏听听效果。

要使用该类，直接将 `Assets/StemPlayer/StemPlayer.cs` 放入您的工程。当前没有网页版文档，但是您可以阅读代码内的文档注释。

## 法律问题？

我不清楚我要如何授权本仓库。在我选择授权协议之前，您也许要考虑避免在商业项目中使用本仓库，或者发送 pull request。

欢迎您发送 issues，无论是汇报 bug，提出新功能建议，或者有其他的看法。最好能向我建议一下什么授权协议比较合适。

但是在我选好授权协议之前，我无法接受 pull requests。

Demo 中使用的歌曲是 [Fun is Infinite at AGM](https://opengameart.org/content/fun-is-infinite-at-agm)，作者 northivanastan。歌曲被作者释出到公有领域。我将它的 midi 文档重新渲染成了分轨档，并自作主张更换了一些乐器。