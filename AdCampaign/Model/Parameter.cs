using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Npgsql;

namespace AdCampaign.Model
{
    public abstract class Parameter
    {
        public string ParameterName { get; protected set; }
        public string ParameterIdentifier { get; protected set; }
        public string ValueType { get; protected set; }
        public abstract string GetValue();
    }

#region type = 1
    public class CheckListItem
    {
        public string Text { get; set; }
        public object Value { get; set; }
        public bool Checked { get; set; }

        public CheckListItem(string text, object value)
        {
            Text = text;
            Value = value;
            Checked = false;
        }
    }
    /// <summary>
    /// TYPE = 1
    /// </summary>
    public class ParameterCheckList : Parameter
    {
        public List<CheckListItem> Variants { get; internal set; }
        NpgsqlConnection connection;
        string query;
        internal ParameterCheckList(string paramname, string paramident, string vtype, string paramquery, NpgsqlConnection conn)
        {
            if (conn == null || paramquery == null) throw new ArgumentNullException("Connection and parameter query can't be null");
            connection = conn;
            query = paramquery;
            ParameterName = paramname;
            ParameterIdentifier = paramident;
            ValueType = vtype;
            Variants = new List<CheckListItem>();
            if (String.IsNullOrEmpty(paramquery)) throw new ArgumentNullException("Parameter query can't be null if parameter has sellection type 1.");
            if (ParameterName == null || ParameterIdentifier == null || ValueType == null) throw new ArgumentNullException("Arguments 'ParameterName', 'ParameterIdentifier' and 'ValueType' can't be null.");
        }

        public void SetVariants()
        {
            using (NpgsqlCommand cmd = new NpgsqlCommand(query, connection))
            {
                using (NpgsqlDataReader r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        try
                        {
                            CheckListItem item = new CheckListItem(r[0] as string, r[1]);
                            this.Variants.Add(item);
                        }
                        catch (IndexOutOfRangeException)
                        {
                            throw new ArgumentException("Parameter has not correct query. Query must return two fields: name and id");
                        }
                    }
                }
            }
        }

        public override string GetValue()
        {
            if (Variants.Count < 1) throw new InvalidOperationException(String.Format("Parameter {0} has no variants", ParameterName));
            string acc = "";
            foreach (CheckListItem i in Variants)
            {
                if (i.Checked) acc += String.Format("'{0}'::{1}, ", i.Value, ValueType);
            }
            return acc.TrimEnd(new char[] { ',', ' ' });
        }
    }
#endregion

#region type = 2
    /// <summary>
    /// TYPE = 2
    /// </summary>
    public class ParameterRange : Parameter
    {
        public string Value1 { get; set; }
        public string Value2 { get; set; }

        internal ParameterRange(string paramname, string paramident, string vtype)
        {
            ParameterName = paramname;
            ParameterIdentifier = paramident;
            ValueType = vtype;
            if (ParameterName == null || ParameterIdentifier == null || ValueType == null) throw new ArgumentNullException("Arguments 'ParameterName', 'ParameterIdentifier' and 'ValueType' can't be null.");
        }

        public override string GetValue()
        {
            if (String.IsNullOrEmpty(Value1) || String.IsNullOrEmpty(Value2)) return null;
            string ParameterValue;
            if (ValueType == "time")
            {

                if (DateTime.Parse(TimeSpan.Parse(Value1).ToString()) <= DateTime.Parse(TimeSpan.Parse(Value2).ToString()))
                {
                    ParameterValue = "'{0}'::{1} AND '{2}'::{1}";
                }
                else
                {
                    ParameterValue = "'{0}'::{1} OR '{2}'::{1}";
                }
            }
            else if (ValueType.Contains("date") || ValueType.Contains("time"))
            {
                DateTime v1 = DateTime.Parse(Value1);
                DateTime v2 = DateTime.Parse(Value2);
                Value1 = v1.ToString("o");
                Value2 = v2.ToString("o");
                if (v1 <= v2)
                {
                    ParameterValue = "'{0}'::{1} AND '{2}'::{1}";
                }
                else
                {
                    ParameterValue = "'{0}'::{1} OR '{2}'::{1}";
                }
            }
            else if (ValueType.Contains("int"))
            {
                if (Int64.Parse(Value1) <= Int64.Parse(Value2))
                {
                    ParameterValue = "'{0}'::{1} AND '{2}'::{1}";
                }
                else
                {
                    ParameterValue = "'{0}'::{1} OR '{2}'::{1}";
                }
            }
            else
            {
                throw new ArgumentException("Wrong values of arguments of SetValue(string, string) method.");
            }
            return String.Format(ParameterValue, Value1, ValueType, Value2);
        }

    }
#endregion

#region type = 2, value_type = date
    class ParameterRangeDate : ParameterRange 
    {
        internal ParameterRangeDate(string paramname, string paramident)
            : base(paramname, paramident, "date")
        { 
        }
    }
#endregion

#region type = 0
    /// <summary>
    /// TYPE = 0
    /// </summary>
    public class ParameterText : Parameter
    {
        public string Value { get; set; }

        internal ParameterText(string paramname, string paramident, string vtype)
        {
            ParameterName = paramname;
            ParameterIdentifier = paramident;
            ValueType = vtype;
            if (ParameterName == null || ParameterIdentifier == null || ValueType == null) throw new ArgumentNullException("Arguments 'ParameterName', 'ParameterIdentifier' and 'ValueType' can't be null.");
        }

        public override string GetValue()
        {
            if (String.IsNullOrEmpty(Value)) return null;
            return String.Format("'{0}'::{1}", Value, ValueType);
        }
    }
#endregion
}
