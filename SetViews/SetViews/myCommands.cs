// (C) Copyright 2021 by  
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
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;
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
                        double H = V4.Length;
                        double W = V1.Length;

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
                        NewVr.CenterPoint = CT;
                        NewVr.Height = H;
                        NewVr.Width = W;
                        VT.Add(NewVr);
                        trans.AddNewlyCreatedDBObject(NewVr, true);
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

        /*
           [CommandMethod("restart")]
           public void Restart()
           {

               Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
               Editor ed = doc.Editor;
               Database db = doc.Database;
               ed.WriteMessage("\n中断啦！");
           }
           */

    }

}
