//----------------------------------------------------------------------------
// <copyright file="EventBroadcast.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//      interface of multiple frame controler.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.UI.Controls
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Shapes;

    /// <summary>
    /// Class of MouseEventBroadcast.
    /// </summary>
    public class MouseEventBroadcast
    {
        #region fields
        
        /// <summary>
        /// The direct children.
        /// </summary>
        private UIElementCollection _children = null;

        /// <summary>
        /// Indicate if it is broadcasting events.
        /// </summary>
        private bool _fBroadcasting = false;
        
        #endregion

        /// <summary>
        /// Setup the mouse events broadcast for the panel.
        /// </summary>
        /// <param name="panel">Panel.</param>
        /// <returns>MouseEventBroadcast.</returns>
        public static MouseEventBroadcast SetupBroadcast(Panel panel)
        {
            MouseEventBroadcast meb = new MouseEventBroadcast();
            meb.ConnectPanel(panel);
            return meb;
        }

        /// <summary>
        /// Connect the broadcast to a panel.
        /// </summary>
        /// <param name="panel">Panel.</param>
        public void ConnectPanel(Panel panel)
        {
            _children = panel.Children;
            foreach (UIElement element in _children)
            {
                element.MouseLeftButtonDown += BroadcastMouseEvent;
                element.MouseLeftButtonUp += BroadcastMouseEvent;
                element.MouseRightButtonDown += BroadcastMouseEvent;
                element.MouseRightButtonUp += BroadcastMouseEvent;
                element.MouseMove += BroadcastMouseEvent;
            }
        }

        /// <summary>
        /// Do the event broadcasting for the children.
        /// </summary>
        /// <param name="sender">Object.</param>
        /// <param name="e">MouseEventArgs.</param>
        protected void BroadcastMouseEvent(object sender, MouseEventArgs e)
        {
            if (_children == null)
            {
                return;
            }

            if (_fBroadcasting)
            {
                return;
            }

            _fBroadcasting = true;
            foreach (UIElement element in _children)
            {
                HitTestResult hitResult = VisualTreeHelper.HitTest(element, e.GetPosition(element));
                if (hitResult != null)
                {
                    UIElement hitElement = hitResult.VisualHit as UIElement;
                    if (hitElement != null)
                    {
                        if (hitElement != e.Source)
                        {
                            hitElement.RaiseEvent(e);
                        }
                    }
                }
            }

            _fBroadcasting = false;
        }
    }
}