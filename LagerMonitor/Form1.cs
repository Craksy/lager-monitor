using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LagerMonitor
{
    public partial class Form1 : Form
    {
        private double insideTemp, insideHum, outsideTemp, outsideHum;
        private MonitorService.monitorSoapClient client;
        public Form1()
        {
            InitializeComponent();
            client = new MonitorService.monitorSoapClient();
            Task.Run(FetchData);
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += this.onTick;
            timer.Start();
        }

        private TimeZoneInfo ukTimeZone, spTimeZone, kbTimeZone;
        private DateTime ukTime, spTime, kbTime;
        private string uk_str, sp_str, kb_str;

      

        private void onTick(object sender, EventArgs e)
        {
            kbTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
            kbTime = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, kbTimeZone);
            kb_str = kbTime.ToString("dddd/dd/MM/yyyy HH:mm:ss");
            RefreshLabel(kb_str, KøbenhavnTime_label);


            ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            ukTime = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, ukTimeZone);
            uk_str = ukTime.ToString("dddd/dd/MM/yyyy HH:mm:ss");
            RefreshLabel(uk_str, londonTime_label);

            spTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            spTime = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, spTimeZone);
            sp_str = spTime.ToString("dddd/dd/MM/yyyy HH:mm:ss");
            RefreshLabel(sp_str, SpigaporeTime_label);


        }

        private async Task FetchData()
        {
            while (true)
            {
                double fetchedDouble = await client.StockTempAsync();
                if(fetchedDouble != insideTemp) {
                    insideTemp = fetchedDouble;
                    RefreshLabel(insideTemp.ToString()+ "°C", StockTemp_label);
                }

                fetchedDouble = await client.StockHumidityAsync();
                if(insideHum != fetchedDouble) {
                    insideHum = fetchedDouble;
                    RefreshLabel(insideHum.ToString()+"%", StockHum_label);
                }
                
                fetchedDouble = await client.OutdoorTempAsync();
                if(outsideTemp != fetchedDouble) {
                    outsideTemp = fetchedDouble;
                    RefreshLabel(outsideTemp.ToString()+ "°C", OutsideTemp_label);
                }

                fetchedDouble = await client.OutdoorHumidityAsync();
                if(outsideHum != fetchedDouble) {
                    outsideHum = fetchedDouble;
                    RefreshLabel(outsideHum.ToString()+"%", OutsideHum_label);
                }

                MonitorService.ArrayOfString stringarray;

                stringarray = client.StockItemsOverMax();
                RefreshBox(stringarray, stockOverMax_Listbox);

                stringarray = client.StockItemsUnderMin();
                RefreshBox(stringarray, stockUnderMin_Listbox);

                stringarray = client.StockItemsMostSold();
                RefreshBox(stringarray, stockMostSold_Listbox);

                Thread.Sleep(60000);



            }
        }

        private void RefreshBox(MonitorService.ArrayOfString newarray, ListBox box)
        {
            if (box.InvokeRequired)
            {
                box.Invoke(new Action(() => RefreshBox(newarray, box)));
            }
            else
            {
                box.Items.Clear();
                foreach(var item in newarray)
                {
                    box.Items.Add(item);
                }
            }
        }

        private void RefreshLabel(string newtext, Control label)
        {
            if (label.InvokeRequired)
            {
                label.Invoke(new Action(() => RefreshLabel(newtext, label)));
            }
            else
            {
                label.Text = newtext;
            }
        }
    }
}
