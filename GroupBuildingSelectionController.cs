using System;
using System.Collections.Generic;
using Mafi;
using Mafi.Collections;
using Mafi.Collections.ImmutableCollections;
using Mafi.Collections.ReadonlyCollections;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Static;
using Mafi.Core.Factory.Transports;
using Mafi.Core.Prototypes;
using Mafi.Core.Terrain;
using Mafi.Unity.Entities;
using Mafi.Unity.InputControl;
using Mafi.Unity.InputControl.AreaTool;
using Mafi.Unity.InputControl.Factory;
using Mafi.Unity.Terrain;
using Mafi.Unity.Ui;
using Mafi.Unity.Ui.Controllers.Tools;
using Mafi.Unity.Ui.Hud;
using Mafi.Unity.UiStatic;
using Mafi.Unity.UiStatic.Cursors;

namespace PriorityManager;

public sealed class GroupBuildingSelectionController : BaseEntityCursorInputController<IStaticEntity> {
  private readonly PriorityGroupStore groups;
  private readonly BuildingPriorityService priorities;
  private PriorityGroup? activeGroup;

  public GroupBuildingSelectionController(
    ToolbarHud toolbar,
    UiContext context,
    CursorPickingManager cursorPickingManager,
    CursorManager cursorManager,
    AreaSelectionToolFactory areaSelectionToolFactory,
    NewInstanceOf<TerrainAreaOutlineRenderer> terrainOutlineRenderer,
    IEntitiesManager entitiesManager,
    NewInstanceOf<EntityHighlighter> entityHighlighter,
    PriorityGroupStore groups,
    BuildingPriorityService priorities)
    : base(
      toolbar,
      context,
      cursorPickingManager,
      cursorManager,
      areaSelectionToolFactory,
      terrainOutlineRenderer,
      entitiesManager,
      entityHighlighter,
      Option<NewInstanceOf<TransportTrajectoryHighlighter>>.None,
      null,
      null,
      "Assets/Unity/UserInterface/Audio/ButtonClick.prefab",
      Option<FilterToolbox>.None) {
    this.groups = groups;
    this.priorities = priorities;
    InitHighlightColors(ColorRgba.CornflowerBlue, ColorRgba.Red, ColorRgba.Red);
  }

  public event Action<PriorityGroup, int>? BuildingsAdded;

  public void Start(PriorityGroup group) {
    activeGroup = group;
    Context.InputMgr.ActivateNewController(this);
  }

  protected override bool Matches(IStaticEntity entity, bool isAreaSelection, bool isLeftClick) {
    return !entity.IsDestroyed && priorities.HasAnyControl(entity);
  }

  protected override bool OnFirstActivated(
    IStaticEntity hoveredEntity,
    Lyst<IStaticEntity> selectedEntities,
    Lyst<SubTransport> selectedPartialTransports) {
    return false;
  }

  protected override void OnEntitiesSelected(
    IIndexable<IStaticEntity> selectedEntities,
    IIndexable<SubTransport> selectedPartialTransports,
    ImmutableArray<TileSurfaceCopyPasteData> selectedSurfaces,
    ImmutableArray<TileSurfaceCopyPasteData> selectedDecals,
    bool isAreaSelection,
    bool isLeftMouse,
    RectangleTerrainArea2i? area) {
    PriorityGroup? group = activeGroup;
    if (group == null || !isLeftMouse) return;

    List<int> entityIds = new List<int>(selectedEntities.Count);
    foreach (IStaticEntity building in selectedEntities) entityIds.Add(building.Id.Value);
    groups.AssignMany(entityIds, group);
    foreach (IStaticEntity building in selectedEntities) priorities.ApplyGroupToBuilding(group, building);

    Context.InputMgr.DeactivateController(this);
    BuildingsAdded?.Invoke(group, entityIds.Count);
  }

  public override void Deactivate() {
    base.Deactivate();
    activeGroup = null;
  }
}
