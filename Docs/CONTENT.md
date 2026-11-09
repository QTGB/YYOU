# 歌曲与谱面

## 自带示例

13 段合成演示音乐采用固定音符表和确定性振荡器生成；没有采样第三方录音。`Tools/generate_demo.py` 同时定义乐句与谱面节奏。首曲约 62 秒，其余为约 29–39 秒的短示例，主要方便快速验证选曲、演奏与曲包流程。

五档采用不同节奏编排。入门偏强拍，简单加入反拍和连打，普通加入双手大音符，困难与极限增加细分。这不代表已经通过商业制谱质量验收。

生成资源需要 Python、numpy 和 ffmpeg。它会覆盖本仓库的示例音频、音效、谱面与 `Tools/demo-catalog.json`，不应对已经自行改过的同名资源直接执行。

```bash
python -m pip install numpy
python Tools/generate_demo.py --ffmpeg ffmpeg
```

然后在 Unity 中执行 `YYOU / Refresh demo catalog`。现成的资源已提交，正常打开工程不需要运行此生成器。

## 替换为自己的歌曲

仅导入你有权使用的音频、封面与谱面。商业录音、歌曲作品和第三方制谱不是本仓库授权的一部分。

1. 将音频放入 `Assets/StreamingAssets/`，推荐 OGG；不要直接依赖编辑器外的绝对路径。
2. 在 `UISongSelection.prefab` 的 `SongSelectionView.songs` 中填写歌曲 ID、标题、艺人、时长、试听起点、相对 `audioPath` 与封面。类别位：虚拟歌手=1、流行=2、动画=4、其他=8、中文=16，可以按位组合。
3. 将每个难度的 JSON 放到 `Assets/GameMain/Resources/DrumCharts/<songId>/<difficulty>.json`。难度 0–4 对应入门、简单、普通、困难、极限。`availableDifficulties` 为位掩码，五档全有时为31。
4. 要加入曲包，在 `Resources/SongPacks/YYOUShop.asset` 同步曲目数据和曲包内的歌曲 ID。当前界面使用4个曲包、每包3首的固定排版，改变数量需一起调整 UI。
5. 保留 `Start.unity` 启动方式，运行规则验证和实际听审。删除示例或更改数量后，请相应修改示例专用校验。

现有 `Tools/demo-catalog.json` 是13个示例槽位的便捷配置；不要在手工改过预制体后执行 Refresh 又覆盖自己的改动。

## JSON 结构

```json
{
  "version": 2,
  "revision": 1,
  "difficulty": 2,
  "songId": "my_song",
  "title": "我的歌曲",
  "label": "普通",
  "audioPath": "MyMusic/my_song.ogg",
  "audioDuration": 60.0,
  "travelSeconds": 3,
  "bpm": 120,
  "gridOffset": 0.5,
  "reviewOnly": false,
  "notes": [
    {"kind": 0, "time": 3.0, "duration": 0},
    {"kind": 1, "time": 3.5, "duration": 0},
    {"kind": 2, "time": 4.0, "duration": 0},
    {"kind": 4, "time": 5.0, "duration": 1.0}
  ]
}
```

`kind`：0蓝、1绿、2双手蓝、3双手绿、4蓝色连打。时间从音频起点开始，以秒计；不要自行加3秒倒计时。`audioDuration` 必须与解码后的音频时长一致，程序拒绝超过0.25秒的差异。连打结束之前不要安排其他音符。

PERFECT ±80 ms，GOOD ±180 ms，同色左右双手配对窗口80 ms。分数保存键包含曲目ID、难度和修订号；更改谱面后增加 `revision`。`reviewOnly=true` 的谱面不会写正式成绩。

曲目通过试听加载完成后才能开始；缺失难度不会自动换成练习谱。当前没有玩家端的歌曲文件导入器，替换歌曲是 Unity 工程内的开发操作。
