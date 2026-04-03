# DesktopTranslation — 桌面即時翻譯工具

> **開發者：Ramen Cat Studio**
> **版本：v1.0.0**
> **授權：MIT License**

## 產品概述

DesktopTranslation 是一款 Windows 桌面翻譯工具，靈感來自 DeepL 的快速翻譯功能。使用者只需雙擊 Ctrl+C，即可將選取的文字即時翻譯並顯示在浮動視窗中。核心使用免費的 Google Translate，同時支援 LLM（Claude / OpenAI）作為進階翻譯引擎。

## 核心功能

### 翻譯觸發
- **雙擊 Ctrl+C**：在 400ms 內連按兩次 Ctrl+C，自動讀取剪貼簿內容並翻譯
- 第一次 Ctrl+C 正常複製文字，第二次觸發翻譯（不影響原有複製行為）
- 使用 Win32 低階鍵盤 Hook（WH_KEYBOARD_LL），支援 VK_LCONTROL / VK_RCONTROL

### 語言偵測與翻譯
- **自動偵測語言**：基於 Unicode 字元範圍分析（CJK、假名、韓字、西里爾、阿拉伯等）
- **翻譯方向**：非中文 → 繁體中文；中文 → 英文
- **手動覆寫**：左右語言下拉選單可手動選擇來源/目標語言
- **語言互換**：⇄ 按鈕一鍵將翻譯結果移到輸入區並反向翻譯
- **支援語言**：繁體中文、簡體中文、English、日本語、한국어、Français、Deutsch、Español、Português、Русский、ไทย、Tiếng Việt、العربية

### 翻譯引擎
- **Google Translate**（預設）：使用 GTranslate 套件，免費、無需 API Key
- **LLM**（可選）：支援 Claude（Anthropic）和 OpenAI，需自備 API Key
- 引擎切換：主視窗標題列的 Segmented Control [ Google | LLM ]
- LLM 未設定 API Key 時按鈕灰色禁用，點擊顯示設定提示

### 浮動視窗
- **左右分欄**：左邊輸入（可編輯）、右邊翻譯結果（唯讀可選取）
- **語言選擇列**：上方統一的語言 ComboBox + ⇄ 互換按鈕
- **自動重新翻譯**：編輯左邊文字後 500ms debounce 自動重新翻譯
- **置頂切換**：圖釘按鈕控制是否常駐最上層
- **位置記憶**：記住視窗位置和大小，下次開啟時恢復
- **可拖曳**：自訂標題列支援滑鼠拖曳

### 系統匣
- **常駐圖示**：藍色「T」圖示（Ai-iro #4A6FA5）
- **左鍵點擊**：顯示/隱藏翻譯視窗
- **右鍵選單**：顯示主視窗、翻譯引擎切換、開機自啟動、設定、關於、結束

### 設定
- 翻譯引擎選擇（Google / LLM）
- LLM Provider（Claude / OpenAI）+ API Key
- 開機自啟動（Registry Run Key）
- 雙擊間隔閾值（預設 400ms）
- TTS 語速調整
- 主題（跟隨系統 / 淺色 / 深色）

### 額外功能
- **TTS 朗讀**：左右兩邊各有喇叭按鈕，朗讀原文或翻譯結果
- **翻譯歷史**：底部可展開面板，保存最近 50 筆翻譯
- **一鍵複製**：右欄複製按鈕將翻譯結果存入剪貼簿

## 技術架構

### 技術棧
- **語言**：C# / .NET 8
- **UI 框架**：WPF (Windows Presentation Foundation)
- **系統匣**：Windows Forms NotifyIcon（跨 WPF/WinForms 互操作）
- **打包**：Inno Setup

### NuGet 套件

| 套件 | 用途 |
|------|------|
| GTranslate | Google Translate（免費，無需 API Key） |
| Polly | 翻譯 API 重試策略（指數退避，2 次重試，10s 超時） |
| Claudia | Claude API SDK |
| OpenAI | OpenAI 官方 .NET SDK |
| H.NotifyIcon.Wpf | WPF 系統匣圖示（備用） |
| System.Speech | TTS 文字轉語音 |
| System.Security.Cryptography.ProtectedData | DPAPI 加密 API Key |

### 專案結構

```
DesktopTranslation/
├── src/DesktopTranslation/
│   ├── App.xaml / App.xaml.cs              — 應用程式入口，服務初始化
│   ├── app.manifest                        — DPI 感知設定
│   ├── Models/
│   │   ├── AppSettings.cs                  — 設定模型（record, immutable）
│   │   ├── TranslationResult.cs            — 翻譯結果模型
│   │   └── TranslationHistory.cs           — 歷史紀錄模型
│   ├── Services/
│   │   ├── ITranslationEngine.cs           — 翻譯引擎介面
│   │   ├── GoogleTranslateEngine.cs        — Google Translate 實作
│   │   ├── LlmTranslateEngine.cs           — Claude / OpenAI 實作
│   │   ├── TranslationService.cs           — 引擎調度 + Polly 重試
│   │   ├── HotkeyService.cs                — WH_KEYBOARD_LL 全局鍵盤 Hook
│   │   ├── DoubleTapDetector.cs            — 雙擊偵測邏輯（可獨立測試）
│   │   ├── ClipboardService.cs             — 剪貼簿讀取（10,000 字元上限）
│   │   ├── LanguageDetector.cs             — Unicode 字元範圍語言偵測
│   │   ├── TtsService.cs                   — System.Speech TTS 封裝
│   │   ├── SettingsService.cs              — JSON 設定 + DPAPI 加密
│   │   ├── HistoryService.cs               — 翻譯歷史（最近 50 筆）
│   │   └── AutoStartService.cs             — Registry 開機自啟
│   ├── Views/
│   │   ├── TranslationWindow.xaml/.cs      — 主翻譯浮動視窗
│   │   ├── SettingsWindow.xaml/.cs         — 設定視窗
│   │   └── TrayIconManager.cs              — WinForms NotifyIcon 系統匣
│   ├── Helpers/
│   │   ├── Win32Interop.cs                 — P/Invoke 宣告（Hook, DPI）
│   │   ├── ThemeHelper.cs                  — 深淺色主題偵測
│   │   └── DataProtectionHelper.cs         — DPAPI 加密/解密
│   ├── Themes/
│   │   ├── JapaneseLight.xaml              — 日式淺色主題
│   │   ├── JapaneseDark.xaml               — 日式深色主題
│   │   └── GlobalStyles.xaml               — 全局控件樣式
│   └── Assets/
│       └── tray-icon.ico                   — 系統匣圖示
├── tests/DesktopTranslation.Tests/
│   ├── Models/                             — AppSettings 測試
│   ├── Services/                           — 各 Service 單元測試
│   ├── Helpers/                            — ThemeHelper 測試
│   └── Integration/                        — 整合測試（翻譯流程、安全、歷史）
├── installer/
│   └── setup.iss                           — Inno Setup 安裝腳本
└── docs/
    └── superpowers/specs/                  — 設計規格文件
```

## 安全設計

| 項目 | 措施 |
|------|------|
| API Key 存儲 | DPAPI 加密（ProtectedData.Protect, CurrentUser scope） |
| Prompt Injection | 輸入截斷 5000 字 + 防禦性 system prompt + XML 標籤隔離 |
| 剪貼簿安全 | 內容長度限制 10,000 字元 |
| 錯誤訊息 | 不洩漏 API Key、系統路徑等敏感資訊 |
| 設定驗證 | 數值 clamp + 枚舉值白名單驗證 |
| 異常處理 | 所有 catch 加 Debug.WriteLine，不 silent swallow |

## 視覺設計

### 日式清晰時尚風格（Japanese Minimal Aesthetic）
- **設計原則**：間（Ma）留白、簡潔、柔和、精緻、和諧
- **強調色**：Ai-iro 藍 #4A6FA5（淺色）/ #7EB0E0（深色）
- **深色背景**：溫暖漆黑 #1E2128（非純黑）
- **陰影**：15% opacity, 24px blur — 柔和日式美學
- **圓角**：視窗 12px、按鈕 6px、下拉選單 6px
- **字體**：Segoe UI + Yu Gothic UI / Microsoft JhengHei UI
- **動畫**：Fade In 150ms CubicOut、Fade Out 100ms
- **DPI 感知**：自動偵測系統縮放比例，4K 螢幕完整支援
- **WCAG AA**：所有色彩對比度合規

### 主題切換
- 跟隨 Windows 系統深淺色設定
- 可在設定中強制淺色或深色
- 使用 DynamicResource + ResourceDictionary 熱切換

## 測試

- **總測試數**：126
- **單元測試**：Models、Services、Helpers、DoubleTapDetector
- **整合測試**：翻譯流程（9）、DPAPI 加密往返（18）、歷史串接（7）、安全驗證（15）
- **Build**：0 errors, 0 warnings

## 打包與安裝

### 編譯
```bash
dotnet publish src/DesktopTranslation -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish/
```

### 安裝程式
- 使用 Inno Setup（installer/setup.iss）
- 自動建立桌面捷徑和開始選單
- 解除安裝時詢問是否保留設定檔
- 支援從 Windows 設定 → 應用程式 → 解除安裝

## 開發歷程

| 輪次 | 團隊 | 成果 |
|------|------|------|
| 1. 開發 | dev-lead + qa-lead + design-lead | 26 檔案、39 tests |
| 2. QA+安全 | code-reviewer + security-reviewer + qa-agent + optimizer | 7 安全修復、70 tests |
| 3. 自動化測試 | test-engineer + refactorer + optimizer | DoubleTapDetector 重構、126 tests |
| 4. UI 美化 | ui-designer + frontend-dev + review-lead | 日式主題、全面重設計 |
| 5. 功能測試 | 手動 + debug logging | 修復 Hook/Tray/DPI/語言偵測等 runtime 問題 |

## 授權

MIT License

Copyright (c) 2026 Ramen Cat Studio. All rights reserved.
