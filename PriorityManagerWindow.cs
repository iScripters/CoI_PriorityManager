using System;
using System.Collections.Generic;
using System.Globalization;
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
  private readonly PriorityManagerLocalization loc;
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
  private readonly Dictionary<int, string> overallDisplayValues = new Dictionary<int, string>();
  private readonly List<ulong> groupHighlightIds = new List<ulong>();
  private List<IStaticEntity>? overallBuildings;
  private int overallPage;
  private int prioritySortCheckFrames;
  private bool overallSortDescending;
  private bool removeGroupsOnReset = true;
  private bool resetUngroupedOnly;

  public PriorityManagerWindow(
    PriorityGroupStore groups,
    BuildingPriorityService priorities,
    GroupBuildingSelectionController buildingSelection,
    CameraController camera,
    InspectorsManager inspectors,
    EntitiesRenderingManager entitiesRendering,
    PriorityManagerLocalization loc)
    : base(loc.Text(PriorityManagerText.WindowTitle)) {
    this.groups = groups;
    this.priorities = priorities;
    this.buildingSelection = buildingSelection;
    this.camera = camera;
    this.inspectors = inspectors;
    this.entitiesRendering = entitiesRendering;
    this.loc = loc;
    buildingSelection.BuildingsAdded += OnBuildingsAdded;
    buildingSelection.BuildingsRemoved += OnBuildingsRemoved;
    OnCloseStart += _ => ClearGroupHighlights();

    WindowSize(new Px(1000), new Px(720));
    MakeMovableAndEnablePositionSaving();

    overallSort = new Dropdown<OverallSort>((value, index, inDropdown) => new Label(SortName(value)));
    overallSort.SetOptions(OverallSort.Name, OverallSort.Priority, OverallSort.Group, OverallSort.Type);
    overallSort.SetValue(OverallSort.Name);
    overallSort.OnValueChanged((value, index) => ChangeOverallSort(value));

    tabs = new TabContainer();
    tabs.Add(loc.Text(PriorityManagerText.TabOverall), BuildOverallTab());
    tabs.Add(loc.Text(PriorityManagerText.TabGroups), BuildGroupsTab());
    tabs.OnTabActivate(RefreshActiveTab);
    Body.Add(tabs);
  }

  public void Refresh() {
    groups.PruneMissing(id => priorities.TryGetBuilding(id, out IStaticEntity building) && priorities.IsGroupEligible(building));
    overallBuildings = null;
    prioritySortValues.Clear();
    overallDisplayValues.Clear();
    expandedGroupIds.Clear();
    RefreshActiveTab();
  }

  public void RefreshAfterBuildingSelection() {
    groups.PruneMissing(id => priorities.TryGetBuilding(id, out IStaticEntity building) && priorities.IsGroupEligible(building));
    overallBuildings = null;
    prioritySortValues.Clear();
    overallDisplayValues.Clear();
    RefreshActiveTab();
  }

  public void RefreshPrioritySortIfChanged() {
    if (tabs.ActiveTabIndex != 0 || overallBuildings == null) return;
    if (++prioritySortCheckFrames < 10) return;
    prioritySortCheckFrames = 0;

    int visibleCount = 0;
    string filter = overallFilter.GetText().Trim();
    foreach (IStaticEntity building in overallBuildings) {
      if (building.IsDestroyed || !MatchesFilter(building, filter)) continue;
      visibleCount++;
      if (!overallDisplayValues.TryGetValue(building.Id.Value, out string? displayed)
        || displayed != OverallDisplayValue(building)
        || (overallSort.SelectedValue == OverallSort.Priority
          && (!prioritySortValues.TryGetValue(building.Id.Value, out int previous)
            || previous != GeneralPriorityForSort(building)))) {
        RebuildOverallRows();
        return;
      }
    }
    if (visibleCount != overallDisplayValues.Count) RebuildOverallRows();
  }

  private void RefreshActiveTab() {
    if (tabs.ActiveTabIndex == 1) RebuildGroupRows();
    else RebuildOverallRows();
  }

  private UiComponent BuildOverallTab() {
    Column page = new Column(2.pt()).AlignItemsStretch();
    Panel controlsPanel = new Panel(noBolts: true).ReducedPadding().BodyGap(2.pt()).AlignSelfStretch();
    controlsPanel.Body.Add(new Label(loc.Text(PriorityManagerText.IndividualPriorityGroupWarning)));
    controlsPanel.Body.Add(overallStatus);
    Row resetRow = new Row(2.pt()).AlignItemsCenter();
    resetRow.Add(new Label(loc.Text(PriorityManagerText.ResetAllWarning)).FlexGrow(1f));
    Toggle removeGroupsToggle = new Toggle(standalone: true).Value(removeGroupsOnReset).OnValueChanged(value => removeGroupsOnReset = value);
    resetRow.Add(removeGroupsToggle.Tooltip(loc.Text(PriorityManagerText.RemoveGroupsTooltip), enabled: true, isError: false, openBelow: false));
    resetRow.Add(new Label(loc.Text(PriorityManagerText.RemoveGroups)));
    Toggle ungroupedOnlyToggle = new Toggle(standalone: true).Value(resetUngroupedOnly).OnValueChanged(value => resetUngroupedOnly = value);
    resetRow.Add(ungroupedOnlyToggle.Tooltip(loc.Text(PriorityManagerText.OnlyUngroupedTooltip), enabled: true, isError: false, openBelow: false));
    resetRow.Add(new Label(loc.Text(PriorityManagerText.OnlyUngrouped)));
    resetRow.Add(new ButtonText(Button.Danger, loc.Text(PriorityManagerText.ResetAllPriorities), ResetAllBuildings));
    controlsPanel.Body.Add(resetRow);
    Row filterRow = new Row(2.pt()).AlignItemsCenter();
    overallFilter.CharLimit(80).Placeholder(loc.Text(PriorityManagerText.FilterBuildings)).FlexGrow(1f).MinWidth(300.px())
      .OnValueChanged(_ => FilterOverallBuildings());
    filterRow.Add(overallFilter);
    filterRow.Add(new Label(loc.Text(PriorityManagerText.Sort)));
    filterRow.Add(overallSort.NoShrink().MinWidth(150.px()));
    Toggle descendingToggle = new Toggle(standalone: true).Value(overallSortDescending)
      .OnValueChanged(ChangeOverallSortDirection);
    filterRow.Add(descendingToggle.Tooltip(loc.Text(PriorityManagerText.ReverseSortTooltip), enabled: true, isError: false, openBelow: false));
    filterRow.Add(new Label(loc.Text(PriorityManagerText.Descending)));
    controlsPanel.Body.Add(filterRow);
    Row navigation = new Row(2.pt()).AlignItemsCenter();
    navigation.Add(new ButtonText(Button.General, loc.Text(PriorityManagerText.Previous), PreviousOverallPage));
    navigation.Add(overallPageLabel);
    navigation.Add(new ButtonText(Button.General, loc.Text(PriorityManagerText.Next), NextOverallPage));
    controlsPanel.Body.Add(navigation);
    page.Add(controlsPanel);
    page.Add(overallRows);
    return page;
  }

  private UiComponent BuildGroupsTab() {
    Column page = new Column(2.pt()).AlignItemsStretch();
    Panel createPanel = new Panel(noBolts: true).ReducedPadding().AlignSelfStretch();
    Row createRow = new Row(2.pt()).AlignItemsCenter();
    newGroupName.CharLimit(64).Placeholder(loc.Text(PriorityManagerText.NewGroupName)).FlexGrow(1f).MinWidth(300.px())
      .OnReturn(CreateGroupAndKeepEditing);
    createRow.Add(newGroupName);
    createRow.Add(new ButtonText(Button.Primary, loc.Text(PriorityManagerText.CreateGroup), CreateGroup));
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
      overallDisplayValues.Clear();
      overallPageLabel.Value(loc.Format(PriorityManagerText.OverallPageRange, 0, 0, 0));
      overallRows.Add(new Label(loc.Text(PriorityManagerText.NoPriorityCapableBuildings)));
      return;
    }

    int pageCount = (buildings.Count + OverallPageSize - 1) / OverallPageSize;
    overallPage = Math.Max(0, Math.Min(overallPage, pageCount - 1));
    int firstIndex = overallPage * OverallPageSize;
    int lastIndex = Math.Min(firstIndex + OverallPageSize, buildings.Count);
    overallPageLabel.Value(loc.Format(PriorityManagerText.OverallPageRange, firstIndex + 1, lastIndex, buildings.Count));
    for (int index = firstIndex; index < lastIndex; index++) overallRows.Add(BuildBuildingRow(buildings[index]));
    CapturePrioritySortValues(buildings);
    CaptureOverallDisplayValues(buildings);
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
    return ContainsLocalized(BuildingName(building).Value, filter)
      || ContainsLocalized(BuildingType(building), filter)
      || (group != null && ContainsLocalized(group.Name, filter));
  }

  private int CompareBuildings(IStaticEntity left, IStaticEntity right) {
    int comparison = 0;
    switch (overallSort.SelectedValue) {
      case OverallSort.Priority:
        comparison = GeneralPriorityForSort(left).CompareTo(GeneralPriorityForSort(right));
        break;
      case OverallSort.Group:
        comparison = CompareLocalized(GroupName(left), GroupName(right));
        break;
      case OverallSort.Type:
        comparison = CompareLocalized(BuildingType(left), BuildingType(right));
        break;
    }
    if (comparison == 0) comparison = CompareLocalized(BuildingName(left).Value, BuildingName(right).Value);
    return overallSortDescending ? -comparison : comparison;
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

  private void ChangeOverallSortDirection(bool descending) {
    if (overallSortDescending == descending) return;
    overallSortDescending = descending;
    overallPage = 0;
    RebuildOverallRows();
  }

  private void CapturePrioritySortValues(List<IStaticEntity> buildings) {
    prioritySortValues.Clear();
    if (overallSort.SelectedValue != OverallSort.Priority) return;
    foreach (IStaticEntity building in buildings) prioritySortValues.Add(building.Id.Value, GeneralPriorityForSort(building));
  }

  private void CaptureOverallDisplayValues(List<IStaticEntity> buildings) {
    overallDisplayValues.Clear();
    foreach (IStaticEntity building in buildings) overallDisplayValues.Add(building.Id.Value, OverallDisplayValue(building));
  }

  private string OverallDisplayValue(IStaticEntity building) {
    return BuildingName(building).Value + "\n" + BuildingType(building) + "\n" + GroupName(building);
  }

  private UiComponent BuildBuildingRow(IStaticEntity building) {
    Panel card = new Panel(noBolts: true).ReducedPadding().BodyGap(2.pt()).MarginBottom(2.pt()).AlignSelfStretch();
    card.OnMouseEnterLeave(() => HighlightBuilding(building), ClearGroupHighlights);
    Row header = new Row(2.pt()).AlignItemsCenter();
    Icon icon = new Icon(GetIconPath(building)).Size(48.px()).OnClick(() => Focus(building.Id.Value));
    header.Add(icon);
    header.Add(new Label(BuildingName(building)).FlexGrow(1f));
    header.Add(new ButtonText(Button.Danger, loc.Text(PriorityManagerText.Reset), () => ResetBuilding(building.Id.Value)));

    PriorityGroup? currentGroup = groups.GetForEntity(building.Id.Value);
    Dropdown<PriorityGroup> groupDropdown = new Dropdown<PriorityGroup>(
      (group, index, inDropdown) => new Label(new LocStrFormatted(group?.Name ?? loc.Value(PriorityManagerText.NoGroup))));
    groupDropdown.IncludeClearOption(loc.Text(PriorityManagerText.NoGroup), null!);
    groupDropdown.SetOptions(groups.Groups);
    groupDropdown.SetValue(currentGroup!);
    groupDropdown.OnValueChanged((selected, index) => ChangeGroup(building.Id.Value, selected));
    header.Add(groupDropdown.NoShrink().MinWidth(180.px()));
    card.Body.Add(header);

    Row controls = new Row(2.pt()).AlignItemsEnd();
    AddIndividualControl(controls, building, PriorityControl.General, PriorityManagerText.General, groupDropdown);
    AddIndividualControl(controls, building, PriorityControl.Import, PriorityManagerText.Import, groupDropdown);
    AddIndividualControl(controls, building, PriorityControl.Export, PriorityManagerText.Export, groupDropdown);
    AddIndividualControl(controls, building, PriorityControl.Generator, PriorityManagerText.Generator, groupDropdown);
    card.Body.Add(controls);
    return card;
  }

  private void AddIndividualControl(Row parent, IStaticEntity building, PriorityControl control, PriorityManagerText title, Dropdown<PriorityGroup> groupDropdown) {
    if (!priorities.Supports(building, control)) return;
    Column item = new Column(1.pt()).AlignItemsCenter();
    item.Add(new Label(loc.Text(title)));
    PriorityDropdown dropdown = CreatePriorityDropdown(priorities.Get(building, control));
    dropdown.OnValueChanged((value, index) => {
      bool removedFromGroup = ChangeIndividualPriority(building.Id.Value, control, value);
      dropdown.SetValue(value);
      if (removedFromGroup) groupDropdown.SetValue(null!);
    });
    item.Add(dropdown);
    parent.Add(item);
  }

  private void RebuildGroupRows() {
    ClearGroupHighlights();
    RemoveChildren(groupRows);
    if (groups.Groups.Count == 0) {
      groupRows.Add(new Label(loc.Text(PriorityManagerText.CreateGroupThenAssign)));
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
      LocStrFormatted membersButtonText = membersVisible
        ? loc.Format(PriorityManagerText.HideBuildings, group.MemberIds.Count)
        : loc.Format(PriorityManagerText.ShowBuildings, group.MemberIds.Count);
      titleRow.Add(new ButtonText(Button.General, membersButtonText, () => ToggleGroupMembers(group)));
    }
    titleRow.Add(new ButtonText(Button.Primary, loc.Text(PriorityManagerText.AddBuildings), () => StartBuildingSelection(group)));
    titleRow.Add(new ButtonText(Button.Danger, loc.Text(PriorityManagerText.RemoveBuildings), () => StartBuildingRemoval(group)));
    titleRow.Add(new ButtonText(Button.Danger, loc.Text(PriorityManagerText.Delete), () => DeleteGroup(group)));
    card.Body.Add(titleRow);

    Row controls = new Row(2.pt()).AlignItemsEnd();
    AddGroupControl(controls, group, PriorityControl.General, PriorityManagerText.General);
    AddGroupControl(controls, group, PriorityControl.Import, PriorityManagerText.Import);
    AddGroupControl(controls, group, PriorityControl.Export, PriorityManagerText.Export);
    AddGroupControl(controls, group, PriorityControl.Generator, PriorityManagerText.Generator);
    card.Body.Add(controls);

    if (group.MemberIds.Count == 0) {
      card.Body.Add(new Label(loc.Text(PriorityManagerText.NoBuildingsAssigned)));
    } else if (expandedGroupIds.Contains(group.Id)) {
      foreach (int memberId in new List<int>(group.MemberIds)) {
        if (priorities.TryGetBuilding(memberId, out IStaticEntity building)) card.Body.Add(BuildGroupMemberRow(group, building));
      }
    }
    return card;
  }

  private void AddGroupControl(Row parent, PriorityGroup group, PriorityControl control, PriorityManagerText title) {
    Column item = new Column(1.pt()).AlignItemsCenter();
    item.Add(new Label(loc.Text(title)));
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
    row.OnMouseEnterLeave(() => HighlightBuilding(building), () => HighlightGroupMembers(group));
    row.Body.Add(new Icon(GetIconPath(building)).Size(30.px()).OnClick(() => Focus(building.Id.Value)));
    row.Body.Add(new Label(BuildingName(building)).FlexGrow(1f));
    row.Body.Add(new ButtonText(Button.Danger, loc.Text(PriorityManagerText.Remove), () => RemoveFromGroup(group, building.Id.Value)));
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
    SetStatus(loc.Format(PriorityManagerText.StatusCreatedGroup, group.Name));
    if (rebuildRows) RebuildGroupRows();
    else {
      if (groups.Groups.Count == 1) RemoveChildren(groupRows);
      groupRows.Add(BuildGroupCard(group));
    }
  }

  private void RenameGroup(PriorityGroup group, string name) {
    groups.Rename(group, name);
    SetStatus(loc.Format(PriorityManagerText.StatusRenamedGroup, group.Name));
  }

  private void DeleteGroup(PriorityGroup group) {
    foreach (int memberId in group.MemberIds) {
      if (priorities.TryGetBuilding(memberId, out IStaticEntity building)) priorities.ResetAll(building);
    }
    groups.Delete(group);
    expandedGroupIds.Remove(group.Id);
    SetStatus(loc.Format(PriorityManagerText.StatusDeletedGroup, group.Name));
    RebuildGroupRows();
  }

  private void ChangeGroup(int entityId, PriorityGroup? selectedGroup) {
    PriorityGroup? currentGroup = groups.GetForEntity(entityId);
    if (!priorities.TryGetBuilding(entityId, out IStaticEntity building)) return;
    if (!priorities.IsGroupEligible(building)) return;

    if (selectedGroup == null) {
      if (currentGroup == null) return;
      groups.Remove(entityId);
      priorities.ResetAll(building);
      SetStatus(loc.Format(PriorityManagerText.StatusRemovedFromGroup, BuildingName(building).Value, currentGroup.Name));
    } else {
      groups.Assign(entityId, selectedGroup);
      priorities.ApplyGroupToBuilding(selectedGroup, building);
      SetStatus(loc.Format(PriorityManagerText.StatusAssignedToGroup, BuildingName(building).Value, selectedGroup.Name));
    }
    RebuildOverallRows();
  }

  private void RemoveFromGroup(PriorityGroup group, int entityId) {
    if (!priorities.TryGetBuilding(entityId, out IStaticEntity building)) return;
    groups.Remove(entityId);
    priorities.ResetAll(building);
    SetStatus(loc.Format(PriorityManagerText.StatusRemovedFromGroup, BuildingName(building).Value, group.Name));
    RebuildGroupRows();
  }

  private void ToggleGroupMembers(PriorityGroup group) {
    if (!expandedGroupIds.Add(group.Id)) expandedGroupIds.Remove(group.Id);
    RebuildGroupRows();
  }

  private void StartBuildingSelection(PriorityGroup group) {
    buildingSelection.Start(group, remove: false);
    SetStatus(loc.Format(PriorityManagerText.StatusDragToAdd, group.Name));
  }

  private void StartBuildingRemoval(PriorityGroup group) {
    buildingSelection.Start(group, remove: true);
    SetStatus(loc.Format(PriorityManagerText.StatusDragToRemove, group.Name));
  }

  private void OnBuildingsAdded(PriorityGroup group, int count) {
    SetStatus(loc.Format(PriorityManagerText.StatusAddedToGroup, count, group.Name));
    RebuildGroupRows();
  }

  private void OnBuildingsRemoved(PriorityGroup group, int count) {
    SetStatus(loc.Format(PriorityManagerText.StatusRemovedFromGroupCount, count, group.Name));
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

  private void HighlightBuilding(IStaticEntity building) {
    ClearGroupHighlights();
    if (building is IRenderedEntity rendered) {
      groupHighlightIds.Add(entitiesRendering.AddHighlight(rendered, ColorRgba.CornflowerBlue));
    }
  }

  private void ClearGroupHighlights() {
    foreach (ulong highlightId in groupHighlightIds) entitiesRendering.RemoveHighlight(highlightId);
    groupHighlightIds.Clear();
  }

  private bool ChangeIndividualPriority(int entityId, PriorityControl control, int priority) {
    if (!priorities.TryGetBuilding(entityId, out IStaticEntity building)) return false;
    PriorityGroup? group = groups.GetForEntity(entityId);
    if (group != null) {
      groups.Remove(entityId);
      SetStatus(loc.Format(PriorityManagerText.StatusChangedPriority, BuildingName(building).Value, group.Name));
    }
    priorities.Set(building, control, priority);
    return group != null;
  }

  private void ChangeGroupPriority(PriorityGroup group, PriorityControl control, int priority) {
    BuildingPriorityService.SetGroupPriority(group, control, priority);
    groups.Save();
    priorities.ApplyGroupControl(group, control);
    SetStatus(loc.Format(PriorityManagerText.StatusUpdatedGroupPriority, ControlName(control).Value, group.Name));
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

  private void SetStatus(LocStrFormatted text) {
    overallStatus.Value(text);
    groupStatus.Value(text);
  }

  private void ResetBuilding(int entityId) {
    if (!priorities.TryGetBuilding(entityId, out IStaticEntity building)) return;
    if (groups.GetForEntity(entityId) != null) groups.Remove(entityId);
    priorities.ResetAll(building);
    SetStatus(loc.Format(PriorityManagerText.StatusResetBuilding, BuildingName(building).Value));
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
    string scope = loc.Value(resetUngroupedOnly ? PriorityManagerText.ResetScopeUngrouped : PriorityManagerText.ResetScopeAll);
    string groupsResult = loc.Value(removeGroupsOnReset ? PriorityManagerText.ResetGroupsCleared : PriorityManagerText.ResetGroupsKept);
    SetStatus(loc.Format(PriorityManagerText.StatusResetAll, count, scope, groupsResult));
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

  private static bool ContainsLocalized(string value, string filter) {
    return LocalizationManager.CurrentCultureInfo.CompareInfo.IndexOf(value, filter, CompareOptions.IgnoreCase) >= 0;
  }

  private static int CompareLocalized(string left, string right) {
    return LocalizationManager.CurrentCultureInfo.CompareInfo.Compare(left, right, CompareOptions.IgnoreCase);
  }

  private LocStrFormatted SortName(OverallSort sort) {
    switch (sort) {
      case OverallSort.Name: return loc.Text(PriorityManagerText.Name);
      case OverallSort.Priority: return loc.Text(PriorityManagerText.Priority);
      case OverallSort.Group: return loc.Text(PriorityManagerText.Group);
      case OverallSort.Type: return loc.Text(PriorityManagerText.Type);
      default: return loc.Text(PriorityManagerText.Name);
    }
  }

  private LocStrFormatted ControlName(PriorityControl control) {
    switch (control) {
      case PriorityControl.General: return loc.Text(PriorityManagerText.General);
      case PriorityControl.Import: return loc.Text(PriorityManagerText.Import);
      case PriorityControl.Export: return loc.Text(PriorityManagerText.Export);
      case PriorityControl.Generator: return loc.Text(PriorityManagerText.Generator);
      default: return new LocStrFormatted("");
    }
  }

  private static void RemoveChildren(UiComponent component) {
    while (component.ChildrenCount > 0) component[0].RemoveFromHierarchy();
  }
}
