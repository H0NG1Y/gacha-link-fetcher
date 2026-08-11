<div align="center">

# 抽卡链接获取工具

**从本地日志与缓存中获取多款游戏的抽卡记录链接**

[![GitHub](https://img.shields.io/badge/GitHub-181717?logo=github&logoColor=white)](https://github.com/lllusorysky/gacha-link-fetcher)
[![Downloads](https://img.shields.io/github/downloads/lllusorysky/gacha-link-fetcher/total?color=orange&label=downloads)](https://github.com/lllusorysky/gacha-link-fetcher/releases/latest)
[![Windows Download](https://img.shields.io/github/v/release/lllusorysky/gacha-link-fetcher?color=brightgreen&label=Windows%20Download&logo=windows&logoColor=white)](https://github.com/lllusorysky/gacha-link-fetcher/releases/latest)
[![C#](https://img.shields.io/badge/C%23-.NET-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Supported Games](https://img.shields.io/badge/Supported%20games-4-5C6BC0)](https://github.com/lllusorysky/gacha-link-fetcher#%E6%94%AF%E6%8C%81%E7%9A%84%E6%B8%B8%E6%88%8F)

</div>

## 项目介绍

这是一个仅在本机运行的 Windows 小工具，用于读取游戏已经生成的抽卡记录链接，便于导入你信任的统计工具。

支持的游戏：

| 游戏 | 记录页面 |
| --- | --- |
| 鸣潮 | 唤取记录 |
| 原神 | 祈愿记录 |
| 崩坏：星穹铁道 | 跃迁记录 |
| 绝区零 | 调频记录 |

> 工具不会联网请求游戏接口，不会上传账号资料，也不会修改游戏文件、注册表或系统权限。

## 下载

请从 [GitHub Releases](https://github.com/lllusorysky/gacha-link-fetcher/releases/latest) 下载最新版本。

## 使用方法

1. 在目标游戏中进入任意卡池，并打开一次对应的记录页面。
2. 关闭游戏页面前等待记录页加载完成。
3. 运行 EXE，在下拉框中**固定选择对应游戏**。
4. 点击“自动获取链接”，成功后点击“复制链接”。
5. 将链接粘贴到你信任的抽卡统计工具。

如果没有自动找到游戏，请点击“手动选择…”，选择实际游戏目录（含游戏 EXE 或 `*_Data` 文件夹）后重试。

## 功能

- 自动检查常见安装目录
- 从本地日志或 `webCaches` 缓存中读取最新记录链接
- 支持手动选择游戏目录
- 一键复制链接到剪贴板
- 可复制不含链接、账号或文件路径的诊断信息，便于反馈问题
- 支持鸣潮官方启动器、WeGame、Steam 与 Epic 常见目录
- 支持 miHoYo Launcher、HoYoPlay 等米哈游常见目录

## 安全提示

抽卡记录链接含有临时查询凭证。请不要公开发布或发送给陌生人；只将它粘贴到你信任的统计服务。

## 常见问题

**提示未找到链接？**
先确认已在游戏内打开记录页并等待加载完成；随后再次点击“自动获取链接”。仍无结果时，请使用“手动选择…”指定实际游戏目录。

**工具会读取或上传账号数据吗？**
不会。工具只读取本地日志或缓存中的记录链接，不会联网、上传资料或修改游戏文件。

## 文件说明

- `GachaLinkFetcher.cs`：C# 源码
- `GachaLinkFetcher.ico`：应用图标
- `README.en.md`：英文说明

## 免责声明

这是一个非官方玩家工具，与库洛游戏、米哈游及 HoYoverse 均无隶属关系。游戏更新后如日志格式或目录发生变化，工具可能需要更新。

[English README](README.en.md)

## Stars 历史图

<a href="https://www.star-history.com/?repos=lllusorysky%2Fgacha-link-fetcher&type=timeline&logscale=&legend=top-left">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=lllusorysky/gacha-link-fetcher&type=timeline&theme=dark&logscale&legend=top-left&sealed_token=ovs6cUVyTRm-QRXSjaIL2S8-mwfcnwhWSKNBiF14ZXlvMFyHX1YBnTz7jI5lwy9vC6rkbLIyDj1vt9sEfe-mINEdwmdx7kfwauSX8KNkiq-dZFDiNXfhxK4g1IGILNRbHz2JjqqBy6vcM7GcCL8NblfBmggY9KZ9ytF65ajlxUS9bD4BLx8Rr3Dkj9mn" />
    <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=lllusorysky/gacha-link-fetcher&type=timeline&logscale&legend=top-left&sealed_token=ovs6cUVyTRm-QRXSjaIL2S8-mwfcnwhWSKNBiF14ZXlvMFyHX1YBnTz7jI5lwy9vC6rkbLIyDj1vt9sEfe-mINEdwmdx7kfwauSX8KNkiq-dZFDiNXfhxK4g1IGILNRbHz2JjqqBy6vcM7GcCL8NblfBmggY9KZ9ytF65ajlxUS9bD4BLx8Rr3Dkj9mn" />
    <img alt="Star History Chart" src="https://api.star-history.com/chart?repos=lllusorysky/gacha-link-fetcher&type=timeline&logscale&legend=top-left&sealed_token=ovs6cUVyTRm-QRXSjaIL2S8-mwfcnwhWSKNBiF14ZXlvMFyHX1YBnTz7jI5lwy9vC6rkbLIyDj1vt9sEfe-mINEdwmdx7kfwauSX8KNkiq-dZFDiNXfhxK4g1IGILNRbHz2JjqqBy6vcM7GcCL8NblfBmggY9KZ9ytF65ajlxUS9bD4BLx8Rr3Dkj9mn" />
  </picture>
</a>
