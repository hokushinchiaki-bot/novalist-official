# Hotkeys reference

This is the full list of default keyboard shortcuts shipped with Novalist. Every binding is **rebindable** under **Settings → Keyboard Shortcuts**.

On macOS, read `Ctrl` as `Cmd` (the SDK translates accordingly).

Keys in this table use their on-screen names. A few aliases:

- `Ctrl+,` (comma) is shown as `Ctrl+OemComma` in the rebind UI.
- `Ctrl+[` and `Ctrl+]` are `Ctrl+OemOpenBrackets` and `Ctrl+OemCloseBrackets`.
- `Ctrl+1` through `Ctrl+9` are `Ctrl+D1` through `Ctrl+D9` in the rebind UI.

## Navigation

| Action | Default | Notes |
| --- | --- | --- |
| Go to Dashboard | `Ctrl+1` | |
| Go to Editor (current scene) | `Ctrl+2` | |
| Go to Entity Editor | `Ctrl+3` | |
| Go to Timeline | `Ctrl+4` | |
| Go to Export | `Ctrl+5` | |
| Go to Image Gallery | `Ctrl+6` | |
| Go to Git | `Ctrl+7` | Only when project is a Git repo. |
| Go to Codex Hub | `Ctrl+8` | |
| Go to Manuscript | `Ctrl+9` | |
| Go to Calendar | `Ctrl+Shift+K` | |
| Go to Relationships | `Ctrl+Shift+R` | |
| Open Settings | `Ctrl+,` | |
| Open Extensions | `Ctrl+Shift+X` | |
| Open Start menu | `Alt+F` | |
| Command Palette | `Ctrl+Shift+P` | |

## Panels

| Action | Default | Notes |
| --- | --- | --- |
| Toggle Explorer | `Ctrl+B` | Conflicts with editor's Bold while in the editor — context-sensitive. |
| Toggle Context sidebar | `Ctrl+Shift+B` | |
| Switch sidebar to Chapters | `Ctrl+Shift+1` | |
| Switch sidebar to Entities | `Ctrl+Shift+2` | |
| Toggle Project Overview popup | `Ctrl+Shift+O` | |
| Toggle Scene Notes | `Ctrl+Shift+N` | |
| Toggle Focus Mode | `F11` | |

## Editor — formatting

| Action | Default |
| --- | --- |
| Bold | `Ctrl+B` |
| Italic | `Ctrl+I` |
| Underline | `Ctrl+U` |
| Align left | `Ctrl+L` |
| Align center | `Ctrl+E` |
| Align right | `Ctrl+R` |
| Align justify | `Ctrl+J` |

## Editor — text actions

| Action | Default |
| --- | --- |
| Find & Replace | `Ctrl+H` |
| Add Comment | `Ctrl+Shift+M` |
| Add Footnote | `Ctrl+Shift+F` |

## Scenes & Chapters

| Action | Default |
| --- | --- |
| Close current scene tab | `Ctrl+W` |
| Create scene | `Ctrl+N` |
| Create chapter | `Ctrl+Shift+M` |
| Next scene | `Ctrl+]` |
| Previous scene | `Ctrl+[` |

## Project

| Action | Default |
| --- | --- |
| Save current scene | `Ctrl+S` |

## Git

| Action | Default | Notes |
| --- | --- | --- |
| Commit all | `Ctrl+Shift+K` | When the active focus context is Git. |
| Push | `Ctrl+Shift+P` | When the active focus context is Git. |
| Pull | `Ctrl+Shift+L` | When the active focus context is Git. |

## Dialog conventions

These behaviors are consistent across every dialog and overlay in Novalist:

- **Auto-focus.** The first meaningful input field is focused the moment a dialog opens. For dialogs whose primary action is a button (e.g. the add-image source picker, the update prompt), that button is focused so `Enter` triggers it immediately.
- **`Enter` confirms** the primary action — OK, Save, Create, Find, Take Snapshot, etc. Inside the Find/Replace dialog, `Enter` runs Find from either input field (Replace All is intentionally not on `Enter` because it is destructive).
- **`Escape` cancels** every dialog. The confirm-delete dialog focuses Cancel by default for safety — press `Tab` to reach the Confirm button.
- **Project Image Picker:** type to filter the list, then `Enter` to pick the first remaining match without reaching for the mouse.
- **Wizard:** each step auto-focuses its primary input. `Ctrl+Enter` advances to the next step (regular `Enter` is reserved for the input, so multi-line text steps work naturally).

## Notes

- Some bindings appear twice because they are context-sensitive. `Ctrl+B` is **Toggle Explorer** when the focus is outside the editor and **Bold** when the focus is in the editor. Likewise `Ctrl+Shift+P` is the Command Palette by default, except when the Git view has focus.
- Bindings can be **cleared** entirely so they don't fire at all.
- Bindings can be **reset to default** by clicking the × next to the current binding in Settings.

## Where to go next

- [Settings](23-settings.md) — find the Keyboard Shortcuts grid here.
- [Command Palette](25-command-palette.md) — every action by name regardless of binding.
- [Extensions](24-extensions.md) — extensions can add their own actions to the grid.
