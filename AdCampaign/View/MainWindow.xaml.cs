using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Npgsql;
using AdCampaign.ViewModel;

namespace AdCampaign.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Authorization.Connection != null)
            {
                Authorization.Connection.Close();
                Authorization.Connection.Dispose();
                NpgsqlConnection.ClearAllPools();
            }
        }

        private void NavigateToSelectCards(object sender, RoutedEventArgs e)
        {
            UiServices.SetBusyState();
            Workspace.Content = new SelectCards();
            UiServices.SetBusyState();
        }

        private void NavigateToCampaignEditor(object sender, RoutedEventArgs e)
        {
            UiServices.SetBusyState();
            Workspace.Content = new CampaignEditor();
            UiServices.SetBusyState();
        }

        private void NavigateToCampaignResult(object sender, RoutedEventArgs e)
        {
            UiServices.SetBusyState();
            Workspace.Content = new CampaignResult();
            UiServices.SetBusyState();
        }

        private void NavigateToStartPage(object sender, RoutedEventArgs e)
        {
            UiServices.SetBusyState();
            Workspace.Content = new StartPage();
            UiServices.SetBusyState();
        }

        private void Workspace_Navigated(object sender, NavigationEventArgs e)
        {
            if (Authorization.Connection == null || Authorization.Connection.State == System.Data.ConnectionState.Open) Menu.Visibility = Visibility.Hidden;
            else Menu.Visibility = Visibility.Visible;
        }
    }
}
