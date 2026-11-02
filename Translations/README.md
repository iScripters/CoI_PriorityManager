# Priority Manager translations

Captain of Industry loads these catalogs automatically from the mod's `Translations/` directory. Each language file uses the exact filename used by the game, such as `de.json`, `nb_NO.json`, `pt_BR.json`, or `zh_Hans.json`.

Each entry is a two-element JSON array:

```json
["PriorityManager.WindowTitle", "Priority Manager"]
```

- Keep every `PriorityManager.*` key from `en.json`.
- Keep the same numbered format placeholders (`{0}`, `{1}`, and so on) as English.
- Keep the top-level JSON array and UTF-8 encoding.
- `en_GB.json` contains only British-English overrides; it currently mirrors the English wording.

After changing UI text, load the mod in game and run `export_mod_translations PriorityManager`. This generates `source.json` and `template.json` in this directory, which can be used to check that shipped catalogs match all registered strings.
