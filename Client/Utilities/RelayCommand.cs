using System;
using System.Windows.Input;

namespace Client.Utilities
{
    public class RelayCommand : ICommand
    {
        private Action execute;
        private Func<bool> canExecute;

        public RelayCommand(Action execute)
            : this(execute, null)
        { }

        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
            => this.canExecute == null ? true : this.canExecute();

        public void Execute(object? parameter)
            => this.execute();
    }

    public class RelayCommand<T> : ICommand
    {
        private Action<T> execute;
        private Predicate<T> canExecute;

        public RelayCommand(Action<T> execute)
            : this(execute, null)
        { }

        public RelayCommand(Action<T> execute, Predicate<T> canExecute)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
            => this.canExecute == null ? true : this.canExecute((T)parameter);

        public void Execute(object? parameter)
            => this.execute((T)parameter);
    }
}
