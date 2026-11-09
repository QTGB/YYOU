# 资源与许可范围

## 本项目

除以下第三方组件外，源代码、项目提供的界面与角色资源、原创合成示例音频及配套谱面按根目录 MIT 许可提供。角色图为项目开发时的 AI 辅助生成素材；不声称存在传统委托画师或 Spine 动画成品。

商店里的价格与购买是本地状态演示，没有支付SDK、网络扣费或真实交易。

## 第三方字体

| 字体 | 原始项目 | 许可文件 |
| --- | --- | --- |
| Noto Sans CJK SC Regular / Bold | https://github.com/notofonts/noto-cjk | `Licenses/NotoSansCJK-OFL.txt` |
| Barlow Condensed SemiBold | https://github.com/jpt/barlow | `Licenses/BarlowCondensed-OFL.txt` |

字体保持原名称与原文件，没有子集化或修改轮廓。它们适用 SIL Open Font License 1.1，不改授本项目的 MIT。所需声明也随安装包分发。

## Unity

Unity Editor、Unity Runtime 和 Unity Package Manager 官方模块适用 Unity 提供的许可条款。源码仓库不包含 Unity Editor 或导入缓存；预编译包包含运行所需 Unity Runtime，不因此将其变为 MIT 软件。

## 已排除内容

内部开发曾使用的商业歌曲、专辑封面、外部谱面、旧样段、参考文档、预算资料、缓存、签名私钥与内部构建没有进入此仓库或公开安装包。公开内容不继承这些资源的任何授权。

本次示例音频完全由 `Tools/generate_demo.py` 定义的振荡器与噪声合成，不依赖鼓机网站的实现代码或采样文件。Python/numpy/ffmpeg 是可选生成工具，不随游戏运行包提供。
