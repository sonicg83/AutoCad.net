// (C) Copyright 2021 by  
//
using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UtilityClass;
using System.Xml;
using System.IO;


// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(SetViewPort.MyCommands))]

namespace SetViewPort
{
    public class Scale
    {
        #region Fields
        private readonly double _viewsize;
        private readonly double _sheetsize;
        #endregion

        #region Constructor
        public Scale(double ViewSize,double SheetSize)
        {
            _viewsize = ViewSize;
            _sheetsize = SheetSize;
        }
        #endregion

        #region Properties
        public double ScaleValue { get => _sheetsize / _viewsize; }
        #endregion

        #region Methods
        public double GetScale()
        {
            double ScaleNumber = _sheetsize / _viewsize;
            return ScaleNumber;
        }
        public string GetCivilScale()
        {
            string Civil = string.Format("1:{0}", Math.Round(1000 / (_sheetsize / _viewsize)).ToString());
            return Civil;
        }
        public string GetBuildingScale()
        {
            string Building = string.Format("1:{0}", Math.Round(1 / (_sheetsize / _viewsize)).ToString());
            return Building;
        }
        #endregion
    }

    public class MyCommands
    {
            Database db = Application.DocumentManager.MdiActiveDocument.Database;
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        //增加方向参数，向右为加，向左为减
        private Viewport GetViewport(ViewTableRecord InputView, Point2d BasePoint, double Scale,bool ToRight)
        {
            Viewport NewViewport = new Viewport();
            if(ToRight)
            {
                NewViewport.CenterPoint = new Point3d(BasePoint.X + (InputView.Width / 2) * Scale, BasePoint.Y + (InputView.Height / 2) * Scale, 0);
            }
            else
            {
                NewViewport.CenterPoint = new Point3d(BasePoint.X - (InputView.Width / 2) * Scale, BasePoint.Y - (InputView.Height / 2) * Scale, 0);
            }
            
            NewViewport.Height = InputView.Height * Scale;
            NewViewport.Width = InputView.Width * Scale;
            NewViewport.ViewCenter = InputView.CenterPoint;
            NewViewport.ViewDirection = InputView.ViewDirection;
            NewViewport.ViewHeight = InputView.Height;
            NewViewport.ViewTarget = InputView.Target;
            NewViewport.TwistAngle = InputView.ViewTwist;
            return NewViewport;
        }
        private string GetSheetSize(string DstPath,string SheetPath)
        {
            string SheetSize = "NotFound";          
            XmlDocument SheetSet = DstViewer.DstToXml(DstPath);
            string Xpath1 = string.Format("/ AcSmDatabase / AcSmSheetSet / AcSmSubset / AcSmSheet[AcSmAcDbLayoutReference[AcSmProp = '{0}']]", SheetPath);
            string Xpath2 = "AcSmCustomPropertyBag/AcSmCustomPropertyValue[@propname='图幅']/AcSmProp[@propname='Value']";
            XmlNodeList NodeList = SheetSet.SelectNodes(Xpath1);
            if(NodeList == null)
            {
                return SheetSize;
            }
            XmlNode Node = NodeList[0].SelectSingleNode(Xpath2);
            if(Node == null || Node.InnerText == "")
            {
                return SheetSize;
            }
            SheetSize = Node.InnerText;              
            return SheetSize;
        }

        // Modal Command with localized name
        [CommandMethod("testvp")]
        public void MyCommand() // This method can have any name
        {
            // Put your command code here
            #region 获取CAD核心变量
            
            #endregion
            #region 定义及初始化变量
            string SheetSize = "NotFound";
            int ScaleFlag = 1;
            Hashtable FlagTable = new Hashtable();
            FlagTable.Add("1", "建筑比例");
            FlagTable.Add("2", "市政比例");
            double SheetLength = 0;
            string ClipLayerName = "TK-视口";
            #endregion


            #region 获取图幅数据
            string DwgFile = db.OriginalFileName;
            string DstFile = Path.GetDirectoryName(DwgFile) + @"\图纸集数据文件.dst";
            if (!new FileInfo(DstFile).Exists)
            {
                ed.WriteMessage("\n未找到图纸集数据文件<图纸集数据文件.dst>，请检查图纸集数据文件是否与图纸文件在同一个文件夹内，以及文件名是否正确。");
                return;
            }
            try
            {
                SheetSize = GetSheetSize(DstFile, DwgFile);
                if(SheetSize == "NotFound")
                {
                    ed.WriteMessage("\n未找到图幅的图纸自定义属性，请检查图纸集文件中的自定义属性内容。");
                    return;
                }
                ed.WriteMessage("\n当前图纸图幅为<{0}>", SheetSize);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception EX)
            {
                ed.WriteMessage("\n出错啦！{0}", EX.ToString());
                return;
            }
            #endregion
            

            #region 读取设置文件
            string inipath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\config.ini";

            INIReader inifile = new INIReader(inipath);
            if (inifile.ExistINIFile())
            {
                try
                {
                    string SheetList = inifile.IniReadValue("SheetList", "List");
                    if(!SheetList.Contains(SheetSize))
                    {
                        ed.WriteMessage("\n配置文件的图幅列表中不包含本图的图幅格式，请检查图纸图幅属性是否正确或在配置文件中添加相关数据。");                        
                        System.Diagnostics.Process.Start("notepad.exe", inipath);
                        ed.WriteMessage("\n已打开配置文件！");
                        return;
                    }
                    SheetLength = Convert.ToDouble(inifile.IniReadValue("Size", SheetSize));
                    ClipLayerName = inifile.IniReadValue("ClipLayer", "name");

                }
                catch (System.Exception EX)
                {
                    ed.WriteMessage("出错了!" + EX.ToString());
                    return;
                }

            }
            else
            {
                ed.WriteMessage("\n未找到配置文件config.ini！，请检查插件所在文件夹。");
                return;
            }
            #endregion

            LayerStateManager layerState = db.LayerStateManager;
            
            using (Transaction Trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    //获取布局列表(剔除模型空间)
                    DBDictionary Layouts = Trans.GetObject(db.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
                    ArrayList Layoutlist = new ArrayList();
                    foreach (DBDictionaryEntry item in Layouts)
                    {
                        if (item.Key != "Model")
                        {
                            Layout layoutobject = Trans.GetObject(item.Value, OpenMode.ForRead) as Layout;
                            Layoutlist.Add(layoutobject);
                        }
                    }
                    //获取view列表,注意哦，是symbotable，不能用viewtable，会闪退
                    SymbolTable MyVt = Trans.GetObject(db.ViewTableId, OpenMode.ForRead) as SymbolTable;
                    ArrayList viewlist = new ArrayList();
                    foreach (ObjectId viewID in MyVt)
                    {
                        ViewTableRecord VR = Trans.GetObject(viewID, OpenMode.ForRead) as ViewTableRecord;
                        viewlist.Add(VR);
                    }
                    if(viewlist.Count == 0)
                    {
                        ed.WriteMessage("\n未发现存储的视图！");
                        return;
                    }

                    PromptIntegerOptions GetIntOption = new PromptIntegerOptions("\n选择");
                    /*
                    double scale = 1;
                    PromptDoubleOptions GetNumberOption = new PromptDoubleOptions("\n输入视口比例，默认为1");
                    GetNumberOption.AllowNegative = false;
                    GetNumberOption.AllowZero = false;
                    GetNumberOption.AllowNone = true;
                    PromptDoubleResult GetNumberResult = ed.GetDouble(GetNumberOption);
                    if (GetNumberResult.Status == PromptStatus.OK)
                    {
                        scale = GetNumberResult.Value;
                    }
                    else
                    {
                        return;
                    }
                    */
                    for (int i = 0; i < Layoutlist.Count; i++)
                    {
                        if (i == viewlist.Count)
                        {
                            break;
                        }
                        Layout LT = Layoutlist[i] as Layout;
                        string[] split = LT.LayoutName.Split(' ');
                        Regex patten = new Regex(@"^0*\d{1,}");
                        Regex replace = new Regex("^0*");
                        string match = replace.Replace(split[0], "");                        
                        var query = from ViewTableRecord view in viewlist
                                    where replace.Replace(view.Name, "") == match
                                    select view;
                        if(!query.Any())
                        {
                            ed.WriteMessage("\n布局“{0}”未找到匹配的视图，没有成功生成视口！", LT.LayoutName);
                            i--;
                            continue;
                        }
                        
                        ViewTableRecord VR = query.First() ;
                        Scale SheetScale = new Scale(VR.Width, SheetLength);
                        Viewport VP = GetViewport(VR, new Point2d(0, 0), SheetScale.ScaleValue, false);
                        #region 创建比例文字
                        TextStyleTable Tst = (TextStyleTable)Trans.GetObject(db.TextStyleTableId, OpenMode.ForRead);
                        ObjectId TextStyleID = new ObjectId();
                        string StyleName = "TK-字段";
                        if(Tst.Has(StyleName))
                        {
                            TextStyleID = Tst[StyleName];
                        }
                        else
                        {
                            TextStyleID = Tst["Standard"];
                        }
                        DBText ScaleMark = new DBText();
                        if(ScaleFlag == 1)
                        {
                            ScaleMark.TextString = SheetScale.GetCivilScale();
                        }
                        else
                        {
                            ScaleMark.TextString = SheetScale.GetBuildingScale();
                        }
                        ScaleMark.TextStyleId = TextStyleID;
                        ScaleMark.AlignmentPoint = new Point3d(-5, 18, 0);
                        ScaleMark.Height = 2.5;
                        ScaleMark.WidthFactor = 0.7;
                        ScaleMark.HorizontalMode = TextHorizontalMode.TextMid;                       
                        #endregion


                        BlockTableRecord BTR = Trans.GetObject(LT.BlockTableRecordId, OpenMode.ForWrite) as BlockTableRecord;
                        BTR.AppendEntity(VP);
                        Trans.AddNewlyCreatedDBObject(VP, true);

                        LayoutManager.Current.SetCurrentLayoutId(LT.Id);
                        VP.On = true;
                        //恢复视图的图层状态
                       // ed.WriteMessage("\n<" + VR.LayerState + ">");
                        if(VR.LayerState != "")
                        {
                            layerState.RestoreLayerState(VR.LayerState, VP.Id, 1, LayerStateMasks.CurrentViewport);
                        }
                        
                        //开始选择多段线裁剪视口
                        TypedValue[] Filter = new TypedValue[]
                       {
                            new TypedValue((int)DxfCode.Operator,"<and"),
                            new TypedValue((int)DxfCode.LayoutName,LT.LayoutName),
                            new TypedValue((int)DxfCode.LayerName,ClipLayerName),
                            new TypedValue((int)DxfCode.Operator,"<or"),
                            new TypedValue((int)DxfCode.Start,"POLYLINE"),
                            new TypedValue((int)DxfCode.Start,"POLYLINE3D"),
                            new TypedValue((int)DxfCode.Start,"POLYLINE2D"),
                            new TypedValue((int)DxfCode.Start,"LWPOLYLINE"),
                            new TypedValue((int)DxfCode.Operator,"or>"),
                            new TypedValue((int)DxfCode.Operator,"and>")
                       };
                        PromptSelectionResult selresult = ed.SelectAll(new SelectionFilter(Filter));
                        if (selresult.Status == PromptStatus.OK)
                        {
                            ObjectId[] IDs = selresult.Value.GetObjectIds();
                            VP.NonRectClipEntityId = IDs[0];
                            VP.NonRectClipOn = true;
                            //ed.WriteMessage("\n呵呵，剪裁了视口哦");
                        }
                        else
                        {
                            ed.WriteMessage("\n布局“{0}”中未找到裁剪视口的多义线！视口裁剪失败！", LT.LayoutName);
                        }

                    }

                    Trans.Commit();
                }
                catch (Autodesk.AutoCAD.Runtime.Exception Ex)
                {
                    ed.WriteMessage("\n出错啦！{0}", Ex.ToString());
                }
                finally
                {
                    Trans.Dispose();

                }

            }
            



        }
        [CommandMethod("restart")]
        public void TestCommand() // This method can have any name
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            Editor ed = doc.Editor;
            ed.WriteMessage("\n中断了哦");

        }
    }

}
