﻿using StarMap.Cll.Filters;
using StarMap.Cll.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StarMap.Cll.Abstractions
{
  public interface IStarDataProvider
  {
    IEnumerable<Constellation> GetConstellations();

    IEnumerable<Star> GetStars(StarFilter filter);

    StarDetail GetStarDetails(int id);
  }
}