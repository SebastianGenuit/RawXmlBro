using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace MyXmlBrowser
{
    public partial class FormMain : Form
    {
        private Color colorAttribute = Color.FromArgb(252, 228, 214);
        private Color colorElement = Color.FromArgb(217, 225, 242);
        private Color colorValue = Color.FromArgb(226, 239, 218);

        private XDocument xDoc;
        private XElement xNode;
        private XElement xPrevNode;
        private string serachText = "";

        private string path;

        public FormMain()
        {
            InitializeComponent();
        }

        #region Event Handlers

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*";
            dialog.FilterIndex = 1;
            dialog.Multiselect = false;            

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                path = dialog.FileName;
                OpenFile(path);
            }
        }

        private void dataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex >= 0 && e.RowIndex >= 0)
            {
                BrowseForwards(e.RowIndex);
            }
        }

        private void buttonBack_Click(object sender, EventArgs e)
        {
            BrowseBackwards();
        }

        private void forwardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BrowseForwards(gridElements.CurrentRow.Index);
        }

        private void backwardsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BrowseBackwards();
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void textBox_Path_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                EvaluateXPath();
            }
        }

        private void gridElements_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                gridAttributes.CurrentCell = null;
                gridElements.CurrentCell = null;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                BrowseForwards(gridElements.CurrentRow.Index);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Back)
            {
                BrowseBackwards();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.L)
            {
                textBox_Path.Focus();
                e.Handled = true;
            }
        }

        private void findElementToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FindData dialog = new FindData();
            dialog.Command = serachText;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                serachText = dialog.Command;
                Search();
            }
        }

        private void continueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Search();
        }

        private void howToUseXPathSyntaxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                
                Process.Start("https://msdn.microsoft.com/en-us/library/ms256086"); //Ex.: DeviceSetting/TB_Set/TB_Parameter[Name="PARA_SetCmd"]
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                        "Error while opening help:\r\n" + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void gridElements_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
                OpenFile(files[0]);

        }

        private void gridElements_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Move;
        }

        #endregion

        #region Private Members

        private void OpenFile(string path)
        {
            gridElements.ColumnHeadersVisible = true;
            DataGridViewCellStyle columnHeaderStyle = new DataGridViewCellStyle();

            try
            {
                XmlTextReader tr = new XmlTextReader(path);

                //tr.Namespaces = false;
                xDoc = XDocument.Load(tr, LoadOptions.SetBaseUri); //xDoc.Root.Attribute("xmlns").Remove();

                xNode = xDoc.Root;
                xPrevNode = null;

                ShowSelected();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    "Error while opening file \"" + path + "\":\r\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BrowseForwards(int row)
        {
            XElement element = gridElements.Rows[row].Tag as XElement;

            if (element != null)
            {
                xNode = element;
                xPrevNode = null;
            }

            if (xNode != null)
            {
                ShowSelected();
            }

            gridElements.Focus();
        }

        private void BrowseBackwards()
        {
            if (xNode != null)
            {
                xPrevNode = xNode;
                xNode = xNode.Parent;
                ShowSelected();
            }
        }

        private string GetXPath()
        {
            List<String> result = new List<string>();
            XElement e = xNode;

            while (e != null)
            {
                result.Add(e.Name.LocalName);

                if (e.Parent != null)
                {
                    result[result.Count - 1] = result[result.Count - 1];

                    int i = e.Parent.Elements().Where(s => s.Name.LocalName == e.Name.LocalName).ToList().IndexOf(e) + 1;
                    if (i != 1) result[result.Count - 1] = result[result.Count - 1] + "[" + i.ToString() + "]";
                }

                e = e.Parent;
            }
            result.Reverse();
            return "/" + string.Join("/", result);

        }

        private void ShowSelected()
        {
            int c;

            if (xDoc == null || xNode == null) return;

            textBox_Path.Text = GetXPath();

            gridElements.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            gridElements.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;

            ClearTables();

            if (xNode.HasAttributes)
            {
                ReadAttributes();
            }

            if (xNode.HasElements)
            {
                ReadElements();
            }
            else
            {
                c = gridElements.Columns.Add("Value", "Value\r\n");
                gridElements.Columns[c].HeaderCell.Style.BackColor = colorValue;

                gridElements.Rows.Add(xNode.Value);
            }

            if (xPrevNode == null)
            {
                gridElements[0, 0].Selected = true;
            }
            else
            {
                gridElements.CurrentCell = gridElements[0, gridElements.Rows.OfType<DataGridViewRow>().First(x => x.Tag == xPrevNode).Index];
                gridElements.CurrentCell.Selected = true;
            }

            AutosizeTables();
            FormatTables();

            gridAttributes.CurrentCell = null;
            gridElements.Focus();
        }

        private void ReadElements()
        {
            int row;
            int c;

            c = gridElements.Columns.Add("Element", "Element\r\n");
            gridElements.Columns[c].HeaderCell.Style.BackColor = colorElement;            

            foreach (XElement element in xNode.Elements())
            {
                row = gridElements.Rows.Add(element.Name.LocalName);
                gridElements.Rows[row].Tag = element;

                if (element.HasAttributes)
                {
                    foreach (XAttribute attribute in element.Attributes())
                    {
                        string attributeName = "attr_" + attribute.Name.LocalName;

                        if (!gridElements.Columns.Contains(attributeName))
                        {
                            c = gridElements.Columns.Add(attributeName, "Attribute\r\n\""+ attribute.Name.LocalName + "\"");
                            gridElements.Columns[c].HeaderCell.Style.BackColor = colorAttribute;
                        }

                        gridElements[attributeName, row].Value = attribute.Value;
                    }
                }

                if (element.HasElements)
                {
                    foreach (XElement subElement in element.Elements())
                    {
                        string subElementName = "sub_" + subElement.Name.LocalName;

                        if (!gridElements.Columns.Contains(subElementName))
                        {
                            c = gridElements.Columns.Add(subElementName, "Sub-Element\r\n\"" + subElement.Name.LocalName + "\"");
                            gridElements.Columns[c].HeaderCell.Style.BackColor = colorValue;
                        }

                        if (string.IsNullOrEmpty(gridElements[subElementName, row].Value as String))
                        {
                            if (subElement.HasElements)
                            {
                                gridElements[subElementName, row].Value = "COMPLEX_DATA";
                            }
                            else
                            {
                                gridElements[subElementName, row].Value = subElement.Value;
                            }
                        }
                        else if ("Array" != gridElements[subElementName, row].Tag as String)
                        {
                            gridElements[subElementName, row].Tag = "Array";
                            gridElements[subElementName, row].Value = "DATA_ARRAY[" + element.Elements().Count(x => x.Name.LocalName == subElement.Name.LocalName).ToString() + "]";
                        }
                    }
                }
                else
                {
                    if (!gridElements.Columns.Contains("Value"))
                    { 
                        c = gridElements.Columns.Add("Value", "Value\r\n");
                        gridElements.Columns[c].HeaderCell.Style.BackColor = colorValue;
                    }

                    gridElements["Value", row].Value = element.Value;
                }
            }
        }

        private void ReadAttributes()
        {
            int row;
            int c;

            c = gridAttributes.Columns.Add("Attribute", "Attribute\r\n");
            gridAttributes.Columns[c].HeaderCell.Style.BackColor = colorAttribute;

            c = gridAttributes.Columns.Add("Value", "Value\r\n");
            gridAttributes.Columns[c].HeaderCell.Style.BackColor = colorAttribute;

            foreach (XAttribute atribute in xNode.Attributes())
            {
                row = gridAttributes.Rows.Add(atribute.Name.LocalName);
                gridAttributes.Rows[row].Tag = atribute;
                gridAttributes["Value", row].Value = atribute.Value;
            }
        }

        private void ClearTables()
        {
            gridAttributes.Columns.Clear();
            gridAttributes.Rows.Clear();
            gridElements.Columns.Clear();
            gridElements.Rows.Clear();
        }

        private void AutosizeTables()
        {
            gridAttributes.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            gridAttributes.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);

            gridElements.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            gridElements.AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);

            int distance = gridAttributes.Columns.Count < 2 ? 3 : gridAttributes.Columns[0].Width + gridAttributes.Columns[1].Width + 3;
            splitContainer1.SplitterDistance = Math.Max(distance, 0);
        }

        private void FormatTables()
        {
            gridAttributes.EnableHeadersVisualStyles = false;
            gridAttributes.ColumnHeadersDefaultCellStyle.Font = new Font(gridAttributes.ColumnHeadersDefaultCellStyle.Font, FontStyle.Bold);
            gridAttributes.DefaultCellStyle.SelectionBackColor = Color.LightGray;

            gridElements.EnableHeadersVisualStyles = false;
            gridElements.ColumnHeadersDefaultCellStyle.Font = new Font(gridElements.ColumnHeadersDefaultCellStyle.Font, FontStyle.Bold);
            gridElements.DefaultCellStyle.SelectionBackColor = Color.LightGray;
        }

        private void EvaluateXPath()
        {
            try
            {
                string path = textBox_Path.Text.Replace("[0]", "");
                XElement element = xDoc.XPathSelectElement(path);

                if (element != null)
                {
                    xNode = element;
                    xPrevNode = null;
                    ShowSelected();
                    Debug.WriteLine("");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error while parsing x-path: \r\n" + ex.Message,
                    "Invalid x-path", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Search()
        {
            int gridAttributesCurrentRow = gridAttributes.CurrentRow != null ? gridAttributes.CurrentRow.Index : 0;
            DataGridViewRow attrRow = GetRow(gridAttributes, row => row.Index > gridAttributesCurrentRow, serachText) 
                ?? GetRow(gridAttributes, row => row.Index <= gridAttributesCurrentRow, serachText);

            int gridElementsCurrentRow = gridElements.CurrentRow != null ? gridElements.CurrentRow.Index : 0;
            DataGridViewRow elementRow = GetRow(gridElements, row => row.Index > gridElementsCurrentRow, serachText)
                ?? GetRow(gridElements, row => row.Index <= gridElementsCurrentRow, serachText);
            
            gridAttributes.ClearSelection();
            gridElements.ClearSelection();

            if (attrRow != null)
            {
                attrRow.Selected = true;
                gridAttributes.CurrentCell = attrRow.Cells[0];
            }

            if (elementRow != null)
            {
                elementRow.Selected = true;
                gridElements.CurrentCell = elementRow.Cells[0];
            }
        }

        private static DataGridViewRow GetRow(DataGridView view, Func<DataGridViewRow, bool> filterRow, string text)
        {
            return view.Rows.Cast<DataGridViewRow>()
                    .Where(filterRow)
                    .FirstOrDefault(row => row.Cells.Cast<DataGridViewCell>()
                        .Select(cell => (cell.Value as string) ?? (""))
                        .FirstOrDefault(s => s.Contains(text)) != null);
        }

        #endregion

    }
}
