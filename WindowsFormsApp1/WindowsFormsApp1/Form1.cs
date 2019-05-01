using ErrRecordAnalysis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        static int rowindex = 0;
        string filepath;
        Dictionary<string, string> ErrDict;
        Dictionary<string, string> IdDict;
        Dictionary<string, string> StateDict;
        //Dictionary<string, string> StepDict;
        Dictionary<string, List<Dictionary<string, string>>> dirctExcetpParam;
        Dictionary<string, Dictionary<int, Dictionary<string, string>>> ParamDict;

        Dictionary<>
        Dictionary<int, Dictionary<string, string>> ATParamDict;
        //Dictionary<int, Dictionary<string, string>> CellParamDict;
        public Form1()
        {
            InitializeComponent();

            //this.richTextBox1.Anchor =  AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            //this.dataGridView1.Anchor = AnchorStyles.Bottom |
            //    AnchorStyles.Left | AnchorStyles.Right ;
            ConvertESealID("434E4F53001BBF16");
            ErrDict = CreateDirct(@"ErrDict.txt", '=');
            IdDict = CreateDirct(@"ID.txt", '=');
            StateDict = CreateDirct(@"State.txt", '=');
            //StepDict = CreateDirct(@"Step.txt", '=');
            BlockErrDict = CreateDirct(@"BlockErrDict.txt", '=');
            ATParamDict = GetParamDictFromFileName("AT");
            var CellParamDict = GetParamDictFromFileName("CELL");
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.BackgroundColor = Color.FromArgb(255, 255, 255);
            this.dataGridView1.Columns.Add("序号", "序号");
            this.dataGridView1.Columns.Add("锁号", "锁号");
            this.dataGridView1.Columns.Add("时间", "时间");
            this.dataGridView1.Columns.Add("phyID", "phyID");
            this.dataGridView1.Columns.Add("状态", "状态");
            this.dataGridView1.Columns.Add("错误", "错误");
            for(int i = 0; i < 8; i++)
            {
                this.dataGridView1.Columns.Add("参数"+ i.ToString(), "参数" + i.ToString());
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string str;
            
            str = this.textBox2.Text;
            Analysis_Try(str.Trim());
        }
        private void AnalysisFrame(string str)
        {
            string tmpstr1, tmpstr2;
            string tmpID;
            if (str.Length == 0)
                return;
            var list = str.Split(' ');
            if(int.Parse(list[19], System.Globalization.NumberStyles.HexNumber) != 0xFE)
            {
                this.richTextBox1.AppendText("数据帧不合适!\r\n");
                return;
            }
            var num = int.Parse(list[23], System.Globalization.NumberStyles.HexNumber);
            int index = 24;
            int utc_time = 0;
            for(int i = 0; i < num; i++)
            {
                //index
                rowindex = this.dataGridView1.Rows.Add();
                this.dataGridView1[0,rowindex].Value = rowindex;
                //utc
                string timestr = "";
                this.richTextBox1.AppendText("Time:");
                for (int j = 0; j < 4; j++) {
                    timestr = timestr+list[index++] ;
                    //this.richTextBox1.AppendText(list[index ++]);
                }
                utc_time = int.Parse(timestr,  System.Globalization.NumberStyles.HexNumber);
                DateTime t = utc.ConvertIntDatetime(utc_time);
                this.richTextBox1.AppendText(t.ToString());
                this.richTextBox1.AppendText("\r\n");
                //grid
                this.dataGridView1.Rows[rowindex].Cells["时间"].Value = t.ToString();

                //id
                tmpstr1 = list[index++];
                tmpstr2 = FindFromDict(IdDict, tmpstr1);
                if (tmpstr2 != null)
                    tmpstr1 = tmpstr2;
                tmpID = tmpstr2;
                this.richTextBox1.AppendText("ID:");
                this.richTextBox1.AppendText(tmpstr1);
                this.richTextBox1.AppendText("\r\n");
                //grid
                this.dataGridView1.Rows[rowindex].Cells["phyID"].Value = tmpstr1;

                //state
                tmpstr1 = list[index++];
                tmpstr2 = FindFromDict(StateDict, tmpstr1);
                if(tmpID == "ERR_RECORD_PHYID_ATDEV")
                {
                    if (tmpstr2 != null)
                        tmpstr1 = tmpstr2;
                }
                this.richTextBox1.AppendText("State:");
                this.richTextBox1.AppendText(tmpstr1);
                this.richTextBox1.AppendText("\r\n");
                this.dataGridView1.Rows[rowindex].Cells["状态"].Value = tmpstr1;

                //err
                this.richTextBox1.AppendText("Err:");
                tmpstr1 = list[index++];
                tmpstr2 = null;
                if (tmpID == "ERR_RECORD_PHYID_ATDEV")
                {
                    tmpstr2 = FindFromDict(ErrDict, tmpstr1);
                }
                else if (tmpID == "ERR_RECORD_PHYID_CTL_BLK")
                {
                    tmpstr2 = FindFromDict(BlockErrDict, tmpstr1);
                }
                if (tmpstr2 != null)
                    tmpstr1 = tmpstr2;
                this.richTextBox1.AppendText(tmpstr1);
                this.richTextBox1.AppendText("\r\n");
                //grid
                this.dataGridView1.Rows[rowindex].Cells["错误"].Value = tmpstr1;

                //Param
                int tmp = int.Parse(list[index++], System.Globalization.NumberStyles.HexNumber);
                this.richTextBox1.AppendText("Param:");
                for (int k = 0; k < tmp; k++)
                {
                    tmpstr1 = list[index++];
                    this.richTextBox1.AppendText(tmpstr1);
                    string tmpstr3 = "参数" + k.ToString();
                    if (tmpID == "ERR_RECORD_PHYID_ATDEV")
                    {
                        dataGridView1.Rows[rowindex].Cells[tmpstr3].Value = ATParamDict[k][tmpstr1]; 
                    }
                    else
                    {
                        dataGridView1.Rows[rowindex].Cells[tmpstr3].Value = tmpstr1;
                    }
                }
                this.richTextBox1.AppendText("\r\n");
            }
            this.richTextBox1.AppendText("--------------------------分割线-----------------------------");
        }

        private Dictionary<string, string> CreateDirct(string path, params char[] separator)
        {
            Dictionary<string, string>  tmpDict = new Dictionary<string, string>();
            FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read);
            if (fs != null)
            {
                string getStr;
                string[] strList;
                StreamReader sr = new StreamReader(fs, Encoding.Default);
                while ((getStr = sr.ReadLine()) != null)
                {
                    strList = getStr.Split(separator);
                    if (strList.Length == 2)
                    {
                        tmpDict.Add(strList[1], strList[0]);
                    }
                }
                fs.Close();
            }
            return tmpDict;
        }

        private string FindFromDict(Dictionary<string,string> dict, string key)
        {
            if (dict.Keys.Contains(key))
            {
                return dict[key];
            } else
            {
                return null;
            }
        }

        private  Dictionary<int, Dictionary<string, string>> GetParamDictFromFileName(string phyID)
        {
            Dictionary<int, Dictionary<string, string>> iddict = new Dictionary<int, Dictionary<string, string>>();

            Dictionary<string, string> Paramdict = new Dictionary<string, string>();
            
            var currentDirectory = Directory.GetCurrentDirectory();
            var fileNamelist = Directory.GetFiles(currentDirectory, "*.txt");
            foreach(var item in fileNamelist)
            {
                var file_name = Path.GetFileName(item);
                
                var list2 = file_name.Split('_');
                if(list2.Length == 3)
                {
                    if(list2[0] == phyID)
                    {
                        Paramdict = CreateDirct(item, '@');
                        iddict.Add(int.Parse(list2[2].Replace(".txt", ""), System.Globalization.NumberStyles.HexNumber), Paramdict);
                    }
                        
                }
            }
            return iddict;
        }

        private Dictionary<string, List<Dictionary<string, string>>> CreateDictExceptParam(List<string> IdList)
        {
            foreach(var i in IdList)
            {
                
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.richTextBox1.Clear();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.textBox2.Clear();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "文本文件(*.txt)|*.txt";
            dialog.ValidateNames = true;
            dialog.CheckPathExists = true;
            dialog.CheckFileExists = true;
            if (dialog.ShowDialog() == DialogResult.OK)

            {
                string strFileName = dialog.FileName;
                this.textBox1.Text = strFileName;
                filepath = strFileName;
                //其他代码
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            string getStr;
            if(this.filepath!=null)
            {
                FileStream fs = File.Open(filepath, FileMode.Open, FileAccess.Read);
                if (fs != null)
                {
                    StreamReader sr = new StreamReader(fs, Encoding.UTF8);
                    while ((getStr = sr.ReadLine()) != null)
                    {
                        getStr = getStr.Trim();
                        if (getStr.StartsWith("收到：")) {
                            getStr.Substring(0, 5);
                            Analysis_Try(getStr);
                        }
                    }
                    fs.Close();
                }
            }
        }

        private void Analysis_Try(string str)
        {
            try
            {
                AnalysisFrame(str.Trim());
                for (int i = 0; i < 3; i++)
                    this.richTextBox1.AppendText("\r\n");
            }
            catch (Exception ex)
            {
                if (ex != null)
                {
                    this.richTextBox1.AppendText(ex.Message);
                }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            this.dataGridView1.Rows.Clear();//this.dataGridView1
            Form1.rowindex = 0;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            ExportToExcel d = new ExportToExcel();
            d.OutputAsExcelFile(dataGridView1);
        }
        private void ConvertESealID(string ESealID)
        {
            if(ESealID.Length != 4)
            {
                string head = ESealID.Substring(0, 8);
                byte[] byteArray = Hex2Byte(head);
                string str = System.Text.Encoding.ASCII.GetString(byteArray);
                this.textBox2.Text = str;
                //foreach(var item in head)
                //{
                //    //idstr = Encoding.ASCII.GetString(byte.Parse(item.ToString()));
                //}

            }
        }
        private byte[] Hex2Byte(string byteStr)
        {
            try
            {
                byteStr = byteStr.ToUpper().Replace(" ", "");
                int len = byteStr.Length / 2;
                byte[] data = new byte[len];
                for (int i = 0; i < len; i++)
                {
                    data[i] = Convert.ToByte(byteStr.Substring(i * 2, 2), 16);
                }
                return data;
            }
            catch (Exception ex)
            { return null; }
        }

        private void CreateDireAll()
        {
            //var currentDirectory = Directory.GetCurrentDirectory();
            //var fileNamelist = Directory.GetFiles(currentDirectory, "*.txt");
            //Dictionary<string, string> tmpdict = new Dictionary<stirng, string>;
            //foreach (var item in fileNamelist)
            //{
            //    var file_name = Path.GetFileName(item);

            //    var list2 = file_name.Split('_');
            //    if (list2.Length == 1)
            //    {
            //        if (list2[0] == "ID")
            //        {

                        //Paramdict = CreateDirct(item, '@');
            //            iddict.Add(int.Parse(list2[2].Replace(".txt", ""), System.Globalization.NumberStyles.HexNumber), Paramdict);
            //            break;
            //        }

            //    }
            //}
            var currentDirectory = Directory.GetCurrentDirectory();
            var fileNamelist = Directory.GetFiles(currentDirectory, "*.txt");
            foreach(var item in fileNamelist)
            {
                var file_name = Path.GetFileName(item);
                var list2 = file_name.Split('_');
                if (list2.Length == 1)
                {
                    if (list2[0] == "ID")
                    {
                        FileStream fs = File.Open(file_name, FileMode.Open, FileAccess.Read);
                        if (fs != null)
                        {
                            string getStr;
                            string[] strList;
                            StreamReader sr = new StreamReader(fs, Encoding.Default);
                            while ((getStr = sr.ReadLine()) != null)
                            {
                                strList = getStr.Split(separator);
                                if (strList.Length == 2)
                                {
                                    tmpDict.Add(strList[1], strList[0]);
                                }
                            }
                            fs.Close();
                        }
                        break;
                    }
                }
            }
        }
    }

    class utc

    {

        public static int ConvertDateTimeInt(System.DateTime time)

        {

            double intResult = 0;

            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));

            intResult = (time - startTime).TotalSeconds;

            return (int)intResult;

        }



        public static DateTime ConvertIntDatetime(double utc)

        {

            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));

            startTime = startTime.AddSeconds(utc);

            //startTime = startTime.AddHours(8);//转化为北京时间(北京时间=UTC时间+8小时 )            

            return startTime;

        }

    }
}
