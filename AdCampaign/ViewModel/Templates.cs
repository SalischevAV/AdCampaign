using System;
using System.Collections.Generic;
using Npgsql;
using NLog;

namespace AdCampaign.ViewModel
{
    public class Template
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
    }

    public class TemplatesCollection : ViewModelBase
    {
        private int index { get; set; }
        public NpgsqlConnection Connection { get; private set; }
        public static Logger logger = LogManager.GetCurrentClassLogger(); 

        public int CurrentTemplate
        {
            get { return index; }
            set 
            {
                index = value;
                OnPropertyChanged("CurrentTemplateId");
                OnCurrTemplateIdChanged();
            }
        }

        public List<Template> TemplatesList { get; private set; }

        public TemplatesCollection(NpgsqlConnection connection)
        {
            Connection = connection;
            index = -1;
            this.TemplatesList = Execute("SELECT templ_id, template_name FROM sel_template ORDER BY templ_id", Connection);
            if (TemplatesList == null || TemplatesList.Count < 1) throw new ApplicationException("List of templates is empty.");
        }

        List<Template> Execute(string query, NpgsqlConnection connection)
        {
            if (connection == null) throw new ArgumentNullException("Connection can't be null.");
            List<Template> lst = new List<Template>();
            NpgsqlCommand command = new NpgsqlCommand(query, connection);
            try
            {
                connection.Open();
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Template item = new Template();
                        item.TemplateId = reader.GetInt32(reader.GetOrdinal("templ_id"));
                        item.TemplateName = reader["template_name"] as string;
                        lst.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.ErrorException(ex.Message, ex);
                throw new Exception(ex.ToString());
            }
            finally
            {
                command.Dispose();
                connection.Close();
            }
            return lst;
        }

#region Event works when CurrentTemplateId will be changed
        public event Action CurrTemplateChanged;
        void OnCurrTemplateIdChanged()
        {
            if (CurrTemplateChanged != null)
                CurrTemplateChanged();
        }
#endregion
    }
}
