using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace LagerMonitor
{
    public partial class Form1 : Form
    {
        private double insideTemp, insideHum, outsideTemp, outsideHum;
        private string rssText = "";
        private MonitorService.monitorSoapClient client;

        private TimeZoneInfo ukTimeZone, spTimeZone, kbTimeZone;
        private DateTime ukTime, spTime, kbTime;
        private string uk_str, sp_str, kb_str;
        public Form1()
        {
            InitializeComponent();

            //Opretter klienten og starter en task som peridisk henter data i baggrunden
            client = new MonitorService.monitorSoapClient();
            Task.Run(FetchData);

            // laver en timer til uret som ticker en gang i sekundet.
            System.Windows.Forms.Timer clockTimer = new System.Windows.Forms.Timer();
            clockTimer.Interval = 1000;
            clockTimer.Tick += ClockTick;
            clockTimer.Start();

            // laver en timer til det scrollende rss-feed som ticker 5 gange i sekundet.
            System.Windows.Forms.Timer rssTimer = new System.Windows.Forms.Timer();
            rssTimer.Interval = 200;
            rssTimer.Tick += this.RssTick;
            rssTimer.Start();
        }


        /// <summary>
        /// Reagerer på et Tick event fra `rssTimer`.
        /// Hvis der er mere text tilbage i variablen `rssText`, fjern det
        /// første bogstav for at give en scrollende effekt.
        /// 
        /// Hvis ikke, så kald metoden `ReadFeed`.
        /// </summary>
        private void RssTick(object sender, EventArgs e)
        {
            if (rssText.Length > 0)
            {
                rssText = rssText.Substring(1);
                label3.Text = rssText;
            }
            else
            {
                ReadFeed();
            }
        }


        /// <summary>
        /// Reagere på et Tick event fra `clockTimer`
        /// Konverterer tiden til en række forskellige tidszoner og formaterer de tilsvarende labels med resultatet.
        /// </summary>
        private void ClockTick(object sender, EventArgs e)
        {
            kbTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
            kbTime = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, kbTimeZone);
            kb_str = kbTime.ToString("dddd/dd/MM/yyyy HH:mm:ss");
            KøbenhavnTime_label.Text = kb_str;

            ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            ukTime = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, ukTimeZone);
            uk_str = ukTime.ToString("dddd/dd/MM/yyyy HH:mm:ss");
            londonTime_label.Text = uk_str;

            spTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            spTime = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, spTimeZone);
            sp_str = spTime.ToString("dddd/dd/MM/yyyy HH:mm:ss");
            SpigaporeTime_label.Text = sp_str;
        }


        /// <summary>
        /// Læser et rrs-feed fra Nordjyske og formaterer variablen `rssText` med resultatet.
        /// For hver nyhed indsæt dato/tid, titlen og resume. Adskil
        /// </summary>
        private void ReadFeed()
        {
            //TODO: trycatch here. breaks if offline.
            string url = "https://nordjyske.dk/rss/nyheder";
            XmlReader reader = XmlReader.Create(url);
            SyndicationFeed feed = SyndicationFeed.Load(reader);
            foreach (SyndicationItem item in feed.Items)
            {
                string subject = item.Title.Text;
                string summary = item.Summary.Text;
                string dato = item.PublishDate.DateTime.ToString();
                rssText += $"[{dato}]  {subject} -- {subject}                                       ";
            }
        }

        /// <summary>
        /// Henter monitoreringsdata asynkront i baggrunden.
        /// Dette gøres for at undgå at blocke UI-tråden mens der kommunikeres med webservicen.
        /// 
        /// Bruger hjælpe metoderne `RefreshLabel` og `RefreshBox` til at
        /// opdatere UI elementer uden at forsage cross-thread collisions.
        /// </summary>
        private async Task FetchData()
        {
            while (true)
            {
                double fetchedDouble = await client.StockTempAsync();
                if (fetchedDouble != insideTemp)
                {
                    insideTemp = fetchedDouble;
                    RefreshLabel(insideTemp.ToString() + "°C", StockTemp_label);
                }

                fetchedDouble = await client.StockHumidityAsync();
                if (insideHum != fetchedDouble)
                {
                    insideHum = fetchedDouble;
                    RefreshLabel(insideHum.ToString() + "%", StockHum_label);
                }

                fetchedDouble = await client.OutdoorTempAsync();
                if (outsideTemp != fetchedDouble)
                {
                    outsideTemp = fetchedDouble;
                    RefreshLabel(outsideTemp.ToString() + "°C", OutsideTemp_label);
                }

                fetchedDouble = await client.OutdoorHumidityAsync();
                if (outsideHum != fetchedDouble)
                {
                    outsideHum = fetchedDouble;
                    RefreshLabel(outsideHum.ToString() + "%", OutsideHum_label);
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

        /// <summary>
        /// Opdater en ListBox med ny data.
        /// Hvis denne metoder bliver kaldt fra UI-tråden så opdater `box` med data fra `newarray`
        /// Ellers så brug Invoke til at kalde den igen, men fra UI tråden.
        /// </summary>
        /// <param name="newarray">Array med strenge som indeholder den nye data</param>
        /// <param name="box">En ListBox control som skal opdateres.</param>
        private void RefreshBox(MonitorService.ArrayOfString newarray, ListBox box)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => RefreshBox(newarray, box)));
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

        /// <summary>
        /// Opdater et Label med ny data.
        /// Hvis denne metoder bliver kaldt fra UI-tråden så opdater `label` med data fra `newtext`
        /// Ellers så brug Invoke til at kalde den igen, men fra UI tråden.
        /// </summary>
        /// <param name="newtext"></param>
        /// <param name="label"></param>
        private void RefreshLabel(string newtext, Control label)
        {
            if (this.InvokeRequired)
                this.Invoke(new Action(() => RefreshLabel(newtext, label)));
            else
                label.Text = newtext;
        }
    }
}
