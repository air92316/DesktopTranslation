<div align="center">

# DesktopTranslation

**雙擊 Ctrl+C，即時翻譯** — 一款靈感來自 DeepL 的 Windows 桌面翻譯工具

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6?logo=windows)](https://github.com/air92316/DesktopTranslation)
[![.NET](https://img.shields.io/badge/.NET%208-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Website](https://img.shields.io/badge/Website-desktop--translation.pages.dev-4A6FA5)](https://desktop-translation.pages.dev)

</div>

---

## 📸 介面預覽

```
┌─────────────────────────────────────────────────────────┐
│  📌  DesktopTranslation    [ Google | LLM ]      — □ ✕ │
├─────────────────────────────────────────────────────────┤
│  English          ⇄          繁體中文                   │
├────────────────────────┬────────────────────────────────┤
│                        │                                │
│  Hello, world!         │  你好，世界！                   │
│                        │                                │
│  🔊                    │  🔊  📋                        │
├────────────────────────┴────────────────────────────────┤
│  ▾ 翻譯歷史 (3)                                         │
│  Hello → 你好  │  ありがとう → 謝謝  │  Bonjour → 你好  │
└─────────────────────────────────────────────────────────┘
```

> 日式極簡 UI（Japanese Minimal Aesthetic）— 支援深色 / 淺色主題自動切換

---

## ✨ 功能特色

| | 功能 | 說明 |
|---|---|---|
| ⌨️ | **雙擊 Ctrl+C 即時翻譯** | 400ms 內連按兩次 Ctrl+C，翻譯結果立即浮現 |
| 🌐 | **多翻譯引擎** | Google Translate（免費）+ Claude + OpenAI |
| 🎨 | **日式極簡 UI** | 藍染色 Ai-iro 主題，深色 / 淺色自動切換 |
| 🔄 | **語言互換** | ⇄ 一鍵將翻譯結果反向翻譯 |
| 📋 | **翻譯歷史** | 保存最近 50 筆翻譯，隨時回顧 |
| 🔊 | **TTS 朗讀** | 原文、譯文皆可語音朗讀 |
| 🔒 | **安全加密** | DPAPI 加密存儲 API Key，絕不明文保存 |
| 📐 | **4K DPI 縮放** | 完整支援高解析度螢幕與系統縮放 |
| 🧠 | **自動語言偵測** | Unicode 字元範圍分析，智慧判斷來源語言 |
| 📌 | **視窗置頂** | 圖釘按鈕控制常駐最上層 |

---

## 🚀 快速開始

### 1. 下載

前往 [Releases](https://github.com/air92316/DesktopTranslation/releases) 下載最新安裝程式。

### 2. 安裝

執行安裝程式，按照精靈指示完成安裝（自動建立桌面捷徑與開始選單）。

### 3. 使用

在任何應用程式中選取文字，**快速按兩下 Ctrl+C**，翻譯視窗即刻彈出！

---

## 💻 系統需求

| 項目 | 需求 |
|---|---|
| 作業系統 | Windows 10 / 11 (x64) |
| 執行環境 | [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| 硬碟空間 | < 50 MB |
| 網路 | 需要網路連線（翻譯 API） |

---

## 📖 使用方式

| 操作 | 說明 |
|---|---|
| `Ctrl+C` `Ctrl+C` | 在 400ms 內連按兩次，觸發翻譯 |
| 編輯左欄 | 修改原文後 500ms 自動重新翻譯 |
| ⇄ 按鈕 | 語言互換，將譯文移至輸入區反向翻譯 |
| 📌 按鈕 | 切換視窗置頂 |
| 🔊 按鈕 | 朗讀原文或譯文 |
| 📋 按鈕 | 一鍵複製翻譯結果 |
| 系統匣左鍵 | 顯示 / 隱藏翻譯視窗 |
| 系統匣右鍵 | 開啟選單（設定、引擎切換、關於等） |

### 翻譯方向

- **非中文** → 自動翻譯為 **繁體中文**
- **中文** → 自動翻譯為 **English**
- 也可透過下拉選單手動指定語言

---

## ⚙️ 設定說明

| 設定項目 | 說明 | 預設值 |
|---|---|---|
| 翻譯引擎 | Google Translate / LLM | Google |
| LLM Provider | Claude / OpenAI | — |
| API Key | LLM 翻譯所需的 API Key（DPAPI 加密存儲） | — |
| 雙擊間隔 | 觸發翻譯的 Ctrl+C 間隔閾值 | 400ms |
| TTS 語速 | 文字轉語音的朗讀速度 | 預設 |
| 主題 | 跟隨系統 / 淺色 / 深色 | 跟隨系統 |
| 開機自啟動 | 開機時自動啟動 | 關閉 |

---

## 🌍 支援語言

<table>
<tr>
<td>🇹🇼 繁體中文</td>
<td>🇨🇳 簡體中文</td>
<td>🇺🇸 English</td>
<td>🇯🇵 日本語</td>
<td>🇰🇷 한국어</td>
</tr>
<tr>
<td>🇫🇷 Français</td>
<td>🇩🇪 Deutsch</td>
<td>🇪🇸 Español</td>
<td>🇧🇷 Português</td>
<td>🇷🇺 Русский</td>
</tr>
<tr>
<td>🇹🇭 ไทย</td>
<td>🇻🇳 Tiếng Việt</td>
<td>🇸🇦 العربية</td>
<td></td>
<td></td>
</tr>
</table>

---

## 🏗️ 技術架構

```
┌──────────────────────────────────────────────────┐
│                    WPF UI Layer                   │
│          (TranslationWindow / Settings)           │
├──────────────────────────────────────────────────┤
│                  Service Layer                    │
│  ┌──────────┐ ┌───────────┐ ┌──────────────────┐ │
│  │ Hotkey   │ │ Clipboard │ │ Translation      │ │
│  │ Service  │ │ Service   │ │ Service + Polly  │ │
│  └──────────┘ └───────────┘ └──────────────────┘ │
│  ┌──────────┐ ┌───────────┐ ┌──────────────────┐ │
│  │ Language │ │ TTS       │ │ History          │ │
│  │ Detector │ │ Service   │ │ Service          │ │
│  └──────────┘ └───────────┘ └──────────────────┘ │
├──────────────────────────────────────────────────┤
│               Translation Engines                 │
│     ┌─────────────┐  ┌────────┐  ┌────────┐     │
│     │   Google     │  │ Claude │  │ OpenAI │     │
│     │  Translate   │  │  API   │  │  API   │     │
│     └─────────────┘  └────────┘  └────────┘     │
├──────────────────────────────────────────────────┤
│              Platform Layer (Win32)               │
│   Keyboard Hook │ DPAPI │ DPI Aware │ Registry   │
└──────────────────────────────────────────────────┘
```

### 技術棧

| 類別 | 技術 |
|---|---|
| 語言 | C# / .NET 8 |
| UI 框架 | WPF (Windows Presentation Foundation) |
| 翻譯 — 免費 | [GTranslate](https://www.nuget.org/packages/GTranslate) |
| 翻譯 — LLM | [Claudia](https://www.nuget.org/packages/Claudia) / [OpenAI](https://www.nuget.org/packages/OpenAI) |
| 重試策略 | [Polly](https://www.nuget.org/packages/Polly)（指數退避，2 次重試） |
| TTS | System.Speech |
| 安全 | DPAPI (System.Security.Cryptography.ProtectedData) |
| 打包 | Inno Setup |

---

## 🔨 從原始碼編譯

### 前置需求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11 x64

### 建置步驟

```bash
# 1. Clone 專案
git clone https://github.com/air92316/DesktopTranslation.git
cd DesktopTranslation

# 2. 還原套件 & 編譯
dotnet build src/DesktopTranslation -c Release

# 3. 執行測試
dotnet test tests/DesktopTranslation.Tests

# 4. 執行程式
dotnet run --project src/DesktopTranslation
```

### 發佈獨立執行檔

```bash
dotnet publish src/DesktopTranslation -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/
```

---

## 📂 專案結構

```
DesktopTranslation/
├── src/DesktopTranslation/
│   ├── Models/           # 資料模型（Settings, Result, History）
│   ├── Services/         # 核心服務（翻譯、熱鍵、TTS、歷史等）
│   ├── Views/            # WPF 視窗與系統匣
│   ├── Helpers/          # Win32 互操作、主題偵測、加密工具
│   ├── Themes/           # 日式淺色 / 深色主題 XAML
│   └── Assets/           # 圖示資源
├── tests/                # 126 項單元測試 & 整合測試
├── installer/            # Inno Setup 安裝腳本
├── docs/                 # 設計規格文件
└── website/              # 官方網站
```

---

## 🤝 貢獻指南

歡迎任何形式的貢獻！

1. **Fork** 這個專案
2. 建立功能分支 (`git checkout -b feature/amazing-feature`)
3. **Commit** 你的變更 (`git commit -m 'feat: add amazing feature'`)
4. **Push** 到分支 (`git push origin feature/amazing-feature`)
5. 建立 **Pull Request**

### Commit 訊息規範

使用 [Conventional Commits](https://www.conventionalcommits.org/) 格式：

```
feat: 新功能
fix: 修復 Bug
docs: 文件更新
refactor: 重構
test: 測試相關
chore: 雜項維護
```

---

## 📄 授權

本專案採用 [MIT License](LICENSE) 授權。

Copyright (c) 2026 **Ramen Cat Studio**. All rights reserved.

---

<div align="center">

Made with ❤️ by [Ramen Cat Studio](https://github.com/air92316)

</div>
