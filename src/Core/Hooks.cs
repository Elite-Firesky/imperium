﻿namespace Oxide.Plugins
{
  using Network;
  using UnityEngine;

  public partial class Imperium : RustPlugin
  {
    void OnUserApprove(Connection connection)
    {
      Users.SetOriginalName(connection.userid.ToString(), connection.username);
    }

    void OnPlayerInit(BasePlayer player)
    {
      if (player == null) return;

      // If the player hasn't fully connected yet, try again in 2 seconds.
      if (player.IsReceivingSnapshot)
      {
        timer.In(2, () => OnPlayerInit(player));
        return;
      }

      Users.Add(player);
    }

    void OnPlayerDisconnected(BasePlayer player)
    {
      if (player != null)
        Users.Remove(player);
    }

    void OnHammerHit(BasePlayer player, HitInfo hit)
    {
      User user = Users.Get(player);

      if (user != null && user.CurrentInteraction != null)
        user.CompleteInteraction(hit);
    }

    object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hit)
    {
      if (entity == null || hit == null)
        return null;

      return Logistics.AlterDamage(entity, hit);
    }

    object OnTrapTrigger(BaseTrap trap, GameObject obj)
    {
      var player = obj.GetComponent<BasePlayer>();

      if (trap == null || player == null)
        return null;

      User defender = Users.Get(player);
      return Logistics.AlterTrapTrigger(trap, defender);
    }

    object CanBeTargeted(BaseCombatEntity target, MonoBehaviour turret)
    {
      if (target == null || turret == null)
        return null;

      // Don't interfere with the helicopter.
      if (turret is HelicopterTurret)
        return null;

      var player = target as BasePlayer;

      if (player == null)
        return null;

      User defender = Users.Get(player);
      var entity = turret as BaseCombatEntity;

      return Logistics.AlterTurretTrigger(entity, defender);
    }

    void OnEntityKill(BaseNetworkable entity)
    {
      // If a player dies in an area or a zone, remove them.
      var player = entity as BasePlayer;
      if (player != null)
      {
        User user = Users.Get(player);
        if (user != null)
        {
          user.CurrentArea = null;
          user.CurrentZones.Clear();
        }
        return;
      }

      // If a claim TC is destroyed, remove the claim from the area.
      var cupboard = entity as BuildingPrivlidge;
      if (cupboard != null)
      {
        var area = Areas.GetByClaimCupboard(cupboard);
        if (area != null)
        {
          PrintToChat(Messages.AreaClaimLostCupboardDestroyedAnnouncement, area.FactionId, area.Id);
          Log($"{area.FactionId} lost their claim on {area.Id} because the tool cupboard was destroyed (hook function)");
          Areas.Unclaim(area);
        }
        return;
      }

      // If a tax chest is destroyed, remove it from the faction data.
      var container = entity as StorageContainer;
      if (Options.EnableTaxation && container != null)
      {
        Faction faction = Factions.GetByTaxChest(container);
        if (faction != null)
        {
          Log($"{faction.Id}'s tax chest was destroyed (hook function)");
          faction.TaxChest = null;
        }
        return;
      }

      // If a helicopter is destroyed, create an event zone around it.
      var helicopter = entity as BaseHelicopter;
      if (helicopter != null)
      {
        Zones.Create(helicopter);
      }
    }

    void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      ProcessTaxesIfApplicable(dispenser, entity, item);
      AwardBadlandsBonusIfApplicable(dispenser, entity, item);
    }

    void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item)
    {
      ProcessTaxesIfApplicable(dispenser, entity, item);
      AwardBadlandsBonusIfApplicable(dispenser, entity, item);
    }

    void OnUserEnteredArea(User user, Area area)
    {
      Area previousArea = user.CurrentArea;

      user.CurrentArea = area;
      user.HudPanel.Refresh();

      if (previousArea == null)
        return;

      if (area.Type == AreaType.Badlands && previousArea.Type != AreaType.Badlands)
      {
        // The player has entered the badlands.
        user.SendChatMessage(Messages.EnteredBadlands);
      }
      else if (area.Type == AreaType.Wilderness && previousArea.Type != AreaType.Wilderness)
      {
        // The player has entered the wilderness.
        user.SendChatMessage(Messages.EnteredWilderness);
      }
      else if (area.Type == AreaType.Town && previousArea.Type != AreaType.Town)
      {
        // The player has entered a town.
        user.SendChatMessage(Messages.EnteredTown, area.Name, area.FactionId);
      }
      else if (area.IsClaimed && !previousArea.IsClaimed)
      {
        // The player has entered a faction's territory.
        user.SendChatMessage(Messages.EnteredClaimedArea, area.FactionId);
      }
      else if (area.IsClaimed && previousArea.IsClaimed && area.FactionId != previousArea.FactionId)
      {
        // The player has crossed a border between the territory of two factions.
        user.SendChatMessage(Messages.EnteredClaimedArea, area.FactionId);
      }
    }

    void OnUserEnteredZone(User user, Zone zone)
    {
      user.CurrentZones.Add(zone);
      user.HudPanel.Refresh();
    }

    void OnUserLeftZone(User user, Zone zone)
    {
      user.CurrentZones.Remove(zone);
      user.HudPanel.Refresh();
    }

    void OnFactionCreated(Faction faction)
    {
      Ui.RefreshForAllPlayers();
    }

    void OnFactionDisbanded(Faction faction)
    {
      Area[] areas = Instance.Areas.GetAllClaimedByFaction(faction);

      if (areas.Length > 0)
      {
        foreach (Area area in areas)
          PrintToChat(Messages.AreaClaimLostFactionDisbandedAnnouncement, area.FactionId, area.Id);

        Areas.Unclaim(areas);
      }

      Wars.EndAllWarsForEliminatedFactions();
      Ui.RefreshForAllPlayers();
    }

    void OnFactionTaxesChanged(Faction faction)
    {
      Ui.RefreshForAllPlayers();
    }

    void OnAreaChanged(Area area)
    {
      Wars.EndAllWarsForEliminatedFactions();
      Images.GenerateMapOverlayImage();
      Ui.RefreshForAllPlayers();
    }

    void OnDiplomacyChanged()
    {
      Ui.RefreshForAllPlayers();
    }
  }
}