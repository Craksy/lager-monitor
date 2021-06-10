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
                Thread.Sleep(60000);

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
