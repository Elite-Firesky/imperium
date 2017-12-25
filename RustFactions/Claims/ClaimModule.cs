﻿namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  public partial class RustFactions
  {
    [ChatCommand("claim")]
    void OnClaimCommand(BasePlayer player, string command, string[] args)
    {
      if (!Options.EnableAreaClaims)
      {
        SendMessage(player, Messages.AreaClaimsDisabled);
        return;
      }

      if (args.Length == 0)
      {
        OnClaimAddCommand(player);
        return;
      }

      var restArguments = args.Skip(1).ToArray();

      switch (args[0].ToLower())
      {
        case "add":
          OnClaimAddCommand(player);
          break;
        case "remove":
          OnClaimRemoveCommand(player);
          break;
        case "hq":
          OnClaimHeadquartersCommand(player);
          break;
        case "cost":
          OnClaimCostCommand(player, restArguments);
          break;
        case "show":
          OnClaimShowCommand(player, restArguments);
          break;
        case "list":
          OnClaimListCommand(player, restArguments);
          break;
        case "delete":
          OnClaimDeleteCommand(player, restArguments);
          break;
        case "help":
        default:
          OnClaimHelpCommand(player);
          break;
      }
    }

    void OnClaimAddCommand(BasePlayer player)
    {
      User user = Users.Get(player);

      if (!EnsureCanChangeFactionClaims(player))
        return;

      SendMessage(player, Messages.SelectClaimCupboardToAdd);
      user.PendingInteraction = new AddingClaimInteraction();
    }

    void OnClaimRemoveCommand(BasePlayer player)
    {
      User user = Users.Get(player);

      if (!EnsureCanChangeFactionClaims(player))
        return;

      SendMessage(player, Messages.SelectClaimCupboardToRemove);
      user.PendingInteraction = new RemovingClaimInteraction();
    }

    void OnClaimHeadquartersCommand(BasePlayer player)
    {
      User user = Users.Get(player);

      if (!EnsureCanChangeFactionClaims(player))
        return;

      SendMessage(player, Messages.SelectClaimCupboardForHeadquarters);
      user.PendingInteraction = new SelectingHeadquartersInteraction();
    }

    void OnClaimCostCommand(BasePlayer player, string[] args)
    {
      User user = Users.Get(player);
      Faction faction = GetFactionForPlayer(player);

      if (faction == null)
      {
        SendMessage(player, Messages.InteractionFailedNotMemberOfFaction);
        return;
      }

      if (faction.MemberSteamIds.Count < Options.MinFactionMembers)
      {
        SendMessage(player, Messages.InteractionFailedFactionTooSmall, Options.MinFactionMembers);
        return;
      }

      if (args.Length > 1)
      {
        SendMessage(player, Messages.CannotShowClaimCostBadUsage);
        return;
      }

      Area area;
      if (args.Length == 0)
        area = user.CurrentArea;
      else
        area = Areas.Get(args[0].Trim().ToUpper());

      if (area == null)
      {
        SendMessage(player, Messages.CannotShowClaimCostBadUsage);
        return;
      }

      Claim claim = Claims.Get(area);
      if (claim != null)
      {
        SendMessage(player, Messages.CannotShowClaimCostAlreadyOwned, area.Id, claim.FactionId);
        return;
      }

      int cost = GetCostToClaimArea(player, faction, area);
      SendMessage(player, Messages.ClaimCost, area.Id, faction.Id, cost);
    }

    void OnClaimShowCommand(BasePlayer player, string[] args)
    {
      if (args.Length != 1)
      {
        SendMessage(player, Messages.CannotShowClaimBadUsage);
        return;
      }

      string areaId = NormalizeAreaId(args[0]);

      if (Badlands.Contains(areaId))
      {
        SendMessage(player, Messages.AreaIsBadlands, areaId);
        return;
      }

      Claim claim = Claims.Get(areaId);

      if (claim == null)
        SendMessage(player, Messages.AreaIsUnclaimed, areaId);
      else if (claim.IsHeadquarters)
        SendMessage(player, Messages.AreaIsHeadquarters, claim.AreaId, claim.FactionId);
      else
        SendMessage(player, Messages.AreaIsClaimed, claim.AreaId, claim.FactionId);
    }

    void OnClaimListCommand(BasePlayer player, string[] args)
    {
      if (args.Length != 1)
      {
        SendMessage(player, Messages.CannotListClaimsBadUsage);
        return;
      }

      string factionId = NormalizeFactionId(args[0]);
      Faction faction = GetFaction(factionId);

      if (faction == null)
      {
        SendMessage(player, Messages.CannotListClaimsUnknownFaction, factionId);
        return;
      }

      Claim[] claims = Claims.GetAllClaimsForFaction(faction);
      Claim headquarters = claims.FirstOrDefault(c => c.IsHeadquarters);

      var sb = new StringBuilder();

      if (claims.Length == 0)
      {
        sb.AppendFormat(String.Format("<color=#ffd479>[{0}]</color> has no land holdings.", factionId));
      }
      else
      {
        float percentageOfMap = (claims.Length / (float)Areas.Count) * 100;
        sb.AppendLine(String.Format("<color=#ffd479>[{0}] owns {1} tiles ({2:F2}% of the known world)</color>", faction.Id, claims.Length, percentageOfMap));
        sb.AppendLine(String.Format("Headquarters: {0}", (headquarters == null) ? "Unknown" : headquarters.AreaId));
        sb.AppendLine(String.Format("Areas claimed: {0}", FormatList(claims.Select(c => c.AreaId))));
      }

      SendMessage(player, sb);
    }

    void OnClaimHelpCommand(BasePlayer player)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("  <color=#ffd479>/claim</color>: Add a claim for your faction");
      sb.AppendLine("  <color=#ffd479>/claim hq</color>: Select your faction's headquarters");
      sb.AppendLine("  <color=#ffd479>/claim remove</color>: Remove a claim for your faction (no undo!)");
      sb.AppendLine("  <color=#ffd479>/claim show XY</color>: Show who owns an area");
      sb.AppendLine("  <color=#ffd479>/claim list FACTION</color>: List all areas claimed for a faction");
      sb.AppendLine("  <color=#ffd479>/claim cost [XY]</color>: Show the cost for your faction to claim an area");
      sb.AppendLine("  <color=#ffd479>/claim help</color>: Prints this message");

      if (permission.UserHasPermission(player.UserIDString, PERM_CHANGE_CLAIMS))
      {
        sb.AppendLine("Admin commands:");
        sb.AppendLine("  <color=#ffd479>/claim delete XY [XY XY XY...]</color>: Remove the claim on the specified areas (no undo!)");
      }

      SendMessage(player, sb);
    }

    void OnClaimDeleteCommand(BasePlayer player, string[] args)
    {
      if (args.Length == 0)
      {
        SendMessage(player, Messages.CannotDeleteClaimsBadUsage);
        return;
      }

      if (!permission.UserHasPermission(player.UserIDString, PERM_CHANGE_CLAIMS))
      {
        SendMessage(player, Messages.CannotDeleteClaimsNoPermission);
        return;
      }

      var claimsToRevoke = new List<Claim>();
      foreach (string arg in args)
      {
        string areaId = NormalizeAreaId(arg);
        Claim claim = Claims.Get(areaId);

        if (claim == null)
        {
          SendMessage(player, Messages.CannotDeleteClaimsAreaNotClaimed, areaId);
          return;
        }

        claimsToRevoke.Add(claim);
      }

      foreach (Claim claim in claimsToRevoke)
      {
        PrintToChat("<color=#ff0000ff>AREA CLAIM REMOVED:</color> [{0}]'s claim on {1} has been removed by an admin.", claim.FactionId, claim.AreaId);
        Puts($"{player.displayName} deleted [{claim.FactionId}]'s claim on {claim.AreaId}");
        Claims.Remove(claim.AreaId);
      }

      OnClaimsChanged();
    }

    bool TryAddClaim(BasePlayer player, HitInfo hit)
    {
      User user = Users.Get(player);
      Faction faction = GetFactionForPlayer(player);
      var cupboard = hit.HitEntity as BuildingPrivlidge;

      if (!EnsureCanChangeFactionClaims(player) || !EnsureCanUseCupboardAsClaim(player, cupboard))
        return false;

      Area area = user.CurrentArea;
      if (area == null)
      {
        PrintWarning("Player attempted to add claim but wasn't in an area. This shouldn't happen.");
        return false;
      }

      if (Badlands.Contains(area.Id))
      {
        SendMessage(player, Messages.CannotClaimAreaBadlands);
        return false;
      }

      bool isHeadquarters = Claims.GetAllClaimsForFaction(faction.Id).Length == 0;

      Claim oldClaim = Claims.Get(area);
      Claim newClaim = new Claim(area.Id, faction.Id, player.userID, cupboard.net.ID, isHeadquarters);

      if (oldClaim != null)
      {
        if (oldClaim.FactionId == newClaim.FactionId)
        {
          // If the same faction claims a new cabinet within the same area, move the claim to the new cabinet.
          SendMessage(player, Messages.ClaimCupboardMoved, area.Id);
        }
        else if (oldClaim.CupboardId == newClaim.CupboardId)
        {
          // If a new faction claims the claim cabinet for an area, they take control of that area.
          SendMessage(player, Messages.ClaimCaptured, area.Id, oldClaim.FactionId);
          PrintToChat("<color=#ff0000ff>AREA CAPTURED:</color> [{0}] has captured {1} from [{2}]!", faction.Id, area.Id, oldClaim.FactionId);
        }
        else
        {
          // A new faction can't make a claim on a new cabinet within an area that is already claimed by another faction.
          SendMessage(player, Messages.ClaimFailedAlreadyClaimed, area.Id, oldClaim.FactionId);
          return false;
        }
      }
      else
      {
        if (!TryCollectClaimCost(player, faction, area, newClaim))
          return false;

        SendMessage(player, Messages.ClaimAdded, area.Id);
        if (isHeadquarters)
          PrintToChat("<color=#00ff00ff>AREA CLAIMED:</color> [{0}] claims {1} as their headquarters!", faction.Id, area.Id);
        else
          PrintToChat("<color=#00ff00ff>AREA CLAIMED:</color> [{0}] claims {1}!", faction.Id, area.Id);
      }

      Claims.Add(newClaim);
      Puts($"{newClaim.FactionId} claims {newClaim.AreaId}");

      return true;
    }

    bool TryRemoveClaim(BasePlayer player, HitInfo hit)
    {
      Faction faction = GetFactionForPlayer(player);
      var cupboard = hit.HitEntity as BuildingPrivlidge;

      if (!EnsureCanChangeFactionClaims(player) || !EnsureCanUseCupboardAsClaim(player, cupboard))
        return false;

      Claim claim = Claims.GetByCupboard(cupboard.net.ID);
      if (claim == null)
      {
        SendMessage(player, Messages.SelectingClaimCupboardFailedNotClaimCupboard);
        return false;
      }

      SendMessage(player, Messages.ClaimRemoved, claim.AreaId);
      PrintToChat("<color=#ff0000ff>CLAIM REMOVED:</color> [{0}] has relinquished their claim on {1}!", faction.Id, claim.AreaId);
      Claims.Remove(claim.AreaId);

      return true;
    }

    bool TrySetHeadquarters(BasePlayer player, HitInfo hit)
    {
      Faction faction = GetFactionForPlayer(player);
      var cupboard = hit.HitEntity as BuildingPrivlidge;

      if (!EnsureCanChangeFactionClaims(player) || !EnsureCanUseCupboardAsClaim(player, cupboard))
        return false;

      Claim headquartersClaim = Claims.GetByCupboard(cupboard);
      if (headquartersClaim == null)
      {
        SendMessage(player, Messages.SelectingClaimCupboardFailedNotClaimCupboard);
        return false;
      }

      SendMessage(player, Messages.HeadquartersSet, headquartersClaim.AreaId);
      PrintToChat("<color=#00ff00ff>HQ CHANGED:</color> [{0}] announces that {1} is their headquarters.", faction.Id, headquartersClaim.AreaId);

      Claims.SetHeadquarters(faction, headquartersClaim);
      Puts($"{faction.Id} designates {headquartersClaim.AreaId} as their headquarters");

      return true;
    }

    int GetCostToClaimArea(BasePlayer player, Faction faction, Area area)
    {
      int numberOfTilesOwned = Claims.GetAllClaimsForFaction(faction).Length;
      return Options.BaseClaimCost + (Options.AdditionalClaimCostPerArea * numberOfTilesOwned);
    }

    bool TryCollectClaimCost(BasePlayer player, Faction faction, Area area, Claim claim)
    {
      int cost = GetCostToClaimArea(player, faction, area);
      if (cost == 0) return true;

      Item scrap = player.inventory.FindItemID("scrap");

      if (scrap == null || scrap.amount < cost)
      {
        SendMessage(player, Messages.CannotClaimAreaCannotAfford, cost);
        return false;
      }

      scrap.amount -= cost;
      scrap.MarkDirty();

      return true;
    }

    bool EnsureCanChangeFactionClaims(BasePlayer player)
    {
      Faction faction = GetFactionForPlayer(player);

      if (faction == null || !faction.IsLeader(player))
      {
        SendMessage(player, Messages.InteractionFailedNotLeaderOfFaction);
        return false;
      }

      if (faction.MemberSteamIds.Count < Options.MinFactionMembers)
      {
        SendMessage(player, Messages.InteractionFailedFactionTooSmall, Options.MinFactionMembers);
        return false;
      }

      return true;
    }

    bool EnsureCanUseCupboardAsClaim(BasePlayer player, BuildingPrivlidge cupboard)
    {
      if (cupboard == null)
      {
        SendMessage(player, Messages.SelectingCupboardFailedInvalidTarget);
        return false;
      }

      if (!cupboard.IsAuthed(player))
      {
        SendMessage(player, Messages.SelectingCupboardFailedNotAuthorized);
        return false;
      }

      return true;
    }

  }

}