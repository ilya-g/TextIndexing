using System;
using System.Windows.Input;

namespace Primitive.Text.Indexing.UI.Commands
{
    public class DelegateCommand : ICommand
    {

        private readonly Action m_Command;

        protected DelegateCommand() { }

        public DelegateCommand(Action command)
        {
            this.m_Command = command;
        }

        #region ICommand Members

        private bool m_CanExecute = true;

        public bool CanExecute
        {
            get { return m_CanExecute; }
            set
            {
                if (m_CanExecute != value)
                {
                    m_CanExecute = value;
                    OnCanExecuteChanged();
                }
            }
        }
        public void OnCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
                CanExecuteChanged(this, EventArgs.Empty);
        }

        bool ICommand.CanExecute(object parameter)
        {
            return m_CanExecute;
        }


        public event EventHandler CanExecuteChanged;

        public virtual void Execute(object parameter)
        {
            m_Command();
        }

        #endregion

        public static DelegateCommand Create(Action command) { return new DelegateCommand(command); }
        public static DelegateCommand<T> Create<T>(Action<T> command) { return new DelegateCommand<T>(command); }
    }

    public class DelegateCommand<T> : DelegateCommand
    {
        private readonly Action<T> m_Command;

        public DelegateCommand(Action<T> command)
        {
            m_Command = command;
        }

        public override void Execute(object parameter)
        {
            m_Command((T)parameter);
        }
    }
}