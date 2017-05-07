﻿using Plugin.Geolocator.Abstractions;
using StarMap.Cll.Models.Geolocation;
using System.Threading.Tasks;

namespace StarMap.Cll.Abstractions
{
  public interface ILocationManager
  {
    Task<ExtendedPosition> GetNewGpsPositionAsync();

    Task<ExtendedPosition> CheckLocationAsync();

    void StoreGpsPosition(Position position);
  }
}