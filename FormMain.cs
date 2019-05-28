using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace JsonViewer
{
    public partial class FormMain : Form
    {
        public FormMain(string strFileName)
        {
            InitializeComponent();
            m_strFileName = strFileName;
            richTextBoxView.AllowDrop = true;
            treeViewObject.AllowDrop = true;
            richTextBoxView.TextChanged += RichTextBoxView_TextChanged;
            richTextBoxView.DragEnter += OnDragEnter;
            richTextBoxView.DragDrop += OnDragDrop; ;

            DragDrop += OnDragDrop;
            DragEnter += OnDragEnter;

            treeViewObject.DragDrop += OnDragDrop; ;
            treeViewObject.DragEnter += OnDragEnter;
            treeViewObject.BeforeExpand += TreeViewObject_BeforeExpand;
            m_RepalceKey = Guid.NewGuid().ToString();

            m_Timer.Tick += M_Timer_Tick;
            m_Timer.Interval = 100;

        }

        private void M_Timer_Tick(object sender, EventArgs e)
        {
            m_Timer.Stop();
            m_ChangeFlag = false;

            if (MessageBox.Show("文件已经修改是否重新载入？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                return;
            OpenFile(m_strFileName);
        }
 
        protected override void OnLoad(EventArgs e)
        {
            if (string.IsNullOrEmpty(m_strFileName))
                return;
            System.IO.FileInfo f = new System.IO.FileInfo(m_strFileName);
            if (!f.Exists)
                return;
            OpenFile(m_strFileName);
        }


        string m_RepalceKey;
        TreeNode ReplaceNode()
        {
            TreeNode node = new TreeNode();
            node.Name = m_RepalceKey;
            return node;
        }

        string GetDropFilePath(DragEventArgs e)
        {
            IDataObject data = e.Data;
            if (!data.GetDataPresent(DataFormats.FileDrop))
                return "";
            string[] objdata = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (null == objdata)
                return "";
            if (objdata.Length < 1)
                return "";
            return objdata[0];
        }
        private void OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;
            string strFileName = GetDropFilePath(e);
            if (string.IsNullOrEmpty(strFileName))
                return;

            System.IO.FileInfo f = new System.IO.FileInfo(strFileName);
            if (string.Compare(f.Extension, ".json", true) != 0)
                return;
            e.Effect = DragDropEffects.Link;
        }
        bool m_bInner = false;
        LightJson.JsonValue m_val;
        void OpenJson(LightJson.JsonValue val)
        {
            m_val = val;
            m_bInner = true;
            richTextBoxView.Text = val.ToString();
            m_bInner = false;
            AssignTree(val);
        }

        void AssignTree(LightJson.JsonValue val)
        {
            TreeNode vRoot = null;
            treeViewObject.Nodes.Clear();

            if (treeViewObject.Nodes.ContainsKey("JSON"))
                vRoot = treeViewObject.Nodes["JSON"];
            else
                vRoot = treeViewObject.Nodes.Add("JSON","JSON");
            vRoot.Tag = val;
            SetNodeIcon(vRoot, val);
            vRoot.Nodes.Add(ReplaceNode());
            
            if (!vRoot.IsExpanded)
                vRoot.Expand();

        }
        private void TreeViewObject_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (!e.Node.Nodes.ContainsKey(m_RepalceKey))
                return;
            e.Node.Nodes.Clear();
            LightJson.JsonValue val = (LightJson.JsonValue)e.Node.Tag;
            if (val.IsJsonObject)
                AssignSubNode(val.AsJsonObject, e.Node);
            else if (val.IsJsonArray)
                AssignSubNode(val.AsJsonArray, e.Node);
        }
        string JsonIcon(LightJson.JsonValue val)
        {
            if (val.IsJsonObject)
                return "Object";
            if (val.IsJsonArray)
                return "Array";
            return "Property";
        }
        void SetNodeIcon(TreeNode vNode,LightJson.JsonValue val)
        {
            string strImg = JsonIcon(val);
            vNode.ImageKey = strImg;
            vNode.ContextMenuStrip = contextMenuStrip1;
            vNode.SelectedImageKey = strImg;
        }
        void AssignSubNode(LightJson.JsonValue val, TreeNode vNode)
        {
            if (val.IsJsonObject)
                vNode.Nodes.Add(ReplaceNode());
            else if (val.IsJsonArray)
                vNode.Nodes.Add(ReplaceNode());
        }
        bool IsSimple(LightJson.JsonValue val)
        {
            if (val.IsJsonObject)
                return false;
            if (val.IsJsonArray)
                return false;
            return true;
        }
        string JsonValueString(LightJson.JsonValue val)
        {
            switch (val.Type)
            {
                case LightJson.JsonValueType.Null:
                    return "[null]";
                case LightJson.JsonValueType.Boolean:
                    return val.AsBoolean.ToString();
                case LightJson.JsonValueType.Number:
                    if (val.IsInteger)
                        return val.AsInteger.ToString();
                    return val.AsNumber.ToString();
                case LightJson.JsonValueType.String:
                    return string.Format("\"{0}\"", val.AsString);
                case LightJson.JsonValueType.Object:
                    break;
                case LightJson.JsonValueType.Array:
                    break;
                default:
                    break;
            }
            return "[null]";
        }
        void AssignSubNode(LightJson.JsonArray val, TreeNode vNode)
        {
            while(vNode.Nodes.Count > val.Count)
                vNode.Nodes.RemoveAt(vNode.Nodes.Count - 1);

           for (int i =0;i<val.Count;i++)
           {
                LightJson.JsonValue subval = val[i];
                string strKey = string.Format("[{0}]", i);
                string strTxt = strKey;
                if (IsSimple(subval))
                    strTxt += string.Format(" : {0}", JsonValueString(subval));
                TreeNode subNode = null;
                if (vNode.Nodes.ContainsKey(strKey))
                    subNode = vNode.Nodes[strKey];
                else
                    subNode = vNode.Nodes.Add(strKey, strTxt);
                subNode.Text = strTxt;
                subNode.Tag = subval;
                SetNodeIcon(subNode, subval);
                AssignSubNode(subval, subNode);
            }
        }

        void AssignSubNode(LightJson.JsonObject val,TreeNode vNode)
        {
            foreach (var item in val)
            {
                string strKey = item.Key;
                TreeNode sub = null;
                if (vNode.Nodes.ContainsKey(strKey))
                    sub = vNode.Nodes[strKey];
                else
                    sub = vNode.Nodes.Add(strKey, strKey);
                if(IsSimple(item.Value))
                {
                    strKey += string.Format(" : {0}", JsonValueString(item.Value));
                    sub.Text = strKey;
                }
                SetNodeIcon(sub, item.Value);
                sub.Tag = item.Value;
                AssignSubNode(item.Value, sub);
            }
        }
        private void RichTextBoxView_TextChanged(object sender, EventArgs e)
        {
            if (m_bInner)
                return;

            try
            {
                LightJson.JsonValue val = LightJson.Serialization.JsonReader.Parse(richTextBoxView.Text);
                if (!val.IsNull)
                    OpenJson(val);
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }
        void WatchFile(string strFileName)
        {
            if (m_Watch != null)
            {
                m_Watch.Changed -= M_Watch_Changed;
                m_Watch.Dispose();
            }
            System.IO.FileInfo f = new System.IO.FileInfo(strFileName);

            m_Watch = new System.IO.FileSystemWatcher(f.Directory.FullName,f.Name);

            m_Watch.Changed += M_Watch_Changed;
            m_Watch.EnableRaisingEvents = true;
        }
        bool m_ChangeFlag = false;
        private void M_Watch_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            if (m_bInner)
                return;
            m_ChangeFlag = true;
            if (!SafeFocused())
                return;
            SafeStart();
        }
        void SafeStart()
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action(SafeStart));
                return;
            }
            m_Timer.Start();
        }
        bool SafeFocused()
        {
            if(InvokeRequired)
            {
                return (bool)this.Invoke(new Func<bool>(SafeFocused));
            }
            return Focused;
        }
        Timer m_Timer = new Timer();
        protected override void OnActivated(EventArgs e)
        {
            if(m_ChangeFlag)
                m_Timer.Start();

        }
        System.IO.FileSystemWatcher m_Watch = null;

        void OpenFile(string strFileName)
        {
            try
            {
                LightJson.JsonValue val = LightJson.Serialization.JsonReader.ParseFile(strFileName);
                if (!val.IsNull)
                {
                    m_strFileName = strFileName;
                    RefreshTitle();
                    OpenJson(val);
                    WatchFile(m_strFileName);
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }
        void RefreshTitle()
        {
            if (string.IsNullOrEmpty(m_strFileName))
                Text = "JsonViewer";
            else
                Text = "JsonViewer   [" + m_strFileName + "]";
        }
        private void OnDragDrop(object sender, DragEventArgs e)
        {
            bool bNewWindow = (e.KeyState & 8)!= 0;

            string strFileName = GetDropFilePath(e);
            if(bNewWindow)
                OpenNewWindow(strFileName);
            else
                OpenFile(strFileName);
        }
        string m_strFileName;
        private void ToolStripButton1_Click(object sender, EventArgs e)
        {
            m_bInner = true;
            richTextBoxView.Text =  m_val.ToString(true);
            m_bInner =false ;
        }

        private void ToolStripButtonSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(m_strFileName))
                SaveAs();
            else
                Save(m_strFileName);
        }
        void Save(string strFileName)
        {
            m_bInner = true;
            System.IO.File.WriteAllText(strFileName, richTextBoxView.Text);
            m_bInner = false;
        }
        void SaveAs()
        {
            using(SaveFileDialog f = new SaveFileDialog())
            {
                f.Filter = "JSON文件|*.json|任意文件|*.*";
                if (f.ShowDialog() != DialogResult.OK)
                    return;
                Save(f.FileName);
                m_strFileName = f.FileName;
                RefreshTitle();
            }
        }
        private void ToolStripButtonSaveAs_Click(object sender, EventArgs e)
        {
            SaveAs();
        }

        private void ToolStripButtonOpen_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog f = new OpenFileDialog())
            {
                f.Filter = "JSON文件|*.json|任意文件|*.*";
                if (f.ShowDialog() != DialogResult.OK)
                    return;
                OpenFile(f.FileName); 
            }
        }

        private void ToolStripButtonNew_Click(object sender, EventArgs e)
        {
            string strPath =  System.Windows.Forms.Application.ExecutablePath;

            System.Diagnostics.Process.Start(strPath);


        }
        void OpenNewWindow(string strFileName)
        {
            string strPath = System.Windows.Forms.Application.ExecutablePath;
            System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo(strPath);
            start.Arguments = strFileName;
            System.Diagnostics.Process.Start(start);
        }
        private void ToolStripButtonOpenNew_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog f = new OpenFileDialog())
            {
                f.Filter = "JSON文件|*.json|任意文件|*.*";
                if (f.ShowDialog() != DialogResult.OK)
                    return;
                OpenNewWindow(f.FileName);
            }
        }

        private void ToolStripButtonTop_Click(object sender, EventArgs e)
        {
            toolStripButtonTop.Checked = !toolStripButtonTop.Checked;
            this.TopMost = toolStripButtonTop.Checked;

        }
        LightJson.JsonValue SelectValue()
        {
            TreeNode vNode = treeViewObject.SelectedNode;

            return (LightJson.JsonValue)vNode.Tag;
        }

        private void ToolStripMenuItemCopyName_Click(object sender, EventArgs e)
        {
            //toolStripMenuItemCopyName
            TreeNode vNode = treeViewObject.SelectedNode;
            Clipboard.SetText(vNode.Name);
        }

        private void ToolStripMenuItemCopyValue_Click(object sender, EventArgs e)
        {
            LightJson.JsonValue val = SelectValue();
            if(IsSimple(val))
                Clipboard.SetText(val.ToString());
            else
            {
                TreeNode vNode = treeViewObject.SelectedNode;
                Clipboard.SetText(vNode.Name);
            }
        }

        private void ToolStripMenuItemCopyAll_Click(object sender, EventArgs e)
        {
            LightJson.JsonValue val = SelectValue();
            TreeNode vNode = treeViewObject.SelectedNode;
            LightJson.JsonObject o = new LightJson.JsonObject();
            o[vNode.Name] = val;
            val = o;

            Clipboard.SetText(val.ToString(true));
        }

        private void TreeViewObject_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            treeViewObject.SelectedNode = e.Node;
        }
    }
}
