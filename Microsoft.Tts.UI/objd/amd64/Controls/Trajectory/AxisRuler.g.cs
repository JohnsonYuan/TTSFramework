﻿#pragma checksum "..\..\..\..\Controls\Trajectory\AxisRuler.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "433C77B75D39D02A4CF52415891C9508"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

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


namespace Microsoft.Tts.UI.Controls.Trajectory {
    
    
    /// <summary>
    /// AxisRuler
    /// </summary>
    public partial class AxisRuler : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 4 "..\..\..\..\Controls\Trajectory\AxisRuler.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal Microsoft.Tts.UI.Controls.Trajectory.AxisRuler _ruler;
        
        #line default
        #line hidden
        
        
        #line 9 "..\..\..\..\Controls\Trajectory\AxisRuler.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Shapes.Rectangle _rulerArea;
        
        #line default
        #line hidden
        
        
        #line 20 "..\..\..\..\Controls\Trajectory\AxisRuler.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Canvas _currentSelectionCanvas;
        
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
            System.Uri resourceLocater = new System.Uri("/Microsoft.Tts.UI;V11.0.0.0;component/controls/trajectory/axisruler.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\Controls\Trajectory\AxisRuler.xaml"
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
            this._ruler = ((Microsoft.Tts.UI.Controls.Trajectory.AxisRuler)(target));
            return;
            case 2:
            this._rulerArea = ((System.Windows.Shapes.Rectangle)(target));
            
            #line 12 "..\..\..\..\Controls\Trajectory\AxisRuler.xaml"
            this._rulerArea.MouseLeave += new System.Windows.Input.MouseEventHandler(this.OnRulerAreaMouseLeave);
            
            #line default
            #line hidden
            
            #line 13 "..\..\..\..\Controls\Trajectory\AxisRuler.xaml"
            this._rulerArea.MouseMove += new System.Windows.Input.MouseEventHandler(this.OnRulerAreaMouseMove);
            
            #line default
            #line hidden
            
            #line 14 "..\..\..\..\Controls\Trajectory\AxisRuler.xaml"
            this._rulerArea.MouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.OnMouseWheel);
            
            #line default
            #line hidden
            
            #line 15 "..\..\..\..\Controls\Trajectory\AxisRuler.xaml"
            this._rulerArea.MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(this.OnMouseLeftButtonDown);
            
            #line default
            #line hidden
            
            #line 16 "..\..\..\..\Controls\Trajectory\AxisRuler.xaml"
            this._rulerArea.MouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(this.OnMouseRightButtonDown);
            
            #line default
            #line hidden
            
            #line 17 "..\..\..\..\Controls\Trajectory\AxisRuler.xaml"
            this._rulerArea.MouseRightButtonUp += new System.Windows.Input.MouseButtonEventHandler(this.OnMouseRightButtonUp);
            
            #line default
            #line hidden
            return;
            case 3:
            this._currentSelectionCanvas = ((System.Windows.Controls.Canvas)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}

