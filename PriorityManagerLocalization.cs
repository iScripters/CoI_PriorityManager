using System;
using System.Collections.Generic;
using System.Globalization;
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
  private const string Comment = "Priority Manager UI text.";

  public static readonly LocStr WindowTitle = Loc.Str("PriorityManager.WindowTitle", "Priority Manager", Comment);
  public static readonly LocStr TabOverall = Loc.Str("PriorityManager.TabOverall", "Overall", Comment);
  public static readonly LocStr TabGroups = Loc.Str("PriorityManager.TabGroups", "Groups", Comment);
  public static readonly LocStr IndividualPriorityGroupWarning = Loc.Str("PriorityManager.IndividualPriorityGroupWarning", "Changing an individual priority removes that building from its group.", Comment);
  public static readonly LocStr ResetAllWarning = Loc.Str("PriorityManager.ResetAllWarning", "Resetting clears group membership and restores game defaults.", Comment);
  public static readonly LocStr RemoveGroupsTooltip = Loc.Str("PriorityManager.RemoveGroupsTooltip", "Remove all group memberships after resetting.", Comment);
  public static readonly LocStr RemoveGroups = Loc.Str("PriorityManager.RemoveGroups", "Remove groups", Comment);
  public static readonly LocStr OnlyUngroupedTooltip = Loc.Str("PriorityManager.OnlyUngroupedTooltip", "Reset only buildings that are not currently assigned to a group.", Comment);
  public static readonly LocStr OnlyUngrouped = Loc.Str("PriorityManager.OnlyUngrouped", "Only ungrouped", Comment);
  public static readonly LocStr ResetAllPriorities = Loc.Str("PriorityManager.ResetAllPriorities", "Reset all priorities", Comment);
  public static readonly LocStr FilterBuildings = Loc.Str("PriorityManager.FilterBuildings", "Filter buildings", Comment);
  public static readonly LocStr Sort = Loc.Str("PriorityManager.Sort", "Sort", Comment);
  public static readonly LocStr ReverseSortTooltip = Loc.Str("PriorityManager.ReverseSortTooltip", "Reverse the selected sort order.", Comment);
  public static readonly LocStr Descending = Loc.Str("PriorityManager.Descending", "Descending", Comment);
  public static readonly LocStr Previous = Loc.Str("PriorityManager.Previous", "Previous", Comment);
  public static readonly LocStr Next = Loc.Str("PriorityManager.Next", "Next", Comment);
  public static readonly LocStr NewGroupName = Loc.Str("PriorityManager.NewGroupName", "New group name", Comment);
  public static readonly LocStr CreateGroup = Loc.Str("PriorityManager.CreateGroup", "Create group", Comment);
  public static readonly LocStr NoPriorityCapableBuildings = Loc.Str("PriorityManager.NoPriorityCapableBuildings", "No priority-capable buildings found.", Comment);
  public static readonly LocStr Reset = Loc.Str("PriorityManager.Reset", "Reset", Comment);
  public static readonly LocStr NoGroup = Loc.Str("PriorityManager.NoGroup", "No group", Comment);
  public static readonly LocStr General = Loc.Str("PriorityManager.General", "General", Comment);
  public static readonly LocStr Import = Loc.Str("PriorityManager.Import", "Import", Comment);
  public static readonly LocStr Export = Loc.Str("PriorityManager.Export", "Export", Comment);
  public static readonly LocStr Generator = Loc.Str("PriorityManager.Generator", "Generator", Comment);
  public static readonly LocStr CreateGroupThenAssign = Loc.Str("PriorityManager.CreateGroupThenAssign", "Create a group, then assign buildings from the Overall tab.", Comment);
  public static readonly LocStr AddBuildings = Loc.Str("PriorityManager.AddBuildings", "Add buildings", Comment);
  public static readonly LocStr RemoveBuildings = Loc.Str("PriorityManager.RemoveBuildings", "Remove buildings", Comment);
  public static readonly LocStr Remove = Loc.Str("PriorityManager.Remove", "Remove", Comment);
  public static readonly LocStr Delete = Loc.Str("PriorityManager.Delete", "Delete", Comment);
  public static readonly LocStr NoBuildingsAssigned = Loc.Str("PriorityManager.NoBuildingsAssigned", "No buildings assigned.", Comment);
  public static readonly LocStr Name = Loc.Str("PriorityManager.Name", "Name", Comment);
  public static readonly LocStr Priority = Loc.Str("PriorityManager.Priority", "Priority", Comment);
  public static readonly LocStr Group = Loc.Str("PriorityManager.Group", "Group", Comment);
  public static readonly LocStr Type = Loc.Str("PriorityManager.Type", "Type", Comment);
  public static readonly LocStr CreatePriorityGroup = Loc.Str("PriorityManager.CreatePriorityGroup", "Create priority group", Comment);
  public static readonly LocStr GroupName = Loc.Str("PriorityManager.GroupName", "Group name", Comment);
  public static readonly LocStr NewGroup = Loc.Str("PriorityManager.NewGroup", "New group", Comment);
  public static readonly LocStr ConfirmGroupName = Loc.Str("PriorityManager.ConfirmGroupName", "Confirm the name with the checkmark.", Comment);
  public static readonly LocStr Cancel = Loc.Str("PriorityManager.Cancel", "Cancel", Comment);
  public static readonly LocStr PriorityGroup = Loc.Str("PriorityManager.PriorityGroup", "Priority group", Comment);
  public static readonly LocStr RestoreGameDefaults = Loc.Str("PriorityManager.RestoreGameDefaults", "Restore game defaults", Comment);
  public static readonly LocStr ResetPriorities = Loc.Str("PriorityManager.ResetPriorities", "Reset priorities", Comment);
  public static readonly LocStr ResetScopeAll = Loc.Str("PriorityManager.ResetScopeAll", "buildings", Comment);
  public static readonly LocStr ResetScopeUngrouped = Loc.Str("PriorityManager.ResetScopeUngrouped", "ungrouped buildings", Comment);
  public static readonly LocStr ResetGroupsCleared = Loc.Str("PriorityManager.ResetGroupsCleared", "cleared all groups", Comment);
  public static readonly LocStr ResetGroupsKept = Loc.Str("PriorityManager.ResetGroupsKept", "kept group memberships", Comment);
  public static readonly LocStr UnnamedGroup = Loc.Str("PriorityManager.UnnamedGroup", "Unnamed group", Comment);

  public static readonly LocStr1 ShowBuildings = Loc.Str1("PriorityManager.ShowBuildings", "Show buildings ({0})", Comment);
  public static readonly LocStr1 HideBuildings = Loc.Str1("PriorityManager.HideBuildings", "Hide buildings ({0})", Comment);
  public static readonly LocStr1 StatusCreatedGroup = Loc.Str1("PriorityManager.StatusCreatedGroup", "Created group '{0}'.", Comment);
  public static readonly LocStr1 StatusRenamedGroup = Loc.Str1("PriorityManager.StatusRenamedGroup", "Renamed group to '{0}'.", Comment);
  public static readonly LocStr1 StatusDeletedGroup = Loc.Str1("PriorityManager.StatusDeletedGroup", "Deleted '{0}' and reset its members to their defaults.", Comment);
  public static readonly LocStr1 StatusDragToAdd = Loc.Str1("PriorityManager.StatusDragToAdd", "Drag over buildings to add them to '{0}'. Press Escape to cancel.", Comment);
  public static readonly LocStr1 StatusDragToRemove = Loc.Str1("PriorityManager.StatusDragToRemove", "Drag over buildings to remove them from '{0}'. Red highlights show eligible members. Press Escape to cancel.", Comment);
  public static readonly LocStr1 StatusResetBuilding = Loc.Str1("PriorityManager.StatusResetBuilding", "Reset {0} to its game defaults.", Comment);
  public static readonly LocStr2 StatusRemovedFromGroup = Loc.Str2("PriorityManager.StatusRemovedFromGroup", "Removed {0} from '{1}' and reset its priorities.", Comment);
  public static readonly LocStr2 StatusAssignedToGroup = Loc.Str2("PriorityManager.StatusAssignedToGroup", "Assigned {0} to '{1}'.", Comment);
  public static readonly LocStr2 StatusAddedToGroup = Loc.Str2("PriorityManager.StatusAddedToGroup", "Added {0} buildings to '{1}' and applied its priorities.", Comment);
  public static readonly LocStr2 StatusRemovedFromGroupCount = Loc.Str2("PriorityManager.StatusRemovedFromGroupCount", "Removed {0} buildings from '{1}' and reset their priorities.", Comment);
  public static readonly LocStr2 StatusChangedPriority = Loc.Str2("PriorityManager.StatusChangedPriority", "Changed {0}; it was removed from '{1}'.", Comment);
  public static readonly LocStr2 StatusUpdatedGroupPriority = Loc.Str2("PriorityManager.StatusUpdatedGroupPriority", "Updated {0} priority for '{1}'.", Comment);
  public static readonly LocStr3 OverallPageRange = Loc.Str3("PriorityManager.OverallPageRange", "{0}-{1} of {2}", Comment);
  public static readonly LocStr3 StatusResetAll = Loc.Str3("PriorityManager.StatusResetAll", "Reset priorities for {0} {1} and {2}.", Comment);

  private static readonly IReadOnlyDictionary<PriorityManagerText, LocStr> Texts = new Dictionary<PriorityManagerText, LocStr> {
    { PriorityManagerText.WindowTitle, WindowTitle }, { PriorityManagerText.TabOverall, TabOverall }, { PriorityManagerText.TabGroups, TabGroups },
    { PriorityManagerText.IndividualPriorityGroupWarning, IndividualPriorityGroupWarning }, { PriorityManagerText.ResetAllWarning, ResetAllWarning }, { PriorityManagerText.RemoveGroupsTooltip, RemoveGroupsTooltip },
    { PriorityManagerText.RemoveGroups, RemoveGroups }, { PriorityManagerText.OnlyUngroupedTooltip, OnlyUngroupedTooltip }, { PriorityManagerText.OnlyUngrouped, OnlyUngrouped },
    { PriorityManagerText.ResetAllPriorities, ResetAllPriorities }, { PriorityManagerText.FilterBuildings, FilterBuildings }, { PriorityManagerText.Sort, Sort },
    { PriorityManagerText.ReverseSortTooltip, ReverseSortTooltip }, { PriorityManagerText.Descending, Descending }, { PriorityManagerText.Previous, Previous }, { PriorityManagerText.Next, Next },
    { PriorityManagerText.NewGroupName, NewGroupName }, { PriorityManagerText.CreateGroup, CreateGroup }, { PriorityManagerText.NoPriorityCapableBuildings, NoPriorityCapableBuildings },
    { PriorityManagerText.Reset, Reset }, { PriorityManagerText.NoGroup, NoGroup }, { PriorityManagerText.General, General }, { PriorityManagerText.Import, Import },
    { PriorityManagerText.Export, Export }, { PriorityManagerText.Generator, Generator }, { PriorityManagerText.CreateGroupThenAssign, CreateGroupThenAssign },
    { PriorityManagerText.AddBuildings, AddBuildings }, { PriorityManagerText.RemoveBuildings, RemoveBuildings }, { PriorityManagerText.Remove, Remove }, { PriorityManagerText.Delete, Delete },
    { PriorityManagerText.NoBuildingsAssigned, NoBuildingsAssigned }, { PriorityManagerText.Name, Name }, { PriorityManagerText.Priority, Priority }, { PriorityManagerText.Group, Group },
    { PriorityManagerText.Type, Type }, { PriorityManagerText.CreatePriorityGroup, CreatePriorityGroup }, { PriorityManagerText.GroupName, GroupName }, { PriorityManagerText.NewGroup, NewGroup },
    { PriorityManagerText.ConfirmGroupName, ConfirmGroupName }, { PriorityManagerText.Cancel, Cancel }, { PriorityManagerText.PriorityGroup, PriorityGroup },
    { PriorityManagerText.RestoreGameDefaults, RestoreGameDefaults }, { PriorityManagerText.ResetPriorities, ResetPriorities }, { PriorityManagerText.ResetScopeAll, ResetScopeAll },
    { PriorityManagerText.ResetScopeUngrouped, ResetScopeUngrouped }, { PriorityManagerText.ResetGroupsCleared, ResetGroupsCleared }, { PriorityManagerText.ResetGroupsKept, ResetGroupsKept },
    { PriorityManagerText.UnnamedGroup, UnnamedGroup }
  };

  public LocStrFormatted Text(PriorityManagerText key) {
    if (!Texts.TryGetValue(key, out LocStr text)) throw new ArgumentOutOfRangeException(nameof(key), key, "This localized text requires format arguments.");
    return text;
  }

  public string Value(PriorityManagerText key) {
    return Text(key).Value;
  }

  public LocStrFormatted Format(PriorityManagerText key, params object[] values) {
    switch (key) {
      case PriorityManagerText.ShowBuildings: return ShowBuildings.Format(ValueAt(values, 0));
      case PriorityManagerText.HideBuildings: return HideBuildings.Format(ValueAt(values, 0));
      case PriorityManagerText.StatusCreatedGroup: return StatusCreatedGroup.Format(ValueAt(values, 0));
      case PriorityManagerText.StatusRenamedGroup: return StatusRenamedGroup.Format(ValueAt(values, 0));
      case PriorityManagerText.StatusDeletedGroup: return StatusDeletedGroup.Format(ValueAt(values, 0));
      case PriorityManagerText.StatusDragToAdd: return StatusDragToAdd.Format(ValueAt(values, 0));
      case PriorityManagerText.StatusDragToRemove: return StatusDragToRemove.Format(ValueAt(values, 0));
      case PriorityManagerText.StatusResetBuilding: return StatusResetBuilding.Format(ValueAt(values, 0));
      case PriorityManagerText.StatusRemovedFromGroup: return StatusRemovedFromGroup.Format(ValueAt(values, 0), ValueAt(values, 1));
      case PriorityManagerText.StatusAssignedToGroup: return StatusAssignedToGroup.Format(ValueAt(values, 0), ValueAt(values, 1));
      case PriorityManagerText.StatusAddedToGroup: return StatusAddedToGroup.Format(ValueAt(values, 0), ValueAt(values, 1));
      case PriorityManagerText.StatusRemovedFromGroupCount: return StatusRemovedFromGroupCount.Format(ValueAt(values, 0), ValueAt(values, 1));
      case PriorityManagerText.StatusChangedPriority: return StatusChangedPriority.Format(ValueAt(values, 0), ValueAt(values, 1));
      case PriorityManagerText.StatusUpdatedGroupPriority: return StatusUpdatedGroupPriority.Format(ValueAt(values, 0), ValueAt(values, 1));
      case PriorityManagerText.OverallPageRange: return OverallPageRange.Format(ValueAt(values, 0), ValueAt(values, 1), ValueAt(values, 2));
      case PriorityManagerText.StatusResetAll: return StatusResetAll.Format(ValueAt(values, 0), ValueAt(values, 1), ValueAt(values, 2));
      default: throw new ArgumentOutOfRangeException(nameof(key), key, "This localized text does not accept format arguments.");
    }
  }

  private static string ValueAt(object[] values, int index) {
    if (index >= values.Length) throw new ArgumentException("Not enough format arguments.", nameof(values));
    return Convert.ToString(values[index], LocalizationManager.CurrentCultureInfo) ?? "";
  }
}
