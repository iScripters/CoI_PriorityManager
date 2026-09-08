using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Mafi.Core.Mods;

namespace PriorityManager;

[DataContract]
public sealed class PriorityGroupState {
  [DataMember(Name = "version")]
  public int Version = 1;

  [DataMember(Name = "groups")]
  public List<PriorityGroup> Groups = new List<PriorityGroup>();

  [DataMember(Name = "inspectorExpanded")]
  public bool InspectorExpanded;
}

[DataContract]
public sealed class PriorityGroup {
  [DataMember(Name = "id")]
  public string Id = Guid.NewGuid().ToString("N");

  [DataMember(Name = "name")]
  public string Name = "New group";

  [DataMember(Name = "generalPriority")]
  public int GeneralPriority = 9;

  [DataMember(Name = "importPriority")]
  public int ImportPriority = 8;

  [DataMember(Name = "exportPriority")]
  public int ExportPriority = 8;

  [DataMember(Name = "generatorPriority")]
  public int GeneratorPriority = 9;

  [DataMember(Name = "memberIds")]
  public List<int> MemberIds = new List<int>();
}

public sealed class PriorityGroupStore {
  private const string GroupsStateKey = "groups_state";
  private readonly ModJsonConfig jsonConfig;
  private PriorityGroupState state;

  public PriorityGroupStore(PriorityManagerMod mod) {
    jsonConfig = mod.JsonConfig;
    state = Deserialize(jsonConfig.GetString(GroupsStateKey, ""));
  }

  public IReadOnlyList<PriorityGroup> Groups => state.Groups;

  public bool IsInspectorExpanded => state.InspectorExpanded;

  public void SetInspectorExpanded(bool expanded) {
    if (state.InspectorExpanded == expanded) return;
    state.InspectorExpanded = expanded;
    Save();
  }

  public PriorityGroup Create(string name) {
    PriorityGroup group = new PriorityGroup { Name = NormalizeName(name) };
    state.Groups.Add(group);
    Save();
    return group;
  }

  public void Rename(PriorityGroup group, string name) {
    group.Name = NormalizeName(name);
    Save();
  }

  public void Delete(PriorityGroup group) {
    state.Groups.Remove(group);
    Save();
  }

  public void Clear() {
    state.Groups.Clear();
    Save();
  }

  public PriorityGroup? GetForEntity(int entityId) {
    foreach (PriorityGroup group in state.Groups) {
      if (group.MemberIds.Contains(entityId)) return group;
    }
    return null;
  }

  public void Assign(int entityId, PriorityGroup group) {
    foreach (PriorityGroup existing in state.Groups) existing.MemberIds.Remove(entityId);
    if (!group.MemberIds.Contains(entityId)) group.MemberIds.Add(entityId);
    Save();
  }

  public void AssignMany(IEnumerable<int> entityIds, PriorityGroup group) {
    HashSet<int> ids = new HashSet<int>(entityIds);
    if (ids.Count == 0) return;
    foreach (PriorityGroup existing in state.Groups) existing.MemberIds.RemoveAll(id => ids.Contains(id));
    foreach (int entityId in ids) group.MemberIds.Add(entityId);
    Save();
  }

  public void Remove(int entityId) {
    foreach (PriorityGroup group in state.Groups) group.MemberIds.Remove(entityId);
    Save();
  }

  public void RemoveMany(IEnumerable<int> entityIds, PriorityGroup group) {
    HashSet<int> ids = new HashSet<int>(entityIds);
    if (ids.Count == 0 || group.MemberIds.RemoveAll(id => ids.Contains(id)) == 0) return;
    Save();
  }

  public bool PruneMissing(Func<int, bool> entityExists) {
    bool changed = false;
    foreach (PriorityGroup group in state.Groups) {
      changed |= group.MemberIds.RemoveAll(id => !entityExists(id)) > 0;
    }
    if (changed) Save();
    return changed;
  }

  public void Save() {
    string serialized = Serialize(state);
    if (!jsonConfig.TrySetValue(GroupsStateKey, serialized, out string error)) {
      Mafi.Log.Warning($"Priority Manager: could not save groups: {error}");
    }
  }

  private static string NormalizeName(string name) {
    string trimmed = (name ?? "").Trim();
    return string.IsNullOrEmpty(trimmed) ? "Unnamed group" : trimmed.Substring(0, Math.Min(trimmed.Length, 64));
  }

  private static PriorityGroupState Deserialize(string serialized) {
    if (string.IsNullOrWhiteSpace(serialized)) return new PriorityGroupState();
    try {
      using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(serialized))) {
        PriorityGroupState? loaded = (PriorityGroupState?)new DataContractJsonSerializer(typeof(PriorityGroupState)).ReadObject(stream);
        if (loaded == null) return new PriorityGroupState();
        loaded.Groups ??= new List<PriorityGroup>();
        foreach (PriorityGroup group in loaded.Groups) {
          group.Id ??= Guid.NewGuid().ToString("N");
          group.Name ??= "Unnamed group";
          group.MemberIds ??= new List<int>();
        }
        return loaded;
      }
    } catch (Exception exception) {
      Mafi.Log.Warning($"Priority Manager: saved groups could not be read: {exception.Message}");
      return new PriorityGroupState();
    }
  }

  private static string Serialize(PriorityGroupState value) {
    using (MemoryStream stream = new MemoryStream()) {
      new DataContractJsonSerializer(typeof(PriorityGroupState)).WriteObject(stream, value);
      return Encoding.UTF8.GetString(stream.ToArray());
    }
  }

}
