# DesktopTranslation - 桌面翻譯工具設計規格

## 概述

一個 Windows 桌面翻譯工具，透過雙擊 Ctrl+C 觸發，自動將剪貼簿中的文字進行翻譯並顯示在浮動視窗中。取代 DeepL 的付費功能，使用免費的 Google Translate 為預設引擎，並支援 LLM（Claude/OpenAI）作為可切換的進階翻譯引擎。

## 核心需求

| 項目 | 內容 |
|------|------|
| 觸發方式 | 雙擊 Ctrl+C（400ms 內連按兩次） |
| 翻譯方向 | 自動偵測語言 → 繁體中文；中文 → 英文 |
| 翻譯引擎 | Google Translate（預設）+ LLM 可切換 |
| UI | 浮動視窗，左欄可編輯輸入、右欄翻譯結果 |
| 視窗行為 | 可拖曳、記住位置、預設置頂 |
| 常駐方式 | 系統匣常駐 + 可選開機自啟動 |
| 額外功能 | TTS 朗讀（原文 + 翻譯）、翻譯歷史（最近 50 筆） |
| 技術棧 | C# + WPF (.NET 8+) |

## 觸發與翻譯流程

```
使用者按 Ctrl+C（第一次）
  → 系統正常複製文字到剪貼簿
  → App 用低階鍵盤 Hook (WH_KEYBOARD_LL) 記錄時間戳

使用者按 Ctrl+C（第二次，400ms 內）
  → 偵測為「雙擊」
  → 讀取剪貼簿內容
  → 本地 Unicode 判斷：CJK 字元 > 30% → 目標英文；否則 → 目標繁體中文
  → 呼叫當前翻譯引擎 → 顯示浮動視窗
```

### 為什麼用低階 Hook

`RegisterHotKey` 會吃掉按鍵，導致第一次 Ctrl+C 無法正常複製。低階 Hook (`WH_KEYBOARD_LL`) 只監聽不攔截，兩次複製都能正常執行。Hook 回呼必須在 300ms 內返回，所以翻譯 API 呼叫必須 Post 到背景執行緒。

### 語言偵測

遍歷輸入文字，統計 CJK 統一表意文字區段（`\u4E00-\u9FFF`）的字元比例。超過 30% 判定為中文，翻譯目標設為英文；否則翻譯目標設為繁體中文。零延遲、零網路依賴。

## 翻譯引擎設計

```csharp
public interface ITranslationEngine
{
    string Name { get; }
    Task<TranslationResult> TranslateAsync(
        string text,
        string targetLanguage,
        CancellationToken ct = default);
}

public record TranslationResult(
    string TranslatedText,
    string DetectedSourceLanguage,
    bool IsSuccess,
    string? ErrorMessage = null);
```

### Google Translate

- 使用 `GTranslate` NuGet 套件（免費、無需 API Key）
- 風險：Google 可能改動端點或加入反爬機制
- 緩解：翻譯引擎可替換，失效時切到 LLM

### LLM 翻譯

- Claude：使用 `Claudia` NuGet
- OpenAI：使用 `OpenAI` 官方 .NET SDK
- 支援 streaming 逐字輸出
- 需要使用者提供 API Key

### 重試策略

使用 `Polly` NuGet，指數退避，最多重試 2 次，超時 10 秒。

## 浮動視窗設計

### 佈局

```
┌─────────────────────────────────────────────────────┐
│ ≡  拖曳區域     [ Google | LLM ]     📌  —  ✕      │  ← 自訂標題列 32px
├────────────────────────┬────────────────────────────┤
│ 英文 - 已偵測    🔊  ✕ │ 繁體中文          🔊  📋  │  ← 語言標籤列
│                        │                            │
│  This is an example    │  這是一個範例文字          │
│  text that was         │  它被複製後自動翻譯        │
│  copied and auto-      │                            │
│  translated.           │                            │
│                        │                            │
│  [可編輯]              │  [唯讀，可選取]            │
├────────────────────────┴────────────────────────────┤
│  ▲ 歷史紀錄 (最近 50 筆)                            │  ← 可展開
└─────────────────────────────────────────────────────┘
```

### 視窗規格

| 項目 | 數值 |
|------|------|
| 預設尺寸 | 720 x 400 px |
| 最小尺寸 | 520 x 280 px |
| 圓角 | 12px |
| 陰影 | DropShadow Blur 20px, Opacity 0.25, Y-offset 4px |
| 左右比例 | 50:50，分隔線 1px 不可拖曳 |
| 字體 | Segoe UI 14px，行高 1.6 |
| 語言標籤 | 12px #888888 |
| 預設置頂 | 是（圖釘可切換） |
| 無邊框 | 是，自訂標題列 |

### 標題列元素（左到右）

- 拖曳區域
- Segmented Control：`[ Google | LLM ]` 快速切換引擎
- 圖釘：置頂切換
- 最小化：縮到系統匣
- 關閉：隱藏到系統匣

### 左欄（輸入）

- 語言標籤（自動偵測結果）
- TTS 朗讀原文按鈕
- 清除內容按鈕
- TextBox 可編輯，修改後自動重新翻譯（debounce）

### 右欄（翻譯）

- 目標語言標籤
- TTS 朗讀翻譯按鈕
- 一鍵複製翻譯結果按鈕
- 唯讀但可選取文字
- Loading 時顯示 Shimmer 骨架屏（三行灰色脈動條）
- 錯誤時內嵌紅色文字 + 重試連結，不彈窗

## 視覺風格

### 深淺色主題（跟隨 Windows 系統）

| 元素 | 淺色模式 | 深色模式 |
|------|----------|----------|
| 背景 | #FFFFFF | #2D2D2D |
| 文字 | #1A1A1A | #E5E5E5 |
| 分隔線 | #E5E5E5 | #404040 |
| 強調色 | #0078D4 (Windows 藍) | #60CDFF |
| 語言標籤 | #888888 | #888888 |

### 動畫

| 場景 | 效果 | 時長 | Easing |
|------|------|------|--------|
| 視窗出現 | Fade In + Scale 0.95→1.0 | 150ms | CubicOut |
| 視窗隱藏 | Fade Out | 100ms | - |
| 翻譯結果出現 | Fade In | 200ms | - |
| LLM streaming | 逐字自然顯示 | 即時 | - |
| 骨架屏脈動 | Shimmer 左→右 | 1.5s 循環 | Linear |

## 系統匣

- 左鍵單擊：顯示/隱藏主視窗（toggle）
- 右鍵選單：
  - 顯示主視窗
  - 翻譯引擎（子選單：Google / LLM）
  - 開機自啟動（打勾項）
  - 設定
  - 關於
  - 結束

## 設定視窗

獨立視窗，從系統匣右鍵或主視窗齒輪圖示開啟。單頁佈局。

設定項目：
- 翻譯引擎選擇
- LLM Provider（Claude / OpenAI）
- API Key 輸入
- 開機自啟動開關
- 雙擊間隔閾值（預設 400ms）
- TTS 語速調整
- 主題（跟隨系統 / 強制淺色 / 強制深色）

### 設定持久化

路徑：`%AppData%/DesktopTranslation/settings.json`

```json
{
  "windowX": 100,
  "windowY": 200,
  "windowWidth": 720,
  "windowHeight": 400,
  "alwaysOnTop": true,
  "engine": "google",
  "llmProvider": "claude",
  "apiKey": "",
  "autoStart": false,
  "doubleTapInterval": 400,
  "ttsSpeed": 1.0,
  "theme": "system"
}
```

## 技術架構

### 專案結構

```
DesktopTranslation/
├── App.xaml / App.xaml.cs          — 應用程式入口、系統匣初始化
├── Services/
│   ├── HotkeyService.cs           — WH_KEYBOARD_LL 全局鍵盤 Hook
│   ├── ClipboardService.cs        — 剪貼簿讀取
│   ├── TranslationService.cs      — 引擎調度（選擇 Google/LLM）
│   ├── GoogleTranslateEngine.cs   — ITranslationEngine 實作
│   ├── LlmTranslateEngine.cs      — ITranslationEngine 實作
│   ├── LanguageDetector.cs        — Unicode CJK 比例判斷
│   ├── TtsService.cs              — System.Speech.Synthesis 封裝
│   ├── SettingsService.cs         — JSON 設定讀寫
│   └── AutoStartService.cs        — Registry Run Key 開機自啟
├── Views/
│   ├── TranslationWindow.xaml      — 主浮動視窗
│   ├── SettingsWindow.xaml         — 設定視窗
│   └── TrayIconManager.cs         — 系統匣管理
├── Models/
│   ├── TranslationResult.cs       — 翻譯結果 record
│   └── AppSettings.cs             — 設定模型
├── Helpers/
│   ├── Win32Interop.cs            — P/Invoke 宣告
│   └── ThemeHelper.cs             — 深淺色主題偵測
└── Assets/
    └── tray-icon.ico              — 系統匣圖示
```

### NuGet 套件

| 用途 | 套件 |
|------|------|
| Google 翻譯 | GTranslate |
| 重試策略 | Polly |
| Claude API | Claudia |
| OpenAI API | OpenAI |
| 系統匣 | H.NotifyIcon.Wpf |
| JSON 設定 | 內建 System.Text.Json |
| TTS | 內建 System.Speech |
| 鍵盤 Hook | 直接 P/Invoke |
| 自動更新（可選） | Velopack |

### TTS

使用 `System.Speech.Synthesis`（Windows 內建 SAPI）。Windows 10/11 內建 Microsoft David/Zira（英文）和 Microsoft Hanhan（繁體中文），不需額外安裝語音包。日後可升級為 Azure Cognitive Services Speech SDK。

### 開機自啟

Registry Run Key：`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`，不需管理員權限。

## 打包與解除安裝

### 打包

```bash
dotnet publish -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

搭配 Inno Setup 製作安裝精靈。

### 解除安裝

Inno Setup 自動生成 `unins000.exe`，清理項目：

| 項目 | 方式 |
|------|------|
| 程式檔案 | Inno Setup 自動移除安裝目錄 |
| 桌面捷徑 | Inno Setup 自動移除 |
| 開始選單項目 | Inno Setup 自動移除 |
| 開機自啟 Registry Key | 安裝腳本加入解除時清理 HKCU Run |
| 設定檔 %AppData%/DesktopTranslation/ | 解除安裝時詢問使用者是否保留 |
| 控制台「新增或移除程式」 | Inno Setup 自動註冊 |

使用者解除安裝方式：
1. Windows 設定 → 應用程式 → DesktopTranslation → 解除安裝
2. 開始選單 → DesktopTranslation → Uninstall
3. 安裝目錄 → 執行 unins000.exe

## 與 DeepL 的差異化

1. **LLM streaming 逐字輸出** — DeepL 要等全部翻完，LLM 可以即時串流
2. **零操作翻譯** — 雙擊 Ctrl+C 直接填入並翻譯，不需手動貼上
3. **本地歷史紀錄** — DeepL 免費版不保留歷史，本工具保存最近 50 筆
4. **免費** — 核心功能完全免費，LLM 引擎僅需使用者自備 API Key
