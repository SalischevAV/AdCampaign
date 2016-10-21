using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Npgsql;
using System.Text.RegularExpressions;

namespace CampCreator
{
    public class Parameter
    {
        public string ParameterName { get; private set; }
        public string ParameterIdentifier { get; private set; }
        public short SelectionType { get; private set; }
        public string ValueType { get; private set; }
        internal string ParameterQuery { get; set; }
        public Dictionary<string, object> Variants { get; internal set; }
        internal string Value { get; set; }

        internal Parameter(string paramname, string paramident, short stype, string vtype)
        {
            ParameterName = paramname;
            ParameterIdentifier = paramident;
            SelectionType = stype;
            ValueType = vtype;
            if (ParameterName == null || ParameterIdentifier == null || ValueType == null) throw new ArgumentNullException("Arguments 'ParameterName' and 'ParameterIdetifier' and 'ValueType' can't be null.");
            if (SelectionType < 0 || SelectionType > 2) throw new ArgumentException("Condition  '0 <= SelectionType <= 2' is not performed.");
        }

        public void SetValue(IEnumerable<object> arr)
        {
            if (arr == null) throw new ArgumentNullException("Argument arr of function SetValue can't be null");
            if (SelectionType != 1) new ApplicationException("Wrong logic of programm. Selection type must be 1 for this method version.");
            string acc = "";
            foreach (object i in arr)
            {
                acc += String.Format("'{0}'::{1}, ", i, ValueType);
            }
            this.Value = acc.TrimEnd(new char[] { ',', ' ' });
        }

        public void SetValue(string value1, string value2)
        {
            if (String.IsNullOrEmpty(value1) || String.IsNullOrEmpty(value2)) throw new ArgumentNullException("Arguments of function SetValue can't be null");
            if (SelectionType != 2) new ApplicationException("Wrong logic of programm. Selection type must be 2 for SetValue(IEnumerable) method version.");
            if (ValueType.Contains("date") || ValueType.Contains("time"))
            {
                if (DateTime.Parse(value1) <= DateTime.Parse(value2))
                {
                    this.Value = "'{0}'::{1} AND '{2}'::{1}";
                }
                else
                {
                    this.Value = "'{0}'::{1} OR '{2}'::{1}";
                }
            }
            else if (ValueType.Contains("int"))
            {
                if (Int64.Parse(value1) <= Int64.Parse(value2))
                {
                    this.Value = "'{0}'::{1} AND '{2}'::{1}";
                }
                else
                {
                    this.Value = "'{0}'::{1} OR '{2}'::{1}";
                }
            }
            else
            {
                throw new ArgumentException("Wrong values of arguments of SetValue(string, string) method.");
            }
            this.Value = String.Format(Value, value1, ValueType, value2);
        }

        public void SetValue(string value)
        {
            this.Value = String.Format("'{0}'::{1}", value, ValueType);
        }
    }

    public class QueryTemplate
    {
        public int TemplateId { get; private set; }
        public string QueryStr { get; private set; }
        public string TemplateName { get; private set; }
        public Dictionary<string, Parameter> Parameters { get; internal set; }

        public QueryTemplate(int templ_id, NpgsqlConnection connection)
        {
            Parameters = new Dictionary<string, Parameter>();
            NpgsqlCommand command = new NpgsqlCommand(String.Format(@"SELECT  template_name, query FROM sel_template WHERE templ_id = '{0}'::int;
                                                                      SELECT param_name, param_identifier, param_query, sel_type, value_type 
                                                                      FROM sel_parameters p
                                                                      INNER JOIN sel_templ_param tp ON p.param_id = tp.parameter_id
                                                                      AND tp.template_id = '{0}'::int", templ_id), connection);
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
                            short sel_type = Convert.ToInt16(reader["sel_type"]);
                            Parameter param = new Parameter(reader["param_name"] as string,
                                    reader["param_identifier"] as string, sel_type, reader["value_type"] as string);
                            switch (sel_type)
                            {
                                case 0:
                                case 2:
                                    this.Parameters.Add(param.ParameterIdentifier, param);
                                    break;
                                case 1:
                                    param.ParameterQuery = reader["param_query"] as string;
                                    this.Parameters.Add(param.ParameterIdentifier, param);
                                    break;
                                default:
                                    throw new ArgumentException("Invalid sellection type");
                            }
                        }
                    }
                }
                foreach (Parameter p in this.Parameters.Values)
                {
                    if (p.SelectionType == 1)
                    {
                        if (String.IsNullOrEmpty(p.ParameterQuery)) throw new ArgumentNullException("Parameter query can't be null if parameter has sellection type 1.");
                        p.Variants = new Dictionary<string, object>();
                        using (NpgsqlCommand cmd = new NpgsqlCommand(p.ParameterQuery, connection))
                        {
                            using (NpgsqlDataReader r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    try
                                    {
                                        p.Variants.Add(r[0].ToString(), r[1]);
                                    }
                                    catch (IndexOutOfRangeException)
                                    {
                                        throw new ArgumentException("Parameter has not correct query. Query must return two fields: name and id");
                                    }
                                }
                            }
                        }
                    }
                }
                if (TemplateName == null || TemplateId == 0) throw new ArgumentException(String.Format("Query template with templ_id = {0} doesn't exists!", templ_id));
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
            foreach (Parameter p in Parameters.Values)
            {
                if (!String.IsNullOrEmpty(p.Value))
                {
                    resQueryStr = resQueryStr.Replace("@" + p.ParameterIdentifier, p.Value);
                }
            }
            resQueryStr = Regex.Replace(resQueryStr, @"\[[^\]@]+@[^\]@]+\]", "");
            resQueryStr = Regex.Replace(resQueryStr, @"[\[\]]", "");
            return resQueryStr;
        }
    }
}
