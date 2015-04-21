using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms; //使用RichTextBox类
using System.Runtime.InteropServices;   //使用win32API

namespace CloneEvolutionAnalyzer
{
    //采用枚举类型定义消息（两种方法都可行）
    public enum WindowsMessage
    {
        //WM_VSCROLL = 0x0115,
        WM_HSCROLL = 276,
        WM_VSCROLL = 277,
        WM_SETCURSOR = 32,
        WM_MOUSEWHEEL = 522,
        WM_MOUSEMOVE = 512,
        WM_MOUSELEAVE = 675,
        WM_MOUSELAST = 521,
        WM_MOUSEHOVER = 673,
        WM_MOUSEFIRST = 512,
        WM_MOUSEACTIVATE = 33
    }

    //采用常量类型定义消息（亦可）
    //public const int WM_HSCROLL = 276;
    //public const int WM_VSCROLL = 277;
    //public const int WM_SETCURSOR = 32;
    //public const int WM_MOUSEWHEEL = 522;
    //public const int WM_MOUSEMOVE = 512;
    //public const int WM_MOUSELEAVE = 675;
    //public const int WM_MOUSELAST = 521;
    //public const int WM_MOUSEHOVER = 673;
    //public const int WM_MOUSEFIRST = 512;
    //public const int WM_MOUSEACTIVATE = 33;

    //定义SendMessage委托

    //使用调用Windows API的方式实现联动的RichTextBoxEx2类（两种方法都可行，ShowDiff类中采用这种方式）

    public class RichTextBoxEx : RichTextBox
    {
        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        //Win32参数类型与c#参数类型对照见http://hi.baidu.com/zifan/item/a0e836bb9ea6ced285dd7958
        public static extern int SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private RichTextBoxEx otherRichTextBox;
        private bool recvMsgflag;   //已接收消息标记

        public void SetOtherRichTextBox(RichTextBoxEx other)
        {
            this.otherRichTextBox = other;
        }

        public RichTextBoxEx()
        {
            this.recvMsgflag = false;
        }

        protected override void WndProc(ref Message m)
        {
            if (otherRichTextBox != null &&
                (m.Msg == (int)WindowsMessage.WM_VSCROLL ||
                m.Msg == (int)WindowsMessage.WM_HSCROLL ||
                m.Msg == (int)WindowsMessage.WM_MOUSEACTIVATE ||
                m.Msg == (int)WindowsMessage.WM_MOUSEFIRST ||
                m.Msg == (int)WindowsMessage.WM_MOUSEHOVER ||
                m.Msg == (int)WindowsMessage.WM_MOUSELAST ||
                m.Msg == (int)WindowsMessage.WM_MOUSELEAVE ||
                m.Msg == (int)WindowsMessage.WM_MOUSEMOVE ||
                m.Msg == (int)WindowsMessage.WM_MOUSEWHEEL ||
                m.Msg == (int)WindowsMessage.WM_SETCURSOR))
            //if(otherRichTextBox != null && m.Msg == (int)WindowsMessage.WM_VSCROLL)
            {
                recvMsgflag = true;
                //只有当对方标记为false时，才转发消息（避免无限递归）
                if (!otherRichTextBox.recvMsgflag)
                {
                    try
                    {
                        SendMessage(otherRichTextBox.Handle, m.Msg, m.WParam, m.LParam);
                    }
                    catch (Exception ee)
                    {
                        MessageBox.Show("Send Message failed! " + ee.Message);
                    }
                }
            }

            base.WndProc(ref m);
            recvMsgflag = false;    //执行完操作后恢复为false
        }
    }
}
