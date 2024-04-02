/******************************************************************************

  *
******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports;
using System.ComponentModel;
using System.Data;
using System.Drawing;

using System.Windows.Forms;



using STUHFL_cs;

namespace RFID_demo
{
    class Program
    {
        public static STUHFL stuhfl;
        

        static void Main(string[] args)
        {
            stuhfl = new STUHFL();
                   

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.SetCompatibleTextRenderingDefault(false);
           
            Application.Run(new Form1());
         
            
        }


    }
}
