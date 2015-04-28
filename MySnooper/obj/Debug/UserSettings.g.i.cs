﻿#pragma checksum "..\..\UserSettings.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "1963B9E7C4BF4B37B76064318DFD4116"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using MahApps.Metro.Controls;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace MySnooper {
    
    
    /// <summary>
    /// UserSettings
    /// </summary>
    public partial class UserSettings : MahApps.Metro.Controls.MetroWindow, System.Windows.Markup.IComponentConnector {
        
        
        #line 19 "..\..\UserSettings.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid GeneralSettingsGrid;
        
        #line default
        #line hidden
        
        
        #line 32 "..\..\UserSettings.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox WAExeText;
        
        #line default
        #line hidden
        
        
        #line 42 "..\..\UserSettings.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid AppearanceGrid;
        
        #line default
        #line hidden
        
        
        #line 53 "..\..\UserSettings.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid WindowGrid;
        
        #line default
        #line hidden
        
        
        #line 64 "..\..\UserSettings.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid NotificationsGrid;
        
        #line default
        #line hidden
        
        
        #line 75 "..\..\UserSettings.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid WormsGrid;
        
        #line default
        #line hidden
        
        
        #line 87 "..\..\UserSettings.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid MessagesGrid;
        
        #line default
        #line hidden
        
        
        #line 93 "..\..\UserSettings.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid MessageStylesGrid;
        
        #line default
        #line hidden
        
        
        #line 106 "..\..\UserSettings.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid SoundsGrid;
        
        #line default
        #line hidden
        
        
        #line 126 "..\..\UserSettings.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock Version;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/Great Snooper;component/usersettings.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\UserSettings.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.GeneralSettingsGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 2:
            this.WAExeText = ((System.Windows.Controls.TextBox)(target));
            return;
            case 3:
            
            #line 33 "..\..\UserSettings.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.WAExeChange);
            
            #line default
            #line hidden
            return;
            case 4:
            this.AppearanceGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 5:
            this.WindowGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 6:
            this.NotificationsGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 7:
            this.WormsGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 8:
            this.MessagesGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 9:
            this.MessageStylesGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 10:
            this.SoundsGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 11:
            this.Version = ((System.Windows.Controls.TextBlock)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

