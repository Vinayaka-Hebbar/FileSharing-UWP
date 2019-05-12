using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FileSharing.ViewModels
{
    public sealed class AsyncCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public AsyncCommand(Func<object, Task> execute)
        {
            _execute = (param) => execute(param);
            _canExecute = (param) => true;
        }

        public AsyncCommand(Func<Task> execute)
        {
            _execute = (param) => execute();
            _canExecute = (param) => true;
        }

        public AsyncCommand(Action<object> execute, Func<object, bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public AsyncCommand(Action execute, Func<bool> canExecute)
        {
            _execute = (param) => execute();
            _canExecute = (param) => canExecute();
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
