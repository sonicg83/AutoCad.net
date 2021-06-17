// (C) Copyright 2021 by  
//
using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using System.Collections;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(viewtest.MyCommands))]

namespace viewtest
{

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
        

        private Viewport SetViewport(ViewTableRecord InputView,Point2d BasePoint,int Scale)
        {
            Viewport NewViewport = new Viewport();
            NewViewport.CenterPoint = new Point3d(BasePoint.X + (InputView.Width / 2) * Scale, BasePoint.Y + (InputView.Height / 2) * Scale, 0);
            NewViewport.Height = InputView.Height * Scale;
            NewViewport.Width = InputView.Width * Scale;
            NewViewport.ViewCenter = InputView.CenterPoint;
            NewViewport.ViewDirection = InputView.ViewDirection;
            NewViewport.ViewHeight = InputView.Height ;
            NewViewport.ViewTarget = InputView.Target ;
            NewViewport.TwistAngle = InputView.ViewTwist;
            return NewViewport;
        }

        // Modal Command with localized name
        [CommandMethod("ViewTest")]
        public void MyCommand() // This method can have any name
        {
            // Put your command code here
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            string ClipLayerName = "TK-视口";

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
                        //ed.WriteMessage("\nhehe:{0}", viewID.ToString());

                    }

                    LayerStateManager layerState = db.LayerStateManager;


                    /*
                    foreach(ViewTableRecord VR in viewlist)
                    {
                        
                        ed.WriteMessage("\n视图名：{0}", VR.Name);
                        ed.WriteMessage("\n视图中心坐标：{0}", VR.CenterPoint);
                        ed.WriteMessage("\n视图高度：{0}", VR.Height);
                        ed.WriteMessage("\n视图target:{0}", VR.Target);
                        ed.WriteMessage("\n视图宽度：{0}", VR.Width);
                        ed.WriteMessage("\n视图角度：{0}", VR.ViewTwist);
                        
                        ed.WriteMessage("\n图层状况{0}", VR.LayerState);
                    }
                    */
                    
                    int scale = 1;
                    PromptIntegerOptions GetNumberOption = new PromptIntegerOptions("\n输入视口比例，默认为1");
                    GetNumberOption.AllowNegative = false;
                    GetNumberOption.AllowZero = false;
                    GetNumberOption.AllowNone = true;
                    PromptIntegerResult GetNumberResult = ed.GetInteger(GetNumberOption);
                    if (GetNumberResult.Status == PromptStatus.OK)
                    {
                        scale = GetNumberResult.Value;
                    }

                    for (int i = 0; i < Layoutlist.Count; i++)
                    {
                        if(i == viewlist.Count)
                        {
                            continue;
                        }
                        ViewTableRecord VR = viewlist[i] as ViewTableRecord;
                        Viewport VP = SetViewport(VR, new Point2d(0, 0),scale);

                        Layout LT = Layoutlist[i] as Layout;
                        BlockTableRecord BTR = Trans.GetObject(LT.BlockTableRecordId, OpenMode.ForWrite) as BlockTableRecord;
                        BTR.AppendEntity(VP);
                        Trans.AddNewlyCreatedDBObject(VP, true);
                        
                        LayoutManager.Current.SetCurrentLayoutId(LT.Id);                     
                        VP.On = true;

                        //恢复视图的图层状态
                        layerState.RestoreLayerState(VR.LayerState, VP.Id,1,LayerStateMasks.CurrentViewport);

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
                            ed.WriteMessage("\n布局“{0}”中未找到裁剪视口的多义线！视口裁剪失败！",LT.LayoutName);
                        }

                    }

                    Trans.Commit();
                }
                catch (Autodesk.AutoCAD.Runtime.Exception Ex)
                {
                    ed.WriteMessage("出错啦！{0}", Ex.ToString());
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


