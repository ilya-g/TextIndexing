using System;
using System.Windows;
using System.Windows.Input;

namespace Primitive.Text.Indexing.UI.Commands
{
    /// <summary>
    /// This class facilitates associating a key binding in XAML markup to a command
    /// defined in a View Model by exposing a Command dependency property.
    /// The class derives from Freezable to work around a limitation in WPF when data-binding from XAML.
    /// </summary>
    public class CommandReference : Freezable, ICommand, ICommandSource
    {
        public CommandReference()
        {
            // Blank
            commandCanExecuteChangedHandler = (s, e) => OnCanExecuteChanged();
        }

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(CommandReference), new PropertyMetadata(OnCommandChanged));
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register("CommandParameter", typeof(object), typeof(CommandReference), new PropertyMetadata(null, OnCommandParameterChanged));

        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public object CommandParameter
        {
            get { return this.GetValue(CommandParameterProperty); }
            set { this.SetValue(CommandParameterProperty, value); }
        }

        IInputElement ICommandSource.CommandTarget { get { return null; } }

        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            if (Command != null)
                return Command.CanExecute(parameter ?? CommandParameter);
            return false;
        }

        public void Execute(object parameter)
        {
            Command.Execute(parameter ?? CommandParameter);
        }

        public event EventHandler CanExecuteChanged;

        private void OnCanExecuteChanged()
        {
            var handler = CanExecuteChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
        private readonly EventHandler commandCanExecuteChangedHandler;

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var commandReference = (CommandReference)d;
            var oldCommand = e.OldValue as ICommand;
            var newCommand = e.NewValue as ICommand;

            if (oldCommand != null)
            {
                oldCommand.CanExecuteChanged -= commandReference.commandCanExecuteChangedHandler;
            }
            if (newCommand != null)
            {
                newCommand.CanExecuteChanged += commandReference.commandCanExecuteChangedHandler;
            }
            commandReference.OnCanExecuteChanged();
        }

        private static void OnCommandParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CommandReference)d).OnCanExecuteChanged();
        }


        #endregion

        #region Freezable

        protected override Freezable CreateInstanceCore()
        {
            return new CommandReference();
        }

        #endregion
    }
}
