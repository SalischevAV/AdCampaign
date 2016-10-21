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
using NLog;

namespace AdCampaign.View
{
    /// <summary>
    /// Interaction logic for CampaignEditor.xaml
    /// </summary>
    public partial class CampaignEditor : Page
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public CampaignEditor()
        {
            try
            {
                InitializeComponent();
                CampaignView cv = new CampaignView(Authorization.Connection, true);
                ((BaseCommand)cv.StartCampaignCommand).Executed += () =>
                    {
                        if (!String.IsNullOrEmpty(cv.StartCampaignCommandError))
                        {
                            MessageBox.Show(cv.StartCampaignCommandError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        this.NavigationService.Content = new CampaignResult(cv.CampaignId);
                    };
                grCampaignEditor.DataContext = cv;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                logger.Fatal(ex.ToString());
                App.Current.Windows[0].Close();
            }
        }

        public CampaignEditor(int id)
        {
            try
            {
                InitializeComponent();
                CampaignView cv = new CampaignView(id, Authorization.Connection, true);
                ((BaseCommand)cv.StartCampaignCommand).Executed += () => 
                    {
                        if (!String.IsNullOrEmpty(cv.StartCampaignCommandError))
                        {
                            MessageBox.Show(cv.StartCampaignCommandError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        this.NavigationService.Content = new CampaignResult(cv.CampaignId);
                    };
                grCampaignEditor.DataContext = cv;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                logger.Fatal(ex.ToString());
                App.Current.Windows[0].Close();
            }
        }
    }
}
