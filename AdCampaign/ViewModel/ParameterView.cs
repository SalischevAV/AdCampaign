using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdCampaign.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AdCampaign.ViewModel
{
#region TYPE = 0
    public class ParameterTextView : ViewModelBase, IDataErrorInfo 
    {
        public string ParameterName
        {
            get { return Param.ParameterName; }
        }
        ParameterText Param;

        public string Value 
        {
            get { return Param.Value; }
            set
            {
                Param.Value = value;
                OnPropertyChanged("Value");
            }
        }

        public ParameterTextView(ParameterText p)
        {
            Param = p;
        }

#region IDataErrorInfo
        string error;
        public string Error
        {
            get { return error; }
            private set
            {
                error = value;
                OnPropertyChanged("Error");
            }
        }
        public string this[string columnName]
        {
            get
            {
                Error = null;
                switch (columnName)
                { 
                    case "Value":
                        if (!String.IsNullOrEmpty(this.Value))
                        {
                            if (Param.ValueType.Contains("int") || Param.ValueType.Contains("numeric"))
                            {
                                int val;
                                if (!Int32.TryParse(this.Value, out val)) Error = "Поле должно содержать число.";
                            }
                            else if (Param.ValueType.Contains("time") || Param.ValueType.Contains("date"))
                            {
                                DateTime dt;
                                if (!DateTime.TryParse(this.Value, out dt)) Error = "Поле должно содержать дату или время";
                            }
                        }
                        break;
                }
                return Error;
            }
        }
#endregion
    }
#endregion 

#region TYPE = 2
    public class ParameterRangeView : ViewModelBase, IDataErrorInfo
    {
        public string ParameterName
        {
            get { return Param.ParameterName; }
        }
        ParameterRange Param;

        public string Value1
        {
            get { return Param.Value1; }
            set
            {
                Param.Value1 = value;
                OnPropertyChanged("Value1");
                OnPropertyChanged("Value2");
            }
        }

        public string Value2
        {
            get { return Param.Value2; }
            set
            {
                Param.Value2 = value;
                OnPropertyChanged("Value2");
                OnPropertyChanged("Value1");
            }
        }

        public ParameterRangeView(ParameterRange p)
        {
            Param = p;
        }

#region IDataErrorInfo
        public string Error
        {
            get;
            set;
        }
        public string this[string columnName]
        {
            get
            {
                string error = String.Empty;
                switch (columnName)
                {
                    case "Value1":
                        if (!String.IsNullOrEmpty(this.Value1))
                        {
                            if (String.IsNullOrEmpty(this.Value1) ^ String.IsNullOrEmpty(this.Value2)) error = "Укажите диапазон";
                            if (Param.ValueType.Contains("int") || Param.ValueType.Contains("numeric"))
                            {
                                int val;
                                if (!Int32.TryParse(this.Value1, out val)) error = "Поле должно содержать число.";
                            }
                            else if (Param.ValueType == "time")
                            {
                                TimeSpan t;
                                DateTime dt;
                                if (!TimeSpan.TryParse(this.Value1, out t)) error = "Поле должно содержать время (чч:мм)";
                                else if (!DateTime.TryParse(t.ToString(), out dt)) error = "Поле должно содержать время (чч:мм)";
                            }
                            else if (Param.ValueType.Contains("time") || Param.ValueType.Contains("date"))
                            {
                                DateTime dt;
                                if (!DateTime.TryParse(this.Value1, out dt)) error = "Поле должно содержать дату (дд.ММ.гггг) и время (чч:мм)";
                            }
                        }
                        break;
                    case "Value2":
                        if (!String.IsNullOrEmpty(this.Value2))
                        {
                            if (String.IsNullOrEmpty(this.Value1) ^ String.IsNullOrEmpty(this.Value2)) error = "Укажите диапазон";
                            if (Param.ValueType.Contains("int") || Param.ValueType.Contains("numeric"))
                            {
                                int val;
                                if (!Int32.TryParse(this.Value2, out val)) error = "Поле должно содержать число.";
                            }
                            else if (Param.ValueType == "time")
                            {
                                TimeSpan t;
                                DateTime dt;
                                if (!TimeSpan.TryParse(this.Value2, out t)) error = "Поле должно содержать время (чч:мм)";
                                else if (!DateTime.TryParse(t.ToString(), out dt)) error = "Поле должно содержать время (чч:мм)";
                            }
                            else if (Param.ValueType.Contains("time") || Param.ValueType.Contains("date"))
                            {
                                DateTime dt;
                                if (!DateTime.TryParse(this.Value2, out dt)) error = "Поле должно содержать дату (дд.ММ.гггг) и время (чч:мм)";
                            }
                        }
                        break;
                }
                Error = error;
                return error;
            }
        }
#endregion
    }
#endregion

#region TYPE = 2, DATE
    class ParameterRangeDateView : ViewModelBase, IDataErrorInfo
    { 
        public string ParameterName
        {
            get { return Param.ParameterName; }
        }
        ParameterRangeDate Param;

        public string Value1
        {
            get { return Param.Value1; }
            set
            {
                Param.Value1 = value;
                OnPropertyChanged("Value1");
                OnPropertyChanged("Value2");
            }
        }

        public string Value2
        {
            get { return Param.Value2; }
            set
            {
                Param.Value2 = value;
                OnPropertyChanged("Value2");
                OnPropertyChanged("Value1");
            }
        }

        public ParameterRangeDateView(ParameterRangeDate p)
        {
            Param = p;
        }

#region IDataErrorInfo
        public string Error
        {
            get;
            set;
        }
        public string this[string columnName]
        {
            get
            {
                string error = String.Empty;
                switch (columnName)
                {
                    case "Value1":
                        if (!String.IsNullOrEmpty(this.Value1))
                        {
                            if (String.IsNullOrEmpty(this.Value1) ^ String.IsNullOrEmpty(this.Value2)) error = "Укажите диапазон";
                        }
                        break;
                    case "Value2":
                        if (!String.IsNullOrEmpty(this.Value2))
                        {
                            if (String.IsNullOrEmpty(this.Value1) ^ String.IsNullOrEmpty(this.Value2)) error = "Укажите диапазон";
                        }
                        break;
                }
                Error = error;
                return error;
            }
        }
#endregion
    }
#endregion

#region TYPE = 1
    public class CheckListItemView : ViewModelBase
    {
        public string Text
        {
            get { return Item.Text; }
        }
        public object Value 
        {
            get { return Item.Value; }
        }
        public bool Checked 
        {
            get { return Item.Checked; }
            set
            {
                Item.Checked = value;
                OnPropertyChanged("Checked");
            }
        }
        CheckListItem Item;

        public CheckListItemView(CheckListItem item)
        {
            Item = item;
        }
    }

    public class ParameterCheckListView : ViewModelBase, IDataErrorInfo
    {
        public string ParameterName
        {
            get { return Param.ParameterName; }
        }
        ParameterCheckList Param;

        public ObservableCollection<CheckListItemView> VariantsView { get; set; }

        public ParameterCheckListView(ParameterCheckList p)
        {
            Param = p;
            VariantsView = new ObservableCollection<CheckListItemView>();
            foreach (CheckListItem item in Param.Variants)
            {
                CheckListItemView itemview = new CheckListItemView(item);
                VariantsView.Add(itemview);
            }
        }

#region IDataErrorInfo
        public string Error { get { return null; } }
        public string this[string columnName]
        {
            get{ return Error; }
        }
#endregion
    }
#endregion
}
