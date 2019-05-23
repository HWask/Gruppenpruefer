using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.IO;
using HtmlAgilityPack;

namespace Gruppenprüfer
{
    public partial class Form1 : Form
    {
        Thread thread1 = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            thread1 = new Thread(new ThreadStart(work));
            thread1.Start();
        }


        static string request()
        {
            try
            {
                string username = "HM3";
                string password = "wird toll";
                string encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));

                var myRequest = (HttpWebRequest)WebRequest.Create("https://gruppenanmeldung.mathematik.uni-stuttgart.de/hm3poeschel/groups.cgi");
                myRequest.Headers.Add("Authorization", "Basic " + encoded);

                var response = myRequest.GetResponse();
                var responseStream = response.GetResponseStream();
                var responseReader = new StreamReader(responseStream);
                var result = responseReader.ReadToEnd();

                responseReader.Close();
                response.Close();

                return result;
            }
            catch(Exception)
            {
                return "";
            }
        }

        void work()
        {
            for (;;)
            {
                parser();
                Thread.Sleep(1000 * 15);
            }
        }

        void parser()
        {
            string html = request();

            while(html == "")
            {
                //play error sound
                System.Media.SystemSounds.Beep.Play();
                Thread.Sleep(1000 * 60);
                html = request();
            }

            //parse
            var document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(html);
            HtmlNodeCollection collection = document.DocumentNode.SelectNodes("//tr");
            collection.RemoveAt(0);

            foreach (HtmlNode rows in collection)
            {
                var cells = rows.SelectNodes("th|td");

                for (int i = cells.Count-1; i >= 0; i--)
                {
                    string inner = cells[i].InnerHtml;
                    var nospace = inner.Trim();
                    if(nospace.Length == 0)
                    {
                        cells.RemoveAt(i);
                    }
                }

                for(int i=0;i<cells.Count;i+=3)
                {
                    string groupNumber = cells[i].InnerHtml;
                    string block = cells[i + 1].InnerText;
                    string people = cells[i + 2].InnerHtml;

                    if((block.Contains("Mo. 1. Block") || block.Contains("Mo. 5. Block")) &&
                        block.Contains("kyb"))
                    {
                        var anVer = people.Split('/');
                        var an = int.Parse(anVer[0]);
                        var ver = int.Parse(anVer[1]);

                        if(an < ver)
                        {
                            notifyIcon1.BalloonTipText = block + " " +an + "/" + ver + " Plätze";
                            notifyIcon1.BalloonTipTitle = "Gruppe frei!";
                            notifyIcon1.ShowBalloonTip(19000);
                        }
                    }
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(thread1 != null)
            {
                thread1.Abort();
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            thread1.Abort();
            Application.Exit();
        }
    }
}
