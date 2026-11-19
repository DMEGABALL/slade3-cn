# SLADE 3.3.1 简体中文版（汉化源码）

> 本仓库是 [SLADE](https://github.com/sirjuddington/SLADE) 3.3.1 的**简体中文汉化分支**，
> 在保持原有界面布局的前提下，为界面文本提供中英双语显示（可切换为纯中文）。
>
> This is a Simplified Chinese localisation of SLADE 3.3.1. It adds a small
> runtime translation layer on top of the upstream source; all UI strings can be
> shown as `中文 (English)` (default), Chinese only, or English only.

---

## ⚠️ AI 汉化声明 / AI Translation Notice

本汉化项目由 **AI 编程助手（OpenCode，模型 DeepSeek）** 在用户指导下完成，包括：

- `lang/zh_CN.txt` 中全部简体中文译文的撰写；
- 运行时翻译层 `src/General/Translation.h` / `Translation.cpp` 的设计与实现；
- 源码中 UI 字符串的批量标记（`tools/localize/Localize.cs`）与逐处接入；
- 文档撰写、编译、打包与界面截图验证。

译文为**机器生成**。虽然已尽量统一术语（归档 / 条目 / 纹理 / 平面 / 拼块 / 扇区 /
线段 / 顶点 / 物件 / 节点构建器），仍可能存在误译、漏译或用词不当。
界面默认使用中英双语 `中文 (English)`，遇疑问请以括号内的英文原文为准。
欢迎通过 Issue 或 Pull Request 指正。

> This Chinese localisation was produced by an **AI coding assistant
> (OpenCode, model DeepSeek)** under user supervision — including all
> translations, the runtime translation layer, build/packaging and verification.
> The translations are machine-generated and may contain mistakes; the original
> English text (shown in parentheses in bilingual mode) is authoritative.
> Corrections via issues or pull requests are welcome.

---

## 目录

- [1. 许可证与源码](#1-许可证与源码)
- [2. 汉化范围](#2-汉化范围)
- [3. 实现方式](#3-实现方式)
- [4. 目录结构](#4-目录结构)
- [5. 构建](#5-构建)
- [6. 语言文件格式](#6-语言文件格式)
- [7. 已知限制](#7-已知限制)
- [8. 跟随上游更新](#8-跟随上游更新)
- [9. 致谢](#9-致谢)

---

## 1. 许可证与源码

SLADE 以 **GNU General Public License v2.0（GPLv2）** 发布，本汉化分支同样遵循 GPLv2，
完整许可证见仓库根目录的 [`LICENSE`](LICENSE)。

- 原始项目：<https://github.com/sirjuddington/SLADE>
- 上游版本：3.3.1（tag `3.3.1`）
- 本分支的全部修改同样以 GPLv2 公开；如果你分发本程序的二进制版本，请一并提供本仓库的源码。

> SLADE - It's a Doom Editor  
> Copyright (C) 2008 - 2026 Simon Judd  
> 本汉化分支的修改部分同样按 GPLv2 授权。

---

## 2. 汉化范围

已完成汉化的界面元素：

| 区域 | 说明 |
| --- | --- |
| 菜单栏 / 右键菜单 | 全部菜单项与子菜单（含快捷键提示） |
| 工具栏 | 按钮文字、标签、状态栏提示 |
| 首选项（Preferences） | 所有设置页标题、分组、选项、说明文字；含键盘快捷键的全部动作名与分组 |
| 各类对话框 | 新建/打开/保存、图形转换、纹理编辑、地图检查、脚本管理等 |
| 归档 / 条目面板 | 标签页标题、列表列名、按钮、提示 |
| 地图编辑器 | 面板、属性页、3D 模式提示、物件/线段/扇区/顶点操作 |
| 文本/纹理编辑器 | 工具栏、对话框、右键菜单 |
| 起始页 | 标题、操作按钮（打开归档 / 打开目录 / 新建归档 / 新建地图）、最近文件 |
| 首次运行向导 | 窗口标题、页面标题与说明、按钮 |
| 启动提示（Tips） | `tips.txt` 中的全部提示 |

默认显示模式为**中英双语**：`中文 (English)`，例如

```text
文件 (&File)
导入文件 (&Import Files)
新建纹理 (New Texture)
```

这样可以一边使用中文界面，一边对照英文术语。较长的句子为避免撑开布局，只显示中文。

---

## 3. 实现方式

没有使用 gettext，也没有改动 `slade.pk3` 里的资源文件，而是新增了一个轻量的运行时翻译层：

1. `src/General/Translation.h` / `Translation.cpp`
   - 启动时从可执行文件目录（以及 `Resources`/`Data`/`User` 目录）加载 `lang/zh_CN.txt`；
   - 提供 `slade::i18n::tr()`（返回 `wxString`）与 `trs()`（返回 `std::string`）；
   - 查表失败时原样返回英文，因此**缺少译文不会导致空白或崩溃**。
2. `src/Application/App.cpp`：在 `log::init()` 之后调用 `i18n::init()`。
3. `src/General/SAction.cpp`：菜单动作的 `text` / `help_text` 在从 JSON 载入时翻译，
   因此 `dist/res/actions/*.json` 保持英文原样，便于合并上游更新。
4. `src/UI/SAuiToolBar.cpp`：工具栏按钮文字、标签、提示。
5. `src/UI/SDialog.cpp`、`src/UI/STopWindow.cpp`：对话框与窗口标题。
6. `src/UI/Controls/SettingsTable.cpp`、`RadioButtonPanel.cpp`：首选项面板标签、说明与单选项。
7. 其余写死在代码中的 UI 字面量：由 `tools/localize/Localize.cs` 批量包裹为
   `slade::i18n::tr(wxS("..."))`（101 个文件、约 733 处）。

共修改 103 个源文件（另有根目录 `README.md`），并新增 `Translation.h` / `Translation.cpp` 两个文件，
主要集中在 `src/UI`、`src/MainEditor`、`src/MapEditor`、`src/TextureEditor`、
`src/Scripting`、`src/TextEditor`。

语言表 `lang/zh_CN.txt` 目前收录 1556 条译文（UTF-8、无 BOM），除 22 项刻意保留英文的
技术缩写/占位符外，源码中所有可翻译的 UI 字符串都已覆盖。

> 汉化字典是**外部文件**，修改译文不需要重新编译，替换 `lang/zh_CN.txt` 即可生效。

---

## 4. 目录结构

```text
.
├─ lang/
│  └─ zh_CN.txt          # 简体中文语言表（UTF-8，无 BOM）
├─ tools/
│  └─ localize/          # 批量标记 UI 字面量的辅助工具（C#）
├─ src/
│  ├─ General/
│  │  ├─ Translation.h   # 运行时翻译层（新增）
│  │  └─ Translation.cpp
│  └─ ...                # 其余为上游源码
├─ dist/res/             # 上游资源（保持英文，不改动）
├─ build-windows.ps1     # Windows 一键构建 + 组装发布目录
├─ CMakeLists.txt
└─ README.md
```

构建完成后，`lang/` 会被自动复制到可执行文件旁边（见 `src/CMakeLists.txt` 中的
`POST_BUILD` 规则），`SLADE.exe` 启动时会自动加载。

---

## 5. 构建

### Windows（推荐）

需要：

- Visual Studio 2022（或更新版本），安装 **“使用 C++ 的桌面开发”** 工作负载；
- [vcpkg](https://github.com/microsoft/vcpkg)，并设置环境变量 `VCPKG_ROOT`；
- 可选：7-Zip 或 `zip`（用于打包 `slade.pk3`）。

在普通 PowerShell 中执行：

```powershell
powershell -ExecutionPolicy Bypass -File build-windows.ps1
```

脚本会自动：

1. 定位 Visual Studio 并进入开发者环境；
2. 使用 CMake 预设 `win-x64-release` 配置工程（vcpkg manifest 模式会自动安装依赖）；
3. 编译 `slade` 目标并打包 `slade.pk3`；
4. 把 `SLADE.exe`、`slade.pk3`、`lang/` 复制到 `..\SLADE3-CN-3.3.1-win64\`。

也可以手动构建：

```powershell
cmake --preset win-x64-release
cmake --build out/build/win-x64-release
```

首次构建需要下载并编译 wxWidgets、SFML、libarchive 等依赖，耗时较长（视网络与 CPU 而定）。

> **已验证的构建环境**：Visual Studio Build Tools 2022（MSVC 19.44 / Windows SDK 10.0.26100）、
> CMake 3.31.6、Ninja 1.12.1、vcpkg（triplet `x64-windows-static`，静态 CRT）。
> 产出的 x64 可执行文件仅依赖系统 DLL；启动日志中会显示
> `Loaded translation table "...\lang/zh_CN.txt"` 以确认汉化已加载。

### Linux

参考 [`COMPILE.md`](COMPILE.md)，依赖安装完成后：

```bash
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build -j
```

`lang/` 同样会被复制到可执行文件所在目录。

---

## 6. 语言文件格式

`lang/zh_CN.txt` 为 UTF-8（无 BOM）纯文本，每行一条：

```text
<英文原文><TAB><中文翻译>[<TAB>zh|both|en]
```

- 以 `#` 开头的行是注释，空行忽略；
- 第三列可选，覆盖该条的显示模式：
  - `zh` —— 只显示中文；
  - `both` —— `中文 (English)`（默认）；
  - `en` —— 只显示英文；
- `\n`、`\t`、`\\` 会分别还原为换行、制表符和反斜杠。

文件头部指令：

```text
@mode both
```

可改为 `@mode zh`（全中文界面）或 `@mode en`（英文界面，用于对照）。

示例：

```text
New Texture	新建纹理
Deep Fog	深雾	zh
Open	打开
```

> 提示：想切换成纯中文，把首行的 `@mode both` 改成 `@mode zh` 即可，无需重新编译。

---

## 7. 已知限制

1. **仓库本身不包含二进制文件**：本仓库是源码；编译好的 `SLADE.exe`、`slade.pk3` 与
   `lang/` 放在配套的 `SLADE3-CN-3.3.1-win64` 发布目录中，可直接运行。
2. 少量技术性缩写、占位符与纯符号刻意不翻译，例如 `3D`、`ACS`、`ASCII`、`SLADE`、
   `X`/`Y`、`K1:`、`out of XX` 等。
3. 运行时动态拼接的字符串（`fmt::format` / `WX_FMT` 生成的部分，如
   `SLADE - MAP01 of doom2.wad`）保持英文或中英混合，不影响使用。
4. `dist/res/config` 中的数据定义（游戏配置、物品/东西名称、调色板名等）来自资源文件，
   属于“内容数据”而非界面文本，保持英文。
5. 地图编辑器的 OpenGL 自绘文字（如 3D 模式中的少量提示）使用内置位图字体，
   中文可能无法显示；这属于引擎限制，不在本次汉化范围内。
6. 双语模式会让部分菜单/标签变长，已为长句启用纯中文显示以避免布局变形；
   若使用极小字体或低分辨率，个别长条目仍可能显示不全。

---

## 8. 跟随上游更新

1. 把上游新版本合并到本仓库；
2. 编译并运行字符串标记工具（幂等，已标记的会跳过）：

   ```powershell
   powershell -ExecutionPolicy Bypass -File tools\localize\build-localize.ps1
   tools\localize\Localize.exe src --apply --report ..\keys.txt
   ```

3. 根据 `keys.txt` 补充 `lang/zh_CN.txt` 中缺失的条目即可。

因为菜单文本来自 `dist/res/actions/*.json` 且在运行时翻译，所以上游新增/修改的菜单项
不会和汉化分支产生冲突，只需要补译文。

---

## 9. 致谢

- 感谢 **Simon Judd (sirjuddington)** 及所有 SLADE 贡献者，二十年来持续维护这个优秀的 Doom 编辑器。
- 感谢 wxWidgets、SFML、libarchive、FluidSynth、libxmp、mpg123、glm、cpptrace 等开源项目。
- 本汉化仅为个人学习与交流之用，所有原始代码版权归原作者所有。

如果这个汉化对你有帮助，欢迎向上游项目 [捐赠或提交反馈](https://github.com/sirjuddington/SLADE)。
