﻿namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class PvpOptions
    {
      [JsonProperty("restrictPvp")]
      public bool RestrictPvp;

      [JsonProperty("allowedInBadlands")]
      public bool AllowedInBadlands;

      [JsonProperty("allowedInClaimedLand")]
      public bool AllowedInClaimedLand;

      [JsonProperty("allowedInTowns")]
      public bool AllowedInTowns;

      [JsonProperty("allowedInWilderness")]
      public bool AllowedInWilderness;

      [JsonProperty("allowedInEventZones")]
      public bool AllowedInEventZones;

      [JsonProperty("allowedInMonumentZones")]
      public bool AllowedInMonumentZones;

      [JsonProperty("allowedInRaidZones")]
      public bool AllowedInRaidZones;

      public static PvpOptions Default = new PvpOptions {
        RestrictPvp = false,
        AllowedInBadlands = true,
        AllowedInClaimedLand = true,
        AllowedInEventZones = true,
        AllowedInMonumentZones = true,
        AllowedInRaidZones = true,
        AllowedInTowns = true,
        AllowedInWilderness = true,
      };
    }
  }
}
