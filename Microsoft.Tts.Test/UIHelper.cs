//----------------------------------------------------------------------------
// <copyright file="UIHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements UIHelper
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Test
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Windows.Automation;
    using System.Windows.Forms;
    using Microsoft.Tts.Offline.Utility;
    
    // MITA
    using MS.Internal.Mita.Foundation;
    using MS.Internal.Mita.Modeling;
    using MS.Internal.Mita.Modeling.Controls;

    #region Enums

    /// <summary>
    /// Window message ID.
    /// </summary>
    public enum WindowMsgs : int
    {
        /// <summary>
        /// The WM_KEYDOWN message is posted to the window with the keyboard
        /// Focus when a nonsystem key is pressed.
        /// </summary>
        WM_KEYDOWN = 0x100,

        /// <summary>
        /// The WM_KEYUP message is sent to a window immediately before
        /// It loses the keyboard focus. 
        /// </summary>
        WM_KEYUP = 0x0101,

        /// <summary>
        /// The WM_CHAR message is posted to the window with the keyboard
        /// Focus when a char key is pressed.
        /// </summary>
        WM_CHAR = 0X0102,

        /// <summary>
        /// The WM_MOUSEMOVE message is posted to a window when the cursor moves.
        /// </summary>
        WM_MOUSEMOVE = 0x0200,

        /// <summary>
        /// The WM_LBUTTONDOWN message is posted when the user presses the left
        /// Mouse button while the cursor is in the client area of a window.
        /// </summary>
        WM_LBUTTONDOWN = 0x0201,

        /// <summary>
        /// The WM_LBUTTONUP message is posted when the user releases the left
        /// Mouse button while the cursor is in the client area of a window.
        /// </summary>
        WM_LBUTTONUP = 0x0202,

        /// <summary>
        /// Mouse click.
        /// </summary>
        BM_CLICK = 0x00F5,
    }

    /// <summary>
    /// Mouse event flag.
    /// </summary>
    [Flags]
    public enum MouseEventFlag : int
    {
        /// <summary>
        /// Mouse move.
        /// </summary>
        Move = 0x0001,

        /// <summary>
        /// Left button down.
        /// </summary>
        LeftDown = 0x0002,

        /// <summary>
        /// Left button up.
        /// </summary>
        LeftUp = 0x0004,

        /// <summary>
        /// Right button down.
        /// </summary>
        RightDown = 0x0008,

        /// <summary>
        /// Right button up.
        /// </summary>
        RightUp = 0x0010,

        /// <summary>
        /// Middle button down.
        /// </summary>
        MiddleDown = 0x0020,

        /// <summary>
        /// Middle button up.
        /// </summary>
        MiddleUp = 0x0040,

        /// <summary>
        /// X button down.
        /// </summary>
        XDown = 0x0080,

        /// <summary>
        /// X button up.
        /// </summary>
        XUp = 0x0100,

        /// <summary>
        /// Wheel scroll.
        /// </summary>
        Wheel = 0x0800,

        /// <summary>
        /// Virtual desk.
        /// </summary>
        VirtualDesk = 0x4000,

        /// <summary>
        /// Absolute coordinate.
        /// </summary>
        Absolute = 0x8000
    }

    #endregion

    #region Structs

    /// <summary>
    /// Retangle.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    #endregion

    /// <summary>
    /// Windows native methods.
    /// </summary>
    public static class NativeMethods
    {
        /// <summary>
        /// Window enumerator.
        /// </summary>
        /// <param name="hwnd">Window handle.</param>
        /// <param name="lparam">L param of window message.</param>
        /// <returns>True if succeeded, otherwise false.</returns>
        public delegate bool WindowEnumerator(IntPtr hwnd, int lparam);

        /// <summary>
        /// Enumerate child windows for given window.
        /// </summary>
        /// <param name="hwnd">Window handle to iterate child windows.</param>
        /// <param name="del">Enumerating delegate.</param>
        /// <param name="lParam">LParam.</param>
        /// <returns>Return code.</returns>
        [DllImport("user32.dll")]
        internal static extern int EnumChildWindows(IntPtr hwnd,
            WindowEnumerator del, IntPtr lParam);

        /// <summary>
        /// Get window test.
        /// </summary>
        /// <param name="hwnd">Window handle.</param>
        /// <param name="bld">String buffer to save window text.</param>
        /// <param name="size">Length of the windows Buffer.</param>
        /// <returns>Return code.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowText(IntPtr hwnd, StringBuilder bld, int size);

        /// <summary>
        /// Set text for given dialog item.
        /// </summary>
        /// <param name="hDlg">Dialog handle.</param>
        /// <param name="nIdDlgItem">Index of the dialog item in the dialog.</param>
        /// <param name="lpszString">Text string to set.</param>
        /// <returns>True if successed, otherwise false.</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool SetDlgItemText(IntPtr hDlg, int nIdDlgItem, string lpszString);

        /// <summary>
        /// Get dialog item.
        /// </summary>
        /// <param name="hDlg">Dialog window handle.</param>
        /// <param name="nIdDlgItem">Id of the dialog item to find.</param>
        /// <returns>Handle to the found dialog item.</returns>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr GetDlgItem(IntPtr hDlg, int nIdDlgItem);

        /// <summary>
        /// Get window class name.
        /// </summary>
        /// <param name="hWnd">Window handle.</param>
        /// <param name="lpClassName">Window class name.</param>
        /// <param name="nMaxCount">Length of class name buffer.</param>
        /// <returns>Return code.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetClassName(IntPtr hWnd, [Out] StringBuilder lpClassName,
            int nMaxCount);

        /// <summary>
        /// Set position of cursor.
        /// </summary>
        /// <param name="X">X coordinate.</param>
        /// <param name="Y">Y coordinate.</param>
        /// <returns>True if successed, otherwise false.</returns>
        [DllImport("user32.dll")]
        internal static extern bool SetCursorPos(int X, int Y);

        /// <summary>
        /// Generate mouse event.
        /// </summary>
        /// <param name="flags">Mouse event flag.</param>
        /// <param name="dx">X coordinate.</param>
        /// <param name="dy">Y coordiate.</param>
        /// <param name="data">Data.</param>
        /// <param name="extraInfo">Extra Info.</param>
        [DllImport("user32.dll", EntryPoint = "mouse_event")]
        internal static extern void SetMouseEvent(MouseEventFlag flags, int dx, int dy, int data,
            IntPtr extraInfo);

        /// <summary>
        /// Get rectangle of window.
        /// </summary>
        /// <param name="hwnd">HWnd.</param>
        /// <param name="rect">Rectangle.</param>
        /// <returns>True if succeeded, otherwise false.</returns>
        [DllImport("user32.dll")]
        internal static extern bool GetWindowRect(HandleRef hwnd, out NativeRect rect);
    }

    /// <summary>
    /// UI Helper class.
    /// </summary>
    public static class UIHelper
    {
        /// <summary>
        /// Wait and handle UI events via Application.DoEvents.
        /// </summary>
        /// <param name="duration">Duration (millisecond) to be wait.</param>
        public static void Wait(int duration)
        {
            int index = 0;
            while (index < duration)
            {
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(20);
                index += 20;
            }
        }

        /// <summary>
        /// Click controls in the dialog.
        /// </summary>
        /// <param name="parameter">Parameter to click dialog.</param>
        public static void ClickDialog(object parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            ClickDialogParameter afdp = (ClickDialogParameter)parameter;
            Debug.Assert(!string.IsNullOrEmpty(afdp.Caption));

            int index = 0;
            while (true)
            {
                IntPtr foundHwnd = FindDialog(afdp.ClassName, afdp.Caption);

                if (foundHwnd != IntPtr.Zero)
                {
                    Thread.Sleep(20);
                    IntPtr buttonHwnd = FindWindow(foundHwnd, afdp.ControlCaption);
                    Helper.PostMessage(buttonHwnd, (int)WindowMsgs.BM_CLICK, (IntPtr)0, IntPtr.Zero);
                    afdp.Return = AutoFileDialogParameter.Succeeded;
                    break;
                }

                Thread.Sleep(10);
                index += 10;

                if (index >= afdp.Timeout)
                {
                    afdp.Return = AutoFileDialogParameter.OperationTimeout;
                    break;
                }
            }
        }

        /// <summary>
        /// Fill path in the file dialog.
        /// </summary>
        /// <param name="parameter">Parameter.</param>
        public static void FillFileDialog(object parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            AutoFileDialogParameter afdp = (AutoFileDialogParameter)parameter;
            Debug.Assert(!string.IsNullOrEmpty(afdp.FilePath));

            int index = 0;
            while (true)
            {
                IntPtr foundHwnd = FindDialog(afdp.ClassName, afdp.Caption);

                if (foundHwnd != IntPtr.Zero)
                {
                    Thread.Sleep(20);
                    NativeMethods.SetDlgItemText(foundHwnd, 1148, afdp.FilePath);
                    IntPtr buttonHwnd = NativeMethods.GetDlgItem(foundHwnd, 1);
                    Helper.PostMessage(buttonHwnd, (int)WindowMsgs.BM_CLICK, (IntPtr)0, IntPtr.Zero);
                    afdp.Return = AutoFileDialogParameter.Succeeded;
                    break;
                }

                Thread.Sleep(10);
                index += 10;

                if (index >= afdp.Timeout)
                {
                    afdp.Return = AutoFileDialogParameter.OperationTimeout;
                    break;
                }
            }
        }

        /// <summary>
        /// Find a control in the target, whose name is controlName.
        /// </summary>
        /// <param name="target">Parent container within which to find specified control.</param>
        /// <param name="controlName">Name of the control to be found.</param>
        /// <returns>Founded control.</returns>
        public static Control Find(Control target, string controlName)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            if (string.IsNullOrEmpty(controlName))
            {
                throw new ArgumentNullException("controlName");
            }

            Control foundTarget = null;

            if (string.Compare(target.Name, controlName, StringComparison.Ordinal) == 0)
            {
                foundTarget = target;
            }
            else
            {
                foreach (Control control in target.Controls)
                {
                    if (string.Compare(target.Name, controlName, StringComparison.Ordinal) == 0)
                    {
                        foundTarget = control;
                    }
                    else
                    {
                        Control result = Find(control, controlName);
                        if (result != null)
                        {
                            foundTarget = result;
                        }
                    }
                }
            }

            return foundTarget;
        }

        /// <summary>
        /// Find dialog with given window class name and caption.
        /// </summary>
        /// <param name="name">Window class name.</param>
        /// <param name="caption">Caption.</param>
        /// <returns>Found dialog window handle, null if not found.</returns>
        private static IntPtr FindDialog(string name, string caption)
        {
            IntPtr foundHwnd = IntPtr.Zero;

            // instantiate the delegate
            NativeMethods.WindowEnumerator del = delegate(IntPtr hwnd, int lparam)
               {
                   // Get the text from the window
                   StringBuilder bld = new StringBuilder(256);
                   if (!string.IsNullOrEmpty(name))
                   {
                       NativeMethods.GetClassName(hwnd, bld, bld.Capacity);
                       if (bld.ToString() != name)
                       {
                           return true;
                       }
                   }

                   NativeMethods.GetWindowText(hwnd, bld, bld.Capacity);
                   string text = bld.ToString();
                   if (bld.ToString() == caption)
                   {
                       foundHwnd = hwnd;
                   }

                   return true;
               };

            // Call the win32 function
            NativeMethods.EnumChildWindows(IntPtr.Zero, del, IntPtr.Zero);

            return foundHwnd;
        }

        /// <summary>
        /// Find window handle with given parent window handle and caption.
        /// </summary>
        /// <param name="parent">Parent window handle.</param>
        /// <param name="caption">Caption.</param>
        /// <returns>Found dialog window handle, null if not found.</returns>
        private static IntPtr FindWindow(IntPtr parent, string caption)
        {
            IntPtr foundHwnd = IntPtr.Zero;

            // instantiate the delegate
            NativeMethods.WindowEnumerator del = delegate(IntPtr hwnd, int lparam)
               {
                   // Get the text from the window
                   StringBuilder bld = new StringBuilder(256);
                   NativeMethods.GetWindowText(hwnd, bld, bld.Capacity);
                   string text = bld.ToString();
                   if (bld.ToString() == caption)
                   {
                       foundHwnd = hwnd;
                   }

                   return true;
               };

            // call the win32 function
            NativeMethods.EnumChildWindows(parent, del, IntPtr.Zero);

            return foundHwnd;
        }
    }

    /// <summary>
    /// Dialog parameter.
    /// </summary>
    public class DialogParameter
    {
        public const int Succeeded = 1;
        public const int OperationTimeout = -1;
        public const string FileDialogClassName = "#32770";

        private int _timeout;
        private int _return;

        private string _caption;
        private string _className;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogParameter"/> class.
        /// </summary>
        /// <param name="caption">Caption of dialog to find.</param>
        /// <param name="timeout">Time out to carry out action.</param>
        /// <param name="className">Window class name.</param>
        public DialogParameter(string caption, int timeout, string className)
        {
            if (caption == null)
            {
                throw new ArgumentNullException("caption");
            }

            if (className == null)
            {
                throw new ArgumentNullException("className");
            }

            Caption = caption;
            Timeout = timeout;
            ClassName = className;
        }

        /// <summary>
        /// Gets or sets Window class name.
        /// </summary>
        public string ClassName
        {
            get
            {
                return _className;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _className = value;
            }
        }

        /// <summary>
        /// Gets or sets Caption.
        /// </summary>
        public string Caption
        {
            get
            {
                return _caption;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _caption = value;
            }
        }

        /// <summary>
        /// Gets or sets Return value.
        /// </summary>
        public int Return
        {
            get { return _return; }
            set { _return = value; }
        }

        /// <summary>
        /// Gets or sets Time out duration.
        /// </summary>
        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }
    }

    /// <summary>
    /// Click dialog parameter.
    /// </summary>
    public class ClickDialogParameter : DialogParameter
    {
        private string _controlCaption;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClickDialogParameter"/> class.
        /// </summary>
        /// <param name="caption">Caption.</param>
        /// <param name="timeout">Timeout duration to carry out action.</param>
        /// <param name="controlCaption">Caption of control to click.</param>
        /// <param name="className">Window class name.</param>
        public ClickDialogParameter(string caption, int timeout, string controlCaption, string className)
            : base(caption, timeout, className)
        {
            if (controlCaption == null)
            {
                throw new ArgumentNullException("controlCaption");
            }

            ControlCaption = controlCaption;
        }

        /// <summary>
        /// Gets or sets Caption of control.
        /// </summary>
        public string ControlCaption
        {
            get
            {
                return _controlCaption;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _controlCaption = value;
            }
        }
    }

    /// <summary>
    /// Auto file dialog filling parameter.
    /// </summary>
    public class AutoFileDialogParameter : DialogParameter
    {
        private string _filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoFileDialogParameter"/> class.
        /// </summary>
        /// <param name="caption">Caption of dialog.</param>
        /// <param name="filePath">The location of file path to fill in.</param>
        /// <param name="timeout">Timeout duration to carry out the cation.</param>
        /// <param name="className">Window class name.</param>
        public AutoFileDialogParameter(string caption, string filePath, int timeout, string className)
            : base(caption, timeout, className)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            FilePath = filePath;
        }

        /// <summary>
        /// Gets or sets Location of file path to fill in the file dialog.
        /// </summary>
        public string FilePath
        {
            get
            {
                return _filePath;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _filePath = value;
            }
        }
    }

    /// <summary>
    /// Class of wpf UI helper.
    /// </summary>
    public class WpfUIHelper
    {
        #region fields

        private Dictionary<string, UIObject> _cache =
            new Dictionary<string, UIObject>();

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets application root.
        /// </summary>
        [CLSCompliant(false)]
        public UIObject RootObject
        {
            get;
            set;
        }

        #endregion

        #region methods

        /// <summary>
        /// Get UI object.
        /// </summary>
        /// <param name="parent">Parent object.</param>
        /// <param name="name">Object name.</param>
        /// <returns>UI object.</returns>
        [CLSCompliant(false)]
        public static UIObject GetUIObject(UIObject parent, string name)
        {
            Debug.Assert(parent != null);
            Debug.Assert(!string.IsNullOrEmpty(name));

            UICondition condition = CreateUICondition(name, null, true, false);
            return parent.Children.Find(condition);
        }

        /// <summary>
        /// Get UI object.
        /// </summary>
        /// <param name="parent">Parent object.</param>
        /// <param name="index">Object index.</param>
        /// <returns>UI object.</returns>
        [CLSCompliant(false)]
        public static UIObject GetUIObject(UIObject parent, int index)
        {
            Debug.Assert(parent != null);
            Debug.Assert(index >= 0 && parent.Children.Count > index);

            return parent.Children[index];
        }

        /// <summary>
        /// Create UI condition.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="controlType">Control type.</param>
        /// <param name="useName">Whether to use name.</param>
        /// <param name="useControlType">Whether to use control type.</param>
        /// <returns>UI condition.</returns>
        [CLSCompliant(false)]
        public static UICondition CreateUICondition(string name, ControlType controlType,
            bool useName, bool useControlType)
        {
            UICondition condition = UICondition.True;
            if (useName && (name != string.Empty))
            {
                condition = condition.AndWith(UICondition.CreateFromName(name));
            }

            if (useControlType && (controlType != null))
            {
                condition = condition.AndWith(UICondition.Create(UIProperty.Get("ControlType"), controlType));
            }

            return condition;
        }

        /// <summary>
        /// Get UI object.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <returns>UI object.</returns>
        [CLSCompliant(false)]
        public UIObject GetUIObject(string path)
        {
            Debug.Assert(RootObject != null);
            UIObject returnObj = null;
            Debug.Assert(!string.IsNullOrEmpty(path));

            if (_cache.ContainsKey(path))
            {
                // Return from cache
                returnObj = _cache[path];
            }
            else
            {
                int slashIndex = path.LastIndexOf('\\');
                Debug.Assert(slashIndex < path.Length - 1);
                UIObject parent = slashIndex > 0 ?
                    GetUIObject(path.Substring(0, slashIndex)) : RootObject;

                // Find, add to cache and return
                UICondition condition = slashIndex > 0 ?
                    CreateUICondition(path.Substring(slashIndex + 1), null, true, false) :
                    CreateUICondition(path, null, true, false);

                returnObj = parent.Children.Find(condition);
                Debug.Assert(returnObj != null);
                _cache.Add(path, returnObj);
            }

            return returnObj;
        }

        #endregion
    }
}