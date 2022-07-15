using System.Text.Json;
using System.Text;
using System.Data;

namespace i18nEx_Script_Writer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private static string script_root = string.Empty;
        private static string muLang_root = string.Empty;
        private static string script_path = string.Empty;
        private static bool script_modified = false;
        private static bool button_pressed = false;
        DataTable dataTable = new();
        private static List<SubtitleData> SubtitlesList = new();
        private static string[] lines = Array.Empty<string>();
        private void SetTreeViewNode(string path, TreeNode node)
        {
            var directories = Directory.GetDirectories(path);
            if (directories != null)
            {
                foreach (var item in directories)
                {
                    var childrenNode = node.Nodes.Add(item.Substring(item.LastIndexOf('\\') + 1));
                    SetTreeViewNode(item, childrenNode);
                }
            }

            var files = Directory.GetFiles(path);
            if (files != null)
            {
                foreach (var file in files)
                {
                    if(file.Contains(".txt")) node.Nodes.Add(file.Substring(file.LastIndexOf('\\') + 1));
                }
            }
        }
        private void Merge_eachFile(string read_path, string write_path, string local_path = "")
        {
            if (string.IsNullOrEmpty(local_path)) local_path = read_path;
            var directories = Directory.GetDirectories(read_path);
            if (directories != null)
            {
                foreach (var item in directories)
                {
                    Merge_eachFile(item, write_path, local_path);
                }
            }
            var files = Directory.GetFiles(read_path);
            if (files != null)
            {
                foreach (var read_file in files)
                {
                    if (read_file.Contains(".txt"))
                    {
                        string write_file = write_path + read_file.Replace(local_path, "");
                        if (File.Exists(write_file))
                        {
                            DataTable dt_w = new();
                            List<SubtitleData> SubList_w = new();
                            dt_w.Columns.Add("Key", typeof(string));
                            dt_w.Columns.Add("J", typeof(string));
                            dt_w.Columns.Add("SC", typeof(string));
                            dt_w.Columns.Add("TC", typeof(string));
                            dt_w.Columns.Add("E", typeof(string));
                            lines = File.ReadAllLines(write_file);
                            foreach (string line in lines)
                            {
                                if (line.StartsWith("@VoiceSubtitle"))
                                {
                                    var line_read = line.Replace("\u180E", "")["@VoiceSubtitle".Length..];
                                    SubtitleData? line_json = JsonSerializer.Deserialize<SubtitleData>(line_read);
                                    if (line_json != null)
                                    {
                                        SubList_w.Add(line_json);
                                        var dt_row = dt_w.Rows.Add(line_json.original);
                                        MutiLangTextRead(dt_w, dt_row, $"{line_json.translation}<SubID>{SubList_w.Count - 1}");
                                    }
                                }
                                else if (line.Contains('\t'))
                                {
                                    var line_split = line.Replace("\u180E", "").Split('\t');
                                    if (line_split.Length >= 1)
                                    {
                                        var Key = line_split[0];
                                        var dt_row = dt_w.Rows.Add(Key);
                                        MutiLangTextRead(dt_w, dt_row, line_split[1]);
                                    }
                                }
                            }
                            DataTable dt_r = new();
                            List<SubtitleData> SubList_r = new();
                            dt_r.Columns.Add("Key", typeof(string));
                            dt_r.Columns.Add("J", typeof(string));
                            lines = File.ReadAllLines(read_file);
                            foreach (string line in lines)
                            {
                                if (line.StartsWith("@VoiceSubtitle"))
                                {
                                    var line_read = line.Replace("\u180E", "")["@VoiceSubtitle".Length..];
                                    SubtitleData? line_json = JsonSerializer.Deserialize<SubtitleData>(line_read);
                                    if (line_json != null)
                                    {
                                        SubList_r.Add(line_json);
                                        var dt_row = dt_r.Rows.Add(line_json.original);
                                        if(!line_json.translation.Contains('<')) line_json.translation = $"<{comboBox1.Text}>{line_json.translation}";
                                        MutiLangTextRead(dt_r, dt_row, $"{line_json.translation}<SubID>{SubList_r.Count - 1}");
                                    }
                                }
                                else if (line.Contains('\t'))
                                {
                                    var line_split = line.Replace("\u180E", "").Split('\t');
                                    if (line_split.Length >= 1)
                                    {
                                        var Key = line_split[0];
                                        var dt_row = dt_r.Rows.Add(Key);
                                        string tl = line_split[1];
                                        if (!tl.Contains('<')) tl = $"<{comboBox1.Text}>{tl}";
                                        MutiLangTextRead(dt_r, dt_row, tl);
                                    }
                                }
                            }
                            if (dt_w.Rows.Count > dt_r.Rows.Count) continue; //To Do: When missing row, make a manually edit list.
                            DataColumn[] keyColumns = new DataColumn[1];
                            keyColumns[0] = dt_w.Columns["Key"];
                            dt_w.PrimaryKey = keyColumns;
                            foreach (DataRow dr_r in dt_r.Rows)
                            {
                                DataRow dr_w_find = dt_w.Rows.Find(dr_r[dt_r.Columns["Key"]]);
                                if (dr_w_find == null) continue; //To Do: When missing row, make a manually edit list.
                                foreach (DataColumn dc_r in dt_r.Columns)
                                {
                                    string key = dc_r.ColumnName;
                                    if (key == "Key" || key == "SubID") continue;
                                    if (dr_w_find[dt_w.Columns[key]] == string.Empty || (checkBox2.Checked && (key == comboBox1.Text) && dr_r[dt_r.Columns[key]] != string.Empty))
                                        dt_w.Rows[dt_w.Rows.IndexOf(dr_w_find)][dt_w.Columns[key]] = dr_r[dt_r.Columns[key]];
                                }
                            }
                            saveScript(write_file + "_.txt", dt_w, SubList_w);
                        }
                    }
                }
            }
        }

        private void InitialDataGrid()
        {
            dataTable = new DataTable();
            SubtitlesList.Clear();
            dataGridView1.DataSource = dataTable;
            dataTable.Columns.Add("Key", typeof(string));
            dataGridView1.Columns["Key"].Width = 200;
            dataGridView1.Columns["Key"].ReadOnly = true;
            dataTable.Columns.Add("J", typeof(string));
            dataGridView1.Columns["J"].Width = 20;
            dataGridView1.RowHeadersWidth = 30;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            string folder_name = LoadScriptFolder(ref script_root);
            if (folder_name == "Script" && File.Exists(script_root + "\\__npc_names.txt"))
            {
                treeView1.Nodes.Clear();
                label1.Text = "[Message] FilePath = " + script_root;
                SetTreeViewNode(script_root, treeView1.Nodes.Add(folder_name));
                for (int i = 0; i < treeView1.Nodes.Count; i++)
                    treeView1.Nodes[i].Expand();
                button1.Enabled = false;
                button2.Enabled = true;
                button3.Enabled = true;
                button4.Enabled = true;
                button6.Enabled = true;
                checkBox1.Enabled = true;
                checkBox2.Enabled = true;
                comboBox1.Enabled = true;
            }
        }

        private string LoadScriptFolder(ref string folder_path)
        {
            FolderBrowserDialog dialog = new();
            dialog.Description = "Open script folder";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                folder_path = dialog.SelectedPath;
                string folder_name = folder_path[(folder_path.LastIndexOf('\\') + 1)..];
                if (folder_name == "Script" && File.Exists(folder_path + "\\__npc_names.txt"))
                    return folder_name;
                else
                {
                    MessageBox.Show("Please select script folder!", "ERROR");
                    return LoadScriptFolder(ref folder_path);
                }
            }
            return string.Empty;
        }
        private void MutiLangTextRead(DataTable dt, DataRow row, string Value, int col_width = 160, DataGridView? dgv = null)
        {
            var Value_split = Value.Split('<');

            foreach (var val in Value_split)
            {
                int sep = val.IndexOf('>');
                if (sep != -1)
                {
                    var tl_key = val[..sep];
                    var tl_value = val[(sep + 1)..];
                    if (!dt.Columns.Contains(tl_key))
                    {
                        dt.Columns.Add(tl_key, typeof(string));
                        if (dgv != null)
                            dgv.Columns[tl_key].Width = col_width;
                    }
                    if (dt.Columns[tl_key] != null)
                        row[dt.Columns[tl_key].Ordinal] = tl_value;
                }
                else
                {
                    if (dt.Columns["J"] != null)
                        row[dt.Columns["J"].Ordinal] = val;
                }
            }
        }
        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            try
            {
                if (e.Node.Text.Contains(".txt"))
                {
                    if (script_modified)
                    {
                        DialogResult resault = MessageBox.Show("Are you sure save the file and open select script?", "Confirm Message", MessageBoxButtons.YesNoCancel);
                        if (resault == DialogResult.Yes)
                        {
                            button3.PerformClick();
                            while (!button_pressed) ;
                        }
                        else if (resault == DialogResult.Cancel) return;
                    }
                    button5.Enabled = false;
                    script_modified = false;
                    InitialDataGrid();
                    script_path = script_root + e.Node.FullPath.Substring(e.Node.FullPath.IndexOf("\\"));
                    label1.Text = "[Message] Open file at: " + script_path;
                    lines = File.ReadAllLines(script_path);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("@VoiceSubtitle"))
                        {
                            var line_read = line.Replace("\u180E", "")["@VoiceSubtitle".Length..];
                            SubtitleData? line_json = JsonSerializer.Deserialize<SubtitleData>(line_read);
                            if (line_json != null)
                            {
                                SubtitlesList.Add(line_json);
                                var dt_row = dataTable.Rows.Add(line_json.original);
                                MutiLangTextRead(dataTable ,dt_row, $"{line_json.translation}<SubID>{SubtitlesList.Count - 1}", col_width: 48);
                            }
                        }
                        else if (line.Contains('\t'))
                        {
                            var line_split = line.Replace("\u180E", "").Split('\t');
                            if (line_split.Length >= 1)
                            {
                                var Key = line_split[0];
                                var dt_row = dataTable.Rows.Add(Key);
                                MutiLangTextRead(dataTable, dt_row, line_split[1]);
                            }
                        }
                    }
                    if (dataGridView1.Columns["SubID"] != null)
                        dataGridView1.Columns["SubID"].ReadOnly = true;
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        row.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
                        row.Height = 25;
                    }
                    if (dataTable.Columns.Count < ((dataTable.Columns["SubID"] == null) ? 5 : 6))
                    {
                        if (dataGridView1.Columns["Key"] != null)
                            dataGridView1.Columns["Key"].Width = 360;
                        if (dataGridView1.Columns["J"] != null)
                            dataGridView1.Columns["J"].Width = 360;
                        button5.Enabled = true;
                    }
                    if (checkBox1.Checked && button5.Enabled) button5.PerformClick();
                }
            }
            // If the file is not found, handle the exception and inform the user.
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            treeView1.CollapseAll();
            for (int i = 0; i < treeView1.Nodes.Count; i++)
                treeView1.Nodes[i].Expand();
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if(e.Button == MouseButtons.Right)
            {
                bool Expand = true, noSubDir = true;
                if(e.Node.Text.Contains(".txt")&&e.Node.Parent.Text != "Script")
                {
                    e.Node.Parent.Collapse();
                    if(e.Node.Parent.Parent != null) e.Node.Parent.Parent.EnsureVisible();
                    return;
                }
                for (int i = 0; i < e.Node.Nodes.Count; i++)
                {
                    if (e.Node.Nodes[i].IsExpanded == true) Expand = false;
                    if (e.Node.Nodes[i].Nodes.Count > 0) noSubDir = false;
                }
                if (noSubDir && e.Node.IsExpanded) Expand = false;
                if (Expand)
                {
                    if (e.Node.Text == "Script")
                        if (MessageBox.Show("Are you sure expand root folder?", "Confirm Message", MessageBoxButtons.OKCancel) == DialogResult.Cancel) return;
                    e.Node.ExpandAll();
                    if(e.Node.Parent != null) e.Node.Parent.EnsureVisible();
                    else e.Node.EnsureVisible();
                }
                else
                {
                    e.Node.Collapse();
                    if (e.Node.Parent != null && noSubDir) e.Node.Parent.EnsureVisible();
                    else e.Node.EnsureVisible();
                    if (e.Node.Text == "Script")
                        for (int i = 0; i < treeView1.Nodes.Count; i++)
                            treeView1.Nodes[i].Expand();
                }
            }
        }

        private void dataGridView1_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                row.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
                row.Height = 25;
            }
            dataGridView1.Rows[e.RowIndex].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dataGridView1.AutoResizeRow(e.RowIndex, DataGridViewAutoSizeRowMode.AllCells);
            if(dataGridView1.Rows[e.RowIndex].Height < 25) dataGridView1.Rows[e.RowIndex].Height = 25;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            saveScript(script_path + "_.txt", dataTable, SubtitlesList);
        }
        private void saveScript(string file_path, DataTable dt, List<SubtitleData> SubData)
        {
            try
            {
                if (file_path != String.Empty)
                {
                    var lineList = new HashSet<string>();
                    foreach (DataRow dr in dt.Rows)
                    {
                        int idx = -1;
                        bool Sub = false;
                        string org = string.Empty;
                        string tl = string.Empty;
                        foreach (DataColumn dt_col in dt.Columns)
                        {
                            string dt_key = dt_col.ColumnName;
                            if (dt_key == "Key") org = dr[dt_key].ToString();
                            else if (dt_key == "J") tl = dr[dt_key].ToString() + tl;
                            else if (dt_key == "SubID") Sub = int.TryParse(dr["SubID"].ToString(), out idx);
                            else tl += $"<{dt_key}>{dr[dt_key]}";
                        }
                        if (Sub)
                        {
                            SubData[idx].translation = tl;
                            JsonSerializerOptions jso = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                            lineList.Add("@VoiceSubtitle" + JsonSerializer.Serialize(SubData[idx], jso));
                        }
                        else lineList.Add($"{org}\t{tl}");
                    }
                    if (lineList.Count != 0)
                        File.WriteAllLines(file_path, lineList.ToArray(), Encoding.UTF8);
                }
                button_pressed = true;
            }
            // If the file is not found, handle the exception and inform the user.
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show("Can't write the File.");
            }
        }
        private void button4_Click(object sender, EventArgs e)
        {
            treeView1.Enabled = false;
            treeView1.Nodes.Clear();
            string folder_name = script_root.Substring(script_root.LastIndexOf('\\') + 1);
            SetTreeViewNode(script_root, treeView1.Nodes.Add(folder_name));
            for (int i = 0; i < treeView1.Nodes.Count; i++)
                treeView1.Nodes[i].Expand();
            treeView1.Enabled = true;
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            script_modified = true;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (dataTable.Columns["Key"] == null)
                dataTable.Columns.Add("Key", typeof(string));
            dataGridView1.Columns["Key"].Width = 200;
            dataGridView1.Columns["Key"].ReadOnly = true;
            if (dataTable.Columns["J"] == null)
                dataTable.Columns.Add("J", typeof(string));
            dataGridView1.Columns["J"].Width = 20;
            if (dataTable.Columns["SC"] == null)
                dataTable.Columns.Add("SC", typeof(string));
            dataGridView1.Columns["SC"].Width = 160;
            if (dataTable.Columns["TC"] == null)
                dataTable.Columns.Add("TC", typeof(string));
            dataGridView1.Columns["TC"].Width = 160;
            if (dataTable.Columns["E"] == null)
                dataTable.Columns.Add("E", typeof(string));
            dataGridView1.Columns["E"].Width = 160;
            button5.Enabled = false;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (script_modified)
            {
                DialogResult resault = MessageBox.Show("Are you sure save the file and open select script?", "Confirm Message", MessageBoxButtons.YesNoCancel);
                if (resault == DialogResult.Yes)
                {
                    button3.PerformClick();
                    while (!button_pressed) ;
                }
                else if (resault == DialogResult.Cancel) return;
            }
            string folder_name = LoadScriptFolder(ref muLang_root);
            if (folder_name == "Script" && File.Exists(muLang_root + "\\__npc_names.txt"))
            {
                if (muLang_root != script_root)
                {
                    label1.Text = "[Message] MultiLang_Path = " + muLang_root;
                    Merge_eachFile(muLang_root, script_root);
                }
                else MessageBox.Show("Please select diffrent script source!", "ERROR");
            }
        }
    }
    public class SubtitleData
    {
        public int addDisplayTime { get; set; }
        public int displayTime { get; set; }
        public bool isCasino { get; set; }
        public string? original { get; set; }
        public int startTime { get; set; }
        public string? translation { get; set; }
        public string? voice { get; set; }
    }
}