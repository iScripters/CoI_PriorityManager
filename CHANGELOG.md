# Changelog

## 0.9.1 - 2026-10-31

### Changed

- Reviewed source compatibility with Captain of Industry 0.8.8 (update 4.3); none of the removed APIs are used by the mod.
- Moved all localization catalogs from the embedded DLL resource to Captain of Industry's native `Translations/` format. The build, deploy, and release package include those files beside the DLL.
- Registered all UI strings with the native localization API, including validated format argument counts and game-managed language switching.
- Removed duplicate dependency registrations that the 0.8.8 global-dependency loader now performs automatically.

## 0.9.0 - 2026-10-12

### Added

- Added complete in-game localization for all 21 languages supported by Captain of Industry.
- Priority Manager now follows the game's selected language for its toolbar entry, manager window, building-inspector panel, tooltips, status messages, sorting labels, and default unnamed group names.

### Improved

- Building filtering and sorting now use the selected game language's culture rules.
- Added catalog validation to ensure every language includes every UI key and preserves formatting placeholders.

## 0.8.0 - 2026-09-10

Priority Manager is getting a major quality-of-life pass focused on faster group management, clearer feedback, and better integration with Captain of Industry's native UI. ⚙️

### ✨ New Features

- Added a collapsible **Priority group** section to native building inspectors for direct group assignment, creation, and priority reset.
- Added shared inspector expansion state: expand or collapse the section once, and every supported inspector remembers it. 📌
- Added map highlights for groups and individual group members, making it easy to find exactly what a row represents. 🔎
- Added game-native **green** add-building selection and **red** remove-building selection, with both single-click and area selection. 🟢🔴
- Added rapid group creation: type a name, press Enter, and immediately type the next one. ⌨️
- Added filter and sort controls to Overall, including ascending and descending order by name, priority, group, or type.

### 🚀 Improvements

- Overall now updates live when buildings are renamed, priorities change, or group membership changes.
- Inactive storage and Cargo Depots can appear in Overall and receive group assignments before a request becomes active.
- Native inspector coverage now includes tab-based inspectors such as Captain's Office.
- Individual priority changes made from Priority Manager immediately clear the building's group assignment.
- Group settings continue to apply only to controls that each member actually supports.

### 🛠️ Fixes

- Changing a grouped building's priority through the native game inspector now removes it from the group while preserving the manually selected value.
- The open native inspector updates its group dropdown to **No group** immediately after a manual override; reopening is no longer required.
- Conveyors, balancers, pipes, molten channels, and other static transport paths can no longer be assigned to groups. Existing saved transport members are removed automatically.
- Cooling towers, smokestacks, and other buildings with no usable priority control are excluded from Priority Manager.

## 0.7.1 - 2026-09-06

First public release.

- Manage supported building priorities from a single Overall view.
- Create priority groups with general, import, export, and generator settings.
- Assign groups through native building inspectors or game-standard click and area selection.
- Reset individual buildings, group members, or all priorities to native defaults.
- Support priority-capable storage and Cargo Depot controls.
