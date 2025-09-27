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

整个仓库当作一个 Unity 工程是一个 demo，里面预置了一首无版权歌曲。可以查看 `Assets/Scripts/FunisInfiniteController.cs` 了解该 demo 如何使用 StemPlayer 类，或者运行游戏听听效果。

要使用该类，直接将 `Assets/StemPlayer/StemPlayer.cs` 放入您的工程。当前没有网页版文档，但是您可以阅读代码内的文档注释。


## 法律问题
我于2025年9月27日将**本仓库中我拥有著作权的部分**使用 [WTFPL 第三版](https://github.com/sirkris/WTFPL/blob/master/WTFPL.md?plain=1) 授权。

**请注意，这不代表您有权利使用本仓库**：请务必阅读下面的解释。如有需要，请您咨询您的法律团队。

### 我（和AI）编写的代码
受影响的文件：
- `Assets/StemPlayer/StemPlayer.cs`
- `Assets/Scripts/FunIsInfiniteController.cs`

我拥有著作权部分的代码通过 [WTFPL 第三版](https://github.com/sirkris/WTFPL/blob/master/WTFPL.md?plain=1) 授权。

部分的代码是借助生成式人工智能 (generative AI) 编写的。具体来讲，我使用了 [Perplexity AI](https://www.perplexity.ai/) 和 [Windsurf （旧称 Codeium）](https://windsurf.com/) 两个工具。 **我无法确认哪部分的代码是部分或全部由AI编写的。** 授权协议的有效性和生效范围取决于法律如何规定AI生成内容的著作权归属——我不了解相关法律，也没有律师执照，无法提供相关法律的信息。

我强烈建议您在使用我的代码之前咨询律师。

### 示例音乐
受影响的文件：
- `Assets/Sample Song/Fun is Infinite at AGM` 中的 `.wav` 文件

这首用于项目中示例的歌曲是 [Fun is Infinite at AGM](https://opengameart.org/content/fun-is-infinite-at-agm)，作者 northivanastan。这首歌曲被原作者释出到公有领域 (public domain)。

我用一个叫做 [Bandlab](https://www.bandlab.com/) 的网页音乐应用将原始 MIDI 文件重新渲染成音频文件，过程中使用了 Bandlab 提供的虚拟乐器和效果器。除非 Bandlab 对授权有任何限制，这些音频文件也通过 [WTFPL 第三版](https://github.com/sirkris/WTFPL/blob/master/WTFPL.md?plain=1) 授权。

我**无法**在 Bandlab 的使用条款里找到**针对虚拟乐器的授权条款**。如果您希望使用该音乐，您也应该考虑阅读 Bandlab 的法律条款并咨询律师。

### 自动生成的和第三方的模板文件和配置文件
受影响的文件：
- 上述未提到的文件，`README.md` 和 `Docs/readme-zh.md` 除外

因为本仓库是一个完整的 Unity 工程，本仓库自然地包含由 Unity 引擎、Visual Studio 和 Git 生成的文件，也包含一个从 [`github/gitignore` 仓库](https://github.com/github/gitignore/blob/main/Unity.gitignore) 获取的 `.gitignore` 文件。我没有这些文件的版权，您应该按照这些文件来源的授权协议使用这些文件。
