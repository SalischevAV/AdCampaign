using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace AdCampaign.ViewModel
{
    public class BaseCommand : ICommand
    {
        Action<object> _execute;
        Predicate<object> _canExecute;
        public event Action Executed;
        public BaseCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");
            _execute = execute;
            _canExecute = canExecute;
        }
 
        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute(parameter);
        }
 
        public void Execute(object parameter)
        {
            _execute(parameter);
            if (Executed != null) Executed();
        }
 
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
