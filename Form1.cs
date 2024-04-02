using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Timers;
using System.IO;
using Newtonsoft.Json;



using STUHFL_cs;


namespace RFID_demo
{



    public partial class Form1 : Form
    {    
        

        public Form1()
        {
            InitializeComponent();
            
        }

        public static STUHFL stuhfl;

        STUHFL_T_ST25RU3993_TxRxCfg txRxCfg = new STUHFL_T_ST25RU3993_TxRxCfg();
        STUHFL_T_ST25RU3993_Gen2_InventoryCfg invGen2Cfg = new STUHFL_T_ST25RU3993_Gen2_InventoryCfg();

        static string[] TagID = new string[500];
        static string[] TagID1 = new string[500];
        static string[] ADC = new string[500];

        static sbyte Receivergain, Receivergain_update, TXpower;

        string Previous_TagID;
        string ID_data;

        static int ii, kk, finish;   

        static ushort adc_display;
        static STUHFL_D_TUNING_STATUS tuningStatus;
        static uint frequency, roundCnt, tagCnt;
        static bool adaptiveRx;
        static bool adaptiveQ;

       


        public static void Save(string ID_dataa)
        {
            JsonData emp = new JsonData();

            emp.ID_tag = ID_dataa;
            emp.Acess_time = DateTime.Now;
            string JSONresult = JsonConvert.SerializeObject(emp);
            string path = @"C:\RFID1\ID_data.json";

            if (File.Exists(path))
            {
                File.Delete(path);
              //  Console.WriteLine("Hello World!");

                using (var tw = new StreamWriter(path, true))
                {
                    tw.WriteLine(JSONresult.ToString());
                    tw.Close();
                }

            }
            else if (!File.Exists(path))
            {
                using (var tw = new StreamWriter(path, true))
                {
                    tw.WriteLine(JSONresult.ToString());
                    tw.Close();
                }
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            stuhfl = new STUHFL();

           
            trackBar1.Value = 19;
            label5.Text = trackBar1.Value.ToString()+" dB";
            Receivergain_update = Convert.ToSByte(trackBar1.Value);

            trackBar2.Value = -2;
            label17.Text = trackBar2.Value.ToString() + " dB";
            TXpower = Convert.ToSByte(trackBar2.Value);

            adaptiveRx=false;
            checkBox1.Checked = false;

            adaptiveQ = false;
            checkBox2.Checked = false;
            
         
            button1.Enabled = false;

            timer1.Enabled = false;
            Start_program.Enabled = false;
            Stop.Enabled = false;

        }      
  
      
        /**
          * @brief      Demonstrates how to Inventory all Gen2 tags in the field.
          */
       

        /**
          * @brief      Demonstrates how to apply on the run a callback on each inventoried tag.
          *             Define a Cycle Callback which outputs EPC of each found tag.
          *             Define a Cycle Callback which informs of inventory end.
          *             Launch Inventory for a given number of rounds (infinite if 0: stop() method must then be used in callback to end inventory).
          *
          * @param[in]  rounds: Number of inventory rounds (infinite if 0: stop() method must then be used in callback to end inventory).
          */
        public void demo_InventoryRunner(UInt32 rounds)
        {
     
          
           SetupGen2Config_live(false, true, STUHFL_D_USED_ANTENNA.ANTENNA_1, STUHFL_D_TUNING_ALGO.EXACT);

            STUHFL_T_Gen2_Inventory inventory = new STUHFL_T_Gen2_Inventory(1000);

            inventory.rssiMode = STUHFL_D_RSSI_MODE.SECOND_BYTE;
            inventory.roundCnt = rounds;
            inventory.inventoryDelay = 0;
            inventory.options = STUHFL_D_INVENTORYREPORT_OPTION.HEARTBEAT;

            InventoryCycleDelegate cycleDelegate = InventoryCycle;
            InventoryFinishedDelegate finishedDelegate = InventoryFinished;

            inventory.start(cycleDelegate, finishedDelegate);
        }

       static STUHFL_ERR InventoryCycle(STUHFL_T_Inventory data)
        {           
            adc_display = data.statistics.adc;
            tuningStatus = data.statistics.tuningStatus;
            frequency = data.statistics.frequency;
            roundCnt = data.statistics.roundCnt;
            tagCnt = data.statistics.tagCnt;

            if (data.tagList != null)
            {                
                foreach (STUHFL_T_InventoryTag element in data.tagList)
                {
                    kk = kk + 1;
                    ii = 1;
                    TagID[kk] = "";
                
                    foreach (byte val in element.epc)
                    {
                        {
                            TagID[kk] = TagID[kk] + val.ToString("x2");
                            ii = ii + 1;
                        }
                    }                   
                    
                }
            }
            
            return STUHFL_ERR.NONE;
        }
        static STUHFL_ERR InventoryFinished(STUHFL_T_Inventory data)
        {      
            finish = 1;   
            return STUHFL_ERR.NONE;
        }

        /**
          * @brief      Demonstrates typical Gen2 setup.
          *
          * @param[in]  singleTag: true if a single tag is expected to be found
          * @param[in]  freqHopping: true if a frequency hopping has to be done (EUROPE frequencies), use single default frequency otherwise
          * @param[in]  antenna: targeted antenna
          * @param[in]  tuning: tuning algorithm to be applied (NONE/FAST/MEDIUM/SLOW)
          */

        public void demo_SetupGen2Config(bool singleTag, bool freqHopping, STUHFL_D_USED_ANTENNA antenna, STUHFL_D_TUNING_ALGO tuning)
        {
          //  STUHFL_T_ST25RU3993_TxRxCfg txRxCfg = new STUHFL_T_ST25RU3993_TxRxCfg();
            txRxCfg.fetch();
            Receivergain = txRxCfg.rxSensitivity;

            // txRxCfg.rxSensitivity = 3;
            // txRxCfg.txOutputLevel = -2;

            txRxCfg.rxSensitivity = Receivergain_update;
            txRxCfg.txOutputLevel = TXpower;

            txRxCfg.usedAntenna = antenna;
            txRxCfg.alternateAntennaInterval = 1;
            txRxCfg.commit();

         //   STUHFL_T_ST25RU3993_Gen2_InventoryCfg invGen2Cfg = new STUHFL_T_ST25RU3993_Gen2_InventoryCfg();
            invGen2Cfg.fetch();
            invGen2Cfg.inventoryOption.fast = true;
            invGen2Cfg.inventoryOption.autoAck = false;
            invGen2Cfg.inventoryOption.readTID = false;
            //   invGen2Cfg.antiCollision.startQ = singleTag ? (byte)0 : (byte)4;
            invGen2Cfg.antiCollision.startQ = adaptiveQ ? (byte)0 : (byte)4;
            invGen2Cfg.antiCollision.adaptiveQ = adaptiveQ; //singleTag;
         //   invGen2Cfg.antiCollision.adaptiveQ = true;

            invGen2Cfg.antiCollision.options = 0;
            invGen2Cfg.antiCollision.minQ = 0;
            invGen2Cfg.antiCollision.maxQ = NativeDefines.STUHFL_D_GEN2_MAXQ;

            for (int i = 0; i < invGen2Cfg.antiCollision.C2.Length; i++)
            {
                invGen2Cfg.antiCollision.C2[i] = 35;
                invGen2Cfg.antiCollision.C1[i] = 15;
            }

       
            invGen2Cfg.autoTuning.interval = 9;//7
            invGen2Cfg.autoTuning.level = 20;
            invGen2Cfg.autoTuning.algorithm = STUHFL_D_TUNING_ALGO.FAST;
          //  invGen2Cfg.autoTuning.falsePositiveDetection = true;
              invGen2Cfg.autoTuning.falsePositiveDetection = false;
           //    invGen2Cfg.adaptiveSensitivity.adaptiveRx = false;

            invGen2Cfg.adaptiveSensitivity.adaptiveRx = adaptiveRx;         

            invGen2Cfg.adaptiveOutputPower.adaptiveTx = false;
           
            for (int i = 0; i < 20; i++)
            {
                invGen2Cfg.adaptiveSensitivity.incThreshold[i] = 100;
                invGen2Cfg.adaptiveSensitivity.decThreshold[i] = -120;
            }         
            
            invGen2Cfg.queryParams.sel = 0;
            invGen2Cfg.queryParams.session = STUHFL_D_GEN2_SESSION.S0;
            invGen2Cfg.queryParams.target = STUHFL_D_GEN2_TARGET.A;
            invGen2Cfg.queryParams.toggleTarget = true;
            invGen2Cfg.queryParams.targetDepletionMode = false;
            invGen2Cfg.queryParams.targetDepletionMode = true;
            invGen2Cfg.commit();

            //
            STUHFL_T_ST25RU3993_Gen2_ProtocolCfg gen2ProtocolCfg = new STUHFL_T_ST25RU3993_Gen2_ProtocolCfg();
            gen2ProtocolCfg.fetch();
            gen2ProtocolCfg.tari = STUHFL_D_GEN2_TARI.T_25_00;
            gen2ProtocolCfg.blf = STUHFL_D_GEN2_BLF.BLF_256;
            gen2ProtocolCfg.coding = STUHFL_D_GEN2_CODING.MILLER8;
            gen2ProtocolCfg.trext = STUHFL_D_TREXT.ON;
            gen2ProtocolCfg.commit();

            STUHFL_T_ST25RU3993_FreqLBT freqLBT = new STUHFL_T_ST25RU3993_FreqLBT();
            freqLBT.fetch();          
            freqLBT.listeningTime = 0; //0
            freqLBT.idleTime = 0;
            freqLBT.rssiLogThreshold = 31;
           // freqLBT.skipLBTcheck = false;
            freqLBT.skipLBTcheck = true;
            freqLBT.commit();

            STUHFL_T_ST25RU3993_ChannelList channelList;
            if (freqHopping)
            {
               channelList = new STUHFL_T_ST25RU3993_ChannelList(STUHFL_D_PROFILE.USA);
             //   channelList = new STUHFL_T_ST25RU3993_ChannelList(STUHFL_D_PROFILE.EUROPE);
            }
            else
            {
                channelList = new STUHFL_T_ST25RU3993_ChannelList(STUHFL_D_PROFILE.USA);         // Allocate a single ChannelList
                                                                                                 //  channelList.itemList[0].frequency = NativeDefines.STUHFL_D_DEFAULT_FREQUENCY;
                                                                                                 // channelList.itemList[0].frequency = 841125;
             //   channelList.itemList[0].frequency = 920250;// 920750; //920250;// 922250;
               
            }
            channelList.persistent = false;
            channelList.channelListIdx = 0;
            channelList.commit();

            STUHFL_T_ST25RU3993_FreqHop FreqHop = new STUHFL_T_ST25RU3993_FreqHop();
            FreqHop.maxSendingTime = 400;
            FreqHop.minSendingTime = 400;
            FreqHop.mode = STUHFL_D_FREQUENCY_HOP_MODE.IGNORE_MIN;
            FreqHop.commit();

            STUHFL_T_Gen2_Select gen2Select = new STUHFL_T_Gen2_Select();
            gen2Select.mode = STUHFL_D_GEN2_SELECT_MODE.CLEAR_LIST;  // Clear all Select filters
            gen2Select.execute();

           demo_TuneFrequencies(STUHFL_D_TUNING_ALGO.EXACT);


        }


        public void SetupGen2Config_live(bool singleTag, bool freqHopping, STUHFL_D_USED_ANTENNA antenna, STUHFL_D_TUNING_ALGO tuning)
        {
          //  STUHFL_T_ST25RU3993_TxRxCfg txRxCfg = new STUHFL_T_ST25RU3993_TxRxCfg();

            txRxCfg.fetch();
            Receivergain = txRxCfg.rxSensitivity;

            // txRxCfg.rxSensitivity = 3;
            // txRxCfg.txOutputLevel = -2;

            txRxCfg.rxSensitivity = Receivergain_update;
            txRxCfg.txOutputLevel = TXpower;

            txRxCfg.usedAntenna = antenna;
            txRxCfg.alternateAntennaInterval = 1;
            txRxCfg.commit();

         //   STUHFL_T_ST25RU3993_Gen2_InventoryCfg invGen2Cfg = new STUHFL_T_ST25RU3993_Gen2_InventoryCfg();

            invGen2Cfg.adaptiveSensitivity.adaptiveRx = adaptiveRx;
            invGen2Cfg.antiCollision.startQ = adaptiveQ ? (byte)0 : (byte)4;
            invGen2Cfg.antiCollision.adaptiveQ = adaptiveQ;
            invGen2Cfg.commit();


        }

        /**
          * @brief      Demonstrates frequencies tuning.
          *             Tune all frequencies defined within demo_SetupGen2Config()
          *
          * @param[in]  tuningAlgo: tuning algorithm to be applied (NONE/FAST/MEDIUM/SLOW)
          */
        public static void demo_TuneFrequencies(STUHFL_D_TUNING_ALGO tuningAlgo)
        {
            if (tuningAlgo == STUHFL_D_TUNING_ALGO.NONE)
            {
                return;
            }

            STUHFL_T_ST25RU3993_TxRxCfg txRxCfg = new STUHFL_T_ST25RU3993_TxRxCfg();
            txRxCfg.fetch();
           // System.Console.Write("Tuning: {0}\n", txRxCfg.usedAntenna);

            STUHFL_T_ST25RU3993_ChannelList channelList = new STUHFL_T_ST25RU3993_ChannelList();
            channelList.persistent = false;
            channelList.fetch();     

            channelList.Tune(true, channelList.persistent, txRxCfg.usedAntenna, tuningAlgo);     // Tune all Channels
                      
            channelList.fetch();
           
        }


                     

        

        private void button10_Click(object sender, EventArgs e)
        {


            STUHFL_T_VersionLib versionLib = new STUHFL_T_VersionLib();
            STUHFL_ERR ret = versionLib.fetch();
                
            listBox1.Items.Clear();
            listBox1.Items.Add("Welcome to the RFID demo ");


            if (ret == STUHFL_ERR.DLL)
            {
                System.Console.WriteLine("\nSTUHFL.dll cannot be loaded\n");
                System.Console.WriteLine("... please press a key to terminate");
                System.Console.ReadKey();
                System.Environment.Exit(-1);
            }

            string[] ports = SerialPort.GetPortNames();
            // label1.Text = ports[0];
            if (ports.Length != 1)
            {
                System.Console.WriteLine("More than 1 com port found..");
                //Environment.Exit(1);
            }

            stuhfl.Connect(String.Format("\\\\.\\{0}", ports[1]));
            Thread.Sleep(600);

                     

            button10.Text = "Connected";
            button10.Enabled = false;
            Start_program.Enabled = true;
            button1.Enabled = true;

        }

        


        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            label5.Text = trackBar1.Value.ToString()+" dB";
            Receivergain_update = Convert.ToSByte(trackBar1.Value);

            if (trackBar1.Value==-7)
            {
                label5.Text = "-6 dB";
                Receivergain_update = -6;
            }
            if (trackBar1.Value == -4)
            {
                label5.Text = "-3 dB";
                Receivergain_update = -3;
            }
            if (trackBar1.Value == -1)
            {
                label5.Text = "0 dB";
                Receivergain_update = 0;
            }
            if (trackBar1.Value == 2)
            {
                label5.Text = "3 dB";
                Receivergain_update = 3;
            }
         
            if (trackBar1.Value == 5)
            {
                label5.Text = "6 dB";
                Receivergain_update = 6;
            }
            if (trackBar1.Value == 8)
            {
                label5.Text = "9 dB";
                Receivergain_update = 9;
            }
            if (trackBar1.Value == 11)
            {
                label5.Text = "10 dB";
                Receivergain_update = 10;
            }
            if (trackBar1.Value == 12)
            {
                label5.Text = "13 dB";
                Receivergain_update = 13;
            }
            if (trackBar1.Value == 14)
            {
                label5.Text = "13 dB";
                Receivergain_update = 13;
            }
            if (trackBar1.Value == 15)
            {
                label5.Text = "16 dB";
                Receivergain_update = 16;
            }
            if (trackBar1.Value == 17)
            {
                label5.Text = "16 dB";
                Receivergain_update = 16;
            }
            if (trackBar1.Value == 18)
            {
                label5.Text = "19 dB";
                Receivergain_update = 19;
            }

          
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
           // stuhfl.Reboot();
            stuhfl.Disconnect();

            Thread.Sleep(1000);

            button10.Enabled = true;               
    
            button1.Enabled = false;

        }



        private void button2_Click_1(object sender, EventArgs e)
        {

            stuhfl.LED_RELAY_ON();

        }

        private void button5_Click_1(object sender, EventArgs e)
        {

            stuhfl.LED_RELAY_OFF();
        }

        

        private void timer1_Tick(object sender, EventArgs e)
        {
            kk = 0;
            finish = 0;
            TagID[kk] = "";
            Array.Clear(TagID, 0, TagID.Length);
            String[] MyDistinctArray;
           
            int result_compare;
          

            listBox1.Items.Clear();


          //  demo_InventoryRunner(10);
            demo_InventoryRunner(100);


            label2.Text = frequency.ToString();
            label3.Text = roundCnt.ToString();
            label4.Text = tagCnt.ToString();
            label12.Text = Receivergain.ToString();
            label13.Text = tuningStatus.ToString();

            label14.Text = adc_display.ToString();

                    

            MyDistinctArray = TagID.Distinct().ToArray();

            foreach (string IDofTag in MyDistinctArray)
            {
                // bool result = IDofTag.Equals("");

                if ((IDofTag != null) && (kk != 0))
                {
                     listBox1.Items.Add(IDofTag);
                      Console.WriteLine(IDofTag);
//ID_data = IDofTag;
  //                  Save(ID_data);           
                    result_compare = String.Compare(IDofTag, Previous_TagID);  

                  
                    Previous_TagID = IDofTag;

                }
              

            }
            kk = 0;

        }

        private void button3_Click(object sender, EventArgs e)
        {
            ID_data = "12345";
            Save(ID_data);
        }

        private void Start_program_Click(object sender, EventArgs e)
        {
            Start_program.Enabled = false;
            demo_SetupGen2Config(true, true, STUHFL_D_USED_ANTENNA.ANTENNA_1, STUHFL_D_TUNING_ALGO.EXACT);
            timer1.Enabled = true;
                       
            Stop.Enabled = true;
        }

        private void Clear_button_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }



        private void Stop_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            Start_program.Enabled = true;


        }
                            



        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked == true)
            {
                adaptiveQ = true; // need a way to hide the Table id='xx'                 
            }
            else
            {
                adaptiveQ = false;

            }
        }

        

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            label17.Text = trackBar2.Value.ToString() + " dB";
            TXpower = Convert.ToSByte(trackBar2.Value);
            
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
              //  adaptiveRx = true; // need a way to hide the Table id='xx'                 
            }
            else
            {
                adaptiveRx = false;
                
            }
        }       

    }
}
