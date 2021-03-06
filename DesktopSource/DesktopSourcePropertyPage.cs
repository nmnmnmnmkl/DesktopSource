﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DirectShow;
using DirectShow.BaseClasses;
using SharpDX.DXGI;
using Sonic;


/// <summary>
/// 
/// </summary>
namespace DesktopSource
{

    /// <summary>
    /// 
    /// </summary>
    [ComVisible(true)]
    [Guid("BC7F9A0C-00DF-460F-A39E-DD9C9098411A")]
    public partial class DesktopSourcePropertyPage : BasePropertyPage
    {

        /// <summary>
        /// Gets or sets the m_ filter settings.
        /// </summary>
        /// <value>
        /// The m_ filter settings.
        /// </value>
        public IChangeCaptureSettings m_FilterSettings { get; set; }


        /// <summary>
        /// 
        /// </summary>
        private class CaptureItem
        {

            /// <summary>
            /// Gets or sets the name of the m_.
            /// </summary>
            /// <value>
            /// The name of the m_.
            /// </value>
            public string m_Name { get; set; }


            /// <summary>
            /// Gets or sets the m_ capture settings.
            /// </summary>
            /// <value>
            /// The m_ capture settings.
            /// </value>
            public CaptureSettings m_CaptureSettings { get; set; }


            /// <summary>
            /// Initializes a new instance of the <see cref="CaptureItem"/> class.
            /// </summary>
            /// <param name="name">The name.</param>
            /// <param name="settings">The settings.</param>
            public CaptureItem(string name, CaptureSettings settings)
            {
                m_Name = name;
                m_CaptureSettings = settings;
            }


            /// <summary>
            /// Returns a <see cref="System.String" /> that represents this instance.
            /// </summary>
            /// <returns>
            /// A <see cref="System.String" /> that represents this instance.
            /// </returns>
            public override string ToString()
            {
                return m_Name;
            }
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="DesktopSourcePropertyPage"/> class.
        /// </summary>
        public DesktopSourcePropertyPage()
        {
            InitializeComponent();
        }



        /// <summary>
        /// Initializes the capture windows.
        /// </summary>
        private void InitializeCaptureWindows()
        {
            EnumWindows(EnumWindows, IntPtr.Zero);
        }


        /// <summary>
        /// Initializes the capture monitors.
        /// </summary>
        private void InitializeCaptureMonitors()
        {

            var factory = new Factory1();

            for (int i = 0; i < factory.GetAdapterCount(); i++)
            {
                for (int j = 0; j < factory.GetAdapter(i).GetOutputCount(); j++)
                {
                    var output = factory.GetAdapter(i).GetOutput(j);

                    CaptureSettings settings = new CaptureSettings();
                    settings.m_Adapter = i;
                    settings.m_Output = j;
                    settings.m_Rect = new DsRect(
                        output.Description.DesktopBounds.Left,
                        output.Description.DesktopBounds.Top,
                        output.Description.DesktopBounds.Right,
                        output.Description.DesktopBounds.Bottom
                    );

                    CaptureItem captureItem = new CaptureItem(output.Description.DeviceName, settings);
                    captureMethodCombo.Items.Add(captureItem);
                }
            }
        }


        /// <summary>
        /// Enums the windows.
        /// </summary>
        /// <param name="hWnd">The h WND.</param>
        /// <param name="lParam">The l parameter.</param>
        /// <returns></returns>
        protected bool EnumWindows(IntPtr hWnd, IntPtr lParam)
        {
            int size = GetWindowTextLength(hWnd);

            if (size++ > 0 && IsWindowVisible(hWnd))
            {
                StringBuilder sb = new StringBuilder(size);
                GetWindowText(hWnd, sb, size);

                CaptureSettings captureSettings = new CaptureSettings
                {
                    m_Adapter = 0,
                    m_Output = 0
                };

                RECT rct;

                GetWindowRect(hWnd, out rct);

                captureSettings.m_Rect = new DsRect(rct.Left, rct.Top, rct.Right, rct.Bottom);

                CaptureItem captureItem = new CaptureItem(sb.ToString(), captureSettings);

                captureMethodCombo.Items.Add(captureItem);
            }

            return true; 
        }


        /// <summary>
        /// Called when [connect].
        /// </summary>
        /// <param name="pUnknown">The p unknown.</param>
        /// <returns></returns>
        public override HRESULT OnConnect(IntPtr pUnknown)
        {
            if (pUnknown == IntPtr.Zero) return HRESULT.E_POINTER;

            m_FilterSettings = (IChangeCaptureSettings) Marshal.GetObjectForIUnknown(pUnknown);

            return HRESULT.NOERROR;
        }


        /// <summary>
        /// Called when [disconnect].
        /// </summary>
        /// <returns></returns>
        public override HRESULT OnDisconnect()
        {
            m_FilterSettings = null;

            return HRESULT.NOERROR;
        }


        /// <summary>
        /// Called when [apply changes].
        /// </summary>
        /// <returns></returns>
        public override HRESULT OnApplyChanges()
        {
            if (m_FilterSettings != null && captureMethodCombo.SelectedItem != null)
            {
                CaptureItem setting = (captureMethodCombo.SelectedItem as CaptureItem);
                if (setting != null)
                {
                    Dirty = false;

                    return m_FilterSettings.ChangeCaptureSettings(setting.m_CaptureSettings);
                }
            }

            return HRESULT.NOERROR;
        }

        #region API

        protected delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)] 
        protected static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Winapi)] 
        protected static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi)] 
        protected static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", CallingConvention = CallingConvention.Winapi)] 
        protected static extern bool IsWindowVisible(IntPtr hWnd); 

        #endregion


        /// <summary>
        /// Handles the SelectedIndexChanged event of the captureMethodCombo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void captureMethodCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            CaptureSettings newSettings = ((CaptureItem) captureMethodCombo.SelectedItem).m_CaptureSettings;
            adapterTxtBox.Text = newSettings.m_Adapter.ToString();
            outputTxtBox.Text = newSettings.m_Output.ToString();
            topTextBox.Text = newSettings.m_Rect.top.ToString();
            leftTextBox.Text = newSettings.m_Rect.left.ToString();
            rightTextBox.Text = newSettings.m_Rect.right.ToString();
            bottomTextBox.Text = newSettings.m_Rect.bottom.ToString();

            Dirty = true;
        }


        /// <summary>
        /// Handles the Click event of the refreshBtn control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void refreshBtn_Click(object sender, EventArgs e)
        {
            captureMethodCombo.Items.Clear();

            InitializeCaptureMonitors();
            InitializeCaptureWindows();
        }


        /// <summary>
        /// Handles the Load event of the DesktopSourcePropertyPage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void DesktopSourcePropertyPage_Load(object sender, EventArgs e)
        {
            captureMethodCombo.Items.Clear();

            InitializeCaptureMonitors();
            InitializeCaptureWindows();
        }
    }
}
