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
    /// Interaction logic for ActionTerms.xaml
    /// </summary>
    public partial class ActionTerms : Page
    {
        CampaignView cv;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public ActionTerms()
        {
            InitializeComponent();
        }

        public ActionTerms(string query) 
            : this()
        {
            try
            {
                cv = new CampaignView(query, Authorization.Connection);
                ((BaseCommand)cv.ConfirmCampaignCreation).Executed += () =>
                    {
                        this.NavigationService.Content = new CampaignEditor(cv.CampaignId);
                    };
                grActionTerms.DataContext = cv;
            }
            catch (Exception ex)
            {
                logger.Fatal(ex.ToString());
                Authorization.Connection.Close();
                throw new Exception(ex.Message, ex.InnerException);
            }
        }

        private void NavigateBackToSelectCards(object sender, RoutedEventArgs e)
        {
            this.NavigationService.GoBack();
        }

        private void btnExportCards_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.SaveFileDialog fileDialog = new Microsoft.Win32.SaveFileDialog();
                fileDialog.DefaultExt = "csv";
                fileDialog.Filter = "CSV file (*.csv)|*.csv|Text file (*.txt)|*.txt";
                fileDialog.FileName = "phones.csv";
                if (fileDialog.ShowDialog() == true)
                {
                    if (((Button)sender).Name == "btnExportCards")
                    {
                        cv.ExportPhonesAndCardsToCsv.Execute(fileDialog.InitialDirectory + fileDialog.FileName);
                    }
                    else
                    {
                        cv.ExportPhonesToCsv.Execute(fileDialog.InitialDirectory + fileDialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Export error", ex);
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
