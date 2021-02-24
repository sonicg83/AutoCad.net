// (C) Copyright 2020 by  
//
using System;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(Transform.MyCommands))]

namespace Transform
{

    // This class is instantiated by AutoCAD for each document when
    // a command is called by the user the first time in the context
    // of a given document. In other words, non static data in this class
    // is implicitly per-document!
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
        public void TransformEntity(Entity entity, Point3d basepoint, double rangle, double scale)
        {
            Point3d origon = new Point3d(0, 0, 0);
            Vector3d zaxis = new Vector3d(0, 0, 1);
            Matrix3d smat = Matrix3d.Scaling(scale, basepoint);
            Matrix3d rmat = Matrix3d.Rotation(rangle, zaxis, basepoint);
            Matrix3d dmat = Matrix3d.Displacement(origon.GetVectorTo(basepoint));
            Matrix3d mat = dmat.PreMultiplyBy(rmat).PreMultiplyBy(smat);

            //Matrix3d mat = Matrix3d.Displacement(origon.GetVectorTo(basepoint)).PreMultiplyBy(rmat);         
            entity.TransformBy(mat);
        }
        public void RecoverEntity(Entity entity, Point3d basepoint, double rangle, double scale)
        {
            Point3d origion = new Point3d(0, 0, 0);
            Vector3d zaxis = new Vector3d(0, 0, 1);
            double newscale = 1 / scale;
            Matrix3d smat = Matrix3d.Scaling(newscale, basepoint);
            Matrix3d rmat = Matrix3d.Rotation(-rangle, zaxis, basepoint);
            Matrix3d dmat = Matrix3d.Displacement(basepoint.GetVectorTo(origion));
            Matrix3d mat = smat.PreMultiplyBy(rmat).PreMultiplyBy(dmat);

            entity.TransformBy(mat);

        }

        [CommandMethod("CoT", CommandFlags.Modal)]
        public void MyCommand() // This method can have any name
        {
            // Put your command code here
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            TypedValue[] filter = new TypedValue[1]     //添加过滤器只选择模型空间的实体
            {
                new TypedValue(410,"Model")
            };
            Point3d basepoint = new Point3d(391090.57816, 2472660.598025, 0);
            double rangle = 0.0170603779;
            double scale = 0.999997425176;
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\config.ini";

            INIClass inifile = new INIClass(path);
            if (inifile.ExistINIFile())
            {
                ed.WriteMessage("\n参数配置文件路径：" + path);
                try
                {
                    double displacementX = Convert.ToDouble(inifile.IniReadValue("Displacement", "X"));
                    double displacementY = Convert.ToDouble(inifile.IniReadValue("Displacement", "Y"));
                    basepoint = new Point3d(displacementX, displacementY, 0);
                    rangle = Convert.ToDouble(inifile.IniReadValue("Rotation", "radian"));
                    scale = Convert.ToDouble(inifile.IniReadValue("Scale", "scale"));

                }
                catch (System.Exception EX)
                {
                    ed.WriteMessage("出错了!" + EX.ToString());
                }

            }
            else
            {
                ed.WriteMessage("\n注意！未找到config.ini，请检查插件所在文件夹。将采用默认参数：偏移后原点（391090.57816, 2472660.598025, 0），旋转角弧度（0.0170603779），缩放因子(0.999997425176)");
            }



            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                PromptSelectionResult selrestult = ed.SelectAll(new SelectionFilter(filter));
                if (selrestult.Status == PromptStatus.OK)
                {
                    SelectionSet selset = selrestult.Value;
                    foreach (SelectedObject selobject in selset)
                    {
                        if (selobject != null)
                        {
                            try
                            {
                                Entity entity = (Entity)trans.GetObject(selobject.ObjectId, OpenMode.ForWrite);
                                TransformEntity(entity, basepoint, rangle, scale);
                            }
                            catch (Autodesk.AutoCAD.Runtime.Exception EX)
                            {
                                ed.WriteMessage("出错了!" + EX.ToString());
                            }
                        }
                    }
                    ed.WriteMessage("\n完成坐标系转换，X方向偏移{0}，Y方向偏移{1}，旋转角弧度{2}，缩放比例{3}", basepoint.X, basepoint.Y, rangle, scale);
                }
                trans.Commit();
            }


        }

        [CommandMethod("UcoT", CommandFlags.Modal)]
        public void MyCommand2() // This method can have any name
        {
            // Put your command code here
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            TypedValue[] filter = new TypedValue[1]     //添加过滤器只选择模型空间的实体
            {
                new TypedValue(410,"Model")
            };
            Point3d basepoint = new Point3d(391090.522451, 2472660.716344, 0);
            double rangle = 0.0170593097;
            double scale = 0.999997530712;
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\config.ini";

            INIClass inifile = new INIClass(path);
            if (inifile.ExistINIFile())
            {
                ed.WriteMessage("\n参数配置文件路径：" + path);
                try
                {
                    double displacementX = Convert.ToDouble(inifile.IniReadValue("Displacement", "X"));
                    double displacementY = Convert.ToDouble(inifile.IniReadValue("Displacement", "Y"));
                    basepoint = new Point3d(displacementX, displacementY, 0);
                    rangle = Convert.ToDouble(inifile.IniReadValue("Rotation", "radian"));
                    scale = Convert.ToDouble(inifile.IniReadValue("Scale", "scale"));

                }
                catch (System.Exception EX)
                {
                    ed.WriteMessage("出错了!" + EX.ToString());
                }

            }
            else
            {
                ed.WriteMessage("\n注意！未找到config.ini，请检查插件所在文件夹。将采用默认参数：偏移后原点（391090.522451, 2472660.716344, 0），旋转角弧度（0.0170593097），缩放因子(0.999997530712)");
            }



            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                PromptSelectionResult selrestult = ed.SelectAll(new SelectionFilter(filter));
                if (selrestult.Status == PromptStatus.OK)
                {
                    SelectionSet selset = selrestult.Value;
                    foreach (SelectedObject selobject in selset)
                    {
                        if (selobject != null)
                        {
                            try
                            {
                                Entity entity = (Entity)trans.GetObject(selobject.ObjectId, OpenMode.ForWrite);
                                RecoverEntity(entity, basepoint, rangle, scale);

                            }
                            catch (Autodesk.AutoCAD.Runtime.Exception EX)
                            {
                                ed.WriteMessage("出错了!" + EX.ToString());
                            }
                        }
                    }
                    ed.WriteMessage("\n完成坐标系恢复至深圳独立坐标!");
                }
                trans.Commit();
            }


        }



    }

}
