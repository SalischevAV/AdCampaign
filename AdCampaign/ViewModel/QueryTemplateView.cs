using System;
using System.Collections.Generic;
using AdCampaign.Model;
using System.Windows.Input;
using System.Linq;
using System.Collections.ObjectModel;
using Npgsql;
using System.ComponentModel;

namespace AdCampaign.ViewModel
{
    public class QueryTemplateView : ViewModelBase
    {
        public TemplatesCollection Templates { get; set; }
        QueryTemplate QueryTempl;
        public string GetQueryString() { return QueryTempl.GenerateQuery(); }
        public bool HasWhere { get { return QueryTempl.NotEmptyParameterCount > 0; } }

        public ObservableCollection<object> ParametersView { get; set; }
        public ICommand NavigateButtonCommand { get; private set; }
#region Constructors
        public QueryTemplateView(NpgsqlConnection connection)
        {
            Templates = new TemplatesCollection(connection);
            Templates.CurrTemplateChanged += CurrentTemplateChanged;
            ParametersView = new ObservableCollection<object>();
            NavigateButtonCommand = new BaseCommand(p => { }, p => IsParametersValid);
        }
#endregion

        void CurrentTemplateChanged()
        {
            QueryTempl = new QueryTemplate(Templates.TemplatesList[Templates.CurrentTemplate].TemplateId, Templates.Connection);
            ParametersView.Clear();
            foreach (Parameter p in QueryTempl.Parameters)
            {
                if (p is ParameterCheckList)
                {
                    ParametersView.Add(new ParameterCheckListView(p as ParameterCheckList));
                }
                else if (p is ParameterRangeDate)
                {
                    ParametersView.Add(new ParameterRangeDateView(p as ParameterRangeDate));
                }
                else if (p is ParameterRange)
                {
                    ParametersView.Add(new ParameterRangeView(p as ParameterRange));
                }
                else if (p is ParameterText)
                {
                    ParametersView.Add(new ParameterTextView(p as ParameterText));
                }
                else
                {
                    throw new ApplicationException(String.Format("Some invalid value type in parameter {0}.", p.ParameterName));
                }
            }
        }

        public bool IsParametersValid
        {
            get
            {
                if (QueryTempl != null)
                {
                    foreach (IDataErrorInfo p in ParametersView)
                    {
                        if (!String.IsNullOrEmpty(p.Error)) return false;
                    }
                    return true;
                }
                return false;
            }
        }
    }
}
