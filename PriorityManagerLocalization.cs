using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using Mafi;
using Mafi.Localization;

namespace PriorityManager;

public enum PriorityManagerText {
  WindowTitle,
  TabOverall,
  TabGroups,
  IndividualPriorityGroupWarning,
  ResetAllWarning,
  RemoveGroupsTooltip,
  RemoveGroups,
  OnlyUngroupedTooltip,
  OnlyUngrouped,
  ResetAllPriorities,
  FilterBuildings,
  Sort,
  ReverseSortTooltip,
  Descending,
  Previous,
  Next,
  NewGroupName,
  CreateGroup,
  OverallPageRange,
  NoPriorityCapableBuildings,
  Reset,
  NoGroup,
  General,
  Import,
  Export,
  Generator,
  CreateGroupThenAssign,
  ShowBuildings,
  HideBuildings,
  AddBuildings,
  RemoveBuildings,
  Remove,
  Delete,
  NoBuildingsAssigned,
  Name,
  Priority,
  Group,
  Type,
  CreatePriorityGroup,
  GroupName,
  NewGroup,
  ConfirmGroupName,
  Cancel,
  PriorityGroup,
  RestoreGameDefaults,
  ResetPriorities,
  StatusCreatedGroup,
  StatusRenamedGroup,
  StatusDeletedGroup,
  StatusRemovedFromGroup,
  StatusAssignedToGroup,
  StatusDragToAdd,
  StatusDragToRemove,
  StatusAddedToGroup,
  StatusRemovedFromGroupCount,
  StatusChangedPriority,
  StatusUpdatedGroupPriority,
  StatusResetBuilding,
  StatusResetAll,
  ResetScopeAll,
  ResetScopeUngrouped,
  ResetGroupsCleared,
  ResetGroupsKept,
  UnnamedGroup
}

[GlobalDependency(RegistrationMode.AsEverything)]
public sealed class PriorityManagerLocalization {
  private const string DefaultCulture = "en-US";
  private const string ResourceName = "PriorityManager.Resources.PriorityManagerTranslations.json";
  private readonly Dictionary<string, Dictionary<string, string>> translations;
  private string currentCulture;

  public PriorityManagerLocalization() {
    translations = LoadTranslations();
    ValidateTranslations(translations);
    currentCulture = CultureId();
  }

  public bool RefreshLanguage() {
    string culture = CultureId();
    if (culture == currentCulture) return false;
    currentCulture = culture;
    return true;
  }

  public LocStrFormatted Text(PriorityManagerText key) {
    return new LocStrFormatted(Value(key));
  }

  public LocStrFormatted Format(PriorityManagerText key, params object[] values) {
    return new LocStrFormatted(string.Format(LocalizationManager.CurrentCultureInfo, Value(key), values));
  }

  public string Value(PriorityManagerText key) {
    string culture = CultureId();
    currentCulture = culture;
    string name = key.ToString();
    if (translations.TryGetValue(culture, out Dictionary<string, string>? localized) && localized.TryGetValue(name, out string? value)) return value;
    return translations[DefaultCulture][name];
  }

  private static string CultureId() {
    return LocalizationManager.CurrentLangInfo.CultureInfoId;
  }

  private static Dictionary<string, Dictionary<string, string>> LoadTranslations() {
    Assembly assembly = typeof(PriorityManagerLocalization).Assembly;
    using (Stream stream = assembly.GetManifestResourceStream(ResourceName)
      ?? throw new InvalidOperationException($"Missing embedded localization resource '{ResourceName}'.")) {
      DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true };
      return (Dictionary<string, Dictionary<string, string>>)new DataContractJsonSerializer(typeof(Dictionary<string, Dictionary<string, string>>), settings).ReadObject(stream);
    }
  }

  private static void ValidateTranslations(Dictionary<string, Dictionary<string, string>> catalog) {
    string[] cultures = {
      "en-US", "ca-ES", "cs-CZ", "de-DE", "es-ES", "et-EE", "fr-FR", "hu-HU", "it-IT", "ja-JP", "ko-KR",
      "nb-NO", "nl-NL", "pl-PL", "pt-BR", "ru-RU", "sv-SE", "tr-TR", "uk-UA", "zh-CN", "zh-Hant"
    };
    foreach (string culture in cultures) {
      if (!catalog.TryGetValue(culture, out Dictionary<string, string>? entries)) throw new InvalidOperationException($"Missing '{culture}' localization catalog.");
      foreach (string key in Enum.GetNames(typeof(PriorityManagerText))) {
        if (!entries.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException($"Missing '{culture}' translation for '{key}'.");
        if (PlaceholderSignature(value) != PlaceholderSignature(catalog[DefaultCulture][key])) {
          throw new InvalidOperationException($"Placeholder mismatch for '{culture}' translation '{key}'.");
        }
      }
    }
  }

  private static string PlaceholderSignature(string value) {
    MatchCollection matches = Regex.Matches(value, @"\{\d+\}");
    string[] placeholders = new string[matches.Count];
    for (int index = 0; index < matches.Count; index++) placeholders[index] = matches[index].Value;
    Array.Sort(placeholders, StringComparer.Ordinal);
    return string.Join(",", placeholders);
  }
}
