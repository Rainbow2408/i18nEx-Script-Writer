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
            FolderBrowserDialog dialog = new();
            dialog.Description = "Open script folder";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                script_root = dialog.SelectedPath;
                string folder_name = script_root.Substring(script_root.LastIndexOf('\\') + 1);
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
                }
                else
                {
                    MessageBox.Show("Please select script folder!", "ERROR");
                    button1.PerformClick();
                }
            }
        }
        private void MutiLangTextRead(DataRow row, string Value, int col_width = 160)
        {
            var Value_split = Value.Split('<');

            foreach (var val in Value_split)
            {
                int sep = val.IndexOf('>');
                if (sep != -1)
                {
                    var tl_key = val[..sep];
                    var tl_value = val[(sep + 1)..];
                    if (!dataTable.Columns.Contains(tl_key))
                    {
                        dataTable.Columns.Add(tl_key, typeof(string));
                        dataGridView1.Columns[tl_key].Width = col_width;
                    }
                    if (dataTable.Columns[tl_key] != null)
                        row[dataTable.Columns[tl_key].Ordinal] = tl_value;
                }
                else
                {
                    if (dataTable.Columns["J"] != null)
                        row[dataTable.Columns["J"].Ordinal] = val;
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
                                MutiLangTextRead(dt_row, $"{line_json.translation}<SubID>{SubtitlesList.Count - 1}", 48);
                            }
                        }
                        else if (line.Contains('\t'))
                        {
                            var line_split = line.Replace("\u180E", "").Split('\t');
                            if (line_split.Length >= 1)
                            {
                                var Key = line_split[0];
                                var dt_row = dataTable.Rows.Add(Key);
                                MutiLangTextRead(dt_row, line_split[1]);
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
            try
            {
                if (script_path != String.Empty)
                {
                    var lineList = new HashSet<string>();
                    foreach (DataRow dr in dataTable.Rows)
                    {
                        int idx = -1;
                        bool Sub = false;
                        string org = string.Empty;
                        string tl = string.Empty;
                        foreach (DataColumn dt_col in dataTable.Columns)
                        {
                            string dt_key = dt_col.ColumnName;
                            if (dt_key == "Key") org = dr[dt_key].ToString();
                            else if (dt_key == "J") tl = dr[dt_key].ToString() + tl;
                            else if (dt_key == "SubID") Sub = int.TryParse(dr["SubID"].ToString(), out idx);
                            else tl += $"<{dt_key}>{dr[dt_key]}";
                        }
                        if (Sub)
                        {
                            SubtitlesList[idx].translation = tl;
                            JsonSerializerOptions jso = new JsonSerializerOptions{ Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                            lineList.Add("@VoiceSubtitle" + JsonSerializer.Serialize(SubtitlesList[idx],jso));
                        }
                        else lineList.Add($"{org}\t{tl}");
                    }
                    if (lineList.Count != 0)
                        File.WriteAllLines(script_path + "_.txt", lineList.ToArray(), Encoding.UTF8);
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