using System;
using System.Windows;
using System.Windows.Controls;
using AdCampaign.ViewModel;

namespace AdCampaign.View
{
    public class ViewTemplateSelector : DataTemplateSelector
    {
        public DataTemplate InputRangeDataTemplate { get; set; }
        public DataTemplate InputValueDataTemplate { get; set; }
        public DataTemplate CheckListDataTemplate { get; set; }
        public DataTemplate InputRangeDataDateTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
                if (item is ParameterTextView)
                    return InputValueDataTemplate;
                else if (item is ParameterCheckListView)
                    return CheckListDataTemplate;
                else if (item is ParameterRangeDateView)
                    return InputRangeDataDateTemplate;
                else if (item is ParameterRangeView)
                    return InputRangeDataTemplate;
                else
                    throw new InvalidCastException("Some kind of error when try to cast one of parameter type.");
        }
    }
}
