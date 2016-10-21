using System;
using System.Collections.Generic;
using Npgsql;
using System.Text.RegularExpressions;
using NLog;

namespace AdCampaign.Model
{
    public class QueryTemplate
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public int TemplateId { get; private set; }
        public string QueryStr { get; private set; }
        public string TemplateName { get; private set; }
        public List<Parameter> Parameters { get; internal set; }
        public NpgsqlConnection Connection { get; set; }
        public string GeneratedQuery { get; private set; }
        public int NotEmptyParameterCount { get; private set; }

        public QueryTemplate(int templ_id, NpgsqlConnection connection)
        {
            if (connection == null) throw new ArgumentNullException("Connection can't be null.");
            Connection = connection;
            Parameters = new List<Parameter>();
            NpgsqlCommand command = new NpgsqlCommand(String.Format(@"SELECT  template_name, query FROM sel_template 
                                                                      WHERE templ_id = '{0}'::integer ORDER BY templ_id;
                                                                      SELECT param_name, param_identifier, param_query, sel_type, value_type 
                                                                      FROM sel_parameters p
                                                                      INNER JOIN sel_templ_param tp ON p.param_id = tp.parameter_id
                                                                      AND tp.template_id = '{0}'::integer
                                                                      ORDER BY param_id", templ_id), connection);
            try
            {
                connection.Open();
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        TemplateId = templ_id;
                        TemplateName = reader["template_name"] as string;
                        QueryStr = reader["query"] as string;
                        if (String.IsNullOrEmpty(TemplateName)) throw new ArgumentException("TemplateName can't be null or empty");
                    }

                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            Parameter p;
                            short sel_type = Convert.ToInt16(reader["sel_type"]);
                            switch (sel_type)
                            {
                                case 0:
                                    p = new ParameterText(reader["param_name"] as string, reader["param_identifier"] as string, reader["value_type"] as string);
                                    this.Parameters.Add(p);
                                    break;
                                case 1:
                                    p = new ParameterCheckList(reader["param_name"] as string, reader["param_identifier"] as string, reader["value_type"] as string, reader["param_query"] as string, connection);
                                    this.Parameters.Add(p);
                                    break;
                                case 2:
                                    string value_type = reader["value_type"] as string;
                                    if (value_type == "date")
                                    {
                                        p = new ParameterRangeDate(reader["param_name"] as string, reader["param_identifier"] as string);
                                    }
                                    else
                                    {
                                        p = new ParameterRange(reader["param_name"] as string, reader["param_identifier"] as string, reader["value_type"] as string);
                                    }
                                    this.Parameters.Add(p);
                                    break;
                                default:
                                    throw new ArgumentException("Invalid sellection type");
                            }
                        }
                    }
                }
                foreach (Parameter p in this.Parameters)
                {
                    if (p is ParameterCheckList) (p as ParameterCheckList).SetVariants();
                }
                if (TemplateName == null || TemplateId == 0) throw new ArgumentException(String.Format("Query template with templ_id = {0} doesn't exists!", templ_id));
            }
            catch (Exception ex)
            {
                logger.ErrorException(ex.Message, ex);
                throw new InvalidOperationException(ex.Message);
            }
            finally
            {
                connection.Close();
                command.Dispose();
            }
        }

        public string GenerateQuery()
        {
            string resQueryStr = this.QueryStr;
            foreach (Parameter p in Parameters)
            {
                string paramvalue = p.GetValue();
                if (!String.IsNullOrEmpty(paramvalue))
                {
                    resQueryStr = resQueryStr.Replace("@" + p.ParameterIdentifier, paramvalue);
                    NotEmptyParameterCount++;
                }
            }
            resQueryStr = Regex.Replace(resQueryStr, @"\[[^\]@]+@[^\]@]+\]", "");
            resQueryStr = Regex.Replace(resQueryStr, @"[\[\]]", "");
            return resQueryStr;
        }
    }
}
