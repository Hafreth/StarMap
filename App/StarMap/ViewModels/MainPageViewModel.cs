﻿using Prism.AppModel;
using Prism.Commands;
using Prism.Navigation;
using Prism.Services;
using StarMap.Bll.Helpers;
using StarMap.Cll.Abstractions;
using StarMap.Cll.Abstractions.Services;
using StarMap.Cll.Constants;
using StarMap.Cll.Exceptions;
using StarMap.Cll.Filters;
using StarMap.Cll.Models.Cosmos;
using StarMap.Core.Models;
using StarMap.Urho;
using StarMap.ViewModels.Core;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Urho;

namespace StarMap.ViewModels
{
  public class MainPageViewModel : StarGazer<Universe, UniverseUrhoException>, IApplicationLifecycle
  {
    IDeviceRotation _motionDetector;

    private ObservantCollection<Constellation> _constellations;
    public ObservantCollection<Constellation> Constellations
    {
      get { return _constellations; }
      set { SetProperty(ref _constellations, value); }
    }

    private Star _selectedStar;
    public Star SelectedStar
    {
      get { return _selectedStar; }
      set { SetProperty(ref _selectedStar, value); }
    }

    private string _statusTextTemplate;
    public string StatusTextTemplate
    {
      get { return _statusTextTemplate; }
      set { SetProperty(ref _statusTextTemplate, value); }
    }

    private StarFilter _starFilter;
    public StarFilter StarFilter
    {
      get { return _starFilter; }
      set { SetProperty(ref _starFilter, value); }
    }

    private ObservableCollection<Star> _visibleStars;
    public ObservableCollection<Star> VisibleStars
    {
      get { return _visibleStars; }
      set { SetProperty(ref _visibleStars, value); }
    }

    //  give this one a different color.
    private Constellation _selectedConstellation;
    public Constellation SelectedConstellation
    {
      get { return _selectedConstellation; }
      set { SetProperty(ref _selectedConstellation, value, () => OnConstellationSelected(value)); }
    }

    private DelegateCommand _resetFiltersCommandCommand;
    public DelegateCommand ResetFiltersCommand =>
        _resetFiltersCommandCommand ?? (_resetFiltersCommandCommand = new DelegateCommand(ResetFilter));

    // Setting individual switches should be handled maybe:
    // extending the model here with an INotifyPropChanged implementation
    // that could add/remove the value to some collection in this VM
    // and each time that collection changes, GetStars is called with the filtered constellations.
    private DelegateCommand<object> _filterConstellationsCommand;
    public DelegateCommand<object> FilterConstellationsCommand =>
      // T could not be bool, weird.
        _filterConstellationsCommand ?? (_filterConstellationsCommand = new DelegateCommand<object>(FilterConstellations));

    private DelegateCommand _getStarsCommand;
    public DelegateCommand GetStarsCommand =>
        _getStarsCommand ?? (_getStarsCommand = new DelegateCommand(GetStars));

    public DelegateCommand SelectStarCommand { get; private set; }
    public DelegateCommand ShowStarDetailsCommand { get; private set; }

    public MainPageViewModel(INavigationService navigationService, IPageDialogService pageDialogService, IStarManager starManager, IDeviceRotation motionDetector) 
      : base(navigationService, pageDialogService, starManager)
    {
      _motionDetector = motionDetector;
      

      //SelectStarCommand = new DelegateCommand(SelectStar);
      ShowStarDetailsCommand = new DelegateCommand(ShowStarDetails, () => SelectedStar != null)
           // Cannot use ObservesCanExecute extension here, but it's OK to use ObservesProperty
           // (That way I dont have to ShowStarDetailsCommand.RaiseCanExecuteChanged() in the SelectedStar setter)
           // And it's fluent, and u can observe as many props as u want
           .ObservesProperty(() => SelectedStar);
    }

    #region methods

    private async void ShowStarDetails()
    //Another option:
    //Navigate($"StarDetailPage?id={SelectedStar.Id}");
    //=> await Navigate("StarDetailPage", "id", SelectedStar.Id);
    => await Navigate(new Uri(Navigation.DetailAbsolute, UriKind.Absolute), Navigation.Keys.StarId, SelectedStar.Id);

    private void OnConstellationSelected(Constellation c)
    {
      // TODO: All the stars in this constellation should be highlighted somehow.
      Debug.WriteLine(c?.Name ?? "null");
    }

    private void OnConstellationFiltered(object sender, PropertyChangedEventArgs e)
    {
      Constellation c = sender as Constellation;
      Debug.WriteLine($"{c.Name} is {(c.IsSelected ? "visible" : "hidden")}");
    }

    private void FilterConstellations(object command)
    {
      bool action = (bool)command;
      foreach (var c in Constellations)
        c.IsSelected = action;
    }

    private async void GetStars()
    {
      await Call(() =>
      {
        var stars = _starManager.GetStars(StarFilter);
        // TODO: verify if newing up is OK, or maybe Clear(), or something else.
        VisibleStars = new ObservableCollection<Star>(stars);

        Debug.WriteLine($"    Count {VisibleStars.Count}");
        Debug.WriteLine($"    Mag   {StarFilter.MagnitudeTo}");
        Debug.WriteLine($"    Dist  {StarFilter.DistanceTo}");
        Debug.WriteLine($"    Name  {StarFilter.DesignationQuery}");
      });
      
    }

    private void GetConstellations()
    {
      var constellations = _starManager.GetConstellations();
      Constellations = new ObservantCollection<Constellation>(constellations);
      Constellations.ElementChanged += OnConstellationFiltered;
    }

    private void ResetFilter()
    {
      //FilterConstellations(true);
      SelectedStar = null;
      Settings.Filter = null;
      StarFilter = new StarFilter();
      GetStars();
    }
    #endregion



    

    protected override async Task Restore(NavigationParameters parameters)
    {
      await base.Restore(parameters);
      SensorStart();

      if (Constellations != null)
        return;

      await Call(() =>
      {
        StarFilter = _starManager.LoadFilter();
        GetConstellations();
        GetStars();
      });
    }    

    protected override async Task CleanUp()
    {
      await base.CleanUp();
      // Since I call Navigate using the CallAsync, which sets IsBusy to true, and this method gets executed 
      // BEFORE the awaited navigation, it would never be executed (canExecute => !isBusy)
      // For now I just disabled the check for canExecute on the Call  method.
      await Call(() =>
      {
        SensorStop();
        Constellations.ElementChanged -= OnConstellationFiltered;
        Constellations.Clear();
        Constellations = null;
        VisibleStars.Clear();
        VisibleStars = null;
      });
    }

    private void OnRotationChanged(object sender, RotationChangedEventArgs e)
    {
      //Debug.WriteLine($"{e.Azimuth}, {e.Pitch}, {e.Roll}");
    }

    public void OnResume()
    {
      SensorStart();
    }

    public void OnSleep()
    {
      SensorStop();
    }

    void SensorStart()
    {
      if (Settings.SensorsOn)
      {
        _motionDetector.Start();
        _motionDetector.RotationChanged += OnRotationChanged;
      }
    }

    void SensorStop()
    {
      if (Settings.SensorsOn)
      {
        _motionDetector.Stop();
        _motionDetector.RotationChanged -= OnRotationChanged;
      }
    }

    public override async Task OnUrhoGenerated()
    {
      UrhoApplication.Input.TouchEnd += SelectStar;
      await Application.InvokeOnMainAsync(() => UrhoApplication.AddStars(VisibleStars));
    }

    private void SelectStar(TouchEndEventArgs obj)
    {
      SelectedStar = UrhoApplication.OnTouched(obj);

      if (SelectedStar != null)
      {
        // Setting this template seems a bit conflicted with the whole mvvm binding goodies. Just put few labels to bind to star properties.
        StatusTextTemplate = $"{(SelectedStar.ConstellationId != null ? Constellations.First(x => x.Id == SelectedStar.ConstellationId.Value).Abbreviation + " | " : "")}" +
          $"Star: {SelectedStar.Designation ?? "No designation"} | Distance: {SelectedStar.ParsecDistance} pc";
      }
      else
      {
        StatusTextTemplate = null;
      }
    }
  }
}
