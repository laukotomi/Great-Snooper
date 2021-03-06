﻿namespace GreatSnooper.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using System.Windows.Threading;

    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.Command;

    using GreatSnooper.Classes;
    using GreatSnooper.Helpers;
    using GreatSnooper.Services;
    using GreatSnooper.Validators;

    using MahApps.Metro.Controls;

    public class ListEditorViewModel : ViewModelBase
    {
        private Action<string> addAction;
        private Dispatcher dispatcher;
        private Action<string> removeAction;
        private string settingName = string.Empty;
        private AbstractValidator validator;
        private bool _isTBFocused;

        public ListEditorViewModel(IEnumerable<string> list, string title, Action<string> addAction, Action<string> removeAction, AbstractValidator validator)
        {
            var observable = (list != null) ? new SortedObservableCollection<string>(list) : new SortedObservableCollection<string>();
            Init(observable, title, addAction, removeAction, validator);
        }

        public ListEditorViewModel(string settingName, string title, AbstractValidator validator)
        {
            this.settingName = settingName;

            string value = SettingsHelper.Load<string>(settingName);
            var observable = SortedObservableCollection<string>.CreateFrom(value);
            Init(observable, title, null, null, validator);
        }

        enum Modes
        {
            Collection, Setting
        }

        public ICommand AddCommand
        {
            get
            {
                return new RelayCommand(Add);
            }
        }

        public string DefaultText
        {
            get;
            private set;
        }

        public IMetroDialogService DialogService
        {
            get;
            set;
        }

        public bool IsTBFocused
        {
            get
            {
                return _isTBFocused;
            }
            private set
            {
                _isTBFocused = value;
                RaisePropertyChanged("IsTBFocused");
                _isTBFocused = false;
                RaisePropertyChanged("IsTBFocused");
            }
        }

        public SortedObservableCollection<string> List
        {
            get;
            private set;
        }

        public ICommand RemoveCommand
        {
            get
            {
                return new RelayCommand(Remove);
            }
        }

        public string Selected
        {
            get;
            set;
        }

        public string Text
        {
            get;
            set;
        }

        public string WindowTitle
        {
            get;
            private set;
        }

        internal void ContentRendered(object sender, EventArgs e)
        {
            var o = (MetroWindow)sender;
            o.ContentRendered -= this.ContentRendered;

            IsTBFocused = true;
        }

        private void Add()
        {
            string text = Text.Trim();
            if (text.Length > 0 && this.List.Contains(text, GlobalManager.CIStringComparer) == false)
            {
                if (validator != null)
                {
                    string errorText = validator.Validate(ref text);
                    if (errorText != string.Empty)
                    {
                        DialogService.ShowDialog(Localizations.GSLocalization.Instance.InvalidValueText, errorText);
                        return;
                    }
                }

                this.List.Add(text);

                if (this.settingName != string.Empty)
                {
                    SettingsHelper.Save(this.settingName, this.List);
                }
                else if (this.addAction != null)
                {
                    this.dispatcher.Invoke(addAction, text);
                }
            }
            Text = string.Empty;
            RaisePropertyChanged("Text");
        }

        private void Init(SortedObservableCollection<string> list, string title, Action<string> addAction, Action<string> removeAction, AbstractValidator validator)
        {
            this.dispatcher = Dispatcher.CurrentDispatcher;
            this.WindowTitle = title;
            this.List = list;
            this.validator = validator;
            this.addAction = addAction;
            this.removeAction = removeAction;

            if (validator != null && validator.GetType() == typeof(NickNameValidator))
            {
                DefaultText = Localizations.GSLocalization.Instance.AddUserToList;
            }
            else
            {
                DefaultText = Localizations.GSLocalization.Instance.EnterTextText;
            }
            Text = DefaultText;
        }

        private void Remove()
        {
            string text = Selected;
            if (text != null)
            {
                this.List.Remove(text);

                if (this.settingName != string.Empty)
                {
                    SettingsHelper.Save(this.settingName, this.List);
                }
                else if (this.removeAction != null)
                {
                    this.dispatcher.Invoke(removeAction, text);
                }
            }
        }
    }
}