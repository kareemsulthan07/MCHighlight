using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Forms = System.Windows.Forms;

namespace MCHighlight
{
    public enum HookType : int
    {
        WH_JOURNALRECORD = 0,
        WH_JOURNALPLAYBACK = 1,
        WH_KEYBOARD = 2,
        WH_GETMESSAGE = 3,
        WH_CALLWNDPROC = 4,
        WH_CBT = 5,
        WH_SYSMSGFILTER = 6,
        WH_MOUSE = 7,
        WH_HARDWARE = 8,
        WH_DEBUG = 9,
        WH_SHELL = 10,
        WH_FOREGROUNDIDLE = 11,
        WH_CALLWNDPROCRET = 12,
        WH_KEYBOARD_LL = 13,
        WH_MOUSE_LL = 14
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public static implicit operator System.Drawing.Point(POINT p)
        {
            return new System.Drawing.Point(p.X, p.Y);
        }

        public static implicit operator POINT(System.Drawing.Point p)
        {
            return new POINT(p.X, p.Y);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public int mouseData;
        public int flags;
        public int time;
        public UIntPtr dwExtraInfo;
    }



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(HookType hookType, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        private HookProc hCallback = null;
        private IntPtr hHook = IntPtr.Zero;
        private const int WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_MOUSEHWHEEL = 0x020E,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205;

        private Ellipse lEllipse, rEllipse;
        private Forms.NotifyIcon notifyIcon = null;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
            Application.Current.Exit += Current_Exit;

            Forms.ContextMenuStrip contextMenu = new Forms.ContextMenuStrip();
            contextMenu.Items.Add("Exit");
            contextMenu.ItemClicked += ContextMenu_ItemClicked;

            notifyIcon = new Forms.NotifyIcon
            {
                Icon = Properties.Resources.app,
                Text = "MC Highlight",
                ContextMenuStrip = contextMenu,
            };
            notifyIcon.Visible = true;
        }

        private void ContextMenu_ItemClicked(object sender, Forms.ToolStripItemClickedEventArgs e)
        {
            try
            {
                switch (e.ClickedItem.Text)
                {
                    case "Exit":
                        this.Close();
                        break;

                    default:
                        break;
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                RegisterHook();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private void Current_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                if (hHook != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(hHook);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private void RegisterHook()
        {
            try
            {
                hCallback = new HookProc(HookCallback);
                using(Process process = Process.GetCurrentProcess())
                using(ProcessModule module = process.MainModule)
                {
                    IntPtr hModule = GetModuleHandle(module.ModuleName);
                    hHook = SetWindowsHookEx(HookType.WH_MOUSE_LL, hCallback, hModule, 0);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (code <0)
                {
                    return CallNextHookEx(hHook, code, wParam, lParam);
                }
                else
                {
                    var _wParam = wParam.ToInt32();
                    switch (_wParam)
                    {
                        case WM_LBUTTONDOWN:
                            {
                                if (lParam != IntPtr.Zero)
                                {
                                    if (rEllipse != null)
                                    {
                                        mainCanvas.Children.Remove(rEllipse);
                                        rEllipse = null;
                                    }

                                    var mLLHookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                                    lEllipse = new Ellipse()
                                    {
                                        Fill = Brushes.Yellow,
                                        Stroke = Brushes.Yellow,
                                        StrokeThickness = 1,
                                        Width = 50,
                                        Height = 50,
                                        Opacity = 0.5,
                                    };

                                    var point = PointFromScreen(new Point(mLLHookStruct.pt.X, mLLHookStruct.pt.Y));
                                    Canvas.SetLeft(lEllipse, point.X - 25);
                                    Canvas.SetTop(lEllipse, point.Y - 25);
                                    mainCanvas.Children.Add(lEllipse);
                                }
                            }
                            break;

                        case WM_LBUTTONUP:
                            {
                                if (lEllipse != null)
                                {
                                    mainCanvas.Children.Remove(lEllipse);
                                    lEllipse = null;
                                }
                            }
                            break;

                        case WM_MOUSEMOVE:
                            {
                                if (lParam != IntPtr.Zero)
                                {
                                    var mLLHookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                                    var point = PointFromScreen(new Point(mLLHookStruct.pt.X, mLLHookStruct.pt.Y));
                                    if (lEllipse != null)
                                    {
                                        Canvas.SetLeft(lEllipse, point.X - 25);
                                        Canvas.SetTop(lEllipse, point.Y - 25);
                                    }
                                    else if(rEllipse != null)
                                    {
                                        Canvas.SetLeft(rEllipse, point.X - 25);
                                        Canvas.SetTop(rEllipse, point.Y - 25);
                                    }
                                }
                            }
                            break;

                        case WM_RBUTTONDOWN:
                            {
                                if (lParam != IntPtr.Zero)
                                {
                                    if (lEllipse != null)
                                    {
                                        mainCanvas.Children.Remove(lEllipse);
                                        lEllipse = null;
                                    }

                                    var mLLHookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                                    rEllipse = new Ellipse()
                                    {
                                        Fill = Brushes.Red,
                                        Stroke = Brushes.Red,
                                        StrokeThickness = 1,
                                        Width = 50,
                                        Height = 50,
                                        Opacity = 0.5,
                                    };

                                    var point = PointFromScreen(new Point(mLLHookStruct.pt.X, mLLHookStruct.pt.Y));

                                    Canvas.SetLeft(rEllipse, point.X - 25);
                                    Canvas.SetTop(rEllipse, point.Y - 25);
                                    mainCanvas.Children.Add(rEllipse);
                                }
                            }
                            break;

                        case WM_RBUTTONUP:
                            {
                                if (rEllipse != null)
                                {
                                    mainCanvas.Children.Remove(rEllipse);
                                    rEllipse = null;
                                }
                            }
                            break;

                        default:
                            break;
                    }



                    return CallNextHookEx(hHook, code, wParam, lParam);
                }
            }
            catch (Exception exception)
            {
                throw;
            }
        }
    }
}



//try
//{

//}
//catch (Exception exception)
//{
//    MessageBox.Show(exception.Message);
//}


