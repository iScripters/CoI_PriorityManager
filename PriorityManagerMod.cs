using System;
using Mafi;
using Mafi.Collections;
using Mafi.Core.Game;
using Mafi.Core.GameLoop;
using Mafi.Core.Mods;
using Mafi.Core.Prototypes;

namespace PriorityManager;

public sealed class PriorityManagerMod : IMod {
  public PriorityManagerMod(ModManifest manifest) {
    Manifest = manifest;
    JsonConfig = new ModJsonConfig(this);
  }

  public ModManifest Manifest { get; }
  public bool IsUiOnly => false;
  public Option<IConfig> ModConfig => Option<IConfig>.None;
  public ModJsonConfig JsonConfig { get; }

  public void RegisterPrototypes(ProtoRegistrator registrator) { }

  public void RegisterDependencies(DependencyResolverBuilder depBuilder, ProtosDb protosDb, bool gameWasLoaded) {
    depBuilder.RegisterDependency<PriorityGroupStore>().AsSelf();
    depBuilder.RegisterDependency<BuildingPriorityService>().AsSelf();
    depBuilder.RegisterDependency<PriorityGroupMembershipMonitor>().AsSelf();
    depBuilder.RegisterDependency<GroupBuildingSelectionController>().AsSelf();
    depBuilder.RegisterDependency<InspectorGroupAssignmentController>().AsSelf();
    depBuilder.RegisterDependency<PriorityManagerWindow>().AsSelf();
    depBuilder.RegisterDependency<PriorityManagerController>().AsSelf();
  }

  public void EarlyInit(DependencyResolver resolver) { }

  public void Initialize(DependencyResolver resolver, bool gameWasLoaded) {
    resolver.Resolve<IGameLoopEvents>().RegisterRendererInitState(this, () => {
      resolver.Resolve<PriorityManagerController>();
      resolver.Resolve<InspectorGroupAssignmentController>();
      resolver.Resolve<PriorityGroupMembershipMonitor>();
    });
  }

  public void MigrateJsonConfig(VersionSlim savedVersion, Dict<string, object> savedValues) { }
  public void Dispose() { }
}
