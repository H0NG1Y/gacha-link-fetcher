#ifndef MyAppVersion
  #define MyAppVersion "4.0.0"
#endif

#define MyAppName "抽卡链接获取工具"
#define MyAppExeName "GachaLinkFetcher.exe"
#define MyAppPublisher "H0NG1Y"
#define MyAppUrl "https://github.com/H0NG1Y/gacha-link-fetcher"

[Setup]
AppId={{72DFD4B5-CA50-45B4-B326-893E2CD79191}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppUrl}
AppSupportURL={#MyAppUrl}/issues
AppUpdatesURL={#MyAppUrl}/releases/latest
DefaultDirName={autopf}\GachaLinkFetcher
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=GachaLinkFetcher-Setup-v{#MyAppVersion}
SetupIconFile=..\GachaLinkFetcher.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
CloseApplications=yes
RestartApplications=yes
AppMutex=Local\GachaLinkFetcher.SingleInstance
SetupLogging=yes
ChangesAssociations=no

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加快捷方式："; Flags: unchecked

[Files]
Source: "..\build\GachaLinkFetcher.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "运行 {#MyAppName}"; Flags: nowait postinstall skipifsilent runasoriginaluser
