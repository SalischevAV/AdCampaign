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
    /// Interaction logic for CampaignResult.xaml
    /// </summary>
    public partial class CampaignResult : Page
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public CampaignResult()
        {
            try
            {
                InitializeComponent();
                grCampaignResult.DataContext = new CampaignView(Authorization.Connection, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                logger.Fatal(ex.ToString());
                App.Current.Windows[0].Close();
            }
        }

        public CampaignResult(int campid)
        {
            try
            {
                InitializeComponent();
                grCampaignResult.DataContext = new CampaignView(campid, Authorization.Connection, false);
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
