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
                            DataTable dt_w = new() ,dt_r = new();
                            List<SubtitleData> SubList_w = new(), SubList_r = new();
                            DataImport.ToDataTable(write_file, ref dt_w, ref SubList_w);
                            DataImport.ToDataTable(read_file, ref dt_r, ref SubList_r, comboBox1.Text);
                            DataColumn[] keyColumns = new DataColumn[1];
                            keyColumns[0] = dt_w.Columns["Key"];
                            dt_w.PrimaryKey = keyColumns;
                            bool Manually_edit = false;
                            foreach (DataRow dr_r in dt_r.Rows)
                            {
                                DataRow dr_w_find = dt_w.Rows.Find(dr_r[dt_r.Columns["Key"]]);
                                if (dr_w_find == null)
                                {
                                    Manually_edit = true;
                                    continue;
                                }
                                foreach (DataColumn dc_r in dt_r.Columns)
                                {
                                    string key = dc_r.ColumnName;
                                    if (key == "Key" || key == "SubID") continue;
                                    if (dr_w_find[dt_w.Columns[key]] == string.Empty || (checkBox2.Checked && (key == comboBox1.Text) && dr_r[dt_r.Columns[key]] != string.Empty))
                                        dt_w.Rows[dt_w.Rows.IndexOf(dr_w_find)][dt_w.Columns[key]] = dr_r[dt_r.Columns[key]];
                                }
                            }
                            if (Manually_edit) { } //To Do: When missing row, call manually edit Form.
                            button_pressed = DataImport.SaveScript(write_file + "_.txt", dt_w, SubList_w);
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
                    button5.Enabled = DataImport.ToDataTable(script_path, ref dataTable, ref SubtitlesList, "J", dataGridView1);
                    if (dataGridView1.Columns["SubID"] != null)
                    {
                        dataGridView1.Columns["SubID"].ReadOnly = true;
                        dataGridView1.Columns["SubID"].Width = 48;
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
        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            script_modified = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            button_pressed = DataImport.SaveScript(script_path + "_.txt", dataTable, SubtitlesList);
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
}