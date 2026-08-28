<div align="center">

# 抽卡链接获取工具

**本地获取链接、同步抽卡记录、备份、导出与分析**

[![GitHub](https://img.shields.io/badge/GitHub-181717?logo=github&logoColor=white)](https://github.com/H0NG1Y/gacha-link-fetcher)
[![Stars](https://img.shields.io/github/stars/H0NG1Y/gacha-link-fetcher?color=yellow&label=stars&logo=github)](https://github.com/H0NG1Y/gacha-link-fetcher/stargazers)
[![Downloads](https://img.shields.io/github/downloads/H0NG1Y/gacha-link-fetcher/total?color=orange&label=downloads)](https://github.com/H0NG1Y/gacha-link-fetcher/releases/latest)
[![Windows Download](https://img.shields.io/github/v/release/H0NG1Y/gacha-link-fetcher?color=brightgreen&label=Windows%20Download&logo=windows&logoColor=white)](https://github.com/H0NG1Y/gacha-link-fetcher/releases/latest)
[![C#](https://img.shields.io/badge/C%23-.NET-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Supported Games](https://img.shields.io/badge/Supported%20games-4-5C6BC0)](https://github.com/H0NG1Y/gacha-link-fetcher#支持的游戏)

</div>

## 项目介绍

一个仅在本机运行的 Windows 工具：先从游戏日志或缓存获取记录链接，再由你主动确认后向对应游戏的官方记录接口请求数据。同步后的记录保存在本机，可备份、恢复、导出和分析。

| 游戏 | 记录页面 |
| --- | --- |
| 鸣潮 | 唤取记录 |
| 原神 | 祈愿记录 |
| 崩坏：星穹铁道 | 跃迁记录 |
| 绝区零 | 调频记录 |

## 下载

请从 [GitHub Releases](https://github.com/H0NG1Y/gacha-link-fetcher/releases/latest) 下载最新版本。

## 使用方法

1. 在目标游戏中进入任意卡池，并打开一次对应的记录页面。
2. 运行 EXE，在下拉框中固定选择对应游戏。
3. 点击“自动获取链接”；未找到时可手动选择实际游戏目录。
4. 如需保存和分析，点击“同步记录”，阅读提示并确认后才会请求官方接口。
5. 在“卡池”下拉框选择“全部卡池”或指定卡池，再查看、分析或导出当前筛选结果。

## 功能

- 自动检查常见安装目录；鸣潮覆盖官方启动器、WeGame、Steam 与 Epic 常见目录
- 从本地日志或 `webCaches` 缓存读取最新记录链接
- 用户确认后遍历所有已知卡池和全部可用分页记录，并按游戏、账号、共享进度卡池和记录 ID 自动去重合并
- 使用正确的中文卡池名称显示记录；共享保底进度的卡池会自动合并，可在“全部卡池”和指定卡池之间切换
- 自动备份本地数据（保留最近 20 份），可手动备份与恢复
- 导出 CSV、Excel 可打开的 SpreadsheetML XML、通用 JSON
- 为原神、崩坏：星穹铁道和绝区零导出 UIGF v4
- 按共享进度卡池统计总抽数、稀有度数量、当前垫数与最高稀有度平均间隔
- 可复制不含链接、账号或文件路径的诊断信息，便于反馈问题

## 数据与隐私

- 获取链接时只读取本地日志或缓存；链接仅保留在程序内存和剪贴板，不会写入本地数据文件。
- 只有点击“同步记录”并在确认窗口中选择“是”后，程序才会使用该链接中的临时凭证请求对应游戏的官方记录接口。
- 不会上传本地日志、缓存或备份；同步到的抽卡记录仅保存于 `%LocalAppData%\GachaLinkFetcher\records.json`。
- 自动备份位于 `%LocalAppData%\GachaLinkFetcher\backups`，可由用户自行删除。
- 工具不会修改游戏文件、注册表或系统权限。

> 抽卡记录链接含有临时查询凭证。不要公开发布、发送给陌生人或提交到 Issue；只将它用于你信任的工具。

## 常见问题

**提示未找到链接？**

确认已在游戏内打开记录页并等待加载完成；再点击“自动获取链接”。仍无结果时，请用“手动选择…”指定实际游戏目录。

**同步失败或提示链接过期？**

游戏记录凭证会过期。重新进入游戏内的记录页，重新获取链接后再同步。游戏接口变更时也可能需要等待工具更新。

**Excel 导出为什么是 XML？**

这是 Excel 原生支持的 SpreadsheetML 工作簿，可直接用 Excel 打开；这样无需内置额外依赖。

## 源码结构

- `Models/`：游戏定义、抽卡记录与本地数据模型
- `Services/`：链接发现、官方记录请求、导出与分析逻辑
- `Storage/`：本地 JSON 数据库与备份
- `UI/`：WinForms 主界面

## 免责声明

这是一个非官方玩家工具，与库洛游戏、米哈游及 HoYoverse 均无隶属关系。游戏更新后如日志格式、目录或记录接口发生变化，工具可能需要更新。

[English README](README.en.md)
