# DesktopTranslation - Japanese UI Style Guide

## Design Philosophy

This guide follows **Japanese UI design principles** to create a clean, sophisticated, and immersive translation tool experience.

### Core Principles

| Principle | Japanese Term | Application |
|-----------|--------------|-------------|
| Breathing space | **Ma (間)** | Generous padding, unhurried layouts, elements never feel cramped |
| Simplicity | **Kanso (簡素)** | Every element earns its place; no decorative clutter |
| Softness | **Yawaraka (柔らか)** | Muted tones, gentle transitions, no harsh contrasts |
| Precision | **Seimitsu (精密)** | 1px separators, exact alignment, consistent spacing |
| Harmony | **Wa (和)** | Unified color palette, no jarring accent colors |

---

## 1. Color System

### Design Rationale

- **No pure black (#000) or pure white (#FFF)** — always slightly warm/cool-shifted for eye comfort
- **Accent color**: A muted indigo-blue inspired by traditional Japanese **Ai-iro (藍色)** — dignified, calm, universally readable
- **Functional colors**: Desaturated to avoid visual screaming; errors are noticeable but not aggressive

### 1.1 Light Theme

```
BACKGROUND HIERARCHY
--------------------
BgPrimary        #FAFBFC    Main window background — warm off-white
BgSecondary      #F2F3F5    Title bar, history panel — subtle contrast layer
BgTertiary       #EBEDF0    Hover states, input fields — third depth level
BgCard           #FFFFFF    Elevated card surfaces (if needed)

TEXT HIERARCHY
--------------
TextPrimary      #1B1F23    Body text — near-black with warmth
TextSecondary    #586069    Labels, descriptions — readable gray
TextTertiary     #8B949E    Hints, placeholders — subtle
TextInverse      #FAFBFC    Text on accent-colored backgrounds

ACCENT (Ai-iro inspired)
-------------------------
AccentPrimary    #4A6FA5    Primary actions, active segments — muted indigo-blue
AccentHover      #3D5F91    Hover state — slightly deeper
AccentActive     #33507A    Active/pressed state — deepest
AccentSubtle     #EEF2F7    Accent background tint — for subtle highlights

FUNCTIONAL
-----------
Success          #4A9E6D    Muted green — confirmation, success states
Warning          #C49A3A    Warm amber — caution
Error            #C75C5C    Soft red — errors without aggression
ErrorSubtle      #FDF2F2    Error background tint

STRUCTURAL
-----------
Separator        #E1E4E8    Divider lines — barely visible, 1px
SeparatorStrong  #D1D5DA    Stronger dividers when needed
Shadow           #1B1F23    Shadow base color (used at low opacity)
Overlay          #1B1F23    Modal overlay base (used at 40% opacity)

INTERACTIVE
-----------
ButtonHover      #F2F3F5    Icon button hover background
SegmentActive    #4A6FA5    Active engine segment background
SegmentInactive  #EBEDF0    Inactive engine segment background
SegmentActiveText   #FAFBFC  Text on active segment
SegmentInactiveText #586069  Text on inactive segment
ShimmerBase      #EBEDF0    Skeleton loading base
ShimmerHighlight #F7F8FA    Skeleton loading shimmer peak
```

### 1.2 Dark Theme

```
BACKGROUND HIERARCHY
--------------------
BgPrimary        #1E2128    Main background — warm dark gray, NOT pure black
BgSecondary      #262A32    Title bar, history — slightly lighter layer
BgTertiary       #2E333C    Hover states, inputs — third depth
BgCard           #2A2F38    Elevated surfaces

TEXT HIERARCHY
--------------
TextPrimary      #E2E5E9    Body text — soft white, NOT pure white
TextSecondary    #8B949E    Labels — muted
TextTertiary     #6B7380    Hints, placeholders
TextInverse      #1E2128    Text on light/accent backgrounds

ACCENT (Ai-iro, brightened for dark bg)
----------------------------------------
AccentPrimary    #7EB0E0    Primary actions — softened sky blue
AccentHover      #93BFE8    Hover — lighter
AccentActive     #6A9FD4    Active — slightly deeper
AccentSubtle     #282E3A    Accent tint on dark background

FUNCTIONAL
-----------
Success          #6BC08A    Brightened for dark background
Warning          #D9B35A    Warm amber, lightened
Error            #E07070    Soft red, readable on dark
ErrorSubtle      #2E2226    Error background tint

STRUCTURAL
-----------
Separator        #363B44    Subtle divider
SeparatorStrong  #444C56    Stronger divider
Shadow           #000000    Shadow base (used at low opacity)
Overlay          #000000    Modal overlay (used at 50% opacity)

INTERACTIVE
-----------
ButtonHover      #2E333C    Icon button hover
SegmentActive    #7EB0E0    Active segment
SegmentInactive  #2E333C    Inactive segment
SegmentActiveText   #1E2128  Text on active segment
SegmentInactiveText #8B949E  Text on inactive segment
ShimmerBase      #2E333C    Skeleton base
ShimmerHighlight #3A4048    Skeleton shimmer peak
```

---

## 2. Typography System

### Font Stack

```
Primary:    "Segoe UI", "Yu Gothic UI", "Microsoft JhengHei UI", sans-serif
Monospace:  "Cascadia Code", "Consolas", monospace   (for API key fields if needed)
```

### Type Scale

| Level | Size | Weight | Line Height | Usage |
|-------|------|--------|-------------|-------|
| H1 | 18px | SemiBold (600) | 1.4 | Window titles (rarely used) |
| H2 | 15px | SemiBold (600) | 1.4 | Section headers in Settings |
| Body | 14px | Regular (400) | 1.6 | Translation text, primary content |
| Label | 12px | Regular (400) | 1.4 | Language labels, status text |
| Caption | 11px | Regular (400) | 1.4 | Segment control text, timestamps |
| Small | 10px | Regular (400) | 1.3 | History arrow indicators |

### Weight Rules

- **SemiBold (600)**: Section headers only — never Bold (700), too heavy for Japanese aesthetic
- **Regular (400)**: Everything else — body, labels, buttons
- **No Light (300)**: Becomes unreadable at small sizes on standard-DPI screens

### Letter Spacing

- Default for all sizes (0px) — CJK characters have built-in spacing
- Segment controls: +0.3px for uppercase labels if any

---

## 3. Spacing System

### Base Unit: 4px

All spacing values are multiples of 4px. This creates consistent visual rhythm.

```
SPACING SCALE
--------------
xs      4px     Tight gaps (between icon and label)
sm      8px     Small gaps (between related elements)
md      12px    Medium gaps (standard content padding)
lg      16px    Section gaps
xl      20px    Major section separation
2xl     24px    Window-edge padding (Settings window)
3xl     32px    Hero spacing (rarely used)
```

### Padding Rules

| Element | Padding | Notes |
|---------|---------|-------|
| Window content | 12px horizontal, 8px vertical | TranslationWindow inner panels |
| Settings window | 24px all sides | More generous, single-page layout |
| Title bar | 0px top/bottom, 12px left | Height fixed at 36px (was 32px, +4 for breathing room) |
| Icon buttons | 8px all sides | Was 6px, slightly more generous |
| Segment control buttons | 10px horizontal, 3px vertical | Comfortable click target |
| History items | 10px horizontal, 6px vertical | Scannable list density |
| Cards / grouped sections | 16px all sides | Settings sections if grouped |

### Margin Rules

| Context | Value | Notes |
|---------|-------|-------|
| Between label row and content | 6px | Language label to text area |
| Between sections in Settings | 16px | Clear separation without excess |
| Separator to content | 0px | Separator is structural, no extra gap |
| Between form fields | 8px | Settings form elements |

---

## 4. Corner Radius

```
RADIUS SCALE
--------------
None        0px     Separators, full-width elements
Subtle      3px     Segment control pills, inline badges
Small       4px     Icon buttons, small interactive elements
Medium      6px     Input fields, dropdowns, list items
Large       8px     Cards, grouped sections, Settings window
XLarge      12px    Main window border, floating panels
```

### Application

| Element | Radius | Rationale |
|---------|--------|-----------|
| Main window | 12px | Soft, floating feel |
| Title bar top corners | 12px | Matches window |
| History panel bottom corners | 12px | Matches window |
| Segment control container | 6px | Pill-like, approachable |
| Segment control buttons | 4px | Nested inside container |
| Icon buttons (hover bg) | 4px | Subtle rounding |
| Shimmer skeleton bars | 4px | Soft loading state |
| Settings window | 8px | Slightly less than main (standard dialog) |
| Input fields / combo boxes | 6px | Medium, consistent |
| Checkboxes | 3px | Subtle softening |

---

## 5. Shadow System

Shadows should be **barely noticeable** — they create depth without drawing attention.

```
SHADOW DEFINITIONS
-------------------

Window Shadow (Light theme):
  Color:      #1B1F23
  Opacity:    15%       (was 25%, reduced for subtlety)
  Blur:       24px      (was 20px, slightly softer spread)
  Y-Offset:   6px      (was 4px, slightly more lift)
  X-Offset:   0px

Window Shadow (Dark theme):
  Color:      #000000
  Opacity:    30%       (dark needs more shadow to be visible)
  Blur:       24px
  Y-Offset:   6px
  X-Offset:   0px

Hover Shadow (floating buttons, if applicable):
  Color:      #1B1F23 / #000000
  Opacity:    8% / 15%
  Blur:       8px
  Y-Offset:   2px
  X-Offset:   0px

Tooltip / Popup Shadow:
  Color:      #1B1F23 / #000000
  Opacity:    12% / 25%
  Blur:       16px
  Y-Offset:   4px
  X-Offset:   0px
```

### Shadow Rules

- **Never use hard shadows** (blur < 8px)
- **Never use colored shadows** (always neutral gray/black at low opacity)
- **Dark theme shadows are stronger** because surrounding context is darker
- **Inner shadows**: Not used. Japanese aesthetic favors flat surfaces with subtle elevation via background color differences.

---

## 6. Animation System

### Duration Standards

| Category | Duration | Use Case |
|----------|----------|----------|
| Instant | 0ms | Focus ring, cursor changes |
| Fast | 100ms | Button hover background, opacity toggles |
| Normal | 150-200ms | Window appear/disappear, panel transitions |
| Relaxed | 300ms | History panel expand/collapse, section reveals |
| Slow | 500ms | First-run onboarding (if any) |

### Easing Standards

```
Default (most transitions):   CubicEase EaseOut    — fast start, gentle stop
Entrance animations:          CubicEase EaseOut    — elements arrive decisively
Exit animations:              CubicEase EaseIn     — elements leave quietly
Expand/Collapse:              QuadraticEase EaseInOut — smooth both directions
Shimmer loop:                 Linear               — continuous, non-distracting
```

### Animation Application

| Element | Animation | Duration | Easing |
|---------|-----------|----------|--------|
| Window appear | Fade 0->1 + Scale 0.96->1.0 | 150ms | CubicOut |
| Window hide | Fade 1->0 | 100ms | CubicIn |
| Translation result appear | Fade 0->1 | 200ms | CubicOut |
| Shimmer skeleton | Translate loop | 1.5s | Linear |
| Button hover bg | BgColor transition | 100ms | CubicOut |
| History panel expand | Height 0->auto | 250ms | QuadInOut |
| History panel collapse | Height auto->0 | 200ms | QuadInOut |
| Segment switch | BgColor + TextColor | 150ms | CubicOut |
| Error appear | Fade 0->1 | 150ms | CubicOut |

### Animation Rules

- **Never animate layout shifts** that cause text reflow during reading
- **Never use bounce or elastic easing** — too playful for a tool
- **Skeleton shimmer opacity**: Keep subtle (0.4 to 0.7 range), not full 0-to-1 flash
- **Respect `prefers-reduced-motion`**: If system accessibility setting is on, skip all non-essential animations

---

## 7. Interactive States

### Button States (Icon Buttons)

```
Default:    Background transparent, Icon color TextSecondary
Hover:      Background BgTertiary, Icon color TextPrimary
Active:     Background Separator, Icon color TextPrimary
Focused:    2px outline AccentPrimary (keyboard navigation)
Disabled:   Opacity 0.4, cursor default
```

### Segment Control States

```
Active Segment:
  Background: AccentPrimary
  Text: TextInverse
  No shadow, flat

Inactive Segment:
  Background: BgTertiary
  Text: TextSecondary
  Hover: Background Separator, Text TextPrimary

Container:
  Background: Separator (light) / Separator (dark)
  Padding: 2px
  CornerRadius: 6px
```

### Text Input States

```
Default:    Background transparent, no border (within panel)
Focused:    No visible border change (borderless design)
Placeholder: Color TextTertiary, italic style
Selection:  Background AccentSubtle
```

### Link / Hyperlink States

```
Default:    Color AccentPrimary, no underline
Hover:      Color AccentHover, underline
Active:     Color AccentActive
```

---

## 8. Iconography

### Style

- **Line icons preferred** — 1.5px stroke weight, rounded caps
- Use Unicode symbols for simplicity (current approach is acceptable)
- If upgrading: Fluent System Icons (Regular weight) or Phosphor Icons (Light weight)

### Icon Sizes

| Context | Size | Notes |
|---------|------|-------|
| Title bar buttons | 14px font size | Close, minimize, pin |
| Action buttons | 14px font size | TTS, copy, clear |
| History arrow | 10px font size | Expand/collapse indicator |
| Segment control text | 11px font size | "Google", "LLM" labels |

### Icon Colors

- Follow the button state colors defined above
- Never use colored icons unless they represent state (e.g., pin active = AccentPrimary)

---

## 9. XAML Resource Dictionary

### 9.1 Light Theme Resources

```xml
<!-- ============================================ -->
<!-- Japanese UI Style — Light Theme              -->
<!-- ============================================ -->

<!-- Background -->
<Color x:Key="BgPrimary">#FAFBFC</Color>
<Color x:Key="BgSecondary">#F2F3F5</Color>
<Color x:Key="BgTertiary">#EBEDF0</Color>
<Color x:Key="BgCard">#FFFFFF</Color>

<!-- Text -->
<Color x:Key="TextPrimary">#1B1F23</Color>
<Color x:Key="TextSecondary">#586069</Color>
<Color x:Key="TextTertiary">#8B949E</Color>
<Color x:Key="TextInverse">#FAFBFC</Color>

<!-- Accent -->
<Color x:Key="AccentPrimary">#4A6FA5</Color>
<Color x:Key="AccentHover">#3D5F91</Color>
<Color x:Key="AccentActive">#33507A</Color>
<Color x:Key="AccentSubtle">#EEF2F7</Color>

<!-- Functional -->
<Color x:Key="Success">#4A9E6D</Color>
<Color x:Key="Warning">#C49A3A</Color>
<Color x:Key="Error">#C75C5C</Color>
<Color x:Key="ErrorSubtle">#FDF2F2</Color>

<!-- Structural -->
<Color x:Key="Separator">#E1E4E8</Color>
<Color x:Key="SeparatorStrong">#D1D5DA</Color>

<!-- Interactive -->
<Color x:Key="ButtonHover">#F2F3F5</Color>
<Color x:Key="SegmentActive">#4A6FA5</Color>
<Color x:Key="SegmentInactive">#EBEDF0</Color>
<Color x:Key="SegmentActiveText">#FAFBFC</Color>
<Color x:Key="SegmentInactiveText">#586069</Color>
<Color x:Key="ShimmerBase">#EBEDF0</Color>
<Color x:Key="ShimmerHighlight">#F7F8FA</Color>

<!-- Brushes (for direct binding) -->
<SolidColorBrush x:Key="BgPrimaryBrush" Color="{StaticResource BgPrimary}"/>
<SolidColorBrush x:Key="BgSecondaryBrush" Color="{StaticResource BgSecondary}"/>
<SolidColorBrush x:Key="BgTertiaryBrush" Color="{StaticResource BgTertiary}"/>
<SolidColorBrush x:Key="BgCardBrush" Color="{StaticResource BgCard}"/>
<SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimary}"/>
<SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondary}"/>
<SolidColorBrush x:Key="TextTertiaryBrush" Color="{StaticResource TextTertiary}"/>
<SolidColorBrush x:Key="TextInverseBrush" Color="{StaticResource TextInverse}"/>
<SolidColorBrush x:Key="AccentPrimaryBrush" Color="{StaticResource AccentPrimary}"/>
<SolidColorBrush x:Key="AccentHoverBrush" Color="{StaticResource AccentHover}"/>
<SolidColorBrush x:Key="AccentActiveBrush" Color="{StaticResource AccentActive}"/>
<SolidColorBrush x:Key="AccentSubtleBrush" Color="{StaticResource AccentSubtle}"/>
<SolidColorBrush x:Key="SuccessBrush" Color="{StaticResource Success}"/>
<SolidColorBrush x:Key="WarningBrush" Color="{StaticResource Warning}"/>
<SolidColorBrush x:Key="ErrorBrush" Color="{StaticResource Error}"/>
<SolidColorBrush x:Key="ErrorSubtleBrush" Color="{StaticResource ErrorSubtle}"/>
<SolidColorBrush x:Key="SeparatorBrush" Color="{StaticResource Separator}"/>
<SolidColorBrush x:Key="SeparatorStrongBrush" Color="{StaticResource SeparatorStrong}"/>
<SolidColorBrush x:Key="ButtonHoverBrush" Color="{StaticResource ButtonHover}"/>
<SolidColorBrush x:Key="SegmentActiveBrush" Color="{StaticResource SegmentActive}"/>
<SolidColorBrush x:Key="SegmentInactiveBrush" Color="{StaticResource SegmentInactive}"/>
<SolidColorBrush x:Key="SegmentActiveTextBrush" Color="{StaticResource SegmentActiveText}"/>
<SolidColorBrush x:Key="SegmentInactiveTextBrush" Color="{StaticResource SegmentInactiveText}"/>
<SolidColorBrush x:Key="ShimmerBaseBrush" Color="{StaticResource ShimmerBase}"/>
<SolidColorBrush x:Key="ShimmerHighlightBrush" Color="{StaticResource ShimmerHighlight}"/>
```

### 9.2 Dark Theme Resources

```xml
<!-- ============================================ -->
<!-- Japanese UI Style — Dark Theme               -->
<!-- ============================================ -->

<!-- Background -->
<Color x:Key="BgPrimary">#1E2128</Color>
<Color x:Key="BgSecondary">#262A32</Color>
<Color x:Key="BgTertiary">#2E333C</Color>
<Color x:Key="BgCard">#2A2F38</Color>

<!-- Text -->
<Color x:Key="TextPrimary">#E2E5E9</Color>
<Color x:Key="TextSecondary">#8B949E</Color>
<Color x:Key="TextTertiary">#6B7380</Color>
<Color x:Key="TextInverse">#1E2128</Color>

<!-- Accent -->
<Color x:Key="AccentPrimary">#7EB0E0</Color>
<Color x:Key="AccentHover">#93BFE8</Color>
<Color x:Key="AccentActive">#6A9FD4</Color>
<Color x:Key="AccentSubtle">#282E3A</Color>

<!-- Functional -->
<Color x:Key="Success">#6BC08A</Color>
<Color x:Key="Warning">#D9B35A</Color>
<Color x:Key="Error">#E07070</Color>
<Color x:Key="ErrorSubtle">#2E2226</Color>

<!-- Structural -->
<Color x:Key="Separator">#363B44</Color>
<Color x:Key="SeparatorStrong">#444C56</Color>

<!-- Interactive -->
<Color x:Key="ButtonHover">#2E333C</Color>
<Color x:Key="SegmentActive">#7EB0E0</Color>
<Color x:Key="SegmentInactive">#2E333C</Color>
<Color x:Key="SegmentActiveText">#1E2128</Color>
<Color x:Key="SegmentInactiveText">#8B949E</Color>
<Color x:Key="ShimmerBase">#2E333C</Color>
<Color x:Key="ShimmerHighlight">#3A4048</Color>

<!-- Brushes (same keys, dark values) -->
<SolidColorBrush x:Key="BgPrimaryBrush" Color="{StaticResource BgPrimary}"/>
<SolidColorBrush x:Key="BgSecondaryBrush" Color="{StaticResource BgSecondary}"/>
<SolidColorBrush x:Key="BgTertiaryBrush" Color="{StaticResource BgTertiary}"/>
<SolidColorBrush x:Key="BgCardBrush" Color="{StaticResource BgCard}"/>
<SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimary}"/>
<SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondary}"/>
<SolidColorBrush x:Key="TextTertiaryBrush" Color="{StaticResource TextTertiary}"/>
<SolidColorBrush x:Key="TextInverseBrush" Color="{StaticResource TextInverse}"/>
<SolidColorBrush x:Key="AccentPrimaryBrush" Color="{StaticResource AccentPrimary}"/>
<SolidColorBrush x:Key="AccentHoverBrush" Color="{StaticResource AccentHover}"/>
<SolidColorBrush x:Key="AccentActiveBrush" Color="{StaticResource AccentActive}"/>
<SolidColorBrush x:Key="AccentSubtleBrush" Color="{StaticResource AccentSubtle}"/>
<SolidColorBrush x:Key="SuccessBrush" Color="{StaticResource Success}"/>
<SolidColorBrush x:Key="WarningBrush" Color="{StaticResource Warning}"/>
<SolidColorBrush x:Key="ErrorBrush" Color="{StaticResource Error}"/>
<SolidColorBrush x:Key="ErrorSubtleBrush" Color="{StaticResource ErrorSubtle}"/>
<SolidColorBrush x:Key="SeparatorBrush" Color="{StaticResource Separator}"/>
<SolidColorBrush x:Key="SeparatorStrongBrush" Color="{StaticResource SeparatorStrong}"/>
<SolidColorBrush x:Key="ButtonHoverBrush" Color="{StaticResource ButtonHover}"/>
<SolidColorBrush x:Key="SegmentActiveBrush" Color="{StaticResource SegmentActive}"/>
<SolidColorBrush x:Key="SegmentInactiveBrush" Color="{StaticResource SegmentInactive}"/>
<SolidColorBrush x:Key="SegmentActiveTextBrush" Color="{StaticResource SegmentActiveText}"/>
<SolidColorBrush x:Key="SegmentInactiveTextBrush" Color="{StaticResource SegmentInactiveText}"/>
<SolidColorBrush x:Key="ShimmerBaseBrush" Color="{StaticResource ShimmerBase}"/>
<SolidColorBrush x:Key="ShimmerHighlightBrush" Color="{StaticResource ShimmerHighlight}"/>
```

---

## 10. Migration Map — Current to New

This table maps existing XAML resource keys to the new design tokens, so the frontend developer knows exactly what to replace.

| Current Key | Current Value | New Key | New Light Value | New Dark Value |
|-------------|---------------|---------|-----------------|----------------|
| `BgBrush` | #FFFFFF | `BgPrimaryBrush` | #FAFBFC | #1E2128 |
| `TextBrush` | #1A1A1A | `TextPrimaryBrush` | #1B1F23 | #E2E5E9 |
| `SeparatorBrush` | #E5E5E5 | `SeparatorBrush` | #E1E4E8 | #363B44 |
| `AccentBrush` | #0078D4 | `AccentPrimaryBrush` | #4A6FA5 | #7EB0E0 |
| `LabelBrush` | #888888 | `TextSecondaryBrush` | #586069 | #8B949E |
| `TitleBarBrush` | #F3F3F3 | `BgSecondaryBrush` | #F2F3F5 | #262A32 |
| `ButtonHoverBrush` | #E8E8E8 | `ButtonHoverBrush` | #F2F3F5 | #2E333C |
| `SegmentActiveBrush` | #0078D4 | `SegmentActiveBrush` | #4A6FA5 | #7EB0E0 |
| `SegmentInactiveBrush` | #E0E0E0 | `SegmentInactiveBrush` | #EBEDF0 | #2E333C |
| `SegmentActiveTextBrush` | #FFFFFF | `SegmentActiveTextBrush` | #FAFBFC | #1E2128 |
| `SegmentInactiveTextBrush` | #555555 | `SegmentInactiveTextBrush` | #586069 | #8B949E |
| `ShimmerBrush` | #E8E8E8 | `ShimmerBaseBrush` | #EBEDF0 | #2E333C |
| `ErrorBrush` | #D32F2F | `ErrorBrush` | #C75C5C | #E07070 |
| `HistoryBgBrush` | #FAFAFA | `BgSecondaryBrush` | #F2F3F5 | #262A32 |

### New Keys (not in current XAML)

These are additions for the enriched design system:

| New Key | Purpose | Light | Dark |
|---------|---------|-------|------|
| `BgTertiaryBrush` | Third-level backgrounds, hover states | #EBEDF0 | #2E333C |
| `TextTertiaryBrush` | Placeholder text, hints | #8B949E | #6B7380 |
| `AccentHoverBrush` | Accent button hover | #3D5F91 | #93BFE8 |
| `AccentActiveBrush` | Accent button pressed | #33507A | #6A9FD4 |
| `AccentSubtleBrush` | Subtle accent background | #EEF2F7 | #282E3A |
| `SuccessBrush` | Success indicators | #4A9E6D | #6BC08A |
| `WarningBrush` | Warning indicators | #C49A3A | #D9B35A |
| `ErrorSubtleBrush` | Error area background | #FDF2F2 | #2E2226 |
| `SeparatorStrongBrush` | Stronger dividers | #D1D5DA | #444C56 |

---

## 11. Layout Adjustments Summary

These are structural tweaks to the existing layout for better adherence to Japanese spacing principles:

| Element | Current | Proposed | Reason |
|---------|---------|----------|--------|
| Title bar height | 32px | 36px | More breathing room (Ma) |
| Icon button padding | 6px | 8px | Larger touch/click target |
| Content panel margin | 12,8,8,8 / 8,8,12,8 | 16,10,12,10 / 12,10,16,10 | More generous internal spacing |
| Label-to-content gap | 4px | 6px | Subtle but noticeable improvement |
| Segment button padding | 8,2 | 10,3 | More comfortable |
| Window shadow opacity | 0.25 | 0.15 | Softer, less aggressive |
| Window shadow blur | 20px | 24px | Softer spread |
| Window shadow Y-offset | 4px | 6px | Slightly more lift |
| Scale animation from | 0.95 | 0.96 | Less dramatic, more refined |
| History item margin | 8,4 | 10,6 | Better scanability |
| Separator width | 1px | 1px | Keep — precision is Japanese |

---

## 12. Accessibility Notes

- **Contrast ratios** (WCAG AA minimum 4.5:1 for normal text):
  - Light: TextPrimary (#1B1F23) on BgPrimary (#FAFBFC) = ~15.5:1 (pass)
  - Light: TextSecondary (#586069) on BgPrimary (#FAFBFC) = ~5.8:1 (pass)
  - Light: TextTertiary (#8B949E) on BgPrimary (#FAFBFC) = ~3.5:1 (decorative only, not for essential info)
  - Dark: TextPrimary (#E2E5E9) on BgPrimary (#1E2128) = ~12.2:1 (pass)
  - Dark: TextSecondary (#8B949E) on BgPrimary (#1E2128) = ~5.3:1 (pass)
- **Accent on backgrounds**:
  - Light: AccentPrimary (#4A6FA5) on BgPrimary (#FAFBFC) = ~4.8:1 (pass)
  - Dark: AccentPrimary (#7EB0E0) on BgPrimary (#1E2128) = ~8.1:1 (pass)
- **Inverse text on accent**:
  - Light: TextInverse (#FAFBFC) on AccentPrimary (#4A6FA5) = ~4.8:1 (pass)
  - Dark: TextInverse (#1E2128) on AccentPrimary (#7EB0E0) = ~8.1:1 (pass)
- **Focus indicators**: Always use 2px outline with AccentPrimary for keyboard navigation
- **Reduced motion**: Honor `SystemParameters.HighContrast` and reduce animations accordingly
