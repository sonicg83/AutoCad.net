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

        // Modal Command with localized name
        [CommandMethod("ViewTest")]
        public void MyCommand() // This method can have any name
        {
            // Put your command code here
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

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
                    /*
                    foreach(ViewTableRecord VR in viewlist)
                    {
                        ed.WriteMessage("\n视图名：{0}", VR.Name);
                        ed.WriteMessage("\n视图中心坐标：{0}", VR.CenterPoint);
                        ed.WriteMessage("\n视图高度：{0}", VR.Height);
                        ed.WriteMessage("\n视图target:{0}", VR.Target);
                        ed.WriteMessage("\n视图宽度：{0}", VR.Width);
                        ed.WriteMessage("\n视图角度：{0}", VR.ViewTwist);
                    }
                    */

                    for (int i = 0; i < Layoutlist.Count; i++)
                    {
                        Viewport VP = new Viewport();
                        VP.CenterPoint = new Point3d(282, 205, 0);
                        VP.Width = 564;
                        VP.Height = 410;
                        Layout LT = Layoutlist[i] as Layout;
                        BlockTableRecord BTR = Trans.GetObject(LT.BlockTableRecordId, OpenMode.ForWrite) as BlockTableRecord;
                        BTR.AppendEntity(VP);
                        Trans.AddNewlyCreatedDBObject(VP, true);
                        ViewTableRecord VR = viewlist[i] as ViewTableRecord;
                        LayoutManager.Current.SetCurrentLayoutId(LT.Id);
                        VP.ViewCenter = VR.CenterPoint;
                        VP.ViewDirection = VR.ViewDirection;
                        VP.ViewHeight = VR.Height;
                        VP.ViewTarget = VR.Target;
                        VP.On = true;

                        TypedValue[] Filter = new TypedValue[]
                       {
                            new TypedValue((int)DxfCode.Operator,"<and"),
                            new TypedValue((int)DxfCode.LayoutName,LT.LayoutName),
                            new TypedValue((int)DxfCode.LayerName,"cut"),
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
                            ed.WriteMessage("\n呵呵，切割了视口哦");
                        }
                        else
                        {
                            ed.WriteMessage("\n呵呵，没有找到切割多义线");
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


