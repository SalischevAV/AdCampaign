using System;
using System.Windows;
using System.Windows.Controls;
using AdCampaign.ViewModel;
using NLog;

namespace AdCampaign.View
{
    /// <summary>
    /// Interaction logic for SelectCards.xaml
    /// </summary>
    public partial class SelectCards : Page
    {
        QueryTemplateView qtv;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public SelectCards()
        {
            try
            {
                InitializeComponent();
                qtv = new QueryTemplateView(Authorization.Connection);
                grCampaign.DataContext = qtv;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                logger.Fatal(ex.ToString());
                App.Current.Windows[0].Close();
            }
        }

        private void NavigateToActionTerms(object sender, RoutedEventArgs e)
        {
            try
            {
                string query = qtv.GetQueryString();
                if (!qtv.HasWhere)
                {
                    if (MessageBox.Show("Внимание! Вы не выбрали ни одного параметра, в следствии чего будут выбраны все данные из базы соответствующие текущему шаблону. Вы желаете продолжить?",
                        "Внимание!!!", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return;
                }
                this.NavigationService.Content = new ActionTerms(query);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                logger.ErrorException(ex.Message, ex);
            }
        }
    }
}
