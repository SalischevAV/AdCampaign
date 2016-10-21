using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using Npgsql;
using System.Windows.Input;

namespace AdCampaign.ViewModel
{
    class ConnectionView : ViewModelBase
    {
        public ObservableCollection<ConnectionViewItem> Items { get; private set; }
        ConnectionViewItem curritem;
        public ICommand EnterCommand { get; private set; }
        public ConnectionViewItem CurrentItem 
        {
            get { return curritem; }
            set
            {
                curritem = value;
                OnPropertyChanged("CurrentItem");
            }
        }
        XDocument xdoc;
        string Path { get; set; }
        public ConnectionView(string path)
        { 
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException("The path of xml file is enpty.");
            Path = path;
            xdoc = XDocument.Load(path);
            Items = new ObservableCollection<ConnectionViewItem>();
            foreach (XElement xe in xdoc.Root.Elements("Connection"))
            {
                ConnectionViewItem ci = new ConnectionViewItem(xe);
                Items.Add(ci);
            }
            CurrentItem = Items.Where(i => i.IsStartUp).First();
            EnterCommand = new BaseCommand(p => { }, p => !String.IsNullOrEmpty(CurrentItem.UserName));
        }

        public string GetConnectionString(string password)
        {
            NpgsqlConnectionStringBuilder csb = new NpgsqlConnectionStringBuilder();
            csb.Host = CurrentItem.Host;
            csb.Port = CurrentItem.Port;
            csb.Database = CurrentItem.Database;
            csb.UserName = CurrentItem.UserName;
            csb.CommandTimeout = CurrentItem.CommandTimeout;
            csb.Add("Password", password);
            return csb.ConnectionString;
        }

        public void Save()
        {
            foreach (ConnectionViewItem cvi in this.Items)
            {
                cvi.IsStartUp = false;
            }
            CurrentItem.IsStartUp = true;
            xdoc.Save(Path);
        }

        public class ConnectionViewItem : ViewModelBase
        {
            XElement ConnectionElement;
#region Connection viewers
            public bool IsStartUp
            {
                get { return bool.Parse(ConnectionElement.Attribute("IsStartUp").Value); }
                set
                {
                    ConnectionElement.Attribute("IsStartUp").SetValue(value);
                }
            }
            public string ConnectionName
            {
                get { return ConnectionElement.Attribute("Name").Value; }
                set
                {
                    ConnectionElement.Attribute("Name").SetValue(value);
                    OnPropertyChanged("ConnectionName");
                }
            }
            public string Host
            {
                get { return ConnectionElement.Element("Host").Value; }
                set
                {
                    ConnectionElement.Element("Host").SetValue(value);
                    OnPropertyChanged("Host");
                }
            }
            public int Port
            {
                get { return Int32.Parse(ConnectionElement.Element("Port").Value); }
                set
                {
                    ConnectionElement.Element("Port").SetValue(value);
                    OnPropertyChanged("Port");
                }
            }
            public string Database
            {
                get { return ConnectionElement.Element("Database").Value; }
                set
                {
                    ConnectionElement.Element("Database").SetValue(value);
                    OnPropertyChanged("Database");
                }
            }
            public string UserName
            {
                get { return ConnectionElement.Element("UserName").Value; }
                set
                {
                    ConnectionElement.Element("UserName").SetValue(value);
                    OnPropertyChanged("UserName");
                }
            }
            public int CommandTimeout
            {
                get { return Int32.Parse(ConnectionElement.Element("CommandTimeout").Value); }
                set
                {
                    ConnectionElement.Element("CommandTimeout").SetValue(value);
                    OnPropertyChanged("CommandTimeout");
                }
            }
#endregion
            public ConnectionViewItem(XElement xe)
            {
                ConnectionElement = xe;
            }
        }
    }
}
