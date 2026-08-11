# 抽卡链接获取工具

一个 Windows 本地小工具，用于从《鸣潮》《原神》《崩坏：星穹铁道》《绝区零》的本地日志或缓存中获取抽卡记录链接，方便导入你信任的统计工具。

## 下载

请从 [GitHub Releases](https://github.com/lllusorysky/wuthering-waves-convene-link-fetcher/releases/latest) 下载最新版本。

## 功能

- 支持《鸣潮》《原神》《崩坏：星穹铁道》《绝区零》
- 可自动识别全部已安装游戏，或手动指定目标游戏
- 自动检查常见安装目录；《鸣潮》保留官方启动器、WeGame、Steam 与 Epic 扫描
- 从《鸣潮》日志或米哈游游戏的 `webCaches` 缓存读取最新记录链接
- 一键复制链接到剪贴板
- 不联网、不上传账号资料、不修改游戏文件、注册表或文件权限

## 使用方法

1. 在目标游戏中进入任意卡池，并打开一次记录页面：
   - 《鸣潮》：唤取记录
   - 《原神》：祈愿记录
   - 《崩坏：星穹铁道》：跃迁记录
   - 《绝区零》：调频记录
2. 等待页面完全加载，运行从 Releases 下载的 EXE。
3. 选择游戏（或保留“自动识别”），点击“自动获取链接”。
4. 成功后点击“复制链接”，再粘贴到你信任的统计工具。

《鸣潮》会自动扫描各磁盘下 WeGame 的 `WeGameApps\\rail_apps` 与 `WeGameApps\\apps`。

如果程序未自动找到游戏，请点击“手动选择…”，选择实际游戏目录（含游戏 EXE 或 `*_Data` 文件夹）后重试；不要只选择启动器目录。

## 安全提示

抽卡记录链接带有临时查询凭证。请不要将它发送给陌生人或公开发布；仅将其粘贴到你信任的统计服务中。

## 文件说明

- [Releases](https://github.com/lllusorysky/wuthering-waves-convene-link-fetcher/releases)：编译好的 Windows EXE
- `GachaLinkFetcher.cs`：C# 源码
- `图标.ico`：应用图标（来自《鸣潮》官网公开站点图标）
- `README.en.md`：英文版说明

## 说明

这是一个非官方的玩家工具，与库洛游戏、米哈游及 HoYoverse 均无隶属关系。游戏更新后若日志格式或目录发生变化，可能需要更新工具。

[English README](README.en.md)

[![GitHub Stars](https://img.shields.io/github/stars/lllusorysky/wuthering-waves-convene-link-fetcher?style=social)](https://github.com/lllusorysky/wuthering-waves-convene-link-fetcher/stargazers)
