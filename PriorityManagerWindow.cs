using System;
using System.Collections.Generic;
using Mafi;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Static;
using Mafi.Core.Prototypes;
using Mafi.Localization;
using Mafi.Unity.Camera;
using Mafi.Unity.Entities;
using Mafi.Unity.Ui;
using Mafi.Unity.Ui.Library.Inspectors;
using Mafi.Unity.UiToolkit;
using Mafi.Unity.UiToolkit.Component;
using Mafi.Unity.UiToolkit.Library;
using UQueryExtensions = UnityEngine.UIElements.UQueryExtensions;

namespace PriorityManager;

public enum OverallSort {
  Name,
  Priority,
  Group,
  Type
}

[GlobalDependency(RegistrationMode.AsEverything)]
public sealed class PriorityManagerWindow : Window {
  private const int OverallPageSize = 50;
  private readonly PriorityGroupStore groups;
  private readonly BuildingPriorityService priorities;
  private readonly GroupBuildingSelectionController buildingSelection;
  private readonly CameraController camera;
  private readonly InspectorsManager inspectors;
  private readonly EntitiesRenderingManager entitiesRendering;
  private readonly Column overallRows = new Column().AlignItemsStretch();
  private readonly Column groupRows = new Column().AlignItemsStretch();
  private readonly Label overallStatus = new Label();
  private readonly Label groupStatus = new Label();
  private readonly Label overallPageLabel = new Label();
  private readonly TextField overallFilter = new TextField();
  private readonly Dropdown<OverallSort> overallSort;
  private readonly TextField newGroupName = new TextField();
  private readonly TabContainer tabs;
  private readonly HashSet<string> expandedGroupIds = new HashSet<string>();
  private readonly Dictionary<int, int> prioritySortValues = new Dictionary<int, int>();
  private readonly List<ulong> groupHighlightIds = new List<ulong>();
  private List<IStaticEntity>? overallBuildings;
  private int overallPage;
  private int prioritySortCheckFrames;
  private bool removeGroupsOnReset = true;
  private bool resetUngroupedOnly;

  public PriorityManagerWindow(
    PriorityGroupStore groups,
    BuildingPriorityService priorities,
    GroupBuildingSelectionController buildingSelection,
    CameraController camera,
    InspectorsManager inspectors,
    EntitiesRenderingManager entitiesRendering)
    : base(new LocStrFormatted("Priority Manager")) {
    this.groups = groups;
    this.priorities = priorities;
    this.buildingSelection = buildingSelection;
    this.camera = camera;
    this.inspectors = inspectors;
    this.entitiesRendering = entitiesRendering;
    buildingSelection.BuildingsAdded += OnBuildingsAdded;
    buildingSelection.BuildingsRemoved += OnBuildingsRemoved;
    OnCloseStart += _ => ClearGroupHighlights();

    WindowSize(new Px(1000), new Px(720));
    MakeMovableAndEnablePositionSaving();

    overallSort = new Dropdown<OverallSort>((value, index, inDropdown) => new Label(new LocStrFormatted(SortName(value))));
    overallSort.SetOptions(OverallSort.Name, OverallSort.Priority, OverallSort.Group, OverallSort.Type);
    overallSort.SetValue(OverallSort.Name);
    overallSort.OnValueChanged((value, index) => ChangeOverallSort(value));

    tabs = new TabContainer();
    tabs.Add(new LocStrFormatted("Overall"), BuildOverallTab());
    tabs.Add(new LocStrFormatted("Groups"), BuildGroupsTab());
    tabs.OnTabActivate(RefreshActiveTab);
    Body.Add(tabs);
  }

  public void Refresh() {
    groups.PruneMissing(id => priorities.TryGetBuilding(id, out _));
    overallBuildings = null;
    prioritySortValues.Clear();
    expandedGroupIds.Clear();
    RefreshActiveTab();
  }

  public void RefreshAfterBuildingSelection() {
    groups.PruneMissing(id => priorities.TryGetBuilding(id, out _));
    overallBuildings = null;
    prioritySortValues.Clear();
    RefreshActiveTab();
  }

  public void RefreshPrioritySortIfChanged() {
    if (tabs.ActiveTabIndex != 0 || overallSort.SelectedValue != OverallSort.Priority || overallBuildings == null) return;
    if (++prioritySortCheckFrames < 10) return;
    prioritySortCheckFrames = 0;

    int visibleCount = 0;
    string filter = overallFilter.GetText().Trim();
    foreach (IStaticEntity building in overallBuildings) {
      if (building.IsDestroyed || !MatchesFilter(building, filter)) continue;
      visibleCount++;
      if (!prioritySortValues.TryGetValue(building.Id.Value, out int previous)
        || previous != GeneralPriorityForSort(building)) {
        RebuildOverallRows();
        return;
      }
    }
    if (visibleCount != prioritySortValues.Count) RebuildOverallRows();
  }

  private void RefreshActiveTab() {
    if (tabs.ActiveTabIndex == 1) RebuildGroupRows();
    else RebuildOverallRows();
  }

  private UiComponent BuildOverallTab() {
    Column page = new Column(2.pt()).AlignItemsStretch();
    Panel controlsPanel = new Panel(noBolts: true).ReducedPadding().BodyGap(2.pt()).AlignSelfStretch();
    controlsPanel.Body.Add(new Label(new LocStrFormatted("Changing an individual priority removes that building from its group.")));
    controlsPanel.Body.Add(overallStatus);
    Row resetRow = new Row(2.pt()).AlignItemsCenter();
    resetRow.Add(new Label(new LocStrFormatted("Resetting clears group membership and restores game defaults.")).FlexGrow(1f));
    Toggle removeGroupsToggle = new Toggle(standalone: true).Value(removeGroupsOnReset).OnValueChanged(value => removeGroupsOnReset = value);
    resetRow.Add(removeGroupsToggle.Tooltip(new LocStrFormatted("Remove all group memberships after resetting."), enabled: true, isError: false, openBelow: false));
    resetRow.Add(new Label(new LocStrFormatted("Remove groups")));
    Toggle ungroupedOnlyToggle = new Toggle(standalone: true).Value(resetUngroupedOnly).OnValueChanged(value => resetUngroupedOnly = value);
    resetRow.Add(ungroupedOnlyToggle.Tooltip(new LocStrFormatted("Reset only buildings that are not currently assigned to a group."), enabled: true, isError: false, openBelow: false));
    resetRow.Add(new Label(new LocStrFormatted("Only ungrouped")));
    resetRow.Add(new ButtonText(Button.Danger, new LocStrFormatted("Reset all priorities"), ResetAllBuildings));
    controlsPanel.Body.Add(resetRow);
    Row filterRow = new Row(2.pt()).AlignItemsCenter();
    overallFilter.CharLimit(80).Placeholder(new LocStrFormatted("Filter buildings")).FlexGrow(1f).MinWidth(300.px())
      .OnValueChanged(_ => FilterOverallBuildings());
    filterRow.Add(overallFilter);
    filterRow.Add(new Label(new LocStrFormatted("Sort")));
    filterRow.Add(overallSort.NoShrink().MinWidth(150.px()));
    controlsPanel.Body.Add(filterRow);
    Row navigation = new Row(2.pt()).AlignItemsCenter();
    navigation.Add(new ButtonText(Button.General, new LocStrFormatted("Previous"), PreviousOverallPage));
    navigation.Add(overallPageLabel);
    navigation.Add(new ButtonText(Button.General, new LocStrFormatted("Next"), NextOverallPage));
    controlsPanel.Body.Add(navigation);
    page.Add(controlsPanel);
    page.Add(overallRows);
    return page;
  }

  private UiComponent BuildGroupsTab() {
    Column page = new Column(2.pt()).AlignItemsStretch();
    Panel createPanel = new Panel(noBolts: true).ReducedPadding().AlignSelfStretch();
    Row createRow = new Row(2.pt()).AlignItemsCenter();
    newGroupName.CharLimit(64).Placeholder(new LocStrFormatted("New group name")).FlexGrow(1f).MinWidth(300.px())
      .OnReturn(CreateGroupAndKeepEditing);
    createRow.Add(newGroupName);
    createRow.Add(new ButtonText(Button.Primary, new LocStrFormatted("Create group"), CreateGroup));
    createPanel.Body.Add(createRow);
    createPanel.Body.Add(groupStatus);
    page.Add(createPanel);
    page.Add(groupRows);
    return page;
  }

  private void RebuildOverallRows() {
    RemoveChildren(overallRows);
    List<IStaticEntity> buildings = FilterAndSortOverallBuildings();

    if (buildings.Count == 0) {
      overallPage = 0;
      prioritySortValues.Clear();
      overallPageLabel.Value(new LocStrFormatted("0 buildings"));
      overallRows.Add(new Label(new LocStrFormatted("No priority-capable buildings found.")));
      return;
    }

    int pageCount = (buildings.Count + OverallPageSize - 1) / OverallPageSize;
    overallPage = Math.Max(0, Math.Min(overallPage, pageCount - 1));
    int firstIndex = overallPage * OverallPageSize;
    int lastIndex = Math.Min(firstIndex + OverallPageSize, buildings.Count);
    overallPageLabel.Value(new LocStrFormatted($"{firstIndex + 1}-{lastIndex} of {buildings.Count}"));
    for (int index = firstIndex; index < lastIndex; index++) overallRows.Add(BuildBuildingRow(buildings[index]));
    CapturePrioritySortValues(buildings);
  }

  private List<IStaticEntity> FilterAndSortOverallBuildings() {
    if (overallBuildings == null) overallBuildings = new List<IStaticEntity>(priorities.GetPriorityBuildings());

    string filter = overallFilter.GetText().Trim();
    List<IStaticEntity> result = new List<IStaticEntity>();
    foreach (IStaticEntity building in overallBuildings) {
      if (!building.IsDestroyed && MatchesFilter(building, filter)) result.Add(building);
    }
    result.Sort(CompareBuildings);
    return result;
  }

  private bool MatchesFilter(IStaticEntity building, string filter) {
    if (string.IsNullOrEmpty(filter)) return true;
    PriorityGroup? group = groups.GetForEntity(building.Id.Value);
    return BuildingName(building).Value.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0
      || BuildingType(building).IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0
      || (group != null && group.Name.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0);
  }

  private int CompareBuildings(IStaticEntity left, IStaticEntity right) {
    switch (overallSort.SelectedValue) {
      case OverallSort.Priority:
        int priorityComparison = GeneralPriorityForSort(left).CompareTo(GeneralPriorityForSort(right));
        if (priorityComparison != 0) return priorityComparison;
        break;
      case OverallSort.Group:
        int groupComparison = string.Compare(GroupName(left), GroupName(right), StringComparison.CurrentCultureIgnoreCase);
        if (groupComparison != 0) return groupComparison;
        break;
      case OverallSort.Type:
        int typeComparison = string.Compare(BuildingType(left), BuildingType(right), StringComparison.CurrentCultureIgnoreCase);
        if (typeComparison != 0) return typeComparison;
        break;
    }
    return string.Compare(BuildingName(left).Value, BuildingName(right).Value, StringComparison.CurrentCultureIgnoreCase);
  }

  private int GeneralPriorityForSort(IStaticEntity building) {
    return priorities.Supports(building, PriorityControl.General) ? priorities.Get(building, PriorityControl.General) : int.MaxValue;
  }

  private string GroupName(IStaticEntity building) {
    return groups.GetForEntity(building.Id.Value)?.Name ?? "";
  }

  private void FilterOverallBuildings() {
    overallPage = 0;
    RebuildOverallRows();
  }

  private void ChangeOverallSort(OverallSort sort) {
    overallPage = 0;
    prioritySortCheckFrames = 0;
    RebuildOverallRows();
  }

  private void CapturePrioritySortValues(List<IStaticEntity> buildings) {
    prioritySortValues.Clear();
    if (overallSort.SelectedValue != OverallSort.Priority) return;
    foreach (IStaticEntity building in buildings) prioritySortValues.Add(building.Id.Value, GeneralPriorityForSort(building));
  }

  private UiComponent BuildBuildingRow(IStaticEntity building) {
    Panel card = new Panel(noBolts: true).ReducedPadding().BodyGap(2.pt()).MarginBottom(2.pt()).AlignSelfStretch();
    Row header = new Row(2.pt()).AlignItemsCenter();
    Icon icon = new Icon(GetIconPath(building)).Size(48.px()).OnClick(() => Focus(building.Id.Value));
    header.Add(icon);
    header.Add(new Label(BuildingName(building)).FlexGrow(1f));
    header.Add(new ButtonText(Button.Danger, new LocStrFormatted("Reset"), () => ResetBuilding(building.Id.Value)));

    PriorityGroup? currentGroup = groups.GetForEntity(building.Id.Value);
    Dropdown<PriorityGroup> groupDropdown = new Dropdown<PriorityGroup>(
      (group, index, inDropdown) => new Label(new LocStrFormatted(group?.Name ?? "No group")));
    groupDropdown.IncludeClearOption(new LocStrFormatted("No group"), null!);
    groupDropdown.SetOptions(groups.Groups);
    groupDropdown.SetValue(currentGroup!);
    groupDropdown.OnValueChanged((selected, index) => ChangeGroup(building.Id.Value, selected));
    header.Add(groupDropdown.NoShrink().MinWidth(180.px()));
    card.Body.Add(header);

    Row controls = new Row(2.pt()).AlignItemsEnd();
    AddIndividualControl(controls, building, PriorityControl.General, "General");
    AddIndividualControl(controls, building, PriorityControl.Import, "Import");
    AddIndividualControl(controls, building, PriorityControl.Export, "Export");
    AddIndividualControl(controls, building, PriorityControl.Generator, "Generator");
    card.Body.Add(controls);
    return card;
  }

  private void AddIndividualControl(Row parent, IStaticEntity building, PriorityControl control, string title) {
    if (!priorities.Supports(building, control)) return;
    Column item = new Column(1.pt()).AlignItemsCenter();
    item.Add(new Label(new LocStrFormatted(title)));
    PriorityDropdown dropdown = CreatePriorityDropdown(priorities.Get(building, control));
    dropdown.OnValueChanged((value, index) => ChangeIndividualPriority(building.Id.Value, control, value));
    item.Add(dropdown);
    parent.Add(item);
  }

  private void RebuildGroupRows() {
    ClearGroupHighlights();
    RemoveChildren(groupRows);
    if (groups.Groups.Count == 0) {
      groupRows.Add(new Label(new LocStrFormatted("Create a group, then assign buildings from the Overall tab.")));
      return;
    }
    foreach (PriorityGroup group in groups.Groups) groupRows.Add(BuildGroupCard(group));
  }

  private UiComponent BuildGroupCard(PriorityGroup group) {
    Panel card = new Panel(noBolts: false).ReducedPadding().BodyGap(2.pt()).MarginBottom(2.pt()).AlignSelfStretch();
    card.OnMouseEnterLeave(() => HighlightGroupMembers(group), ClearGroupHighlights);
    Row titleRow = new Row(2.pt()).AlignItemsCenter();
    TextField name = new TextField().Text(group.Name).CharLimit(64).OnEditEnd(value => RenameGroup(group, value)).FlexGrow(1f).MinWidth(300.px());
    titleRow.Add(name);
    if (group.MemberIds.Count > 0) {
      bool membersVisible = expandedGroupIds.Contains(group.Id);
      string membersButtonText = membersVisible ? $"Hide buildings ({group.MemberIds.Count})" : $"Show buildings ({group.MemberIds.Count})";
      titleRow.Add(new ButtonText(Button.General, new LocStrFormatted(membersButtonText), () => ToggleGroupMembers(group)));
    }
    titleRow.Add(new ButtonText(Button.Primary, new LocStrFormatted("Add buildings"), () => StartBuildingSelection(group)));
    titleRow.Add(new ButtonText(Button.Danger, new LocStrFormatted("Remove buildings"), () => StartBuildingRemoval(group)));
    titleRow.Add(new ButtonText(Button.Danger, new LocStrFormatted("Delete"), () => DeleteGroup(group)));
    card.Body.Add(titleRow);

    Row controls = new Row(2.pt()).AlignItemsEnd();
    AddGroupControl(controls, group, PriorityControl.General, "General");
    AddGroupControl(controls, group, PriorityControl.Import, "Import");
    AddGroupControl(controls, group, PriorityControl.Export, "Export");
    AddGroupControl(controls, group, PriorityControl.Generator, "Generator");
    card.Body.Add(controls);

    if (group.MemberIds.Count == 0) {
      card.Body.Add(new Label(new LocStrFormatted("No buildings assigned.")));
    } else if (expandedGroupIds.Contains(group.Id)) {
      foreach (int memberId in new List<int>(group.MemberIds)) {
        if (priorities.TryGetBuilding(memberId, out IStaticEntity building)) card.Body.Add(BuildGroupMemberRow(group, building));
      }
    }
    return card;
  }

  private void AddGroupControl(Row parent, PriorityGroup group, PriorityControl control, string title) {
    Column item = new Column(1.pt()).AlignItemsCenter();
    item.Add(new Label(new LocStrFormatted(title)));
    PriorityDropdown dropdown = CreatePriorityDropdown(BuildingPriorityService.GetGroupPriority(group, control));
    dropdown.OnValueChanged((value, index) => {
      ChangeGroupPriority(group, control, value);
      dropdown.SetValue(BuildingPriorityService.GetGroupPriority(group, control));
    });
    item.Add(dropdown);
    parent.Add(item);
  }

  private UiComponent BuildGroupMemberRow(PriorityGroup group, IStaticEntity building) {
    PanelRow row = new PanelRow(2.pt(), noBolts: true).AlignSelfStretch();
    row.Body.Add(new Icon(GetIconPath(building)).Size(30.px()).OnClick(() => Focus(building.Id.Value)));
    row.Body.Add(new Label(BuildingName(building)).FlexGrow(1f));
    row.Body.Add(new ButtonText(Button.Danger, new LocStrFormatted("Remove"), () => RemoveFromGroup(group, building.Id.Value)));
    return row;
  }

  private static PriorityDropdown CreatePriorityDropdown(int selectedValue) {
    PriorityDropdown dropdown = PriorityDropdown.Create(PriorityDropdown.GeneralPriorityFactory, Button.General, 0, 14);
    dropdown.SetValue(selectedValue);
    return dropdown;
  }

  private void CreateGroup() {
    CreateGroup(rebuildRows: true);
  }

  private void CreateGroupAndKeepEditing() {
    CreateGroup(rebuildRows: false);
    newGroupName.Schedule.Execute(() => {
      // Return moves focus to the field's composite root; the inner text element receives characters.
      UnityEngine.UIElements.TextElement? input = UQueryExtensions.Q<UnityEngine.UIElements.TextElement>(newGroupName.RootElement);
      if (input != null) input.Focus();
      else newGroupName.Focus();
      newGroupName.MoveCaretToEnd();
    });
  }

  private void CreateGroup(bool rebuildRows) {
    PriorityGroup group = groups.Create(newGroupName.GetText());
    newGroupName.ClearValue();
    SetStatus($"Created group '{group.Name}'.");
    if (rebuildRows) RebuildGroupRows();
    else {
      if (groups.Groups.Count == 1) RemoveChildren(groupRows);
      groupRows.Add(BuildGroupCard(group));
    }
  }

  private void RenameGroup(PriorityGroup group, string name) {
    groups.Rename(group, name);
    SetStatus($"Renamed group to '{group.Name}'.");
  }

  private void DeleteGroup(PriorityGroup group) {
    foreach (int memberId in group.MemberIds) {
      if (priorities.TryGetBuilding(memberId, out IStaticEntity building)) priorities.ResetAll(building);
    }
    groups.Delete(group);
    expandedGroupIds.Remove(group.Id);
    SetStatus($"Deleted '{group.Name}' and reset its members to their defaults.");
    RebuildGroupRows();
  }

  private void ChangeGroup(int entityId, PriorityGroup? selectedGroup) {
    PriorityGroup? currentGroup = groups.GetForEntity(entityId);
    if (!priorities.TryGetBuilding(entityId, out IStaticEntity building)) return;

    if (selectedGroup == null) {
      if (currentGroup == null) return;
      groups.Remove(entityId);
      priorities.ResetAll(building);
      SetStatus($"Removed {BuildingName(building).Value} from '{currentGroup.Name}' and reset its priorities.");
    } else {
      groups.Assign(entityId, selectedGroup);
      priorities.ApplyGroupToBuilding(selectedGroup, building);
      SetStatus($"Assigned {BuildingName(building).Value} to '{selectedGroup.Name}'.");
    }
    RebuildOverallRows();
  }

  private void RemoveFromGroup(PriorityGroup group, int entityId) {
    if (!priorities.TryGetBuilding(entityId, out IStaticEntity building)) return;
    groups.Remove(entityId);
    priorities.ResetAll(building);
    SetStatus($"Removed {BuildingName(building).Value} from '{group.Name}' and reset its priorities.");
    RebuildGroupRows();
  }

  private void ToggleGroupMembers(PriorityGroup group) {
    if (!expandedGroupIds.Add(group.Id)) expandedGroupIds.Remove(group.Id);
    RebuildGroupRows();
  }

  private void StartBuildingSelection(PriorityGroup group) {
    buildingSelection.Start(group, remove: false);
    SetStatus($"Drag over buildings to add them to '{group.Name}'. Press Escape to cancel.");
  }

  private void StartBuildingRemoval(PriorityGroup group) {
    buildingSelection.Start(group, remove: true);
    SetStatus($"Drag over buildings to remove them from '{group.Name}'. Red highlights show eligible members. Press Escape to cancel.");
  }

  private void OnBuildingsAdded(PriorityGroup group, int count) {
    SetStatus($"Added {count} buildings to '{group.Name}' and applied its priorities.");
    RebuildGroupRows();
  }

  private void OnBuildingsRemoved(PriorityGroup group, int count) {
    SetStatus($"Removed {count} buildings from '{group.Name}' and reset their priorities.");
    RebuildGroupRows();
  }

  private void HighlightGroupMembers(PriorityGroup group) {
    ClearGroupHighlights();
    foreach (int memberId in group.MemberIds) {
      if (priorities.TryGetBuilding(memberId, out IStaticEntity building) && building is IRenderedEntity rendered) {
        groupHighlightIds.Add(entitiesRendering.AddHighlight(rendered, ColorRgba.CornflowerBlue));
      }
    }
  }

  private void ClearGroupHighlights() {
    foreach (ulong highlightId in groupHighlightIds) entitiesRendering.RemoveHighlight(highlightId);
    groupHighlightIds.Clear();
  }

  private void ChangeIndividualPriority(int entityId, PriorityControl control, int priority) {
    if (!priorities.TryGetBuilding(entityId, out IStaticEntity building)) return;
    PriorityGroup? group = groups.GetForEntity(entityId);
    if (group != null) {
      groups.Remove(entityId);
      SetStatus($"Changed {BuildingName(building).Value}; it was removed from '{group.Name}'.");
    }
    priorities.Set(building, control, priority);
    if (group != null) RebuildOverallRows();
  }

  private void ChangeGroupPriority(PriorityGroup group, PriorityControl control, int priority) {
    BuildingPriorityService.SetGroupPriority(group, control, priority);
    groups.Save();
    priorities.ApplyGroupControl(group, control);
    SetStatus($"Updated {ControlName(control)} priority for '{group.Name}'.");
  }

  private bool GroupSupports(PriorityGroup group, PriorityControl control) {
    foreach (int memberId in group.MemberIds) {
      if (priorities.TryGetBuilding(memberId, out IStaticEntity building) && priorities.SupportsGroupControl(building, control)) return true;
    }
    return false;
  }

  private void PreviousOverallPage() {
    if (overallPage == 0) return;
    overallPage--;
    RebuildOverallRows();
  }

  private void NextOverallPage() {
    overallPage++;
    RebuildOverallRows();
  }

  private void Focus(int entityId) {
    if (!priorities.TryGetBuilding(entityId, out IStaticEntity building)) return;
    camera.PanTo(building.Position2f);
    inspectors.TryActivateFor(building);
  }

  private void SetStatus(string message) {
    LocStrFormatted text = new LocStrFormatted(message);
    overallStatus.Value(text);
    groupStatus.Value(text);
  }

  private void ResetBuilding(int entityId) {
    if (!priorities.TryGetBuilding(entityId, out IStaticEntity building)) return;
    if (groups.GetForEntity(entityId) != null) groups.Remove(entityId);
    priorities.ResetAll(building);
    SetStatus($"Reset {BuildingName(building).Value} to its game defaults.");
    RebuildOverallRows();
  }

  private void ResetAllBuildings() {
    int count = 0;
    foreach (IStaticEntity building in priorities.GetPriorityBuildings()) {
      if (resetUngroupedOnly && groups.GetForEntity(building.Id.Value) != null) continue;
      priorities.ResetAll(building);
      count++;
    }
    if (removeGroupsOnReset) groups.Clear();
    overallBuildings = null;
    string scope = resetUngroupedOnly ? "ungrouped buildings" : "buildings";
    string groupsResult = removeGroupsOnReset ? "cleared all groups" : "kept group memberships";
    SetStatus($"Reset priorities for {count} {scope} and {groupsResult}.");
    RebuildOverallRows();
  }

  private static LocStrFormatted BuildingName(IStaticEntity building) {
    return new LocStrFormatted(building.GetTitle());
  }

  private static string GetIconPath(IStaticEntity building) {
    return building.Prototype is IProtoWithIcon icon ? icon.IconPath : "Assets/Unity/UserInterface/General/Building.svg";
  }

  private static string BuildingType(IStaticEntity building) {
    return building.GetType().Name;
  }

  private static string SortName(OverallSort sort) {
    switch (sort) {
      case OverallSort.Name: return "Name";
      case OverallSort.Priority: return "Priority";
      case OverallSort.Group: return "Group";
      case OverallSort.Type: return "Type";
      default: return "Name";
    }
  }

  private static string ControlName(PriorityControl control) {
    switch (control) {
      case PriorityControl.General: return "general";
      case PriorityControl.Import: return "import";
      case PriorityControl.Export: return "export";
      case PriorityControl.Generator: return "generator";
      default: return "";
    }
  }

  private static void RemoveChildren(UiComponent component) {
    while (component.ChildrenCount > 0) component[0].RemoveFromHierarchy();
  }
}
