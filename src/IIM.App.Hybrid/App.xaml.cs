using System;
using Microsoft.Maui.Controls;
namespace IIM.App.Hybrid;
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new MainPage();
    }
}
