# 构建

## 环境

- Unity 2022.3.62f3c1。
- Windows 构建：Windows Build Support，StandaloneWindows64 / Mono。
- Android 构建：Android Build Support，使用 Unity 随附 SDK、NDK、OpenJDK，IL2CPP，ARM64 + ARMv7，OpenGLES3。
- Android 最低 API 23，目标 API 34。这里提供的是原型 APK，不是满足未来商店上架要求的配置。

当前资源已经配置完成。打开 `Assets/GameMain/Scenes/Start.unity` 即可运行，不需要重建 UI 或重新生成音乐。

## 编辑器菜单

- `YYOU / Build Windows x64`：输出 `Builds/Windows/YYOU.exe`。
- `YYOU / Build Android APK`：输出 `Builds/YYOU-0.3.0-Android.apk`。
- `YYOU / Validate charts`：验证 65 张示例谱面的理想输入、音符边界、连打隔离和曲包唯一性。
- `YYOU / Refresh demo catalog`：从 `Tools/demo-catalog.json` 更新示例选曲、四个曲包与公开构建配置。该动作会覆盖对应配置，修改曲库前请保留自己的版本。

发布构建使用 `Start.unity` 单场景，其余页面通过预制体创建。安装包身份为 `io.github.qtgb.yyou`；应用名 `YYOU Open Source`，与内部演示包分别保存状态。

## 命令行示例（PowerShell）

将路径换成你的 Unity 安装位置和本地仓库位置。每次只对同一个工程运行一个 Unity 进程。

```powershell
$unity = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3c1\Editor\Unity.exe'
$project = 'D:\Projects\YYOU'
$output = 'D:\YYOU-builds'
& $unity -batchmode -quit -projectPath $project -executeMethod PublicReleaseBuild.Windows -yyouOutput $output -logFile "$output\windows.log"
& $unity -batchmode -quit -projectPath $project -buildTarget Android -executeMethod PublicReleaseBuild.Android -yyouOutput $output -logFile "$output\android.log"
```

实际运行自动检查（不要加 `-quit`；检查会自行退出）：

```powershell
& $unity -batchmode -projectPath $project -executeMethod PublicReleaseValidation.Run -yyouOutput "$output\validation" -logFile "$output\validation.log"
```

此检查会实际播放完整示例曲，自动输入、暂停、恢复、重开、查看结算，并输出三个比例和三种布局的截图。验证用独立的成绩/获取/音量键空间。截图会瞬间增加负载，因此不是性能基准测试。

## 分发

Windows 压缩包必须包含 `YYOU.exe`、`YYOU_Data`、UnityPlayer、Mono 运行库及随包许可文件。不要只发布单个 EXE。

Android 构建当前使用本机测试签名。自己的正式发行应设置自己的签名，保管在仓库之外；本仓库不含任何签名私钥或密码。更换签名不能直接覆盖安装另一份同包名但不同签名的 APK。

发布源码无需 `Library`、`Temp`、`Logs`、`UserSettings` 或打包产物；这些目录已忽略。字体保留完整原文件与 OFL 声明。
