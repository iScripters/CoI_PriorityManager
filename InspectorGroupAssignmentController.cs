using System;
using System.Collections.Generic;
using System.Reflection;
using Mafi;
using Mafi.Core.Entities.Static;
using Mafi.Localization;
using Mafi.Unity;
using Mafi.Unity.InputControl;
using Mafi.Unity.Ui;
using Mafi.Unity.Ui.Library;
using Mafi.Unity.UiToolkit.Component;
using Mafi.Unity.UiToolkit.Library;

namespace PriorityManager;

[GlobalDependency(RegistrationMode.AsEverything)]
public sealed class InspectorGroupAssignmentController {
  private sealed class CreateGroupPopup : Window {
    private readonly TitleWithRename nameEditor;

    public CreateGroupPopup(Action<string> onCreate)
      : base(new LocStrFormatted("Create priority group")) {
      WindowSize(new Px(420), new Px(180));
      MakeMovableAndEnablePositionSaving();

      Body.Add(new Label(new LocStrFormatted("Group name")));
      nameEditor = new TitleWithRename(new LocStrFormatted("New group"));
      nameEditor.EnableRename(name => {
        onCreate(name);
        Close();
      });
      Body.Add(nameEditor);
      Body.Add(new Label(new LocStrFormatted("Confirm the name with the checkmark.")));
      Row buttons = new Row(2.pt()).AlignItemsCenter();
      buttons.Add(new ButtonText(Button.General, new LocStrFormatted("Cancel"), Close));
      Body.Add(buttons);
      nameEditor.StartRename();
    }
  }

  private sealed class InspectorBinding {
    public InspectorBinding(UiComponent root, Action<Window> onClose) {
      Root = root;
      OnClose = onClose;
    }

    public UiComponent Root { get; }
    public Action<Window> OnClose { get; }
  }

  private readonly PriorityGroupStore groups;
  private readonly BuildingPriorityService priorities;
  private readonly IUnityInputMgr input;
  private readonly UiContext context;
  private readonly Dictionary<IEntityInspector, InspectorBinding> bindings = new Dictionary<IEntityInspector, InspectorBinding>();
  private CreateGroupPopup? createGroupPopup;

  public InspectorGroupAssignmentController(
    PriorityGroupStore groups,
    BuildingPriorityService priorities,
    IUnityInputMgr input,
    UiContext context) {
    this.groups = groups;
    this.priorities = priorities;
    this.input = input;
    this.context = context;
    input.ControllerActivated += OnControllerActivated;
    input.ControllerDeactivated += OnControllerDeactivated;

    foreach (IUnityInputController controller in input.ActiveControllers) OnControllerActivated(controller);
  }

  private void OnControllerActivated(IUnityInputController controller) {
    if (!(controller is IEntityInspector inspector)) return;
    TryAttach(inspector);
    if (inspector is Window window) window.Schedule.Execute(() => TryAttach(inspector));
  }

  private void TryAttach(IEntityInspector inspector) {
    if (!(inspector.EntityUntyped is IStaticEntity building)
      || building.IsDestroyed
      || !priorities.HasAnyPotentialControl(building)
      || bindings.ContainsKey(inspector)) return;

    Column? host = TryGetMainBody(inspector);
    if (host == null && inspector is Window window) host = window.Body;
    if (host == null) return;

    int entityId = building.Id.Value;
    PanelWithHeader panel = BuildPanel(inspector, entityId);
    host.Add(panel);

    Action<Window> onClose = _ => RemoveBinding(inspector);
    inspector.OnCloseStart += onClose;
    bindings.Add(inspector, new InspectorBinding(panel, onClose));
  }

  private PanelWithHeader BuildPanel(IEntityInspector inspector, int entityId) {
    PanelWithHeader panel = new PanelWithHeader().Title(new LocStrFormatted("Priority group"));
    panel.Collapsed(!groups.IsInspectorExpanded);
    panel.Header.OnClick((Action)(() => {
      panel.Collapsed(!panel.IsCollapsed);
      groups.SetInspectorExpanded(!panel.IsCollapsed);
    }));

    Row row = new Row(2.pt()).AlignItemsCenter();

    Dropdown<PriorityGroup> dropdown = new Dropdown<PriorityGroup>(
      (group, index, inDropdown) => new Label(new LocStrFormatted(group?.Name ?? "No group")));
    dropdown.IncludeClearOption(new LocStrFormatted("No group"), null!);
    dropdown.SetOptions(groups.Groups);
    dropdown.SetValue(groups.GetForEntity(entityId)!);
    dropdown.OnValueChanged((group, index) => ChangeGroup(inspector, entityId, group));
    row.Add(dropdown.FlexGrow(1f).MinWidth(180.px()));
    row.Add(new ButtonText(Button.IconOnly, new LocStrFormatted("+"), () => ShowCreateGroupPopup(inspector, entityId, dropdown))
      .NoShrink()
      .Size(28.px())
      .Tooltip(new LocStrFormatted("Create group"), enabled: true, isError: false, openBelow: false));
    panel.Body.Add(row);

    Row resetRow = new Row(2.pt()).AlignItemsCenter();
    resetRow.Add(new Label(new LocStrFormatted("Restore game defaults")).FlexGrow(1f));
    resetRow.Add(new ButtonText(Button.Danger, new LocStrFormatted("Reset priorities"), () => ResetBuilding(inspector, entityId, dropdown)));
    panel.Body.Add(resetRow);
    return panel;
  }

  private void ChangeGroup(IEntityInspector inspector, int entityId, PriorityGroup? group) {
    if (!(inspector.EntityUntyped is IStaticEntity building)
      || building.IsDestroyed
      || building.Id.Value != entityId) return;

    if (group == null) {
      if (groups.GetForEntity(entityId) == null) return;
      groups.Remove(entityId);
      priorities.ResetAll(building);
    } else {
      groups.Assign(entityId, group);
      priorities.ApplyGroupToBuilding(group, building);
    }
  }

  private void ResetBuilding(IEntityInspector inspector, int entityId, Dropdown<PriorityGroup> dropdown) {
    if (!(inspector.EntityUntyped is IStaticEntity building)
      || building.IsDestroyed
      || building.Id.Value != entityId) return;

    groups.Remove(entityId);
    priorities.ResetAll(building);
    dropdown.SetValue(null!);
  }

  private void ShowCreateGroupPopup(IEntityInspector inspector, int entityId, Dropdown<PriorityGroup> dropdown) {
    if (createGroupPopup != null) return;
    createGroupPopup = new CreateGroupPopup(name => CreateAndAssignGroup(inspector, entityId, dropdown, name));
    createGroupPopup.OnCloseStart += _ => createGroupPopup = null;
    createGroupPopup.Open(context.UiRoot);
  }

  private void CreateAndAssignGroup(IEntityInspector inspector, int entityId, Dropdown<PriorityGroup> dropdown, string name) {
    PriorityGroup group = groups.Create(name);
    dropdown.SetOptions(groups.Groups);
    dropdown.SetValue(group);
    ChangeGroup(inspector, entityId, group);
  }

  private void OnControllerDeactivated(IUnityInputController controller) {
    if (controller is IEntityInspector inspector) RemoveBinding(inspector);
  }

  private void RemoveBinding(IEntityInspector inspector) {
    if (!bindings.TryGetValue(inspector, out InspectorBinding binding)) return;
    bindings.Remove(inspector);
    inspector.OnCloseStart -= binding.OnClose;
    binding.Root.RemoveFromHierarchy();
  }

  private static Column? TryGetMainBody(IEntityInspector inspector) {
    for (Type? type = inspector.GetType(); type != null; type = type.BaseType) {
      if (!type.IsGenericType || type.GetGenericTypeDefinition().FullName != "Mafi.Unity.Ui.Library.Inspectors.BaseInspector`1") continue;
      return type.GetField("MainBody", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)?.GetValue(inspector) as Column;
    }
    return null;
  }
}
