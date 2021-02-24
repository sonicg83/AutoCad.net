// (C) Copyright 2020 by  
//
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Collections;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(GetLayouts.MyCommands))]

namespace GetLayouts
{
    #region inireadclass
    public class INIClass
    {
        public string inipath;
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(
       string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(
       string section, string key,
       string def, StringBuilder retVal,
       int size, string filePath);
        /// ﹤summary﹥  
        /// 构造方法  
        /// ﹤/summary﹥  
        /// ﹤param name="INIPath"﹥文件路径﹤/param﹥  
        public INIClass(string INIPath)
        {
            inipath = INIPath;
        }
        /// ﹤summary﹥  
        /// 写入INI文件  
        /// ﹤/summary﹥  
        /// ﹤param name="Section"﹥项目名称(如 [TypeName] )﹤/param﹥  
        /// ﹤param name="Key"﹥键﹤/param﹥  
        /// ﹤param name="Value"﹥值﹤/param﹥  
        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.inipath);
        }
        /// ﹤summary﹥  
        /// 读出INI文件  
        /// ﹤/summary﹥  
        /// ﹤param name="Section"﹥项目名称(如 [TypeName] )﹤/param﹥  
        /// ﹤param name="Key"﹥键﹤/param﹥  
        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(500);
            int i = GetPrivateProfileString(Section, Key, "", temp, 500, this.inipath);
            return temp.ToString();
        }
        /// ﹤summary﹥  
        /// 验证文件是否存在  
        /// ﹤/summary﹥  
        /// ﹤returns﹥布尔值﹤/returns﹥  
        public bool ExistINIFile()
        {
            return File.Exists(inipath);
        }
    }
    #endregion
    // This class is instantiated by AutoCAD for each document when
    // a command is called by the user the first time in the context
    // of a given document. In other words, non static data in this class
    // is implicitly per-document!
    public class MyCommands
    {
        // The CommandMethod attribute can be applied to any public  member 
        // function of any public class.
        // The function should take no arguments and return nothing.
        // If the method is an intance member then the enclosing class is 
        // intantiated for each document. If the member is a static member then
        // the enclosing class is NOT intantiated.
        //
        // NOTE: CommandMethod has overloads where you can provide helpid and
        // context menu.

        // Modal Command with localized name
        // List all the layouts in the current drawing
        [CommandMethod("GetLayouts")]
        public void ListLayouts()
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            ArrayList LayoutList = new ArrayList();
            string serpatten = "";
            string namepatten = "";
            #region ReadINIfile
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\config.ini";
            INIClass inifile = new INIClass(path);
            if (inifile.ExistINIFile())
            {
                acDoc.Editor.WriteMessage("\n参数配置文件路径：" + path);
                try
                {
                    serpatten = @inifile.IniReadValue("RegExp", "serial");
                    namepatten = @inifile.IniReadValue("RegExp", "name");
                }
                catch (System.Exception EX)
                {
                    acDoc.Editor.WriteMessage("出错了!" + EX.ToString());
                }

            }
            else
            {
                acDoc.Editor.WriteMessage("\n注意！未找到config.ini，请检查插件所在文件夹。");
                return;
            }
            #endregion
            // Get the layout dictionary of the current database
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                DBDictionary lays =
                    acTrans.GetObject(acCurDb.LayoutDictionaryId,
                        OpenMode.ForRead) as DBDictionary;

                // Step through and list each named layout except Model
                foreach (DBDictionaryEntry item in lays)
                {
                    ObjectId id = item.Value;
                    Handle handle = id.Handle;
                    Hashtable Layout = new Hashtable
                    {
                        { "name", item.Key },
                        { "serial","" },
                        { "sheetname","" },
                        { "handle", handle.ToString() }
                    };
                    if ((string)Layout["name"] != "Model")
                    {
                        LayoutList.Add(Layout);
                    }
                }
                foreach (Hashtable item in LayoutList)
                {
                    ArrayList TextList = new ArrayList();
                    TypedValue[] Filter = new TypedValue[]
                        {
                            new TypedValue((int)DxfCode.Operator,"<and"),
                            new TypedValue((int)DxfCode.LayoutName,item["name"]),
                            new TypedValue((int)DxfCode.Operator,"<or"),
                            //注意下面，创建选择过滤规则时单行文字是“Text”，不是“DBText”！
                            new TypedValue((int)DxfCode.Start,"Text"),
                            new TypedValue((int)DxfCode.Start,"MText"),
                            new TypedValue((int)DxfCode.Operator,"or>"),
                            new TypedValue((int)DxfCode.Operator,"and>"),
                        };
                    PromptSelectionResult selresult = acDoc.Editor.SelectAll(new SelectionFilter(Filter));
                    if (selresult.Status == PromptStatus.OK)
                    {
                        ObjectId[] IDs = selresult.Value.GetObjectIds();

                        foreach (ObjectId ID in IDs)
                        {
                            DBObject ent = acTrans.GetObject(ID, OpenMode.ForRead);
                            if (ent != null)
                            {
                                switch (ent.GetType().Name)
                                {
                                    case "DBText":
                                        {
                                            DBText Db = (DBText)ent;
                                            TextList.Add(Db.TextString);
                                            break;
                                        }
                                    case "MText":
                                        {
                                            MText Mt = (MText)ent;
                                            TextList.Add(Mt.Text);
                                            break;
                                        }
                                }
                            }
                        }
                    }
                    foreach(string text in TextList)
                    {
                        Regex regserial = new Regex(serpatten);
                        Regex regname = new Regex(namepatten);
                        if(regserial.IsMatch(text.Trim()))
                        {
                            item["serial"] = regserial.Match(text.Trim()).ToString();
                        }
                        if(regname.IsMatch(text))
                        {
                            item["sheetname"] = text.Trim();
                        } 
                    }
                }
                // Abort the changes to the database
                acTrans.Abort();
            }


            
            #region testcode
            acDoc.Editor.WriteMessage("\nLayouts:");
            foreach (Hashtable layout in LayoutList)
            {
                acDoc.Editor.WriteMessage("\n" + layout["name"] + "的句柄：" + layout["handle"]);
                acDoc.Editor.WriteMessage("\n" + layout["name"] + "的图名：" + layout["sheetname"]);
                acDoc.Editor.WriteMessage("\n" + layout["name"] + "的序号：" + layout["serial"]);
            }
            #endregion

            string DwgName = Path.GetFileNameWithoutExtension(acDoc.Name);
            string filepath = string.Format("{0}\\{1}temphandle.csv", Environment.GetEnvironmentVariable("TEMP"), DwgName);
            
            acDoc.Editor.WriteMessage("\n" + "临时文件保存路径：" + filepath);
            
            using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write))
            {
                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default);
                sw.WriteLine("LayoutName,Serial,SheetName,Handle");
                foreach (Hashtable layout in LayoutList)
                {
                    sw.WriteLine(layout["name"] + "," + layout["serial"] + "," + layout["sheetname"] + "," + layout["handle"]);
                }
                sw.Flush();
                sw.Close();
            }
     
        }
    }

}
