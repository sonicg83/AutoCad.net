// (C) Copyright 2020 by  
//
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(GetSheetset.MyCommands))]

namespace GetSheetset
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
        [CommandMethod("GetSheetset")]
        public void ListLayouts()
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            ArrayList TextList = new ArrayList();
            string Patten_Project = "";
            string Patten_Unit = "";
            string Patten_Mcode = "";
            string Patten_Ccode = "";
            string Patten_Period = "";
            string Patten_Date = "";
            string Patten_First = "";
            string Patten_Second = "";
            string Patten_Third = "";
            string Patten_Fourth = "";
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\config.ini";
            INIClass inifile = new INIClass(path);
            if (inifile.ExistINIFile())
            {
                acDoc.Editor.WriteMessage("\n参数配置文件路径：" + path);
                try
                {
                     Patten_Project = inifile.IniReadValue("RegExp","project");
                     Patten_Unit = inifile.IniReadValue("RegExp", "unit");
                     Patten_Mcode = inifile.IniReadValue("RegExp", "Mcode");
                     Patten_Ccode = inifile.IniReadValue("RegExp", "Ccode");
                    Patten_Period = inifile.IniReadValue("RegExp", "period");
                    Patten_Date = inifile.IniReadValue("RegExp", "date");
                     Patten_First = inifile.IniReadValue("RegExp", "first");
                     Patten_Second = inifile.IniReadValue("RegExp", "second");
                     Patten_Third = inifile.IniReadValue("RegExp", "third");
                     Patten_Fourth = inifile.IniReadValue("RegExp", "fourth");
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
            
            // Get the layout dictionary of the current database
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                TypedValue[] Filter = new TypedValue[]
                        {
                            new TypedValue((int)DxfCode.Operator,"<or"),
                            //注意下面，创建选择过滤规则时单行文字是“Text”，不是“DBText”！
                            new TypedValue((int)DxfCode.Start,"Text"),
                            new TypedValue((int)DxfCode.Start,"MText"),
                            new TypedValue((int)DxfCode.Operator,"or>"),
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
                // Abort the changes to the database
                acTrans.Abort();
            }


            Hashtable SheetSet = new Hashtable
            {
                { "project","" },
                { "unit","" },
                { "Mcode","" },
                { "serial","" },
                { "version","" },
                { "Ccode","" },
                { "period","" },
                { "zhuanye","" },
                { "date","" },
                { "first","" },
                { "second","" },
                { "third","" },
                { "fourth","" },
                { "sheetsetname","" },
            };

            Regex Reg_Project = new Regex(Patten_Project);
            Regex Reg_Unit = new Regex(Patten_Unit);
            Regex Reg_Mcode = new Regex(Patten_Mcode);
            Regex Reg_Ccode = new Regex(Patten_Ccode);
            Regex Reg_Period = new Regex(Patten_Period);
            Regex Reg_Date = new Regex(Patten_Date);
            Regex Reg_First = new Regex(Patten_First);
            Regex Reg_Second = new Regex(Patten_Second);
            Regex Reg_Third = new Regex(Patten_Third);
            Regex Reg_Fourth = new Regex(Patten_Fourth);

            foreach(string text in TextList)
            {
                if(Reg_Project.IsMatch(text))
                {
                    SheetSet["project"] = text.Replace(" ", "");
                }
                if(Reg_Unit.IsMatch(text))
                {
                    SheetSet["unit"] = text.Trim();
                }
                if(Reg_Mcode.IsMatch(text))
                {
                    string[] codelist = text.Split(new char[] { '/' });
                    SheetSet["Mcode"] = Reg_Mcode.Matches(text)[0].ToString();
                    SheetSet["serial"] = codelist[7].TrimEnd(new char[] { '0' });
                    SheetSet["version"] = codelist[8];
                }
                if(Reg_Ccode.IsMatch(text))
                {
                    SheetSet["Ccode"] = Reg_Ccode.Match(text).ToString();
                    string[] codelist = Reg_Ccode.Match(text).ToString().Split(new char[] { '-' });
                    SheetSet["zhuanye"] = codelist[3];
                }
                if(Reg_Period.IsMatch(text))
                {
                    SheetSet["period"] = text.Trim().Replace(" ", "");
                }
                if(Reg_Date.IsMatch(text))
                {
                    string[] dates = Reg_Date.Match(text).ToString().Trim().Split(new char[] { '年' });
                    string year = dates[0];
                    string month = dates[1].TrimEnd(new char[] { '月' }).PadLeft(2,'0');
                    SheetSet["date"] = year + "." + month;
                }
                if(Reg_First.IsMatch(text))
                {
                    SheetSet["first"] = text.Trim();
                }
                if(Reg_Second.IsMatch(text))
                {
                    SheetSet["second"] = text.Trim();
                }
                if(Reg_Third.IsMatch(text))
                {
                    SheetSet["third"] = text.Trim();
                }
                if(Reg_Fourth.IsMatch(text))
                {
                    SheetSet["fourth"] = text.Trim();
                }
                    
            }
            SheetSet["sheetsetname"] = string.Format("{0} {1} {2} {3} {4}", SheetSet["period"], SheetSet["first"], SheetSet["second"], SheetSet["third"], SheetSet["fourth"]);
            //测试用代码
            #region testcode
            acDoc.Editor.WriteMessage("\n该图册信息:");
            acDoc.Editor.WriteMessage("\n 工程名称：" + SheetSet["project"]);
            acDoc.Editor.WriteMessage("\n 工点单位：" + SheetSet["unit"]);
            acDoc.Editor.WriteMessage("\n 地铁编码前缀：" + SheetSet["Mcode"]);
            acDoc.Editor.WriteMessage("\n 管理号前缀：" + SheetSet["Ccode"]);
            acDoc.Editor.WriteMessage("\n 版本：" + SheetSet["version"]);
            acDoc.Editor.WriteMessage("\n 专业代码：" + SheetSet["zhuanye"]);
            acDoc.Editor.WriteMessage("\n 序号前缀：" + SheetSet["serial"]);
            acDoc.Editor.WriteMessage("\n 出图日期：" + SheetSet["date"]);
            acDoc.Editor.WriteMessage("\n 图册名称：" + SheetSet["sheetsetname"]);
            #endregion
            string DwgName = Path.GetFileNameWithoutExtension(acDoc.Name);
            string filepath = string.Format("{0}\\{1}SheetSet.csv", Environment.GetEnvironmentVariable("TEMP"),DwgName);
            acDoc.Editor.WriteMessage("\n" + "图册信息临时文件保存路径：" + filepath);

            
            using (FileStream fs = new FileStream(filepath, FileMode.Create, FileAccess.Write))
            {
                StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default);
                sw.WriteLine("Project,Unit,Mcode,Ccode,Version,Zhuanye,serial,Date,SheetSetName");
                sw.WriteLine(SheetSet["project"] + "," + SheetSet["unit"] + "," + SheetSet["Mcode"] + "," + SheetSet["Ccode"] + "," + SheetSet["version"] + "," + SheetSet["zhuanye"] + "," + SheetSet["serial"] + "," + SheetSet["date"] + "," + SheetSet["sheetsetname"]);
                sw.Flush();
                sw.Close();
            }
        }
    }

}
