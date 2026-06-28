# CodeTextBox (WinForms Control)

---

## ☝🏼 - Description

This control provides a flexible code-editing experience within Windows Forms, complete with line numbers, zoom support, and visual change markers for improved clarity. It is designed to make reading and editing C# code more comfortable, offering smooth navigation and a clean overview of your document. You can adjust the zoom level to suit your workflow, quickly spot modified lines through subtle markers, and enjoy a more organised editing environment. The control integrates seamlessly into existing projects and can be used wherever a lightweight yet capable editor is needed.

---

## 📔 - Version Information

Current version: **1.0.0.0** Release date: **06/12/2025**

Latest Updates:

- Removed the global `_changedLines`
- The costly full diff logic previously executed on every `TextChanged` event (iterating over all lines and maintaining a `HashSet`) has been removed.
- Line-change detection is now performed on the fly during `Paint`, comparing only visible lines against `_savedLines`.
- `TextChanged` now operates in O(1).
- Instead of comparing all lines, it now only triggers `Invalidate()`.
- `Paint` now processes only visible lines  
    The loop stops immediately once `y` exceeds the panel height.
- `IsSaved()` performs its heavy comparison only when explicitly called
- A full comparison between `currentLines` and `_savedLines` is now done only when checking whether the document is fully saved.
    
    ---
    
    ## ⚠️ - Software Requirements
    
- VisualStudio 2019-2026
- .NET 6.0 - .NET 10.0
- WinForms Framework
    
    ---
    

## 🚀 - Installation Guide

### Installation via NuGet (Visual Studio)

1.  Open **Visual Studio**.
2.  Create a new project or open an existing one.
3.  Open the graphical NuGet Package Manager:  
    **Project → Manage NuGet Packages… → Browse**
4.  Search for the package **Leekz.CodeTextBox** published by **PIN0L33KZ**.
5.  Select the package and click **Install** in the right-hand pane.

### Installation via CLI (dotnet CLI)

You can install the package directly using the **dotnet CLI**:

```
dotnet add package Leekz.CodeTextBox
```

This command adds **Leekz.CodeTextBox** (published by **PIN0L33KZ**) to your project without opening Visual Studio or the graphical NuGet manager.

---

## 😄 - Usage

### **Usage via Toolbox (Visual Studio)**

1.  In the Visual Studio Toolbox, locate the category **LeekzCodeTextBox**.
2.  Select the control named **LeekzCodeTextBox (CodeTextBox)**.
3.  Click on your form to place and add the control.

### Usage via Code (C#)

To create the control programmatically, instantiate it as usual, adjust size and layout properties, and add it to your form or panel:

```csharp
var codeTextBox = new LeekzCodeTextBox();
codeTextBox.Size = new Size(400, 200);
codeTextBox.Margin = new Padding(10);
codeTextBox.Location = new Point(10, 10);

this.Controls.Add(codeTextBox);
```

To handle events such as `TextChanged`, define the event handler:

```csharp
private void codeTextBox_TextChanged(object? sender, EventArgs e)
{
    // Handle text changed event
}
```

Then subscribe to the event, typically within your form’s `Load` event:

```csharp
codeTextBox.TextChanged += codeTextBox_TextChanged;
```

Configuring Properties via Code (C#):

```csharp
// Line number styling
codeTextBox.LineNumberBackColor = Color.FromArgb(38, 34, 34);
codeTextBox.LineNumberChangedColor = Color.Red;
codeTextBox.LineNumberDock = DockStyle.Left;
codeTextBox.LineNumberForeColor = Color.Gray;
codeTextBox.LineNumberSeparatorColor = Color.Silver;
codeTextBox.LineNumberSeparatorWidth = 4;

// Zoom behaviour
codeTextBox.MaxZoomFactor = 5f;
codeTextBox.MinZoomFactor = 0.5f;

// Code area styling
codeTextBox.CodeBackColor = SystemColors.Window;
codeTextBox.CodeForeColor = SystemColors.WindowText;

// Fonts can be set with any System.Drawing.Font
codeTextBox.CodeFont = new Font("Lexend", 11.25f, FontStyle.Regular);

// Word wrap behaviour
codeTextBox.CodeWordWrap = false;
```

Saved-State Functions:

The control provides two useful methods for checking whether the current text is fully saved, and for marking it as saved after you write it to disk or another destination.

`codeTextBox.IsSaved()`  
This method returns a **boolean value**.  
It returns **true** when the entire text is currently marked as saved, and **false** when there are still unsaved changes.  
Internally, it performs a one-off comparison between the current lines and the last saved state.

```csharp
bool isSaved = codeTextBox.IsSaved();

if (!isSaved)
{
    // Text has unsaved changes
}
```

`codeTextBox.MarkAsSaved()`  
This method marks the current text as the new **saved state**.  
All change indicators (including the coloured markers next to the line numbers and behind the separator) are reset, because the text is now treated as clean.

```csharp
codeTextBox.MarkAsSaved();
```

You typically call this method after successfully saving the text so that the control knows the document is up to date.

---

### Public properties – overview

| Property | Type | What it does |
| --- | --- | --- |
| `CodeBackColor` | `Color` | Background colour of the code area (the inner `RichTextBox`). |
| `CodeForeColor` | `Color` | Text colour of the code in the editor. |
| `CodeFont` | `Font` | Font and size used for the code. This is also the basis for the line number font and zoom. |
| `CodeWordWrap` | `bool` | Enables or disables word wrapping in the code area (`true` = wrap, `false` = horizontal scrolling). |
| `LineNumberBackColor` | `Color` | Background colour of the line-number panel. |
| `LineNumberForeColor` | `Color` | Text colour of the line numbers. |
| `LineNumberFont` | `Font` (read-only) | Actual font used to draw the line numbers. Internally derived from `CodeFont` (always Arial, size = `CodeFont.Size - 1`). |
| `LineNumberSeperatorColor` | `Color` | Base colour of the vertical separator between line numbers and code. |
| `LineNumberChangedColor` | `Color` | Colour of the separator segments that mark lines changed since the last `MarkAsSaved()` call. |
| `LineNumberSeperatorWith` | `int` | Width of the separator line in pixels (used for both the base line and change segments). |
| `LineNumberDock` | `LineNumberDockSide` (`Left` / `Right`) | Positions the line-number panel on the left or right of the code area. |
| `MinZoomFactor` | `float` | Minimum allowed zoom factor (e.g. `0.5` = 50%). `ZoomFactor` cannot go below this. |
| `MaxZoomFactor` | `float` | Maximum allowed zoom factor (e.g. `5.0` = 500%). `ZoomFactor` cannot go above this. |
| `ZoomFactor` | `float` | Current zoom factor for both code and line numbers (`1.0` = 100%). Also modified via `Ctrl + mouse wheel`. |
| `Text` | `string` | Full text content of the editor. Get/set forwards directly to the internal `RichTextBox`. |

### Key methods

| Method | Return type | What it does |
| --- | --- | --- |
| `MarkAsSaved()` | `void` | Takes a snapshot of the current lines and clears all change markers; this becomes the new “saved” state. |
| `IsSaved()` | `bool` | Compares the current text with the last snapshot from `MarkAsSaved()` and returns `true` if there are no differences. |
