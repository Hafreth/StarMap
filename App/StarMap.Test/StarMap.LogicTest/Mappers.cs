﻿using StarMap.Core.Extensions;
using NUnit.Framework;
using System.Collections.Generic;
using StarMap.Cll.Models;
using StarMap.Dal.Mappers;

namespace StarMap.LogicTest
{
  [TestFixture]
  public class Mappers
  {
    [Test]
    public void ColorsMapper()
    {
      Assert.AreEqual(Color.Blue, Colors.MapColor("Blaw;kdh 87gb a ajwd   "));
      Assert.AreEqual(Color.Yellow, Colors.MapColor("8172tg 1bdi1ud82 fv"));
      Assert.AreEqual(Color.Yellow, Colors.MapColor(null));
      Assert.AreEqual(Color.Yellow, Colors.MapColor(""));
    }
  }
}