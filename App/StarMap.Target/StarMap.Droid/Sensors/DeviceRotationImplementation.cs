﻿using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Runtime;
using Android.Views;
using StarMap.Cll.Abstractions.Services;
using StarMap.Core.Models;
using StarMap.Droid.Sensors;
using System;
using System.Collections.Generic;
using Axis = Android.Hardware.Axis;

[assembly: Xamarin.Forms.Dependency(typeof(DeviceRotationImplementation))]
namespace StarMap.Droid.Sensors
{
  // Thanks to Java.Lang.Object, I don't have to (neither should I) implement the IntPtr Handle from the IJavaObject interface
  // (inherited by ISensorEventListener).
  public class DeviceRotationImplementation : Java.Lang.Object, ISensorEventListener, IDeviceRotation
  {
    IWindowManager _windowManager;
    SensorManager _sensorManager;
    IList<Sensor> _sensors;
    bool _on, _accReady, _magReady;
    float[] _gravity = new float[3],
      _magnet = new float[3],
      _orientation = new float[3],
      R = new float[9],
      rotatedR = new float[9];

    static readonly object _lock = new object();

    public DeviceRotationImplementation()
    {
      Init();
    }

    public void Init()
    {
      var context = Application.Context;
      _windowManager = context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>(); // TODO: check if this funny casting is a must
      _sensorManager = (SensorManager)context.GetSystemService(Context.SensorService);
      _sensors = new List<Sensor>()
      {
        _sensorManager.GetDefaultSensor(SensorType.Accelerometer),
        _sensorManager.GetDefaultSensor(SensorType.MagneticField)
      };
    }

    #region ISensorEventListener implementation

    public void OnSensorChanged(SensorEvent e)
    {
      // Neither the emulator, nor my LG Spirit, give higher readings than this.
      //if (e.Accuracy == SensorStatus.Unreliable) return;

      // I don't think the lock is needed. Threads don't get confused on which sensor fired the event.
      // Maybe it's to prevent from raising RotationVhanged event multiple times, by many threads, then OK.
      // But e.g. in standard Windows application, Filewatcher catches events synchronously, one thread at a time, even if 
      // multuiple events are raised simultanously - each one waits for it's turn. From what I see here it's the same.
      lock (_lock)
      {
        switch (e.Sensor.Type)
        {
          case SensorType.Accelerometer:
            if (_accReady = !_accReady)
              e.Values.CopyTo(_gravity, 0);
            break;
          case SensorType.MagneticField:
            if (_magReady = !_magReady)
              e.Values.CopyTo(_magnet, 0);
            break;
        }

        if (!(_accReady && _magReady))
          return;

        SensorManager.GetRotationMatrix(R, null, _gravity, _magnet);
        // I don't really see the point in setting this here, and checking upon copying if is not ready (i.e. new).
        // So what, if I send an event with just one sensor updated, the other one with an old value. But anyway, that;s what this does
        // might as well keep it. 
        _accReady = _magReady = false;

        // Cellphone's natural orientation is portrait, tilted to the left it returns display (rotation) = 1,
        // to the right = 3.
        // However, a tablet is already in landscape, so the natural application displays are 0 or 2.
        Axis x = Axis.X, y = Axis.Y;
        switch (_windowManager.DefaultDisplay.Rotation)
        {
          case SurfaceOrientation.Rotation180:
            x = Axis.MinusX;
            y = Axis.MinusY;
            break;
          case SurfaceOrientation.Rotation270:
            x = Axis.MinusY; // TODO: check if they are not in fact inverted.
            y = Axis.X;
            break;
          case SurfaceOrientation.Rotation90: // phone tilted to the left
            x = Axis.Y;
            y = Axis.MinusX;
            break;
        }

        SensorManager.RemapCoordinateSystem(R, x, y, rotatedR);
        SensorManager.GetOrientation(rotatedR, _orientation);
        
        _rotationChanged?.Invoke(this, new RotationChangedEventArgs(_orientation));
      }
    }    

    public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy) { }

    #endregion

    #region IDeviceRotation implementation

    static readonly object _eventLocker = new object();
    EventHandler<RotationChangedEventArgs> _rotationChanged;
    // This nice piece of code ensures that only one Handler is attached to the event.
    // No need to worry about any checks. Alternatively, I could expose some 'Subscribe' and 'Unsubscribe'
    // methods, that would take an Action and hide te event altogether.
    public event EventHandler<RotationChangedEventArgs> RotationChanged
    {
      add
      {
        lock (_eventLocker)
        {
          _rotationChanged -= value;
          _rotationChanged += value;
        }
      }

      remove
      {
        lock (_eventLocker)
        {
          _rotationChanged -= value;
        }
      }
    }

    public void Start()
    {
      if (_on) return;

      foreach (var sensor in _sensors)
        _sensorManager.RegisterListener(this, sensor, SensorDelay.Normal);

      _on = true;
    }

    public void Stop()
    {
      if (!_on) return;

      _sensorManager.UnregisterListener(this);
      _on = false;

    }


    #endregion

    // Auto-generated by auto implementing the interface.
    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual new void Dispose(bool disposing)
    {
      if (!disposedValue)
      {
        if (disposing)
        {
          Stop();
          _sensorManager = null;
          _sensors.Clear();
        }

        // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
        // TODO: set large fields to null.

        disposedValue = true;
      }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    ~DeviceRotationImplementation()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(false);
    }

    // This code added to correctly implement the disposable pattern.
    public new void Dispose()
    {
      // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
      Dispose(true);
      // TODO: uncomment the following line if the finalizer is overridden above.
      GC.SuppressFinalize(this);
    }
    
    #endregion

  }
}