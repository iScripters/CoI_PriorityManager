using System;
using System.Collections.Generic;
using Mafi.Core;
using Mafi.Core.Entities.Static;
using Mafi.Core.GameLoop;

namespace PriorityManager;

public sealed class PriorityGroupMembershipMonitor {
  private sealed class TrackedControl {
    public TrackedControl(string groupId, int expectedValue, bool hasMatchedGroupValue) {
      GroupId = groupId;
      ExpectedValue = expectedValue;
      HasMatchedGroupValue = hasMatchedGroupValue;
    }

    public string GroupId { get; }
    public int ExpectedValue { get; }
    public bool HasMatchedGroupValue { get; set; }
  }

  private readonly PriorityGroupStore groups;
  private readonly BuildingPriorityService priorities;
  private readonly Dictionary<string, TrackedControl> trackedControls = new Dictionary<string, TrackedControl>();

  public event Action<int>? MembershipRemoved;

  public PriorityGroupMembershipMonitor(
    PriorityGroupStore groups,
    BuildingPriorityService priorities,
    IGameLoopEvents gameLoopEvents) {
    this.groups = groups;
    this.priorities = priorities;
    gameLoopEvents.SyncUpdate.AddNonSaveable(this, OnSyncUpdate);
  }

  private void OnSyncUpdate(GameTime _) {
    HashSet<string> activeControls = new HashSet<string>();
    HashSet<int> membersToRemove = new HashSet<int>();

    foreach (PriorityGroup group in groups.Groups) {
      foreach (int entityId in new List<int>(group.MemberIds)) {
        if (!priorities.TryGetBuilding(entityId, out IStaticEntity building)) continue;
        if (!priorities.IsGroupEligible(building)) {
          membersToRemove.Add(entityId);
          continue;
        }
        if (HasManualOverride(building, group, activeControls)) membersToRemove.Add(entityId);
      }
    }

    foreach (int entityId in membersToRemove) {
      groups.Remove(entityId);
      RemoveTrackedControls(entityId);
      MembershipRemoved?.Invoke(entityId);
    }

    List<string> staleKeys = new List<string>();
    foreach (string key in trackedControls.Keys) {
      if (!activeControls.Contains(key)) staleKeys.Add(key);
    }
    foreach (string key in staleKeys) trackedControls.Remove(key);
  }

  private bool HasManualOverride(IStaticEntity building, PriorityGroup group, HashSet<string> activeControls) {
    foreach (PriorityControl control in AllControls) {
      if (!priorities.SupportsGroupControl(building, control)) continue;

      string key = ControlKey(building.Id.Value, control);
      activeControls.Add(key);
      int expectedValue = BuildingPriorityService.GetGroupPriority(group, control);
      int currentValue = priorities.Get(building, control);

      if (!trackedControls.TryGetValue(key, out TrackedControl? tracked)
        || tracked.GroupId != group.Id
        || tracked.ExpectedValue != expectedValue) {
        // A new assignment or group edit may still have an input command waiting to apply.
        trackedControls[key] = new TrackedControl(group.Id, expectedValue, currentValue == expectedValue);
        continue;
      }

      if (currentValue == expectedValue) {
        tracked.HasMatchedGroupValue = true;
        continue;
      }

      if (tracked.HasMatchedGroupValue) return true;
    }
    return false;
  }

  private void RemoveTrackedControls(int entityId) {
    List<string> keys = new List<string>();
    string prefix = entityId + ":";
    foreach (string key in trackedControls.Keys) {
      if (key.StartsWith(prefix, StringComparison.Ordinal)) keys.Add(key);
    }
    foreach (string key in keys) trackedControls.Remove(key);
  }

  private static string ControlKey(int entityId, PriorityControl control) {
    return entityId + ":" + control;
  }

  private static readonly PriorityControl[] AllControls = {
    PriorityControl.General,
    PriorityControl.Import,
    PriorityControl.Export,
    PriorityControl.Generator
  };
}
