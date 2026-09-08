using System;
using System.Collections.Generic;
using System.Reflection;
using Mafi.Core;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Commands;
using Mafi.Core.Entities.Priorities;
using Mafi.Core.Entities.Static;
using Mafi.Core.Factory.ElectricPower;
using Mafi.Core.Input;

namespace PriorityManager;

public enum PriorityControl {
  General,
  Import,
  Export,
  Generator
}

public sealed class BuildingPriorityService {
  private const string ImportPriorityId = "ImportPrio";
  private const string ExportPriorityId = "ExportPrio";
  private const string FuelImportPriorityId = "FuelImportPrio";
  private const string FuelExportPriorityId = "FuelExportPrio";
  private readonly IEntitiesManager entities;
  private readonly IInputScheduler inputScheduler;

  public BuildingPriorityService(IEntitiesManager entities, IInputScheduler inputScheduler) {
    this.entities = entities;
    this.inputScheduler = inputScheduler;
  }

  public IEnumerable<IStaticEntity> GetPriorityBuildings() {
    foreach (IEntity entity in entities.Entities) {
      if (entity is IStaticEntity building && !building.IsDestroyed && HasAnyControl(building)) {
        yield return building;
      }
    }
  }

  public bool TryGetBuilding(int entityId, out IStaticEntity building) {
    return entities.TryGetEntity(new EntityId(entityId), out building) && !building.IsDestroyed;
  }

  public bool HasAnyControl(IStaticEntity building) {
    return Supports(building, PriorityControl.General)
      || Supports(building, PriorityControl.Import)
      || Supports(building, PriorityControl.Export)
      || Supports(building, PriorityControl.Generator);
  }

  public bool HasAnyPotentialControl(IStaticEntity building) {
    return building is IEntityWithGeneralPriority
      || building is IEntityWithCustomPriority
      || building is IElectricityGeneratingEntity;
  }

  public bool Supports(IStaticEntity building, PriorityControl control) {
    switch (control) {
      case PriorityControl.General:
        return building is IEntityWithGeneralPriority general && general.IsGeneralPriorityVisible;
      case PriorityControl.Import:
        return GetCustomPriorityId(building, PriorityControl.Import) != null;
      case PriorityControl.Export:
        return GetCustomPriorityId(building, PriorityControl.Export) != null;
      case PriorityControl.Generator:
        return building is IElectricityGeneratingEntity;
      default:
        return false;
    }
  }

  public int Get(IStaticEntity building, PriorityControl control) {
    switch (control) {
      case PriorityControl.General:
        return ((IEntityWithGeneralPriority)building).GeneralPriority;
      case PriorityControl.Import:
      case PriorityControl.Export:
        return ((IEntityWithCustomPriority)building).GetCustomPriority(GetCustomPriorityId(building, control)!);
      case PriorityControl.Generator:
        return ((IElectricityGeneratingEntity)building).ElectricityGenerator.GenerationPriority;
      default:
        throw new ArgumentOutOfRangeException(nameof(control));
    }
  }

  public void Set(IStaticEntity building, PriorityControl control, int priority) {
    if (priority < 0 || priority > 14 || !Supports(building, control)) return;
    switch (control) {
      case PriorityControl.General:
        inputScheduler.ScheduleInputCmd(new SetGeneralPriorityCmd(building, priority));
        break;
      case PriorityControl.Import:
      case PriorityControl.Export:
        inputScheduler.ScheduleInputCmd(new SetCustomPriorityCmd(
          (IEntityWithCustomPriority)building,
          GetCustomPriorityId(building, control)!,
          priority));
        break;
      case PriorityControl.Generator:
        inputScheduler.ScheduleInputCmd(new SetElectricityGenerationPriorityCmd((IElectricityGeneratingEntity)building, priority));
        break;
    }
  }

  public void ApplyGroupControl(PriorityGroup group, PriorityControl control) {
    int priority = GetGroupPriority(group, control);
    foreach (int memberId in group.MemberIds) {
      if (TryGetBuilding(memberId, out IStaticEntity building)) Set(building, control, priority);
    }
  }

  public void ApplyGroupToBuilding(PriorityGroup group, IStaticEntity building) {
    foreach (PriorityControl control in AllControls) {
      if (SupportsGroupControl(building, control)) Set(building, control, GetGroupPriority(group, control));
    }
  }

  public void ResetAll(IStaticEntity building) {
    foreach (PriorityControl control in AllControls) Reset(building, control);
  }

  // A group must be able to restore every control it changes when a member leaves.
  public bool SupportsGroupControl(IStaticEntity building, PriorityControl control) {
    return Supports(building, control)
      && (control != PriorityControl.Generator || TryGetGeneratorDefault(building).HasValue);
  }

  public void Reset(IStaticEntity building, PriorityControl control) {
    if (!Supports(building, control)) return;
    switch (control) {
      case PriorityControl.General:
        Set(building, control, building.Prototype.Costs.DefaultPriority);
        break;
      case PriorityControl.Import:
      case PriorityControl.Export:
        Set(building, control, 8);
        break;
      case PriorityControl.Generator:
        int? generatorDefault = TryGetGeneratorDefault(building);
        if (generatorDefault.HasValue) Set(building, control, generatorDefault.Value);
        break;
    }
  }

  public static int GetGroupPriority(PriorityGroup group, PriorityControl control) {
    switch (control) {
      case PriorityControl.General: return group.GeneralPriority;
      case PriorityControl.Import: return group.ImportPriority;
      case PriorityControl.Export: return group.ExportPriority;
      case PriorityControl.Generator: return group.GeneratorPriority;
      default: throw new ArgumentOutOfRangeException(nameof(control));
    }
  }

  public static void SetGroupPriority(PriorityGroup group, PriorityControl control, int priority) {
    switch (control) {
      case PriorityControl.General: group.GeneralPriority = priority; break;
      case PriorityControl.Import: group.ImportPriority = priority; break;
      case PriorityControl.Export: group.ExportPriority = priority; break;
      case PriorityControl.Generator: group.GeneratorPriority = priority; break;
      default: throw new ArgumentOutOfRangeException(nameof(control));
    }
  }

  private static readonly PriorityControl[] AllControls = {
    PriorityControl.General,
    PriorityControl.Import,
    PriorityControl.Export,
    PriorityControl.Generator
  };

  private static string? GetCustomPriorityId(IStaticEntity building, PriorityControl control) {
    if (!(building is IEntityWithCustomPriority custom)) return null;
    if (control == PriorityControl.Import) {
      if (custom.IsCustomPriorityVisible(ImportPriorityId)) return ImportPriorityId;
      if (custom.IsCustomPriorityVisible(FuelImportPriorityId)) return FuelImportPriorityId;
    }
    if (control == PriorityControl.Export) {
      if (custom.IsCustomPriorityVisible(ExportPriorityId)) return ExportPriorityId;
      if (custom.IsCustomPriorityVisible(FuelExportPriorityId)) return FuelExportPriorityId;
    }
    return null;
  }

  private static int? TryGetGeneratorDefault(IStaticEntity building) {
    FieldInfo? field = building.Prototype.GetType().GetField("GenerationPriority", BindingFlags.Instance | BindingFlags.Public);
    return field?.FieldType == typeof(int) ? (int?)field.GetValue(building.Prototype) : null;
  }
}
