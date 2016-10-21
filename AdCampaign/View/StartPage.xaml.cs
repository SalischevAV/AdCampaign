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
using AdCampaign.ViewModel;

namespace AdCampaign.View
{
    /// <summary>
    /// Interaction logic for StartPage.xaml
    /// </summary>
    public partial class StartPage : Page
    {
        public StartPage()
        {
            InitializeComponent();
        }

        private void GoToCampaignResult(object sender, RoutedEventArgs e)
        {
            UiServices.SetBusyState();
            this.NavigationService.Content = new CampaignResult();
            UiServices.SetBusyState();
        }

        private void GoToCampaignEditor(object sender, RoutedEventArgs e)
        {
            UiServices.SetBusyState();
            this.NavigationService.Content = new CampaignEditor();
            UiServices.SetBusyState();
        }

        private void GoToSelectCards(object sender, RoutedEventArgs e)
        {
            UiServices.SetBusyState();
            this.NavigationService.Content = new SelectCards();
            UiServices.SetBusyState();
        }
    }
}
