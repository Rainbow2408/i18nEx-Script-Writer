using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Text.Json;

namespace i18nEx_Script_Writer
{
    internal class DataImport
    {
        internal static List<string> KeyNameList
        {
            get
            {
                List<string> list = new List<string>();
                string path = "LangKeys.txt";
                try
                {
                    if (!File.Exists(path))
                    {
                        string[] createText = {"SC", "TC", "E"};
                        File.WriteAllLines(path, createText, Encoding.UTF8);
                        foreach (string key in createText) list.Add(key);
                    }
                    else
                    {
                        string[] lines = File.ReadAllLines(path);
                        foreach (string line in lines) list.Add(line);
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
                return list;
            }
        }
        internal static bool ToDataTable(string file_path, 
                                         ref DataTable dt, 
                                         ref List<SubtitleData> SubList,
                                         string DefaultKey = "E",
                                         DataGridView? dgv = null,
                                         int col_width = 160)
        {
            bool _button = false;
            if (dt.Columns["Key"] == null)
                dt.Columns.Add("Key", typeof(string));
            if (dt.Columns["J"] == null)
                dt.Columns.Add("J", typeof(string));
            foreach (string key in KeyNameList)
                dt.Columns.Add(key, typeof(string));
            string[] lines = File.ReadAllLines(file_path);
            foreach (string line in lines)
            {
                if (line.StartsWith("@VoiceSubtitle"))
                {
                    var line_read = line.Replace("\u180E", "")["@VoiceSubtitle".Length..];
                    SubtitleData? line_json = JsonSerializer.Deserialize<SubtitleData>(line_read);
                    if (line_json != null)
                    {
                        SubList.Add(line_json);
                        var dt_row = dt.Rows.Add(line_json.original);
                        if (!line_json.translation.Contains('<')) line_json.translation = $"<{DefaultKey}>{line_json.translation}";
                        MutiLangTextRead(dt, dt_row, $"{line_json.translation}<SubID>{SubList.Count - 1}");
                    }
                }
                else if (line.Contains('\t'))
                {
                    var line_split = line.Replace("\u180E", "").Split('\t');
                    if (line_split.Length >= 1)
                    {
                        var Key = line_split[0];
                        var dt_row = dt.Rows.Add(Key);
                        string tl = line_split[1];
                        if (!tl.Contains('<')) tl = $"<{DefaultKey}>{tl}";
                        MutiLangTextRead(dt, dt_row, tl);
                    }
                }
            }
            if (dgv != null)
            {
                if (dt.Columns.Count < ((dt.Columns["SubID"] == null) ? 5 : 6))
                {
                    if (dgv.Columns["Key"] != null)
                        dgv.Columns["Key"].Width = 360;
                    if (dgv.Columns["J"] != null)
                        dgv.Columns["J"].Width = 360;
                    _button = true;
                }
                else foreach (DataColumn dc in dt.Columns)
                    if (dc.ColumnName != "Key" && dc.ColumnName != "J" && dc.ColumnName != "SubID")
                        dgv.Columns[dc.ColumnName].Width = col_width;
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    row.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
                    row.Height = 25;
                }
            }
            return _button;
        }
        private static void MutiLangTextRead(DataTable dt, DataRow row, string Value)
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
                        dt.Columns.Add(tl_key, typeof(string));
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

        internal static bool SaveScript(string file_path, DataTable dt, List<SubtitleData> SubData)
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
            }
            // If the file is not found, handle the exception and inform the user.
            catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show("Can't write the File.");
            }
            return true;
        }
    }
}
