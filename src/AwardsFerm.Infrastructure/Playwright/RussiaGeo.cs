namespace AwardsFerm.Infrastructure.Playwright;

using AwardsFerm.Core.Models;

internal static class RussiaGeo
{
  private static readonly ProxyGeoLocation[] Cities =
  [
      new() { Latitude = 59.9343, Longitude = 30.3351, Timezone = "Europe/Moscow", Locale = "ru-RU", Label = "Санкт-Петербург, Россия" },
      new() { Latitude = 55.7558, Longitude = 37.6173, Timezone = "Europe/Moscow", Locale = "ru-RU", Label = "Москва, Россия" },
      new() { Latitude = 55.7961, Longitude = 49.1064, Timezone = "Europe/Moscow", Locale = "ru-RU", Label = "Казань, Россия" },
      new() { Latitude = 56.3269, Longitude = 44.0059, Timezone = "Europe/Moscow", Locale = "ru-RU", Label = "Нижний Новгород, Россия" },
      new() { Latitude = 45.0355, Longitude = 38.9753, Timezone = "Europe/Moscow", Locale = "ru-RU", Label = "Краснодар, Россия" },
      new() { Latitude = 47.2357, Longitude = 39.7015, Timezone = "Europe/Moscow", Locale = "ru-RU", Label = "Ростов-на-Дону, Россия" }
  ];

  public static ProxyGeoLocation PickForProfile(string profileId, DesktopProfile fallback)
  {
      if (Cities.Length == 0)
          return FromProfile(fallback);

      var index = profileId switch
      {
          "session-001" => 0,
          "session-002" => 1,
          "session-003" => 2,
          _ => Math.Abs(profileId.GetHashCode(StringComparison.Ordinal)) % Cities.Length
      };

      return Cities[index % Cities.Length];
  }

  public static ProxyGeoLocation FromProfile(DesktopProfile profile) => new()
  {
      Latitude = profile.Latitude,
      Longitude = profile.Longitude,
      Timezone = "Europe/Moscow",
      Locale = "ru-RU",
      Label = "Санкт-Петербург, Россия"
  };
}
