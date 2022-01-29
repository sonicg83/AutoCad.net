﻿// (C) Copyright 2021 by  
//
using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(SetViews.MyCommands))]

namespace SetViews
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
        [CommandMethod("SetViews")]
        public void MyCommand() // This method can have any name
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            LayerStateManager LayerState = db.LayerStateManager;
            PromptIntegerResult numres = ed.GetInteger("\n输入视图起始编号");
            if (numres.Status != PromptStatus.OK)
            {
                return;
            }
            int viewnum = numres.Value;
            int counter = 0;
            while (true)
            {
            inputstart:
                PromptEntityOptions ops = new PromptEntityOptions("\n选择视图框");
                ops.SetRejectMessage("\n只能选择封闭的矩形！");
                ops.AddAllowedClass(typeof(Polyline), true);
                ops.AllowNone = false;
                ops.AllowObjectOnLockedLayer = true;
                PromptEntityResult res = ed.GetEntity(ops);
                if (res.Status != PromptStatus.OK)
                {
                    ed.WriteMessage("\n共创建{0}个视图。", counter);
                    break;
                }
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        Polyline PL = trans.GetObject(res.ObjectId, OpenMode.ForRead) as Polyline;
                        if (PL.NumberOfVertices != 4 && !PL.Closed)
                        {
                            ed.WriteMessage("\n选择的不是封闭的矩形！");
                            goto inputstart;
                        }
                        Point3d P1 = PL.GetPoint3dAt(0);
                        Point3d P2 = PL.GetPoint3dAt(1);
                        Point3d P3 = PL.GetPoint3dAt(2);
                        Point3d P4 = PL.GetPoint3dAt(3);
                        Vector3d V1 = P1.GetVectorTo(P2);
                        Vector3d V2 = P2.GetVectorTo(P3);
                        Vector3d V3 = P3.GetVectorTo(P4);
                        Vector3d V4 = P4.GetVectorTo(P1);
                        if (!(V1.IsPerpendicularTo(V2) && V2.IsPerpendicularTo(V3) && V3.IsPerpendicularTo(V4)))
                        {
                            ed.WriteMessage("\n选择的不是封闭的矩形！");
                            goto inputstart;
                        }
                        Point2d CT = new Point2d((P1.X + P3.X) / 2, (P1.Y + P3.Y) / 2);
                        double H = 1;
                        double W = 1;

                        if(V1.Length > V4.Length)
                        {
                            H = V4.Length;
                            W = V1.Length;
                        }
                        else
                        {
                            H = V1.Length;
                            W = V4.Length;
                        }

                        
                        Vector2d UserXaxix = new Vector2d(db.Ucsxdir.X,db.Ucsxdir.Y);
                        Vector2d WXaxix = new Vector2d(1, 0);
                        double TwistAngle = WXaxix.GetAngleTo(UserXaxix);
                        if (db.Ucsxdir.Y > 0)
                        {
                            TwistAngle = Math.PI * 2 - WXaxix.GetAngleTo(UserXaxix);
                        }
                       
                        //Matrix2d WCS2DCS = Matrix2d.Rotation(new Vector2d(1, 0).GetAngleTo(UserXaxix),new Point2d(0,0));
                        Matrix2d WCS2DCS = Matrix2d.Rotation(TwistAngle,new Point2d(0,0));
                        Point3d UcsCenter = db.Ucsorg;
                        //Point2d DcsCenter = new Point2d(UcsCenter.X, UcsCenter.Y).TransformBy(TransWCSToDCS);
                        Point2d DcsCenter = CT.TransformBy(WCS2DCS);
                        ed.WriteMessage("\n计算旋转角：{0}", WXaxix.GetAngleTo(UserXaxix));
                        ed.WriteMessage("\nTwistAngle：{0}", TwistAngle);
                        ed.WriteMessage("\nWCS的中点：{0}", CT.ToString());
                        ed.WriteMessage("\nUCS的中点:{0}", new Point3d(CT.X,CT.Y,0).TransformBy(ed.CurrentUserCoordinateSystem).ToString());
                        ed.WriteMessage("\nDCS的中点：{0}", DcsCenter.ToString());
                        


                        SymbolTable VT = trans.GetObject(db.ViewTableId, OpenMode.ForWrite) as SymbolTable;
                        if (VT.Has(numres.Value.ToString()))
                        {
                            foreach (ObjectId viewid in VT)
                            {
                                ViewTableRecord VR = trans.GetObject(viewid, OpenMode.ForWrite) as ViewTableRecord;
                                if (VR.Name == viewnum.ToString())
                                {
                                    VR.Erase();
                                    break;
                                }
                            }
                        }
                        ViewTableRecord NewVr = new ViewTableRecord();
                        NewVr.Name = viewnum.ToString();
                        
                        NewVr.CenterPoint = DcsCenter;
                        NewVr.Height = H;
                        NewVr.Width = W;
                        NewVr.ViewTwist = TwistAngle;
                        NewVr.SetUcs(db.Ucsorg, db.Ucsxdir, db.Ucsydir);                                            
                        VT.Add(NewVr);
                        trans.AddNewlyCreatedDBObject(NewVr, true);
                        //添加图层快照属性要在把view添加到数据库里后再操作，要不会报错eNoDataBase...
                        string LayerStateName = string.Format("ACAD_VIEWS_{0}", NewVr.Name);
                        if(LayerState.HasLayerState(LayerStateName))
                        {
                            LayerState.DeleteLayerState(LayerStateName);
                        }
                        LayerState.SaveLayerState(LayerStateName, LayerStateMasks.None, new ObjectId());
                        NewVr.LayerState = LayerStateName;
                        trans.Commit();
                        ed.WriteMessage("\n成功创建编号为{0}的视图。", viewnum);
                        viewnum++;
                        counter++;

                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception EX)
                    {
                        ed.WriteMessage("\n出错了！{0}", EX.ToString());
                    }
                    finally
                    {
                        trans.Dispose();
                    }
                }

            }
        }

        #region testcode
        [CommandMethod("restart")]
           public void Restart()
           {

               Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
               Editor ed = doc.Editor;
               Database db = doc.Database;
               ed.WriteMessage("\n中断啦！");
           }
        [CommandMethod("lstate")]
        public void Lstate()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;
            using (Transaction trans = db.TransactionManager.StartTransaction())
            {
                try
                {
                    SymbolTable MyVt = trans.GetObject(db.ViewTableId, OpenMode.ForRead) as SymbolTable;
                    System.Collections.ArrayList viewlist = new System.Collections.ArrayList();
                    foreach (ObjectId viewID in MyVt)
                    {
                        ViewTableRecord VR = trans.GetObject(viewID, OpenMode.ForRead) as ViewTableRecord;
                        viewlist.Add(VR);
                    }
                    if (viewlist.Count == 0)
                    {
                        ed.WriteMessage("\n未发现存储的视图！");
                        return;
                    }
                    foreach(ViewTableRecord VR in viewlist)
                    {
                        if(VR.LayerState == "")
                        {
                            ed.WriteMessage("\n视图{0}的存储图层状态为空值",VR.Name);
                        }
                        else if(VR.LayerState == null)
                        {
                            ed.WriteMessage("\n视图{0}的存储图层状态为空空", VR.Name);
                        }
                        else
                        {
                            ed.WriteMessage("\n视图{0}的存储图层状态为:{1}", VR.Name, VR.LayerState);
                        }
                        
                    }
                }
                catch (Autodesk.AutoCAD.Runtime.Exception EX)
                {
                    ed.WriteMessage("\n出错了！{0}", EX.ToString());
                }
                finally
                {
                    trans.Dispose();
                }

            }
                
        }

        #endregion
    }

}
