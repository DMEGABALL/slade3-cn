# Localize —— UI 字符串标记辅助工具

`Localize.cs` 是汉化过程中使用的小工具：扫描 `src/**/*.cpp`，把写死在代码里的
UI 字符串字面量包进 `slade::i18n::tr(...)`（或把 `std::string` 参数包进
`slade::i18n::trs(...)`），这样程序运行时就能在外部语言表中查找对应译文。

## 编译

只依赖 Windows 自带的 .NET Framework 4.x 编译器：

```powershell
powershell -ExecutionPolicy Bypass -File build-localize.ps1
```

## 用法

```text
Localize.exe <源码根目录> [--apply] [--report <输出文件>]
```

- 默认是"预演"模式，只统计、不修改文件。
- `--apply` 真正写回源文件。
- `--report` 输出所有被标记的字符串及其首次出现位置（制表符分隔），可用于生成待翻译清单。

示例（在源码根目录下执行）：

```powershell
tools\localize\Localize.exe src --apply --report ..\keys.txt
```

## 幂等性

已经包过 `tr(...)` 的字面量会被跳过，因此把上游新版本合并进来之后可以放心地
再次运行本工具，它只会处理新增的、尚未标记的字符串。

## 覆盖的调用点

`wxStaticText`、`wxButton`、`wxCheckBox`、`wxRadioButton`、`wxMessageBox`、
`SetLabel`、`SetTitle`、`SetToolTip`、`AddPage`、`AppendSubMenu`、`Append`、
`createLabelHBox/VBox`、`createSectionSeparator`。

其余 UI 文本（菜单动作、工具栏、首选项面板等）由 `src/General/Translation.*`
及少量手工接入点负责，详见仓库根目录的 README。
