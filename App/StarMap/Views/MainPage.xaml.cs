﻿using StarMap.Cll.Abstractions.Urho;
using StarMap.Controls;
using System;
using System.Threading.Tasks;
using Urho.Forms;
using Xamarin.Forms;
using a = Urho;

namespace StarMap.Views
{
  public partial class MainPage : ContentPage
  {
    public Overlay RightPanel { get; private set; }

    public MainPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
      base.OnAppearing();      

      RightPanel = new Overlay(rightOverlay, DockSide.Right, rightPanelButtons.Width + rightOverlay.Padding.Left * 2);

      RightPanel.Collapse(length: 0);

      await ((IUrhoHandler)BindingContext).GenerateUrho(surface);      
    }

    protected override async void OnDisappearing()
    {
      base.OnDisappearing();
      await a.Application.Current.Exit();
      UrhoSurface.OnDestroy();
    }

    void OnConstellationsButtonClicked(object sender, EventArgs args)
    {
      if (RightPanel.IsExpanded && constellationFilters.IsVisible)
        RightPanel.Collapse(Easing.CubicOut);
      else
        RightPanel.Expand(Easing.CubicIn);

      starFilters.IsVisible = false;
      constellationFilters.IsVisible = true;      
    }

    void OnStarFilterPanelButtonClicked(object sender, EventArgs args)
    {
      if (RightPanel.IsExpanded && starFilters.IsVisible)
        RightPanel.Collapse(Easing.CubicOut);
      else
        RightPanel.Expand(Easing.CubicIn);

      constellationFilters.IsVisible = false;
      starFilters.IsVisible = true;
    }
  }
}
