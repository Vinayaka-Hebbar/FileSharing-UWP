using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FileSharing.ViewModels
{
    public sealed class AsyncCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<object, bool> _canExecute;

        public AsyncCommand(Func<Task> execute)
        {
            _execute = () => execute();
            _canExecute = (param) => true;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }
}
