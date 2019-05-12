using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Popups;

namespace FileSharing.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        bool isBusy = false;
        public bool IsBusy
        {
            get { return isBusy; }
            set { SetProperty(ref isBusy, value); }
        }

        public static ICommand CreateCommand(Action<object> action)
        {
            return new Command(action);
        }

        public static ICommand CreateAsyncCommand(Func<object, Task> action)
        {
            return new AsyncCommand(action);
        }

        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName]string propertyName = "",
            Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public sealed class Command : ICommand
        {
            private readonly Action<object> command;
            public Command(Action<object> action)
            {
                command = action;
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                command(parameter);
            }
        }

        protected async Task NotifyAsync(string message)
        {
            await new MessageDialog("Somthing wrong.. Please Restart the app and try again").ShowAsync();
        }
    }
}
