using System;
using System.Windows;
using System.Windows.Controls;
using AdCampaign.ViewModel;
using NLog;
using Npgsql;

namespace AdCampaign.View
{
    /// <summary>
    /// Interaction logic for Authorization.xaml
    /// </summary>
    public partial class Authorization : Page
    {
        public static NpgsqlConnection Connection { get; private set; }
        private static Logger logger = LogManager.GetCurrentClassLogger();
        
        public Authorization()
        {
            InitializeComponent();
            ConnectionView cv = new ConnectionView("Connections.xml");
            grAuthorization.DataContext = cv;
            ((BaseCommand)cv.EnterCommand).Executed += () => 
            {
                Connection = new NpgsqlConnection(cv.GetConnectionString(pbPassword.Password));
                UiServices.SetBusyState();
                try
                {
                    Connection.Open();
                    Window win = (Window)Window.GetWindow(this);
                    win.Title = String.Format("CampaignCreator ({0} : {1})", cv.CurrentItem.ConnectionName, cv.CurrentItem.UserName);
                    this.NavigationService.Content = new StartPage();
                    logger.Info("Произведен вход:\r\n Хост: {0}:{1}\r\n База данных: {2}\r\n Пользователь: {3}",
                        cv.CurrentItem.Host, cv.CurrentItem.Port, cv.CurrentItem.Database, cv.CurrentItem.UserName);
                    cv.Save();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка подключения к базе данных. " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    logger.Error("Ошибка подключения к базе данных. " + ex.Message);
                }
                finally
                {
                    UiServices.SetBusyState();
                    Connection.Close();
                }
            };
        }
    }
}
