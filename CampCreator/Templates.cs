using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Npgsql;

namespace CampCreator
{
    public class Template
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
    }

    public class Templates
    {
        private int index { get; set; }

        public Template CurrentTemplate
        {
            get { if (TemplatesList != null) return TemplatesList[index]; else throw new ApplicationException("List of templates is null."); }
        }

        public List<Template> TemplatesList { get; private set; }

        public Templates(NpgsqlConnection connection)
        {
            this.TemplatesList = Execute("SELECT templ_id, template_name FROM sel_template", connection);
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
                throw new Exception(ex.ToString());
            }
            finally
            {
                command.Dispose();
                connection.Close();
            }
            return lst;
        }
    }
}
