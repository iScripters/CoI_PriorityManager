using System;
using Mafi;
using Mafi.Unity.InputControl;
using Mafi.Unity.Ui;
using Mafi.Unity.Ui.Hud;
using Mafi.Unity.UiStatic.Toolbar;
using Mafi.Unity.UiToolkit.Library;

namespace PriorityManager;

[GlobalDependency(RegistrationMode.AsEverything)]
public sealed class PriorityManagerController : IToolbarItemController {
  private readonly PriorityManagerWindow window;

  private readonly UiContext context;
  private bool isActive;
  private bool preserveStateOnNextActivate;

  public PriorityManagerController(
    PriorityManagerWindow window,
    GroupBuildingSelectionController buildingSelection,
    ToolbarHud toolbar,
    UiContext context) {
    this.window = window;
    this.context = context;
    toolbar.AddMainMenuButton(
      new Mafi.Localization.LocStrFormatted("Priority Manager"),
      this,
      "Assets/Unity/UserInterface/General/Priority.svg",
      1500f,
      null);
    window.OnCloseStart += OnWindowClose;
    buildingSelection.BuildingsAdded += OnBuildingsAdded;
    buildingSelection.BuildingsRemoved += OnBuildingsAdded;
    VisibilityChanged?.Invoke(this);
  }

  public ControllerConfig Config => ControllerConfig.Window;
  // Toolbar visibility means "available to activate", not "window currently open".
  public bool IsVisible => true;
  public bool DeactivateShortcutsIfNotVisible => false;
  public event Action<IToolbarItemController>? VisibilityChanged;

  public void Activate() {
    if (isActive) return;
    isActive = true;
    if (preserveStateOnNextActivate) window.RefreshAfterBuildingSelection();
    else window.Refresh();
    preserveStateOnNextActivate = false;
    window.Open(context.UiRoot);
  }

  public void Deactivate() {
    if (!isActive) return;
    isActive = false;
    window.Close();
  }

  public bool InputUpdate() {
    window.RefreshPrioritySortIfChanged();
    return window.InputUpdate();
  }

  private void OnWindowClose(Window _) {
    if (isActive) context.InputMgr.DeactivateController(this);
  }

  private void OnBuildingsAdded(PriorityGroup group, int count) {
    if (isActive) {
      window.RefreshAfterBuildingSelection();
      return;
    }
    preserveStateOnNextActivate = true;
    context.InputMgr.ActivateNewController(this);
  }
}
