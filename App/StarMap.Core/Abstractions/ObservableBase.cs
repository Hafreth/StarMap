﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StarMap.Core.Abstractions
{
  /// <summary>
  /// Custom implementation of INotifyPropertyChanged
  /// </summary>
  public abstract class ObservableBase : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
      => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
    {
      if (Equals(storage, value)) return false;

      storage = value;
      OnPropertyChanged(propertyName);
      return true;
    }
  }
}